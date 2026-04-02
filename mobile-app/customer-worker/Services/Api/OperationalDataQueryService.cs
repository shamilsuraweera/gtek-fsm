namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Common;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Results;

public sealed record LiveQueryResult<T>(bool IsLive, IReadOnlyList<T> Items, string Message = "");

public interface IRequestQueryService
{
    Task<LiveQueryResult<GetRequestsResponse>> QueryRequestsAsync(CancellationToken cancellationToken = default);
}

public interface IJobQueryService
{
    Task<LiveQueryResult<GetJobsResponse>> QueryJobsAsync(CancellationToken cancellationToken = default);
}

public interface ICategoryQueryService
{
    Task<LiveQueryResult<CategoryResponse>> QueryActiveCategoriesAsync(CancellationToken cancellationToken = default);
}

public sealed record RequestCreationResult(bool IsSuccess, CreateServiceRequestResponse Request, string Message = "");

public interface IServiceRequestCreationService
{
    Task<RequestCreationResult> CreateRequestAsync(string title, CancellationToken cancellationToken = default);
}

public sealed record JobDetailQueryResult(bool IsSuccess, GetJobDetailResponse Detail, string Message = "", string ErrorCode = "", bool IsConflict = false);

public sealed record RequestDetailQueryResult(bool IsSuccess, GetServiceRequestDetailResponse Detail, string Message = "", string ErrorCode = "", bool IsConflict = false);

public sealed record RequestStatusTransitionResult(bool IsSuccess, TransitionServiceRequestStatusResponse Transition, string Message = "", string ErrorCode = "", bool IsConflict = false);

public interface IWorkerExecutionService
{
    Task<JobDetailQueryResult> GetJobDetailAsync(string jobId, CancellationToken cancellationToken = default);

    Task<RequestDetailQueryResult> GetRequestDetailAsync(string requestId, CancellationToken cancellationToken = default);

    Task<RequestStatusTransitionResult> TransitionRequestStatusAsync(
        string requestId,
        string nextStatus,
        string rowVersion,
        CancellationToken cancellationToken = default);
}

public interface IRequestDetailQueryService
{
    Task<RequestDetailQueryResult> GetRequestDetailAsync(string requestId, CancellationToken cancellationToken = default);
}

