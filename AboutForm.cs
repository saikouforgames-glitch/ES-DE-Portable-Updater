namespace ESDEUpdater;

public partial class AboutForm : Form
{
    public AboutForm(AppThemeMode theme)
    {
        InitializeComponent();
        ThemeService.ApplyTheme(this, theme);

        lblVersion.Text = $"Version {Application.ProductVersion}";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void LinkGitHub_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenUrl("https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater");
    }

    private void LinkDocs_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenUrl("https://github.com/saikouforgames-glitch/ES-DE-Portable-Updater/blob/main/DOCUMENTATION.md");
    }

    private void LinkEsDe_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenUrl("https://es-de.org/");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore — user can copy the URL manually
        }
    }
}
