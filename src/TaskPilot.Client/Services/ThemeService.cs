using Microsoft.JSInterop;
using MudBlazor;

namespace TaskPilot.Client.Services;

public enum ThemeMode { Light, Dark, System }

public class ThemeService
{
    private readonly IJSRuntime _js;
    private ThemeMode _mode = ThemeMode.System;
    private bool _systemPrefersDark = false;

    public ThemeMode Mode => _mode;
    public bool IsDarkMode => _mode == ThemeMode.Dark || (_mode == ThemeMode.System && _systemPrefersDark);

    public event Action? ThemeChanged;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", "taskpilot-theme");
            if (Enum.TryParse<ThemeMode>(stored, out var mode))
            {
                _mode = mode;
            }
            _systemPrefersDark = await _js.InvokeAsync<bool>("window.matchMedia('(prefers-color-scheme: dark)').matches");
        }
        catch { }
    }

    public async Task SetModeAsync(ThemeMode mode)
    {
        _mode = mode;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "taskpilot-theme", mode.ToString());
        }
        catch { }
        ThemeChanged?.Invoke();
    }

    public MudTheme GetTheme()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#6255EC",
                Secondary = "#8B5CF6",
                Background = "#F8F8FC",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#6255EC",
                Secondary = "#8B5CF6",
                Background = "#0F0F13",
                Surface = "#1A1A22",
                DrawerBackground = "#1A1A22",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["DM Sans", "Helvetica", "Arial", "sans-serif"]
                },
                H1 = new H1Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
                H2 = new H2Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
                H3 = new H3Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
                H4 = new H4Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
                H5 = new H5Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
                H6 = new H6Typography { FontFamily = ["Plus Jakarta Sans", "Helvetica", "Arial", "sans-serif"] },
            }
        };
    }
}
