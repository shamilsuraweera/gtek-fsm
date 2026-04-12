namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Configuration;
using GTEK.FSM.MobileApp.Pages.Auth;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.Services.Notifications;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.MobileApp.Services.Security;
using GTEK.FSM.MobileApp.State;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// App-level state containers
		builder.Services.AddSingleton<SessionContextState>();
		builder.Services.AddSingleton<TenantContextState>();
		builder.Services.AddSingleton<ThemePreferenceState>();
		builder.Services.AddSingleton<ConnectivityRecoveryState>();
		builder.Services.AddSingleton<MobileDiagnosticsState>();
		builder.Services.AddSingleton<MobileNotificationInboxState>();

		// Environment-aware API configuration
		builder.Services.AddSingleton<ApiEndpointConfiguration>();
		builder.Services.AddSingleton<IMobileDiagnosticsLogger, MobileDiagnosticsLogger>();
		builder.Services.AddSingleton<IMobileSecurityLifecycleService, MobileSecurityLifecycleService>();
		builder.Services.AddSingleton<IIdentityTokenProvider, StoredIdentityTokenProvider>();
		builder.Services.AddSingleton<ITenantContextInitializer, JwtTenantContextInitializer>();
		builder.Services.AddTransient<AuthPage>();
		builder.Services.AddSingleton<AuthApiClient>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new AuthApiClient(httpClient);
		});
		builder.Services.AddSingleton<IAuthenticatedApiProbeService>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var tokenProvider = serviceProvider.GetRequiredService<IIdentityTokenProvider>();
			var diagnostics = serviceProvider.GetRequiredService<IMobileDiagnosticsLogger>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new AuthenticatedApiProbeService(httpClient, tokenProvider, diagnostics);
		});

		builder.Services.AddSingleton<ITenantOwnershipProbeService>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var tokenProvider = serviceProvider.GetRequiredService<IIdentityTokenProvider>();
			var tenantContextState = serviceProvider.GetRequiredService<TenantContextState>();
			var diagnostics = serviceProvider.GetRequiredService<IMobileDiagnosticsLogger>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new TenantOwnershipProbeService(httpClient, tokenProvider, tenantContextState, diagnostics);
		});

		builder.Services.AddSingleton<OperationalDataQueryService>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var tokenProvider = serviceProvider.GetRequiredService<IIdentityTokenProvider>();
			var diagnostics = serviceProvider.GetRequiredService<IMobileDiagnosticsLogger>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new OperationalDataQueryService(httpClient, tokenProvider, diagnostics);
		});
		builder.Services.AddSingleton<IRequestQueryService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IJobQueryService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<ICategoryQueryService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IServiceRequestCreationService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IRequestDetailQueryService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IWorkerExecutionService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IFeedbackSubmissionService>(serviceProvider => serviceProvider.GetRequiredService<OperationalDataQueryService>());
		builder.Services.AddSingleton<IMobileOperationalRealtimeClient, SignalRMobileOperationalRealtimeClient>();
		builder.Services.AddSingleton<ILocalNotificationPublisher, InAppLocalNotificationPublisher>();
		builder.Services.AddSingleton<IMobileNotificationDeepLinkNavigator, ShellNotificationDeepLinkNavigator>();
		builder.Services.AddSingleton<IMobilePushNotificationOrchestrator, MobilePushNotificationOrchestrator>();

		builder.Services.AddSingleton<IConnectivityRecoveryService, ConnectivityRecoveryService>();

		return builder.Build();
	}
}
