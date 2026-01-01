using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace Savora.BlazorWasm.Services;

public interface IThemeService
{
    Task<bool> IsDarkModeAsync();
    Task ToggleThemeAsync();
    Task InitializeThemeAsync();
}

public class ThemeService : IThemeService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;
    private const string ThemeKey = "theme";
    private const string DarkThemeValue = "dark";
    private const string LightThemeValue = "light";

    public ThemeService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> IsDarkModeAsync()
    {
        var theme = await _localStorage.GetItemAsStringAsync(ThemeKey);
        return theme == DarkThemeValue;
    }

    public async Task ToggleThemeAsync()
    {
        var isDark = await IsDarkModeAsync();
        var newTheme = isDark ? LightThemeValue : DarkThemeValue;
        await _localStorage.SetItemAsStringAsync(ThemeKey, newTheme);
        await ApplyThemeAsync(newTheme == DarkThemeValue);
    }

    public async Task InitializeThemeAsync()
    {
        var theme = await _localStorage.GetItemAsStringAsync(ThemeKey);
        if (string.IsNullOrEmpty(theme))
        {
            // Default to light mode, or detect system preference
            try
            {
                var prefersDark = await _jsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
                theme = prefersDark ? DarkThemeValue : LightThemeValue;
            }
            catch
            {
                // Fallback to light mode if detection fails
                theme = LightThemeValue;
            }
            await _localStorage.SetItemAsStringAsync(ThemeKey, theme);
        }
        
        await ApplyThemeAsync(theme == DarkThemeValue);
    }

    private async Task ApplyThemeAsync(bool isDark)
    {
        await _jsRuntime.InvokeVoidAsync("applyTheme", isDark);
    }
}

