using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Common;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.WebPortal.Services.Management;

public interface IManagementWorkersApiClient
{
    Task<IReadOnlyList<WorkerProfileResponse>> ListAsync(string? searchText, bool includeInactive, CancellationToken cancellationToken = default);

    Task<WorkerProfileResponse> CreateAsync(CreateWorkerProfileRequest request, CancellationToken cancellationToken = default);

    Task<WorkerProfileResponse> UpdateAsync(string workerId, UpdateWorkerProfileRequest request, CancellationToken cancellationToken = default);
}

public sealed class ManagementWorkersApiClient : IManagementWorkersApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public ManagementWorkersApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WorkerProfileResponse>> ListAsync(string? searchText, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"includeInactive={includeInactive.ToString().ToLowerInvariant()}",
            "page=1",
            "pageSize=100",
        };

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query.Add($"searchText={Uri.EscapeDataString(searchText)}");
        }

        using var response = await this.httpClient.GetAsync($"api/v1/management/workers?{string.Join("&", query)}", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetWorkersListResponse>(response, cancellationToken);

        return envelope.Data?.Items ?? [];
    }

    public async Task<WorkerProfileResponse> CreateAsync(CreateWorkerProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PostAsJsonAsync("api/v1/management/workers", request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<WorkerProfileResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementWorkersApiException(HttpStatusCode.InternalServerError, "WORKER_RESPONSE_EMPTY", "Worker create response payload was empty.");
    }

    public async Task<WorkerProfileResponse> UpdateAsync(string workerId, UpdateWorkerProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/management/workers/{workerId}")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await this.httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<WorkerProfileResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementWorkersApiException(HttpStatusCode.InternalServerError, "WORKER_RESPONSE_EMPTY", "Worker update response payload was empty.");
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new ManagementWorkersApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new ManagementWorkersApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class ManagementWorkersApiException : Exception
{
    public ManagementWorkersApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
