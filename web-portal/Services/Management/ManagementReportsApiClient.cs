using System.Net;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.WebPortal.Services.Management;

public interface IManagementReportsApiClient
{
    Task<GetManagementAnalyticsOverviewResponse> GetOverviewAsync(int? windowDays = null, int? trendBuckets = null, CancellationToken cancellationToken = default);
}

public sealed class ManagementReportsApiClient : IManagementReportsApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public ManagementReportsApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<GetManagementAnalyticsOverviewResponse> GetOverviewAsync(int? windowDays = null, int? trendBuckets = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (windowDays.HasValue)
        {
            query.Add($"windowDays={windowDays.Value}");
        }

        if (trendBuckets.HasValue)
        {
            query.Add($"trendBuckets={trendBuckets.Value}");
        }

        var path = "api/v1/management/reports/overview";
        if (query.Count > 0)
        {
            path = $"{path}?{string.Join("&", query)}";
        }

        using var response = await this.httpClient.GetAsync(path, cancellationToken);
        var envelope = await ReadSuccessEnvelopeAsync<GetManagementAnalyticsOverviewResponse>(response, cancellationToken);
        return envelope.Data ?? throw new ManagementReportsApiException(HttpStatusCode.InternalServerError, "REPORTS_RESPONSE_EMPTY", "Reports overview payload was empty.");
    }

    private static async Task<ApiResponse<T>> ReadSuccessEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, SerializerOptions);
            if ((envelope is null) || !envelope.Success)
            {
                throw new ManagementReportsApiException(response.StatusCode, "API_RESPONSE_INVALID", "The API response was not in the expected success format.");
            }

            return envelope;
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new ManagementReportsApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"The request failed with status code {(int)response.StatusCode}.");
    }
}

public sealed class ManagementReportsApiException : Exception
{
    public ManagementReportsApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
