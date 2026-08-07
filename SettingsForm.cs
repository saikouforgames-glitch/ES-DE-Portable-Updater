namespace ESDEUpdater;

public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        ThemeService.ApplyTheme(this, _settings.Theme);
        LoadSettingsIntoControls();
    }

    public AppSettings Settings => _settings;

    private void LoadSettingsIntoControls()
    {
        chkRememberFolders.Checked = _settings.RememberLastFolders;
        chkAutoDeletePackage.Checked = _settings.AutoDeletePackage;

        chkEnableBackup.Checked = _settings.EnableBackup;
        chkBackupEmulators.Checked = _settings.BackupEmulators;
        chkBackupEsDe.Checked = _settings.BackupEsDe;
        chkBackupRoms.Checked = _settings.BackupRoms;
        chkBackupRomsAll.Checked = _settings.BackupRomsAll;

        rdoThemeSystem.Checked = _settings.Theme == AppThemeMode.System;
        rdoThemeLight.Checked = _settings.Theme == AppThemeMode.Light;
        rdoThemeDark.Checked = _settings.Theme == AppThemeMode.Dark;

        UpdateBackupControlsState();
    }

    private void ChkEnableBackup_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateBackupControlsState();
    }

    private void UpdateBackupControlsState()
    {
        var enabled = chkEnableBackup.Checked;
        chkBackupEmulators.Enabled = enabled;
        chkBackupEsDe.Enabled = enabled;
        chkBackupRoms.Enabled = enabled;
        chkBackupRomsAll.Enabled = enabled;
    }

    private void BtnRestoreDefaults_Click(object? sender, EventArgs e)
    {
        chkRememberFolders.Checked = true;
        chkAutoDeletePackage.Checked = true;
        chkEnableBackup.Checked = false;
        chkBackupEmulators.Checked = true;
        chkBackupEsDe.Checked = true;
        chkBackupRoms.Checked = true;
        chkBackupRomsAll.Checked = false;
        rdoThemeSystem.Checked = true;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _settings.RememberLastFolders = chkRememberFolders.Checked;
        _settings.AutoDeletePackage = chkAutoDeletePackage.Checked;
        _settings.EnableBackup = chkEnableBackup.Checked;
        _settings.BackupEmulators = chkBackupEmulators.Checked;
        _settings.BackupEsDe = chkBackupEsDe.Checked;
        _settings.BackupRoms = chkBackupRoms.Checked;
        _settings.BackupRomsAll = chkBackupRomsAll.Checked;
        _settings.Theme = rdoThemeDark.Checked
            ? AppThemeMode.Dark
            : rdoThemeLight.Checked
                ? AppThemeMode.Light
                : AppThemeMode.System;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
