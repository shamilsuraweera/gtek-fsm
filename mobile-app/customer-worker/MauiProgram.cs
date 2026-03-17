namespace GTEK.FSM.MobileApp;

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

		builder.Services.AddSingleton<SessionContextState>();
		builder.Services.AddSingleton<TenantContextState>();
		builder.Services.AddSingleton<ThemePreferenceState>();

		return builder.Build();
	}
}
