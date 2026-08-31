using MudBlazor;

namespace MultiClusterMgmtSys.Components.Common;

/// <summary>
/// Swiss Industrial Print 主题:纯亮色基底,墨色 + 单一琥珀强调色。
/// 暗色模式已移除(含 localStorage 偏好逻辑)。
/// </summary>
public static class ThemeManager
{
    public static MudTheme Theme { get; } = BuildTheme();

    private static MudTheme BuildTheme()
    {
        var theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#111111",
                PrimaryDarken = "#2E2A24",
                PrimaryLighten = "#3A352E",
                PrimaryContrastText = "#FCFBF7",
                Secondary = "#6E675C",
                SecondaryContrastText = "#FCFBF7",
                Background = "#F4F4F0",
                Surface = "#FCFBF7",
                AppbarBackground = "#FCFBF7",
                AppbarText = "#111111",
                DrawerBackground = "#FCFBF7",
                DrawerText = "#111111",
                TextPrimary = "#111111",
                TextSecondary = "#6E675C",
                Divider = "#E2DED5",
                Success = "#346538",
                Warning = "#D97706",
                Error = "#9F2F2D",
                Info = "#146C7C",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "3px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography { FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "PingFang SC", "Microsoft YaHei", "Noto Sans SC", "sans-serif"] },
                H4 = new H4Typography { FontFamily = ["Space Grotesk", "PingFang SC", "Microsoft YaHei", "Noto Sans SC", "sans-serif"], FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-0.01em" },
                H5 = new H5Typography { FontFamily = ["Space Grotesk", "PingFang SC", "Microsoft YaHei", "Noto Sans SC", "sans-serif"], FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-0.005em" },
                H6 = new H6Typography { FontFamily = ["Space Grotesk", "PingFang SC", "Microsoft YaHei", "Noto Sans SC", "sans-serif"], FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.35" },
                Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.5" },
                Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5" },
                Body1 = new Body1Typography { FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.6" },
                Body2 = new Body2Typography { FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.5" },
                Button = new ButtonTypography { FontSize = "0.8125rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0.01em" },
                Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4" },
                Overline = new OverlineTypography { FontSize = "0.6875rem", FontWeight = "600", LineHeight = "1.4", LetterSpacing = "0.08em" },
            },
        };
        return theme;
    }
}