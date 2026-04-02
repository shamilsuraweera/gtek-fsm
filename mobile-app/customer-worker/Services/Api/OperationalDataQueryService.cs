namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;

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

public sealed class OperationalDataQueryService : IRequestQueryService, IJobQueryService, ICategoryQueryService, IServiceRequestCreationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly IMobileDiagnosticsLogger _diagnostics;

    private static readonly CreateServiceRequestResponse EmptyCreateResponse = new();

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
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var directItems = JsonSerializer.Deserialize<List<T>>(root.GetRawText(), JsonOptions) ?? new List<T>();
                items = directItems;
                return true;
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryReadEnvelopeData(root, out var dataElement))
            {
                return false;
            }

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
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryReadEnvelopeData(root, out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        item = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), JsonOptions)!;
                        return item is not null;
                    }

                    return false;
                }

                item = JsonSerializer.Deserialize<T>(root.GetRawText(), JsonOptions)!;
                return item is not null;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryReadEnvelopeData(JsonElement root, out JsonElement data)
    {
        data = default;

        if (!root.TryGetProperty("data", out data))
        {
            return false;
        }

        var hasSuccess = root.TryGetProperty("success", out var success) && success.ValueKind is JsonValueKind.True or JsonValueKind.False;
        var hasIsSuccess = root.TryGetProperty("isSuccess", out var isSuccess) && isSuccess.ValueKind is JsonValueKind.True or JsonValueKind.False;

        if (hasSuccess && !success.GetBoolean())
        {
            return false;
        }

        if (hasIsSuccess && !isSuccess.GetBoolean())
        {
            return false;
        }

        return true;
    }
}