public sealed class OperationalDataQueryService : IRequestQueryService, IJobQueryService, ICategoryQueryService, IServiceRequestCreationService, IWorkerExecutionService, IRequestDetailQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly IMobileDiagnosticsLogger _diagnostics;

    private static readonly CreateServiceRequestResponse EmptyCreateResponse = new();
    private static readonly GetJobDetailResponse EmptyJobDetail = new();
    private static readonly GetServiceRequestDetailResponse EmptyRequestDetail = new();
    private static readonly TransitionServiceRequestStatusResponse EmptyRequestTransition = new();

    public OperationalDataQueryService(
        HttpClient httpClient,
        IIdentityTokenProvider tokenProvider,
        IMobileDiagnosticsLogger diagnostics)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _diagnostics = diagnostics;
    }

    public Task<LiveQueryResult<GetRequestsResponse>> QueryRequestsAsync(CancellationToken cancellationToken = default)
    {
        return QueryListAsync<GetRequestsResponse>(
            route: "/api/v1/requests?offset=0&limit=20&sortBy=UpdatedUtc&sortDirection=desc",
            category: "requests.query",
            cancellationToken);
    }

    public Task<LiveQueryResult<GetJobsResponse>> QueryJobsAsync(CancellationToken cancellationToken = default)
    {
        return QueryListAsync<GetJobsResponse>(
            route: "/api/v1/jobs?offset=0&limit=20&sortBy=AssignedUtc&sortDirection=asc",
            category: "jobs.query",
            cancellationToken);
    }

    public Task<LiveQueryResult<CategoryResponse>> QueryActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return QueryListAsync<CategoryResponse>(
            route: "/api/v1/categories",
            category: "categories.query",
            cancellationToken);
    }

    public async Task<RequestCreationResult> CreateRequestAsync(string title, CancellationToken cancellationToken = default)
    {
        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn("requests.create", "Create request skipped because JWT token is missing.");
            return new RequestCreationResult(IsSuccess: false, Request: EmptyCreateResponse, Message: "Missing token");
        }

        var payload = new CreateServiceRequestRequest
        {
            Title = title,
        };

        var jsonPayload = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/requests")
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            _diagnostics.Info("requests.create", "Dispatching create request command to '/api/v1/requests'.");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _diagnostics.Warn("requests.create", $"Create request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                return new RequestCreationResult(IsSuccess: false, Request: EmptyCreateResponse, Message: $"HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (TryExtractItem<CreateServiceRequestResponse>(json, out var createdRequest) && createdRequest is not null)
            {
                _diagnostics.Info("requests.create", $"Create request succeeded for request '{createdRequest.RequestId}'.");
                return new RequestCreationResult(IsSuccess: true, Request: createdRequest, Message: "Created");
            }

            _diagnostics.Warn("requests.create", "Create request returned an unexpected payload shape.");
            return new RequestCreationResult(IsSuccess: false, Request: EmptyCreateResponse, Message: "Unexpected payload");
        }
        catch (Exception ex)
        {
            _diagnostics.Error("requests.create", $"Create request threw an exception: {ex.Message}");
            return new RequestCreationResult(IsSuccess: false, Request: EmptyCreateResponse, Message: ex.Message);
        }
    }

    public async Task<JobDetailQueryResult> GetJobDetailAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(jobId, out _))
        {
            return new JobDetailQueryResult(false, EmptyJobDetail, "Invalid job ID.", "INVALID_JOB_ID");
        }

        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn("jobs.detail", "Job detail query skipped because JWT token is missing.");
            return new JobDetailQueryResult(false, EmptyJobDetail, "Missing token", "AUTH_MISSING_TOKEN");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await SendForItemAsync(
            request,
            "jobs.detail",
            EmptyJobDetail,
            static (isSuccess, data, message, errorCode, isConflict) => new JobDetailQueryResult(isSuccess, data, message, errorCode, isConflict),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RequestDetailQueryResult> GetRequestDetailAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(requestId, out _))
        {
            return new RequestDetailQueryResult(false, EmptyRequestDetail, "Invalid request ID.", "INVALID_REQUEST_ID");
        }

        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn("requests.detail", "Request detail query skipped because JWT token is missing.");
            return new RequestDetailQueryResult(false, EmptyRequestDetail, "Missing token", "AUTH_MISSING_TOKEN");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/requests/{requestId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await SendForItemAsync(
            request,
            "requests.detail",
            EmptyRequestDetail,
            static (isSuccess, data, message, errorCode, isConflict) => new RequestDetailQueryResult(isSuccess, data, message, errorCode, isConflict),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RequestStatusTransitionResult> TransitionRequestStatusAsync(
        string requestId,
        string nextStatus,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(requestId, out _))
        {
            return new RequestStatusTransitionResult(false, EmptyRequestTransition, "Invalid request ID.", "INVALID_REQUEST_ID");
        }

        if (string.IsNullOrWhiteSpace(nextStatus))
        {
            return new RequestStatusTransitionResult(false, EmptyRequestTransition, "Next status is required.", "VALIDATION_FAILED");
        }

        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn("requests.transition", "Status transition skipped because JWT token is missing.");
            return new RequestStatusTransitionResult(false, EmptyRequestTransition, "Missing token", "AUTH_MISSING_TOKEN");
        }

        var payload = new TransitionServiceRequestStatusRequest
        {
            NextStatus = nextStatus,
            RowVersion = rowVersion,
        };

        var jsonPayload = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/requests/{requestId}/status")
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await SendForItemAsync(
            request,
            "requests.transition",
            EmptyRequestTransition,
            static (isSuccess, data, message, errorCode, isConflict) => new RequestStatusTransitionResult(isSuccess, data, message, errorCode, isConflict),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<LiveQueryResult<T>> QueryListAsync<T>(
        string route,
        string category,
        CancellationToken cancellationToken)
    {
        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn(category, "Live query skipped because JWT token is missing.");
            return new LiveQueryResult<T>(IsLive: false, Items: Array.Empty<T>(), Message: "Missing token");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            _diagnostics.Info(category, $"Dispatching live query to '{route}'.");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _diagnostics.Warn(category, $"Live query failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                return new LiveQueryResult<T>(
                    IsLive: false,
                    Items: Array.Empty<T>(),
                    Message: $"HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (TryExtractItems<T>(json, out var items))
            {
                _diagnostics.Info(category, $"Live query succeeded with {items.Count} item(s).");
                return new LiveQueryResult<T>(IsLive: true, Items: items);
            }

            _diagnostics.Warn(category, "Live query returned an unexpected payload shape.");
            return new LiveQueryResult<T>(IsLive: false, Items: Array.Empty<T>(), Message: "Unexpected payload");
        }
        catch (Exception ex)
        {
            _diagnostics.Error(category, $"Live query threw an exception: {ex.Message}");
            return new LiveQueryResult<T>(IsLive: false, Items: Array.Empty<T>(), Message: ex.Message);
        }
    }

    private static bool TryExtractItems<T>(string json, out IReadOnlyList<T> items)
    {
        items = Array.Empty<T>();

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(json, JsonOptions);
            if (envelope is null || !envelope.Success || envelope.Data.ValueKind == JsonValueKind.Undefined || envelope.Data.ValueKind == JsonValueKind.Null)
            {
                return false;
            }

            var dataElement = envelope.Data;

            if (dataElement.ValueKind == JsonValueKind.Array)
            {
                var arrayItems = JsonSerializer.Deserialize<List<T>>(dataElement.GetRawText(), JsonOptions) ?? new List<T>();
                items = arrayItems;
                return true;
            }

            if (dataElement.ValueKind == JsonValueKind.Object && dataElement.TryGetProperty("items", out var nestedItems))
            {
                var pagedItems = JsonSerializer.Deserialize<List<T>>(nestedItems.GetRawText(), JsonOptions) ?? new List<T>();
                items = pagedItems;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryExtractItem<T>(string json, out T item)
    {
        item = default!;

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(json, JsonOptions);
            if (envelope is not null && envelope.Success && envelope.Data.ValueKind == JsonValueKind.Object)
            {
                item = JsonSerializer.Deserialize<T>(envelope.Data.GetRawText(), JsonOptions)!;
                return item is not null;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private async Task<TResult> SendForItemAsync<TItem, TResult>(
        HttpRequestMessage request,
        string category,
        TItem fallback,
        Func<bool, TItem, string, string, bool, TResult> resultFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            _diagnostics.Info(category, $"Dispatching request to '{request.RequestUri}'.");
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var failure = ExtractFailure(json, response.StatusCode);
                _diagnostics.Warn(category, $"Request failed with HTTP {(int)response.StatusCode}: {failure.Message}");
                return resultFactory(false, fallback, failure.Message, failure.ErrorCode, failure.IsConflict);
            }

            if (TryExtractItem<TItem>(json, out var item) && item is not null)
            {
                _diagnostics.Info(category, "Request succeeded.");
                return resultFactory(true, item, "OK", string.Empty, false);
            }

            _diagnostics.Warn(category, "Request succeeded but payload shape was unexpected.");
            return resultFactory(false, fallback, "Unexpected payload", "UNEXPECTED_PAYLOAD", false);
        }
        catch (Exception ex)
        {
            _diagnostics.Error(category, $"Request threw an exception: {ex.Message}");
            return resultFactory(false, fallback, ex.Message, "CLIENT_EXCEPTION", false);
        }
    }

    private static ApiFailure ExtractFailure(string json, HttpStatusCode statusCode)
    {
        var fallbackMessage = $"HTTP {(int)statusCode}";
        var fallbackErrorCode = string.Empty;

        if (string.IsNullOrWhiteSpace(json))
        {
            return new ApiFailure(fallbackMessage, fallbackErrorCode, statusCode == HttpStatusCode.Conflict);
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<object>>(json, JsonOptions);
            if (envelope is null)
            {
                return new ApiFailure(fallbackMessage, fallbackErrorCode, statusCode == HttpStatusCode.Conflict);
            }

            var message = string.IsNullOrWhiteSpace(envelope.Message) ? fallbackMessage : envelope.Message;
            var errorCode = envelope.ErrorCode ?? string.Empty;

            if (statusCode == HttpStatusCode.Conflict && TryExtractItem<ConflictResponse>(json, out var conflict) && conflict is not null)
            {
                if (!string.IsNullOrWhiteSpace(conflict.Message))
                {
                    message = conflict.Message;
                }

                if (!string.IsNullOrWhiteSpace(conflict.ErrorCode))
                {
                    errorCode = conflict.ErrorCode;
                }
            }

            return new ApiFailure(message, errorCode, statusCode == HttpStatusCode.Conflict || string.Equals(errorCode, "CONCURRENCY_CONFLICT", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return new ApiFailure(fallbackMessage, fallbackErrorCode, statusCode == HttpStatusCode.Conflict);
        }
    }

    private sealed record ApiFailure(string Message, string ErrorCode, bool IsConflict);
}