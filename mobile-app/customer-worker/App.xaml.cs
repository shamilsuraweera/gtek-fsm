namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.State;

public partial class App : Application
{
	public App(
		ThemePreferenceState themePreferenceState,
		IIdentityTokenProvider tokenProvider,
		IConnectivityRecoveryService connectivityRecoveryService,
		ITenantContextInitializer tenantContextInitializer)
	{
		InitializeComponent();

		UserAppTheme = themePreferenceState.Preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};

		MainPage = new AppShell();

		// Fire-and-forget probe to validate JWT-authenticated mobile-to-API connectivity when a token is provided.
		if (!string.IsNullOrWhiteSpace(tokenProvider.GetAccessToken()))
		{
			tenantContextInitializer.TryInitializeFromToken();
			_ = connectivityRecoveryService.EvaluateStartupConnectivityAsync();
		}
	}
}
