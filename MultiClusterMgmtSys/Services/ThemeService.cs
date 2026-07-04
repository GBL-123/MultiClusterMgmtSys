using Microsoft.JSInterop;
using MudBlazor;

namespace MultiClusterMgmtSys.Services;

public class ThemeService(IJSRuntime js)
{
    private readonly IJSRuntime _js = js;
    private const string StorageKey = "mcm-theme-dark-mode";

    public MudTheme Theme { get; } = BuildTheme();
    public bool IsDarkMode { get; set; }
    // When true, system preference changes auto-update IsDarkMode via MudThemeProvider;
    // when user explicitly toggles, set false to lock their choice.
    public bool ObserveSystemDarkModeChange { get; private set; } = true;

    /// <summary>
    /// Call from MainLayout OnAfterRenderAsync(firstRender). Order: saved preference > system > false.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var saved = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (saved is not null)
            {
                IsDarkMode = saved == "true";
                ObserveSystemDarkModeChange = false; // user has explicit preference, lock it
                return;
            }
            // No saved value: let MudThemeProvider auto-follow system (ObserveSystemDarkModeChange stays true).
            // Initial IsDarkMode stays false; provider's built-in tracking will update it via IsDarkModeChanged.
        }
        catch
        {
            // JS interop not available (e.g. prerender) — keep defaults
        }
    }

    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        ObserveSystemDarkModeChange = false; // explicit user choice overrides system tracking
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, IsDarkMode.ToString().ToLower());
        }
        catch
        {
            // ignore JS errors
        }
    }

    private static MudTheme BuildTheme()
    {
        var theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2563EB",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#64748B",
                SecondaryContrastText = "#FFFFFF",
                Background = "#F8FAFC",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#0F172A",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#0F172A",
                TextPrimary = "#0F172A",
                TextSecondary = "#475569",
                Divider = "#E2E8F0",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                Info = "#0891B2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#60A5FA",
                PrimaryContrastText = "#0F172A",
                Secondary = "#94A3B8",
                SecondaryContrastText = "#0F172A",
                Background = "#0F172A",
                Surface = "#1E293B",
                AppbarBackground = "#1E293B",
                AppbarText = "#F1F5F9",
                DrawerBackground = "#0F172A",
                DrawerText = "#F1F5F9",
                TextPrimary = "#F1F5F9",
                TextSecondary = "#94A3B8",
                Divider = "#334155",
                Success = "#4ADE80",
                Warning = "#FBBF24",
                Error = "#F87171",
                Info = "#22D3EE",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "6px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography { FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "Noto Sans", "PingFang SC", "Microsoft YaHei", "sans-serif"] },
                H4 = new H4Typography { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-0.01em" },
                H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35", LetterSpacing = "-0.005em" },
                H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4" },
                Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.5" },
                Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5" },
                Body1 = new Body1Typography { FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.6" },
                Body2 = new Body2Typography { FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.5" },
                Button = new ButtonTypography { FontSize = "0.8125rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0.02em" },
                Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4" },
                Overline = new OverlineTypography { FontSize = "0.6875rem", FontWeight = "500", LineHeight = "1.4", LetterSpacing = "0.08em" },
            },
        };
        return theme;
    }
}
