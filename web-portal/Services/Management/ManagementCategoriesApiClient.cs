using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.WebPortal.Services.Management;

public interface IManagementCategoriesApiClient
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(bool includeDisabled, CancellationToken cancellationToken = default);

    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryResponse> UpdateAsync(string categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryResponse> DisableAsync(string categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryResponse>> ReorderAsync(ReorderCategoriesRequest request, CancellationToken cancellationToken = default);
}

public sealed class ManagementCategoriesApiClient : IManagementCategoriesApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public ManagementCategoriesApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(bool includeDisabled, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.GetAsync($"api/v1/categories?includeDisabled={includeDisabled.ToString().ToLowerInvariant()}", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetCategoriesListResponse>(response, cancellationToken);
        return envelope.Data?.Items ?? [];
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PostAsJsonAsync("api/v1/management/categories", request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<CategoryResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementCategoriesApiException(HttpStatusCode.InternalServerError, "CATEGORY_RESPONSE_EMPTY", "Category create response payload was empty.");
    }

    public async Task<CategoryResponse> UpdateAsync(string categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/management/categories/{categoryId}")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await this.httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<CategoryResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementCategoriesApiException(HttpStatusCode.InternalServerError, "CATEGORY_RESPONSE_EMPTY", "Category update response payload was empty.");
    }

    public async Task<CategoryResponse> DisableAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/management/categories/{categoryId}/disable");
        using var response = await this.httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<CategoryResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementCategoriesApiException(HttpStatusCode.InternalServerError, "CATEGORY_RESPONSE_EMPTY", "Category disable response payload was empty.");
    }

    public async Task<IReadOnlyList<CategoryResponse>> ReorderAsync(ReorderCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PostAsJsonAsync("api/v1/management/categories/reorder", request, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetCategoriesListResponse>(response, cancellationToken);
        return envelope.Data?.Items ?? [];
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new ManagementCategoriesApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new ManagementCategoriesApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class ManagementCategoriesApiException : Exception
{
    public ManagementCategoriesApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
