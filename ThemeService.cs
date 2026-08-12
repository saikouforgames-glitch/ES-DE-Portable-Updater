using Microsoft.Win32;

namespace ESDEUpdater;

public static class ThemeService
{
    private static readonly Color LightBackground = Color.FromArgb(240, 240, 240);
    private static readonly Color LightForeground = Color.FromArgb(30, 30, 30);
    private static readonly Color LightControl = Color.White;
    private static readonly Color LightBorder = Color.FromArgb(180, 180, 180);
    private static readonly Color DarkBackground = Color.Black;
    private static readonly Color DarkForeground = Color.White;
    private static readonly Color DarkControl = Color.Black;
    private static readonly Color DarkBorder = Color.White;

    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsDarkMode(AppThemeMode mode)
    {
        return mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => IsSystemDarkMode()
        };
    }

    public static Color ResolveColor(AppThemeMode mode, Color light, Color dark) =>
        IsDarkMode(mode) ? dark : light;

    public static void ApplyTheme(Form form, AppThemeMode mode)
    {
        var useDark = IsDarkMode(mode);

        var back = useDark ? DarkBackground : LightBackground;
        var fore = useDark ? DarkForeground : LightForeground;
        var control = useDark ? DarkControl : LightControl;
        var border = useDark ? DarkBorder : LightBorder;

        ApplyToControlTree(form, back, fore, control, border, useDark);
    }

    private static void ApplyToControlTree(Control control, Color back, Color fore, Color controlBack, Color border, bool useDark)
    {
        if (control is Form form)
        {
            form.BackColor = back;
            form.ForeColor = fore;
        }
        else if (control is TextBox textBox)
        {
            textBox.BackColor = controlBack;
            textBox.ForeColor = fore;
            if (textBox is ThemedTextBox themedTextBox)
            {
                themedTextBox.BorderColorValue = border;
            }
        }
        else if (control is ProgressBar progressBar)
        {
            if (progressBar is ThemedProgressBar themedProgressBar)
            {
                themedProgressBar.FillColorValue = useDark ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 120, 215);
                themedProgressBar.TrackColorValue = useDark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(200, 200, 200);
            }
        }
        else if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;

            if (useDark)
            {
                button.FlatAppearance.BorderColor = Color.White;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 30, 30);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 20, 20);
                button.BackColor = Color.Black;
                button.ForeColor = Color.White;
            }
            else
            {
                button.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(210, 210, 210);
                button.BackColor = Color.FromArgb(240, 240, 240);
                button.ForeColor = Color.FromArgb(30, 30, 30);
            }

            if (button is ThemedButton themed)
            {
                themed.DisabledBackColorValue = useDark
                    ? Color.Black
                    : Color.FromArgb(240, 240, 240);
                themed.DisabledForeColorValue = useDark
                    ? Color.FromArgb(110, 110, 110)
                    : Color.FromArgb(160, 160, 160);
                themed.DisabledBorderColorValue = useDark
                    ? Color.FromArgb(70, 70, 70)
                    : Color.FromArgb(200, 200, 200);
            }
        }
        else if (control is CheckedListBox checkedListBox)
        {
            checkedListBox.BackColor = controlBack;
            checkedListBox.ForeColor = fore;
        }
        else if (control is Label or GroupBox or CheckBox or RadioButton)
        {
            control.BackColor = back;
            control.ForeColor = fore;
            if (control is ThemedCheckBox themedCheckBox)
            {
                themedCheckBox.DisabledForeColorValue = useDark ? Color.White : SystemColors.GrayText;
            }
        }

        foreach (Control child in control.Controls)
        {
            ApplyToControlTree(child, back, fore, controlBack, border, useDark);
        }
    }
}
