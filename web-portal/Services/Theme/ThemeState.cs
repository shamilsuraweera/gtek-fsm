using Microsoft.JSInterop;

namespace GTEK.FSM.WebPortal.Services.Theme;

public sealed class ThemeState
{
    private const string StorageKey = "gtek.portal.theme";
    private readonly IJSRuntime _jsRuntime;

    public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;

    public ThemeState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        var stored = await _jsRuntime.InvokeAsync<string?>("gtekTheme.getStoredTheme", StorageKey);

        CurrentMode = stored switch
        {
            "dark" => ThemeMode.Dark,
            "light" => ThemeMode.Light,
            _ => await DetectSystemThemeAsync()
        };

        await ApplyThemeAsync(CurrentMode, persist: false);
    }

    public Task ToggleAsync()
    {
        var next = CurrentMode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        return ApplyThemeAsync(next, persist: true);
    }

    private async Task<ThemeMode> DetectSystemThemeAsync()
    {
        var system = await _jsRuntime.InvokeAsync<string>("gtekTheme.getSystemTheme");
        return system == "dark" ? ThemeMode.Dark : ThemeMode.Light;
    }

    private async Task ApplyThemeAsync(ThemeMode mode, bool persist)
    {
        CurrentMode = mode;

        var value = mode == ThemeMode.Dark ? "dark" : "light";
        await _jsRuntime.InvokeVoidAsync("gtekTheme.applyTheme", value);

        if (persist)
        {
            await _jsRuntime.InvokeVoidAsync("gtekTheme.setStoredTheme", StorageKey, value);
        }
    }
}
