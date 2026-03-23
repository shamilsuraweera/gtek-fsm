namespace GTEK.FSM.MobileApp.Pages.Worker;

using GTEK.FSM.MobileApp.Services.Security;
using Microsoft.Extensions.DependencyInjection;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Log out",
            "Clear sensitive session state on this device?",
            "Log out",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var securityLifecycleService = services?.GetService<IMobileSecurityLifecycleService>();
        securityLifecycleService?.Logout("User initiated logout from Worker settings.");
    }
}
