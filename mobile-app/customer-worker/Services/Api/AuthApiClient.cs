using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.MobileApp.Services.Api;

public sealed class AuthApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<AuthSessionResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        return this.PostAsync("api/v1/auth/login", request, cancellationToken);
    }

    public Task<AuthSessionResponse> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
    {
        return this.PostAsync("api/v1/auth/register", request, cancellationToken);
    }

    private async Task<AuthSessionResponse> PostAsync<TRequest>(string relativeUrl, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await this.httpClient.PostAsJsonAsync(relativeUrl, request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<AuthSessionResponse>>(content, SerializerOptions);
            if (envelope?.Success == true && envelope.Data is not null)
            {
                return envelope.Data;
            }

            throw new MobileAuthApiException(HttpStatusCode.InternalServerError, "AUTH_RESPONSE_INVALID", "Authentication response was invalid.");
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new MobileAuthApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"Authentication request failed with status {(int)response.StatusCode}.");
    }
}

public sealed class MobileAuthApiException : Exception
{
    public MobileAuthApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
