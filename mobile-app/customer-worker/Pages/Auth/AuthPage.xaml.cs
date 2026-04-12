using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Configuration;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

namespace GTEK.FSM.MobileApp.Pages.Auth;

public partial class AuthPage : ContentPage
{
    private readonly AuthApiClient authApiClient;
    private readonly ApiEndpointConfiguration endpointConfiguration;
    private readonly IIdentityTokenProvider tokenProvider;
    private bool isRegisterMode;

    public AuthPage(AuthApiClient authApiClient, ApiEndpointConfiguration endpointConfiguration, IIdentityTokenProvider tokenProvider)
    {
        InitializeComponent();
        this.authApiClient = authApiClient;
        this.endpointConfiguration = endpointConfiguration;
        this.tokenProvider = tokenProvider;
        this.ApiTargetLabel.Text = $"API target: {this.endpointConfiguration.ApiBaseUrl}";
        this.ApplyMode();
    }

    private void OnLoginModeClicked(object? sender, EventArgs e)
    {
        this.isRegisterMode = false;
        this.ApplyMode();
    }

    private void OnRegisterModeClicked(object? sender, EventArgs e)
    {
        this.isRegisterMode = true;
        this.ApplyMode();
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        this.ErrorLabel.IsVisible = false;
        this.SubmitButton.IsEnabled = false;
        this.SubmitButton.Text = "Working...";

        try
        {
            if (this.isRegisterMode)
            {
                if (string.IsNullOrWhiteSpace(this.DisplayNameEntry.Text))
                {
                    throw new InvalidOperationException("Display name is required for registration.");
                }

                var registration = await this.authApiClient.RegisterAsync(new RegisterLocalUserRequest
                {
                    DisplayName = this.DisplayNameEntry.Text,
                    Email = this.EmailEntry.Text,
                    Password = this.PasswordEntry.Text,
                    TenantCode = this.TenantCodeEntry.Text,
                });

                this.tokenProvider.SetAccessToken(registration.AccessToken);
            }
            else
            {
                var session = await this.authApiClient.LoginAsync(new LoginRequest
                {
                    Email = this.EmailEntry.Text,
                    Password = this.PasswordEntry.Text,
                });

                this.tokenProvider.SetAccessToken(session.AccessToken);
            }

            (Application.Current as App)?.ShowAuthenticatedShell();
        }
        catch (MobileAuthApiException ex)
        {
            this.ErrorLabel.Text = ex.Message;
            this.ErrorLabel.IsVisible = true;
        }
        catch (HttpRequestException)
        {
            this.ErrorLabel.Text = $"Cannot reach API at {this.endpointConfiguration.ApiBaseUrl}. For physical Android, use '--run' so adb reverse is set, or set GTEK_FSM_API_BASE_URL to your PC LAN IP.";
            this.ErrorLabel.IsVisible = true;
        }
        catch (Exception ex)
        {
            this.ErrorLabel.Text = ex.Message;
            this.ErrorLabel.IsVisible = true;
        }
        finally
        {
            this.SubmitButton.IsEnabled = true;
            this.SubmitButton.Text = this.isRegisterMode ? "Create Customer Account" : "Sign In";
        }
    }

    private void ApplyMode()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var activeBackground = Color.FromArgb("#F38808");
        var inactiveBackground = isDark ? Color.FromArgb("#35506C") : Color.FromArgb("#E9EEF4");
        var activeText = Colors.White;
        var inactiveText = isDark ? Colors.White : Color.FromArgb("#2B435F");

        this.Title = this.isRegisterMode ? "Register" : "Sign In";
        this.RegisterFieldsLayout.IsVisible = this.isRegisterMode;
        this.SubmitButton.Text = this.isRegisterMode ? "Create Customer Account" : "Sign In";
        this.LoginModeButton.BackgroundColor = this.isRegisterMode ? inactiveBackground : activeBackground;
        this.LoginModeButton.TextColor = this.isRegisterMode ? inactiveText : activeText;
        this.RegisterModeButton.BackgroundColor = this.isRegisterMode ? activeBackground : inactiveBackground;
        this.RegisterModeButton.TextColor = this.isRegisterMode ? activeText : inactiveText;
    }
}
