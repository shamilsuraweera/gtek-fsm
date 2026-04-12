using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.JSInterop;

namespace GTEK.FSM.WebPortal.Services.Security;

public sealed class PortalAuthState
{
    private const string SessionStorageKey = "gtek-fsm.portal.session";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;
    private readonly IJSRuntime jsRuntime;

    public PortalAuthState(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        this.httpClient = httpClient;
        this.jsRuntime = jsRuntime;
    }

    public event Action? Changed;

    public bool IsInitialized { get; private set; }

    public AuthSessionResponse? Session { get; private set; }

    public bool IsAuthenticated => this.Session is not null && this.Session.ExpiresAtUtc > DateTimeOffset.UtcNow;

    public string CurrentRole => this.Session?.Role ?? string.Empty;

    public string CurrentTenantId => this.Session?.TenantId ?? string.Empty;

    public async Task InitializeAsync()
    {
        if (this.IsInitialized)
        {
            return;
        }

        var serialized = await this.jsRuntime.InvokeAsync<string?>("gtekAuth.getStoredSession", SessionStorageKey);
        if (!string.IsNullOrWhiteSpace(serialized))
        {
            var session = JsonSerializer.Deserialize<AuthSessionResponse>(serialized, SerializerOptions);
            if (session is not null && session.ExpiresAtUtc > DateTimeOffset.UtcNow)
            {
                this.Session = session;
            }
            else
            {
                await this.jsRuntime.InvokeVoidAsync("gtekAuth.clearStoredSession", SessionStorageKey);
            }
        }

        this.ApplyAuthorizationHeader();
        this.IsInitialized = true;
        this.Changed?.Invoke();
    }

    public async Task LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PostAsJsonAsync("api/v1/auth/login", request, cancellationToken);
        var session = await ReadSessionAsync(response, cancellationToken);
        await this.SetSessionAsync(session);
    }

    public async Task RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await this.httpClient.PostAsJsonAsync("api/v1/auth/register", request, cancellationToken);
        var session = await ReadSessionAsync(response, cancellationToken);
        await this.SetSessionAsync(session);
    }

    public async Task LogoutAsync()
    {
        this.Session = null;
        this.ApplyAuthorizationHeader();
        await this.jsRuntime.InvokeVoidAsync("gtekAuth.clearStoredSession", SessionStorageKey);
        this.Changed?.Invoke();
    }

    public string? GetAccessToken()
    {
        return this.IsAuthenticated ? this.Session?.AccessToken : null;
    }

    private async Task SetSessionAsync(AuthSessionResponse session)
    {
        this.Session = session;
        this.ApplyAuthorizationHeader();

        var serialized = JsonSerializer.Serialize(session, SerializerOptions);
        await this.jsRuntime.InvokeVoidAsync("gtekAuth.setStoredSession", SessionStorageKey, serialized);
        this.Changed?.Invoke();
    }

    private void ApplyAuthorizationHeader()
    {
        this.httpClient.DefaultRequestHeaders.Authorization = this.IsAuthenticated
            ? new AuthenticationHeaderValue("Bearer", this.Session!.AccessToken)
            : null;
    }

    private static async Task<AuthSessionResponse> ReadSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<AuthSessionResponse>>(content, SerializerOptions);
            if (envelope?.Success == true && envelope.Data is not null)
            {
                return envelope.Data;
            }

            throw new PortalAuthApiException(HttpStatusCode.InternalServerError, "AUTH_RESPONSE_INVALID", "The authentication response was invalid.");
        }

        var errorEnvelope = JsonSerializer.Deserialize<ApiResponse<object>>(content, SerializerOptions);
        throw new PortalAuthApiException(
            response.StatusCode,
            errorEnvelope?.ErrorCode,
            errorEnvelope?.Message ?? $"Authentication request failed with status {(int)response.StatusCode}.");
    }
}

public sealed class PortalAuthApiException : Exception
{
    public PortalAuthApiException(HttpStatusCode statusCode, string? errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }
}
