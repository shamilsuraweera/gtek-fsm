namespace GTEK.FSM.MobileApp.State;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public sealed class ThemePreferenceState
{
    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    public void SetPreference(ThemePreference preference)
    {
        Preference = preference;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void ResetToSystem()
    {
        Preference = ThemePreference.System;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
