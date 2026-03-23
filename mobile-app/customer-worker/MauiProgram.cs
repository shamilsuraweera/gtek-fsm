namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Configuration;
using GTEK.FSM.MobileApp.Services.Api;
using GTEK.FSM.MobileApp.Services.Identity;
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

		// Environment-aware API configuration
		builder.Services.AddSingleton<ApiEndpointConfiguration>();
		builder.Services.AddSingleton<IIdentityTokenProvider, EnvironmentIdentityTokenProvider>();
		builder.Services.AddSingleton<ITenantContextInitializer, JwtTenantContextInitializer>();
		builder.Services.AddSingleton<IAuthenticatedApiProbeService>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var tokenProvider = serviceProvider.GetRequiredService<IIdentityTokenProvider>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new AuthenticatedApiProbeService(httpClient, tokenProvider);
		});

		builder.Services.AddSingleton<ITenantOwnershipProbeService>(serviceProvider =>
		{
			var config = serviceProvider.GetRequiredService<ApiEndpointConfiguration>();
			var tokenProvider = serviceProvider.GetRequiredService<IIdentityTokenProvider>();
			var tenantContextState = serviceProvider.GetRequiredService<TenantContextState>();
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.ApiBaseUrl),
			};

			return new TenantOwnershipProbeService(httpClient, tokenProvider, tenantContextState);
		});

		builder.Services.AddSingleton<IConnectivityRecoveryService, ConnectivityRecoveryService>();

		return builder.Build();
	}
}
