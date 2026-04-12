namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Pages.Auth;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.Services.Notifications;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.MobileApp.Services.Security;
using GTEK.FSM.MobileApp.State;
using Microsoft.Extensions.DependencyInjection;

public partial class App : Application
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IMobileSecurityLifecycleService _securityLifecycleService;
	private readonly IMobileOperationalRealtimeClient _realtimeClient;
	private readonly IMobilePushNotificationOrchestrator _notificationOrchestrator;
	private readonly MobileNotificationInboxState _notificationInbox;
	private readonly SessionContextState _sessionContextState;
	private readonly IIdentityTokenProvider _tokenProvider;
	private readonly IConnectivityRecoveryService _connectivityRecoveryService;
	private readonly ITenantContextInitializer _tenantContextInitializer;

	public App(
		IServiceProvider serviceProvider,
		ThemePreferenceState themePreferenceState,
		SessionContextState sessionContextState,
		IIdentityTokenProvider tokenProvider,
		IConnectivityRecoveryService connectivityRecoveryService,
		ITenantContextInitializer tenantContextInitializer,
		IMobileOperationalRealtimeClient realtimeClient,
		IMobilePushNotificationOrchestrator notificationOrchestrator,
		MobileNotificationInboxState notificationInbox,
		IMobileSecurityLifecycleService securityLifecycleService)
	{
		_serviceProvider = serviceProvider;
		_securityLifecycleService = securityLifecycleService;
		_realtimeClient = realtimeClient;
		_notificationOrchestrator = notificationOrchestrator;
		_notificationInbox = notificationInbox;
		_sessionContextState = sessionContextState;
		_tokenProvider = tokenProvider;
		_connectivityRecoveryService = connectivityRecoveryService;
		_tenantContextInitializer = tenantContextInitializer;
		InitializeComponent();

		UserAppTheme = themePreferenceState.Preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};

		if (!string.IsNullOrWhiteSpace(_tokenProvider.GetAccessToken()) && _securityLifecycleService.ValidateCurrentToken())
		{
			ShowAuthenticatedShell();
			return;
		}

		ShowAuthenticationPage();
	}

	public void ShowAuthenticatedShell()
	{
		_tenantContextInitializer.TryInitializeFromToken();
		MainPage = new AppShell(_sessionContextState);
		_ = _connectivityRecoveryService.EvaluateStartupConnectivityAsync();
		_ = _realtimeClient.EnsureConnectedAsync();
		_ = _notificationOrchestrator.StartAsync();
	}

	public void ShowAuthenticationPage()
	{
		_notificationOrchestrator.Stop();
		_ = _realtimeClient.DisconnectAsync();
		MainPage = new NavigationPage(_serviceProvider.GetRequiredService<AuthPage>());
	}

	protected override void OnSleep()
	{
		base.OnSleep();
		_securityLifecycleService.ApplyBackgroundPrivacyMask();
		_notificationOrchestrator.Stop();
	}

	protected override void OnResume()
	{
		base.OnResume();
		_securityLifecycleService.RestoreFromBackground();

		if (!string.IsNullOrWhiteSpace(_tokenProvider.GetAccessToken()) && !_securityLifecycleService.ValidateCurrentToken())
		{
			_securityLifecycleService.Logout("Token was invalid or expired when app resumed.");
			return;
		}

		if (string.IsNullOrWhiteSpace(_tokenProvider.GetAccessToken()))
		{
			return;
		}

		_ = _realtimeClient.EnsureConnectedAsync();
		_ = _notificationOrchestrator.StartAsync();

		var latestNotification = _notificationInbox.PullLatest();
		if (!string.IsNullOrWhiteSpace(latestNotification.Route))
		{
			_ = _notificationOrchestrator.HandleNotificationTapAsync(latestNotification);
		}
	}
}
