namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.Services.Security;
using GTEK.FSM.MobileApp.State;

public partial class App : Application
{
	private readonly IMobileSecurityLifecycleService _securityLifecycleService;

	public App(
		ThemePreferenceState themePreferenceState,
		SessionContextState sessionContextState,
		IIdentityTokenProvider tokenProvider,
		IConnectivityRecoveryService connectivityRecoveryService,
		ITenantContextInitializer tenantContextInitializer,
		IMobileSecurityLifecycleService securityLifecycleService)
	{
		_securityLifecycleService = securityLifecycleService;
		InitializeComponent();

		UserAppTheme = themePreferenceState.Preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};

		// Fire-and-forget probe to validate JWT-authenticated mobile-to-API connectivity when a token is provided.
		if (!string.IsNullOrWhiteSpace(tokenProvider.GetAccessToken()))
		{
			if (!_securityLifecycleService.ValidateCurrentToken())
			{
				_securityLifecycleService.Logout("Token was invalid or expired during app startup.");
				return;
			}

			tenantContextInitializer.TryInitializeFromToken();
			_ = connectivityRecoveryService.EvaluateStartupConnectivityAsync();
		}

		MainPage = new AppShell(sessionContextState);
	}

	protected override void OnSleep()
	{
		base.OnSleep();
		_securityLifecycleService.ApplyBackgroundPrivacyMask();
	}

	protected override void OnResume()
	{
		base.OnResume();
		_securityLifecycleService.RestoreFromBackground();

		if (!_securityLifecycleService.ValidateCurrentToken())
		{
			_securityLifecycleService.Logout("Token was invalid or expired when app resumed.");
		}
	}
}
