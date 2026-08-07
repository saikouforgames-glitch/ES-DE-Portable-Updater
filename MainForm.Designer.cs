namespace ESDEUpdater;

partial class MainForm
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
        lblTitle = new Label();
        btnSettings = new ThemedButton();
        lblOldFolder = new Label();
        txtOldFolder = new ThemedTextBox();
        btnBrowseOld = new ThemedButton();
        lblNewFolder = new Label();
        txtNewFolder = new ThemedTextBox();
        btnBrowseNew = new ThemedButton();
        lblOldVersion = new Label();
        lblNewVersion = new Label();
        btnStartUpdate = new ThemedButton();
        btnDownloadLatest = new ThemedButton();
        lblBackupStatus = new Label();
        btnDeleteBackup = new ThemedButton();
        btnDeletePackage = new ThemedButton();
        lblStatus = new Label();
        txtStatusLog = new ThemedTextBox();
        progressDownload = new ThemedProgressBar();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.Location = new Point(16, 16);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(224, 25);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ES-DE Portable Updater";
        // 
        // btnSettings
        // 
        btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSettings.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnSettings.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnSettings.DisabledForeColorValue = Color.White;
        btnSettings.Location = new Point(690, 16);
        btnSettings.Name = "btnSettings";
        btnSettings.Size = new Size(94, 25);
        btnSettings.TabIndex = 1;
        btnSettings.Text = "Settings";
        btnSettings.UseVisualStyleBackColor = true;
        btnSettings.Click += BtnSettings_Click;
        // 
        // lblOldFolder
        // 
        lblOldFolder.AutoSize = true;
        lblOldFolder.Location = new Point(16, 60);
        lblOldFolder.Name = "lblOldFolder";
        lblOldFolder.Size = new Size(124, 15);
        lblOldFolder.TabIndex = 1;
        lblOldFolder.Text = "Current ES-DE (In Use)";
        // 
        // txtOldFolder
        // 
        txtOldFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOldFolder.BorderColorValue = SystemColors.WindowFrame;
        txtOldFolder.BorderStyle = BorderStyle.FixedSingle;
        txtOldFolder.Location = new Point(16, 78);
        txtOldFolder.Name = "txtOldFolder";
        txtOldFolder.ReadOnly = true;
        txtOldFolder.Size = new Size(668, 23);
        txtOldFolder.TabIndex = 2;
        // 
        // btnBrowseOld
        // 
        btnBrowseOld.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOld.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnBrowseOld.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnBrowseOld.DisabledForeColorValue = Color.White;
        btnBrowseOld.Location = new Point(690, 77);
        btnBrowseOld.Name = "btnBrowseOld";
        btnBrowseOld.Size = new Size(94, 25);
        btnBrowseOld.TabIndex = 3;
        btnBrowseOld.Text = "Browse";
        btnBrowseOld.UseVisualStyleBackColor = true;
        btnBrowseOld.Click += BtnBrowseOld_Click;
        // 
        // lblNewFolder
        // 
        lblNewFolder.AutoSize = true;
        lblNewFolder.Location = new Point(16, 114);
        lblNewFolder.Name = "lblNewFolder";
        lblNewFolder.Size = new Size(165, 15);
        lblNewFolder.TabIndex = 4;
        lblNewFolder.Text = "Upgrade/Downgrade Package";
        // 
        // txtNewFolder
        // 
        txtNewFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtNewFolder.BorderColorValue = SystemColors.WindowFrame;
        txtNewFolder.BorderStyle = BorderStyle.FixedSingle;
        txtNewFolder.Location = new Point(16, 132);
        txtNewFolder.Name = "txtNewFolder";
        txtNewFolder.ReadOnly = true;
        txtNewFolder.Size = new Size(668, 23);
        txtNewFolder.TabIndex = 5;
        // 
        // btnBrowseNew
        // 
        btnBrowseNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseNew.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnBrowseNew.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnBrowseNew.DisabledForeColorValue = Color.White;
        btnBrowseNew.Location = new Point(690, 131);
        btnBrowseNew.Name = "btnBrowseNew";
        btnBrowseNew.Size = new Size(94, 25);
        btnBrowseNew.TabIndex = 6;
        btnBrowseNew.Text = "Browse";
        btnBrowseNew.UseVisualStyleBackColor = true;
        btnBrowseNew.Click += BtnBrowseNew_Click;
        // 
        // lblOldVersion
        // 
        lblOldVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblOldVersion.Location = new Point(690, 60);
        lblOldVersion.Name = "lblOldVersion";
        lblOldVersion.Size = new Size(94, 15);
        lblOldVersion.TabIndex = 12;
        lblOldVersion.TextAlign = ContentAlignment.MiddleRight;
        lblOldVersion.UseMnemonic = false;
        // 
        // lblNewVersion
        // 
        lblNewVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblNewVersion.Location = new Point(690, 114);
        lblNewVersion.Name = "lblNewVersion";
        lblNewVersion.Size = new Size(94, 15);
        lblNewVersion.TabIndex = 13;
        lblNewVersion.TextAlign = ContentAlignment.MiddleRight;
        lblNewVersion.UseMnemonic = false;
        // 
        // btnStartUpdate
        // 
        btnStartUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnStartUpdate.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnStartUpdate.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnStartUpdate.DisabledForeColorValue = Color.White;
        btnStartUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnStartUpdate.Location = new Point(16, 172);
        btnStartUpdate.Name = "btnStartUpdate";
        btnStartUpdate.Size = new Size(668, 36);
        btnStartUpdate.TabIndex = 7;
        btnStartUpdate.Text = "Start";
        btnStartUpdate.UseVisualStyleBackColor = true;
        btnStartUpdate.Click += BtnStartUpdate_Click;
        // 
        // btnDownloadLatest
        // 
        btnDownloadLatest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDownloadLatest.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnDownloadLatest.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnDownloadLatest.DisabledForeColorValue = Color.White;
        btnDownloadLatest.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnDownloadLatest.Location = new Point(690, 172);
        btnDownloadLatest.Name = "btnDownloadLatest";
        btnDownloadLatest.Size = new Size(94, 36);
        btnDownloadLatest.TabIndex = 14;
        btnDownloadLatest.Text = "Download\r\nLatest";
        btnDownloadLatest.UseVisualStyleBackColor = true;
        btnDownloadLatest.Click += BtnDownloadLatest_Click;
        // 
        // lblBackupStatus
        // 
        lblBackupStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblBackupStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblBackupStatus.Location = new Point(16, 219);
        lblBackupStatus.Name = "lblBackupStatus";
        lblBackupStatus.Size = new Size(660, 17);
        lblBackupStatus.TabIndex = 8;
        lblBackupStatus.Text = "Backup: Off";
        lblBackupStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblBackupStatus.UseMnemonic = false;
        // 
        // btnDeleteBackup
        // 
        btnDeleteBackup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDeleteBackup.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnDeleteBackup.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnDeleteBackup.DisabledForeColorValue = Color.White;
        btnDeleteBackup.Enabled = false;
        btnDeleteBackup.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnDeleteBackup.Location = new Point(690, 211);
        btnDeleteBackup.Name = "btnDeleteBackup";
        btnDeleteBackup.Size = new Size(94, 25);
        btnDeleteBackup.TabIndex = 9;
        btnDeleteBackup.Text = "Delete Backup";
        btnDeleteBackup.UseVisualStyleBackColor = true;
        btnDeleteBackup.Click += BtnDeleteBackup_Click;
        // 
        // btnDeletePackage
        // 
        btnDeletePackage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDeletePackage.DisabledBackColorValue = Color.FromArgb(55, 55, 55);
        btnDeletePackage.DisabledBorderColorValue = Color.FromArgb(70, 70, 70);
        btnDeletePackage.DisabledForeColorValue = Color.White;
        btnDeletePackage.Enabled = false;
        btnDeletePackage.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnDeletePackage.Location = new Point(690, 240);
        btnDeletePackage.Name = "btnDeletePackage";
        btnDeletePackage.Size = new Size(94, 25);
        btnDeletePackage.TabIndex = 15;
        btnDeletePackage.Text = "Delete Package";
        btnDeletePackage.UseVisualStyleBackColor = true;
        btnDeletePackage.Click += BtnDeletePackage_Click;
        // 
        // lblStatus
        // 
        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(16, 240);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(42, 15);
        lblStatus.TabIndex = 10;
        lblStatus.Text = "Status:";
        // 
        // txtStatusLog
        // 
        txtStatusLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtStatusLog.BorderColorValue = SystemColors.WindowFrame;
        txtStatusLog.BorderStyle = BorderStyle.FixedSingle;
        txtStatusLog.Font = new Font("Consolas", 9F);
        txtStatusLog.Location = new Point(16, 281);
        txtStatusLog.Multiline = true;
        txtStatusLog.Name = "txtStatusLog";
        txtStatusLog.ReadOnly = true;
        txtStatusLog.ScrollBars = ScrollBars.Vertical;
        txtStatusLog.Size = new Size(768, 296);
        txtStatusLog.TabIndex = 11;
        txtStatusLog.WordWrap = false;
        // 
        // progressDownload
        // 
        progressDownload.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressDownload.FillColorValue = Color.FromArgb(0, 120, 215);
        progressDownload.Location = new Point(16, 267);
        progressDownload.Name = "progressDownload";
        progressDownload.Size = new Size(768, 8);
        progressDownload.TabIndex = 12;
        progressDownload.TrackColorValue = Color.FromArgb(200, 200, 200);
        progressDownload.Visible = false;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 581);
        Controls.Add(txtStatusLog);
        Controls.Add(progressDownload);
        Controls.Add(lblStatus);
        Controls.Add(btnDeleteBackup);
        Controls.Add(btnDeletePackage);
        Controls.Add(lblBackupStatus);
        Controls.Add(btnStartUpdate);
        Controls.Add(btnDownloadLatest);
        Controls.Add(lblNewVersion);
        Controls.Add(lblOldVersion);
        Controls.Add(btnBrowseNew);
        Controls.Add(txtNewFolder);
        Controls.Add(lblNewFolder);
        Controls.Add(btnBrowseOld);
        Controls.Add(txtOldFolder);
        Controls.Add(lblOldFolder);
        Controls.Add(btnSettings);
        Controls.Add(lblTitle);
        MinimumSize = new Size(640, 480);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ES-DE Portable Updater";
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private ThemedButton btnSettings;
    private Label lblOldFolder;
    private ThemedTextBox txtOldFolder;
    private ThemedButton btnBrowseOld;
    private Label lblNewFolder;
    private ThemedTextBox txtNewFolder;
    private ThemedButton btnBrowseNew;
    private Label lblOldVersion;
    private Label lblNewVersion;
    private ThemedButton btnStartUpdate;
    private ThemedButton btnDownloadLatest;
    private Label lblBackupStatus;
    private ThemedButton btnDeleteBackup;
    private ThemedButton btnDeletePackage;
    private Label lblStatus;
    private ThemedTextBox txtStatusLog;
    private ThemedProgressBar progressDownload;
}
