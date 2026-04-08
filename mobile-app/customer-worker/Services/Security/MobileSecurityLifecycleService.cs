namespace GTEK.FSM.MobileApp.Services.Security;

using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.State;

public interface IMobileSecurityLifecycleService
{
    bool ValidateCurrentToken();
    void ApplyBackgroundPrivacyMask();
    void RestoreFromBackground();
    void Logout(string reason);
}

public sealed class MobileSecurityLifecycleService : IMobileSecurityLifecycleService
{
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly SessionContextState _sessionContextState;
    private readonly TenantContextState _tenantContextState;
    private readonly ConnectivityRecoveryState _connectivityRecoveryState;
    private readonly MobileDiagnosticsState _mobileDiagnosticsState;
    private readonly IMobileDiagnosticsLogger _diagnostics;
    private bool _privacyMaskApplied;
    private Page _pageBeforeMask;

    public MobileSecurityLifecycleService(
        IIdentityTokenProvider tokenProvider,
        SessionContextState sessionContextState,
        TenantContextState tenantContextState,
        ConnectivityRecoveryState connectivityRecoveryState,
        MobileDiagnosticsState mobileDiagnosticsState,
        IMobileDiagnosticsLogger diagnostics)
    {
        _tokenProvider = tokenProvider;
        _sessionContextState = sessionContextState;
        _tenantContextState = tenantContextState;
        _connectivityRecoveryState = connectivityRecoveryState;
        _mobileDiagnosticsState = mobileDiagnosticsState;
        _diagnostics = diagnostics;
    }

    public bool ValidateCurrentToken()
    {
        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _diagnostics.Warn("security.token", "Token validation skipped because no token is present.");
            return false;
        }

        if (!JwtTokenInspector.TryGetExpiryUtc(token, out var expiryUtc))
        {
            _diagnostics.Error("security.token", "Token is missing a valid exp claim. Session will be treated as invalid.");
            return false;
        }

        var isExpired = expiryUtc <= DateTimeOffset.UtcNow;
        if (isExpired)
        {
            _diagnostics.Warn("security.token", $"Token expired at {expiryUtc:O}.");
            return false;
        }

        _diagnostics.Info("security.token", $"Token is valid until {expiryUtc:O}.");
        return true;
    }

    public void ApplyBackgroundPrivacyMask()
    {
        if (_privacyMaskApplied)
        {
            return;
        }

        var page = TryGetWindowPage();
        if (page is null)
        {
            return;
        }

        _pageBeforeMask = page;
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        window.Page = BuildPrivacyMaskPage();
        _privacyMaskApplied = true;
        _diagnostics.Info("security.privacy", "Applied background privacy mask.");
    }

    public void RestoreFromBackground()
    {
        if (!_privacyMaskApplied)
        {
            return;
        }

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        if (_pageBeforeMask is not null)
        {
            window.Page = _pageBeforeMask;
        }

        _pageBeforeMask = null;
        _privacyMaskApplied = false;
        _diagnostics.Info("security.privacy", "Removed background privacy mask.");
    }

    public void Logout(string reason)
    {
        _sessionContextState.Clear();
        _tenantContextState.Clear();
        _connectivityRecoveryState.Clear();
        _mobileDiagnosticsState.Clear();
        _tokenProvider.ClearAccessToken();

        _diagnostics.Warn("security.logout", $"Session cleared: {reason}");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            (Application.Current as App)?.ShowAuthenticationPage();
        });
    }

    private static Page TryGetWindowPage()
    {
        return Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    private static Page BuildPrivacyMaskPage()
    {
        return new ContentPage
        {
            BackgroundColor = Colors.Black,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = "Session hidden while app is in background.",
                        TextColor = Colors.White,
                        FontSize = 18,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                },
            },
        };
    }
}