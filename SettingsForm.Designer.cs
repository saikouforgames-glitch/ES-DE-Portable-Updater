namespace ESDEUpdater;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        grpGeneral = new GroupBox();
        chkRememberFolders = new ThemedCheckBox();
        grpBackup = new GroupBox();
        chkEnableBackup = new ThemedCheckBox();
        chkBackupEmulators = new ThemedCheckBox();
        chkBackupEsDe = new ThemedCheckBox();
        chkBackupRoms = new ThemedCheckBox();
        chkBackupRomsAll = new ThemedCheckBox();
        grpAppearance = new GroupBox();
        rdoThemeDark = new RadioButton();
        rdoThemeLight = new RadioButton();
        rdoThemeSystem = new RadioButton();
        grpAdvanced = new GroupBox();
        chkAutoDeletePackage = new ThemedCheckBox();
        btnRestoreDefaults = new ThemedButton();
        btnSave = new ThemedButton();
        btnCancel = new ThemedButton();
        grpGeneral.SuspendLayout();
        grpBackup.SuspendLayout();
        grpAppearance.SuspendLayout();
        grpAdvanced.SuspendLayout();
        SuspendLayout();

        grpGeneral.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpGeneral.Controls.Add(chkAutoDeletePackage);
        grpGeneral.Controls.Add(chkRememberFolders);
        grpGeneral.Location = new Point(12, 12);
        grpGeneral.Name = "grpGeneral";
        grpGeneral.Size = new Size(360, 88);
        grpGeneral.TabIndex = 0;
        grpGeneral.TabStop = false;
        grpGeneral.Text = "General";

        chkRememberFolders.AutoSize = true;
        chkRememberFolders.Location = new Point(16, 25);
        chkRememberFolders.Name = "chkRememberFolders";
        chkRememberFolders.Size = new Size(152, 19);
        chkRememberFolders.TabIndex = 0;
        chkRememberFolders.Text = "Remember Last Folders";
        chkRememberFolders.UseVisualStyleBackColor = true;

        chkAutoDeletePackage.AutoSize = true;
        chkAutoDeletePackage.Checked = true;
        chkAutoDeletePackage.CheckState = CheckState.Checked;
        chkAutoDeletePackage.Location = new Point(16, 50);
        chkAutoDeletePackage.Name = "chkAutoDeletePackage";
        chkAutoDeletePackage.Size = new Size(297, 19);
        chkAutoDeletePackage.TabIndex = 1;
        chkAutoDeletePackage.Text = "Auto-delete downloaded package after a successful update";
        chkAutoDeletePackage.UseVisualStyleBackColor = true;

        grpBackup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpBackup.Controls.Add(chkBackupRomsAll);
        grpBackup.Controls.Add(chkBackupRoms);
        grpBackup.Controls.Add(chkBackupEsDe);
        grpBackup.Controls.Add(chkBackupEmulators);
        grpBackup.Controls.Add(chkEnableBackup);
        grpBackup.Location = new Point(12, 104);
        grpBackup.Name = "grpBackup";
        grpBackup.Size = new Size(360, 156);
        grpBackup.TabIndex = 1;
        grpBackup.TabStop = false;
        grpBackup.Text = "Backup Options";

        chkEnableBackup.AutoSize = true;
        chkEnableBackup.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chkEnableBackup.Location = new Point(16, 25);
        chkEnableBackup.Name = "chkEnableBackup";
        chkEnableBackup.Size = new Size(113, 19);
        chkEnableBackup.TabIndex = 0;
        chkEnableBackup.Text = "Enable Backup";
        chkEnableBackup.UseVisualStyleBackColor = true;
        chkEnableBackup.CheckedChanged += ChkEnableBackup_CheckedChanged;

        chkBackupEmulators.AutoSize = true;
        chkBackupEmulators.Checked = true;
        chkBackupEmulators.CheckState = CheckState.Checked;
        chkBackupEmulators.Location = new Point(16, 50);
        chkBackupEmulators.Name = "chkBackupEmulators";
        chkBackupEmulators.Size = new Size(105, 19);
        chkBackupEmulators.TabIndex = 1;
        chkBackupEmulators.Text = "Emulators";
        chkBackupEmulators.UseVisualStyleBackColor = true;

        chkBackupEsDe.AutoSize = true;
        chkBackupEsDe.Checked = true;
        chkBackupEsDe.CheckState = CheckState.Checked;
        chkBackupEsDe.Location = new Point(16, 75);
        chkBackupEsDe.Name = "chkBackupEsDe";
        chkBackupEsDe.Size = new Size(167, 19);
        chkBackupEsDe.TabIndex = 2;
        chkBackupEsDe.Text = "ES-DE / .emulationstation";
        chkBackupEsDe.UseVisualStyleBackColor = true;

        chkBackupRoms.AutoSize = true;
        chkBackupRoms.Checked = true;
        chkBackupRoms.CheckState = CheckState.Checked;
        chkBackupRoms.Location = new Point(16, 100);
        chkBackupRoms.Name = "chkBackupRoms";
        chkBackupRoms.Size = new Size(61, 19);
        chkBackupRoms.TabIndex = 3;
        chkBackupRoms.Text = "ROMs";
        chkBackupRoms.UseVisualStyleBackColor = true;

        chkBackupRomsAll.AutoSize = true;
        chkBackupRomsAll.Location = new Point(16, 125);
        chkBackupRomsAll.Name = "chkBackupRomsAll";
        chkBackupRomsAll.Size = new Size(83, 19);
        chkBackupRomsAll.TabIndex = 4;
        chkBackupRomsAll.Text = "ROMs_ALL";
        chkBackupRomsAll.UseVisualStyleBackColor = true;

        grpAppearance.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpAppearance.Controls.Add(rdoThemeDark);
        grpAppearance.Controls.Add(rdoThemeLight);
        grpAppearance.Controls.Add(rdoThemeSystem);
        grpAppearance.Location = new Point(12, 268);
        grpAppearance.Name = "grpAppearance";
        grpAppearance.Size = new Size(360, 104);
        grpAppearance.TabIndex = 2;
        grpAppearance.TabStop = false;
        grpAppearance.Text = "Appearance";

        rdoThemeDark.AutoSize = true;
        rdoThemeDark.Location = new Point(16, 75);
        rdoThemeDark.Name = "rdoThemeDark";
        rdoThemeDark.Size = new Size(49, 19);
        rdoThemeDark.TabIndex = 2;
        rdoThemeDark.TabStop = true;
        rdoThemeDark.Text = "Dark";
        rdoThemeDark.UseVisualStyleBackColor = true;

        rdoThemeLight.AutoSize = true;
        rdoThemeLight.Location = new Point(16, 50);
        rdoThemeLight.Name = "rdoThemeLight";
        rdoThemeLight.Size = new Size(51, 19);
        rdoThemeLight.TabIndex = 1;
        rdoThemeLight.TabStop = true;
        rdoThemeLight.Text = "Light";
        rdoThemeLight.UseVisualStyleBackColor = true;

        rdoThemeSystem.AutoSize = true;
        rdoThemeSystem.Location = new Point(16, 25);
        rdoThemeSystem.Name = "rdoThemeSystem";
        rdoThemeSystem.Size = new Size(61, 19);
        rdoThemeSystem.TabIndex = 0;
        rdoThemeSystem.TabStop = true;
        rdoThemeSystem.Text = "System";
        rdoThemeSystem.UseVisualStyleBackColor = true;

        grpAdvanced.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpAdvanced.Controls.Add(btnRestoreDefaults);
        grpAdvanced.Location = new Point(12, 380);
        grpAdvanced.Name = "grpAdvanced";
        grpAdvanced.Size = new Size(360, 64);
        grpAdvanced.TabIndex = 3;
        grpAdvanced.TabStop = false;
        grpAdvanced.Text = "Advanced";

        btnRestoreDefaults.Location = new Point(16, 24);
        btnRestoreDefaults.Name = "btnRestoreDefaults";
        btnRestoreDefaults.Size = new Size(160, 25);
        btnRestoreDefaults.TabIndex = 0;
        btnRestoreDefaults.Text = "Restore Default Settings";
        btnRestoreDefaults.UseVisualStyleBackColor = true;
        btnRestoreDefaults.Click += BtnRestoreDefaults_Click;

        btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnSave.Location = new Point(192, 500);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(84, 27);
        btnSave.TabIndex = 4;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += BtnSave_Click;

        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.Location = new Point(288, 500);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(84, 27);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += BtnCancel_Click;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(384, 539);
        Controls.Add(btnCancel);
        Controls.Add(btnSave);
        Controls.Add(grpAdvanced);
        Controls.Add(grpAppearance);
        Controls.Add(grpBackup);
        Controls.Add(grpGeneral);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        grpGeneral.ResumeLayout(false);
        grpGeneral.PerformLayout();
        grpBackup.ResumeLayout(false);
        grpBackup.PerformLayout();
        grpAppearance.ResumeLayout(false);
        grpAppearance.PerformLayout();
        grpAdvanced.ResumeLayout(false);
        grpAdvanced.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private GroupBox grpGeneral;
    private ThemedCheckBox chkRememberFolders;
    private ThemedCheckBox chkAutoDeletePackage;
    private GroupBox grpBackup;
    private ThemedCheckBox chkEnableBackup;
    private ThemedCheckBox chkBackupEmulators;
    private ThemedCheckBox chkBackupEsDe;
    private ThemedCheckBox chkBackupRoms;
    private ThemedCheckBox chkBackupRomsAll;
    private GroupBox grpAppearance;
    private RadioButton rdoThemeDark;
    private RadioButton rdoThemeLight;
    private RadioButton rdoThemeSystem;
    private GroupBox grpAdvanced;
    private ThemedButton btnRestoreDefaults;
    private ThemedButton btnSave;
    private ThemedButton btnCancel;
}
