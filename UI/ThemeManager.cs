using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FNFBot.Services;
using Microsoft.Win32;

namespace FNFBot.UI;

public static class ThemeManager
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
    private static readonly ConditionalWeakTable<Form, FormThemeState> FormThemeStates = new();

    public const string SemanticText = "theme:text";
    public const string SemanticMuted = "theme:muted";
    public const string SemanticSuccess = "theme:success";
    public const string SemanticWarning = "theme:warning";
    public const string SemanticError = "theme:error";
    public const string SemanticInfo = "theme:info";

    public static void Apply(Control root, ColorScheme scheme)
    {
        ThemePalette palette = ThemePalette.For(scheme);
        root.SuspendLayout();
        try
        {
            ApplyControl(root, palette);
        }
        finally
        {
            root.ResumeLayout(true);
        }

        if (root is Form form)
        {
            FormThemeState state = FormThemeStates.GetOrCreateValue(form);
            state.UseDarkTitleBar = palette.IsDark;
            form.HandleCreated -= ApplyTitleBarTheme;
            form.HandleCreated += ApplyTitleBarTheme;
            if (form.IsHandleCreated)
            {
                SetTitleBarTheme(form, palette.IsDark);
            }
        }
    }

    public static Color GetSemanticColor(ColorScheme scheme, string semantic)
    {
        ThemePalette palette = ThemePalette.For(scheme);
        return semantic switch
        {
            SemanticMuted => palette.Muted,
            SemanticSuccess => palette.Success,
            SemanticWarning => palette.Warning,
            SemanticError => palette.Error,
            SemanticInfo => palette.Info,
            _ => palette.Text
        };
    }

    private static void ApplyControl(Control control, ThemePalette palette)
    {
        control.BackColor = palette.Control;
        control.ForeColor = palette.Text;

        switch (control)
        {
            case TextBoxBase:
            case ListBox:
            case ComboBox:
            case NumericUpDown:
                control.BackColor = palette.Input;
                control.ForeColor = palette.Text;
                break;
            case Button button:
                button.BackColor = palette.Button;
                button.ForeColor = palette.Text;
                button.FlatStyle = palette.UseFlatButtons ? FlatStyle.Flat : FlatStyle.Standard;
                if (palette.UseFlatButtons)
                {
                    button.FlatAppearance.BorderColor = palette.Border;
                }
                break;
        }

        if (control.Tag is string semantic && semantic.StartsWith("theme:", StringComparison.Ordinal))
        {
            control.ForeColor = GetSemanticColor(palette.Scheme, semantic);
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child, palette);
        }
    }

    private static void ApplyTitleBarTheme(object? sender, EventArgs eventArgs)
    {
        if (sender is Form form && FormThemeStates.TryGetValue(form, out FormThemeState? state))
        {
            SetTitleBarTheme(form, state.UseDarkTitleBar);
        }
    }

    private static void SetTitleBarTheme(Form form, bool useDarkMode)
    {
        int enabled = useDarkMode ? 1 : 0;
        if (DwmSetWindowAttribute(
                form.Handle,
                DwmUseImmersiveDarkMode,
                ref enabled,
                sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(
                form.Handle,
                DwmUseImmersiveDarkModeBefore20H1,
                ref enabled,
                sizeof(int));
        }
    }

    private static bool SystemUsesDarkMode()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                null);
            if (value is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0;
            }
        }
        catch
        {
            // Fall through to the Windows theme API.
        }

        try
        {
            return ShouldAppsUseDarkMode();
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    [DllImport("uxtheme.dll", EntryPoint = "#132")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShouldAppsUseDarkMode();

    private sealed class FormThemeState
    {
        public bool UseDarkTitleBar { get; set; }
    }

    private sealed record ThemePalette(
        ColorScheme Scheme,
        Color Control,
        Color Input,
        Color Button,
        Color Text,
        Color Muted,
        Color Border,
        Color Success,
        Color Warning,
        Color Error,
        Color Info,
        bool UseFlatButtons,
        bool IsDark)
    {
        public static ThemePalette For(ColorScheme scheme)
        {
            return scheme switch
            {
                ColorScheme.Light => new ThemePalette(
                    scheme,
                    Color.FromArgb(245, 246, 248),
                    Color.White,
                    Color.FromArgb(235, 237, 240),
                    Color.FromArgb(25, 28, 33),
                    Color.FromArgb(100, 106, 115),
                    Color.FromArgb(185, 190, 198),
                    Color.FromArgb(20, 130, 70),
                    Color.FromArgb(190, 110, 0),
                    Color.FromArgb(190, 45, 45),
                    Color.FromArgb(35, 95, 190),
                    true,
                    false),
                ColorScheme.Dark => new ThemePalette(
                    scheme,
                    Color.FromArgb(30, 32, 36),
                    Color.FromArgb(42, 45, 50),
                    Color.FromArgb(52, 55, 61),
                    Color.FromArgb(235, 237, 240),
                    Color.FromArgb(165, 170, 178),
                    Color.FromArgb(82, 87, 96),
                    Color.FromArgb(90, 205, 130),
                    Color.FromArgb(245, 180, 70),
                    Color.FromArgb(245, 105, 105),
                    Color.FromArgb(105, 165, 255),
                    true,
                    true),
                _ => SystemUsesDarkMode()
                    ? For(ColorScheme.Dark) with { Scheme = ColorScheme.System }
                    : For(ColorScheme.Light) with { Scheme = ColorScheme.System }
            };
        }
    }
}
