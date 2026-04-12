using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Common;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Feedback;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Results;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Models;

namespace GTEK.FSM.WebPortal.Services.Requests;

public interface IRequestWorkspaceApiClient
{
    Task<RequestWorkspaceSnapshot> GetAsync(string requestId, CancellationToken cancellationToken = default);

    Task<RequestWorkspaceOperationResult> TransitionStatusAsync(string requestId, RequestStage nextStage, string? rowVersion, CancellationToken cancellationToken = default);

    Task<RequestWorkspaceOperationResult> AssignWorkerAsync(string requestId, string workerUserId, string? rowVersion, bool isReassignment, CancellationToken cancellationToken = default);
}

public sealed class RequestWorkspaceApiClient : IRequestWorkspaceApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public RequestWorkspaceApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<RequestWorkspaceSnapshot> GetAsync(string requestId, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.GetAsync($"api/v1/requests/{requestId}", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetServiceRequestDetailResponse>(response, cancellationToken);
        var detail = envelope.Data ?? throw new RequestWorkspaceApiException(HttpStatusCode.InternalServerError, "REQUEST_DETAIL_EMPTY", "The request detail response did not include a payload.");

        using var feedbackResponse = await this.httpClient.GetAsync($"api/v1/requests/{requestId}/feedback", cancellationToken);
        var feedbackEnvelope = await ReadSuccessEnvelopeAsync<List<FeedbackResponse>>(feedbackResponse, cancellationToken);

        return MapSnapshot(detail, feedbackEnvelope.Data ?? []);
    }

    public async Task<RequestWorkspaceOperationResult> TransitionStatusAsync(string requestId, RequestStage nextStage, string? rowVersion, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/requests/{requestId}/status")
        {
            Content = JsonContent.Create(new TransitionServiceRequestStatusRequest
            {
                NextStatus = nextStage.ToString(),
                RowVersion = rowVersion,
            }),
        };

        using var response = await this.httpClient.SendAsync(request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<TransitionServiceRequestStatusResponse>(response, cancellationToken);

        return new RequestWorkspaceOperationResult(
            envelope.Message ?? "Request status updated.",
            envelope.Data?.RowVersion);
    }

    public async Task<RequestWorkspaceOperationResult> AssignWorkerAsync(string requestId, string workerUserId, string? rowVersion, bool isReassignment, CancellationToken cancellationToken = default)
    {
        var relativeUri = isReassignment
            ? $"api/v1/requests/{requestId}/reassign"
            : $"api/v1/requests/{requestId}/assign";

        var payload = isReassignment
            ? JsonContent.Create(new ReassignServiceRequestRequest
            {
                WorkerUserId = workerUserId,
                RowVersion = rowVersion,
            })
            : JsonContent.Create(new AssignServiceRequestRequest
            {
                WorkerUserId = workerUserId,
                RowVersion = rowVersion,
            });

        using var request = new HttpRequestMessage(HttpMethod.Post, relativeUri)
        {
            Content = payload,
        };

        using var response = await this.httpClient.SendAsync(request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<ServiceRequestAssignmentResponse>(response, cancellationToken);

        return new RequestWorkspaceOperationResult(
            envelope.Message ?? "Assignment updated.",
            envelope.Data?.RowVersion);
    }

    private static RequestWorkspaceSnapshot MapSnapshot(GetServiceRequestDetailResponse detail, IReadOnlyList<FeedbackResponse> feedback)
    {
        var requestId = detail.RequestId ?? string.Empty;
        var stage = ParseStage(detail.Status);
        var createdAtUtc = detail.CreatedAtUtc == default ? detail.UpdatedAtUtc : detail.CreatedAtUtc;
        var ageMinutes = Math.Max(0, (int)Math.Floor((DateTime.UtcNow - createdAtUtc).TotalMinutes));
        var isSlaBreach = ageMinutes >= 240;

        return new RequestWorkspaceSnapshot
        {
            RowVersion = detail.RowVersion,
            CustomerUserId = detail.CustomerUserId,
            CreatedAtUtc = createdAtUtc,
            ActiveJobId = detail.ActiveJobId,
            ActiveJobStatus = detail.ActiveJobStatus,
            Item = new OperationalQueueItem
            {
                RequestId = requestId,
                Reference = requestId,
                Customer = detail.CustomerUserId ?? "Unknown customer",
                TenantId = detail.TenantId ?? string.Empty,
                Stage = RequestStagePresentation.MapWorkspaceStage(stage),
                Priority = isSlaBreach ? "High" : "Standard",
                Summary = detail.Title ?? "Untitled service request",
                UpdatedAtUtc = detail.UpdatedAtUtc,
                Status = stage,
                UrgencyLevel = MapUrgency(ageMinutes, stage),
                AgeMinutes = ageMinutes,
                IsEscalated = false,
                IsSLABreach = isSlaBreach,
                AssignedWorker = detail.AssignedWorkerUserId,
                AssignedWorkerId = detail.AssignedWorkerUserId,
                WorkloadHint = BuildWorkloadHint(ageMinutes, stage, detail.ActiveJobStatus),
            },
            Timeline = detail.Timeline
                .Select(x => new RequestWorkspaceTimelineEntry(
                    Guid.NewGuid(),
                    x.OccurredAtUtc,
                    string.IsNullOrWhiteSpace(x.ActorUserId)
                        ? $"{x.EventType}: {x.Message}"
                        : $"{x.EventType}: {x.Message} ({x.ActorUserId})"))
                .ToList(),
            Feedback = feedback.ToList(),
        };
    }

    private static RequestStage ParseStage(string? status)
    {
        return Enum.TryParse<RequestStage>(status, ignoreCase: true, out var parsed)
            ? parsed
            : RequestStage.New;
    }

    private static UrgencyLevel MapUrgency(int ageMinutes, RequestStage stage)
    {
        if ((stage == RequestStage.OnHold) || (ageMinutes >= 240))
        {
            return UrgencyLevel.High;
        }

        if ((stage == RequestStage.Assigned) || (stage == RequestStage.InProgress) || (ageMinutes >= 120))
        {
            return UrgencyLevel.Moderate;
        }

        return UrgencyLevel.Normal;
    }

    private static string BuildWorkloadHint(int ageMinutes, RequestStage stage, string? activeJobStatus)
    {
        if (ageMinutes >= 240)
        {
            return "Aging request. Refresh coordination and resolve blockers quickly.";
        }

        if (!string.IsNullOrWhiteSpace(activeJobStatus))
        {
            return $"Active job status: {activeJobStatus}.";
        }

        return stage switch
        {
            RequestStage.Assigned => "Assigned and waiting on dispatch execution.",
            RequestStage.InProgress => "Active work is in flight.",
            RequestStage.OnHold => "Request is paused pending follow-up action.",
            _ => "Request is ready for operator action.",
        };
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new RequestWorkspaceApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = JsonSerializer.Deserialize<ConflictResponse>(content, SerializerOptions);
            if (conflict is not null)
            {
                throw new RequestWorkspaceConflictException(conflict.Message, conflict.ErrorCode, conflict.CurrentRowVersion);
            }
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new RequestWorkspaceApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class RequestWorkspaceSnapshot
{
    public OperationalQueueItem Item { get; set; } = new();

    public string? RowVersion { get; set; }

    public string? CustomerUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? ActiveJobId { get; set; }

    public string? ActiveJobStatus { get; set; }

    public List<RequestWorkspaceTimelineEntry> Timeline { get; set; } = [];

    public List<FeedbackResponse> Feedback { get; set; } = [];
}

public sealed record RequestWorkspaceTimelineEntry(Guid Id, DateTime Timestamp, string Message);

public sealed record RequestWorkspaceOperationResult(string Message, string? RowVersion);

public class RequestWorkspaceApiException : Exception
{
    public RequestWorkspaceApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}

public sealed class RequestWorkspaceConflictException : RequestWorkspaceApiException
{
    public RequestWorkspaceConflictException(string message, string? errorCode, string? currentRowVersion)
        : base(HttpStatusCode.Conflict, errorCode, message)
    {
        this.CurrentRowVersion = currentRowVersion;
    }

    public string? CurrentRowVersion { get; }
}