namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.Configuration;
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

		// Environment-aware API configuration
		builder.Services.AddSingleton<ApiEndpointConfiguration>();

		return builder.Build();
	}
}
