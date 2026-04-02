using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Common;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.WebPortal.Services.Management;

public interface IManagementSubscriptionsApiClient
{
    Task<GetOrganizationSubscriptionResponse> GetOrganizationAsync(CancellationToken cancellationToken = default);

    Task<GetOrganizationSubscriptionResponse> UpdateOrganizationAsync(UpdateOrganizationSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<GetSubscriptionUsersListResponse> GetUsersAsync(string? searchText, int page, int pageSize, CancellationToken cancellationToken = default);
}

public sealed class ManagementSubscriptionsApiClient : IManagementSubscriptionsApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public ManagementSubscriptionsApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<GetOrganizationSubscriptionResponse> GetOrganizationAsync(CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.GetAsync("api/v1/management/subscriptions/organization", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetOrganizationSubscriptionResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementSubscriptionsApiException(HttpStatusCode.InternalServerError, "SUBSCRIPTION_RESPONSE_EMPTY", "Subscription response payload was empty.");
    }

    public async Task<GetOrganizationSubscriptionResponse> UpdateOrganizationAsync(UpdateOrganizationSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, "api/v1/management/subscriptions/organization")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await this.httpClient.SendAsync(httpRequest, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetOrganizationSubscriptionResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementSubscriptionsApiException(HttpStatusCode.InternalServerError, "SUBSCRIPTION_RESPONSE_EMPTY", "Subscription update response payload was empty.");
    }

    public async Task<GetSubscriptionUsersListResponse> GetUsersAsync(string? searchText, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query.Add($"searchText={Uri.EscapeDataString(searchText)}");
        }

        using var response = await this.httpClient.GetAsync($"api/v1/management/subscriptions/users?{string.Join("&", query)}", cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetSubscriptionUsersListResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementSubscriptionsApiException(HttpStatusCode.InternalServerError, "SUBSCRIPTION_RESPONSE_EMPTY", "Subscription users response payload was empty.");
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new ManagementSubscriptionsApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new ManagementSubscriptionsApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class ManagementSubscriptionsApiException : Exception
{
    public ManagementSubscriptionsApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
