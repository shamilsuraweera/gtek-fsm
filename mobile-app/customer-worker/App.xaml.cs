namespace GTEK.FSM.MobileApp;

using GTEK.FSM.MobileApp.State;

public partial class App : Application
{
	public App(ThemePreferenceState themePreferenceState)
	{
		InitializeComponent();

		UserAppTheme = themePreferenceState.Preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};

		MainPage = new AppShell();
	}
}
