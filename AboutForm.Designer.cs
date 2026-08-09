namespace ESDEUpdater;

partial class AboutForm
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
        lblVersion = new Label();
        lblAuthor = new Label();
        lblLicense = new Label();
        linkGitHub = new LinkLabel();
        linkDocs = new LinkLabel();
        linkEsDe = new LinkLabel();
        btnOk = new ThemedButton();
        SuspendLayout();

        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.Location = new Point(16, 16);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(224, 25);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "ES-DE Portable Updater";

        lblVersion.AutoSize = true;
        lblVersion.Location = new Point(16, 48);
        lblVersion.Name = "lblVersion";
        lblVersion.Size = new Size(60, 15);
        lblVersion.TabIndex = 1;
        lblVersion.Text = "Version";

        lblAuthor.AutoSize = true;
        lblAuthor.Location = new Point(16, 72);
        lblAuthor.Name = "lblAuthor";
        lblAuthor.Size = new Size(130, 15);
        lblAuthor.TabIndex = 2;
        lblAuthor.Text = "\u00a9 2026 Evander Aston";

        lblLicense.AutoSize = true;
        lblLicense.Location = new Point(16, 96);
        lblLicense.Name = "lblLicense";
        lblLicense.Size = new Size(100, 15);
        lblLicense.TabIndex = 3;
        lblLicense.Text = "MIT License";

        linkGitHub.AutoSize = true;
        linkGitHub.Location = new Point(16, 128);
        linkGitHub.Name = "linkGitHub";
        linkGitHub.Size = new Size(220, 15);
        linkGitHub.TabIndex = 4;
        linkGitHub.TabStop = true;
        linkGitHub.Text = "GitHub: ES-DE Portable Updater";
        linkGitHub.LinkClicked += LinkGitHub_LinkClicked;

        linkDocs.AutoSize = true;
        linkDocs.Location = new Point(16, 152);
        linkDocs.Name = "linkDocs";
        linkDocs.Size = new Size(220, 15);
        linkDocs.TabIndex = 5;
        linkDocs.TabStop = true;
        linkDocs.Text = "Full Documentation";
        linkDocs.LinkClicked += LinkDocs_LinkClicked;

        linkEsDe.AutoSize = true;
        linkEsDe.Location = new Point(16, 176);
        linkEsDe.Name = "linkEsDe";
        linkEsDe.Size = new Size(220, 15);
        linkEsDe.TabIndex = 6;
        linkEsDe.TabStop = true;
        linkEsDe.Text = "ES-DE Official Site (es-de.org)";
        linkEsDe.LinkClicked += LinkEsDe_LinkClicked;

        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.Location = new Point(256, 204);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(84, 27);
        btnOk.TabIndex = 7;
        btnOk.Text = "OK";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += BtnOk_Click;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(356, 245);
        Controls.Add(btnOk);
        Controls.Add(linkEsDe);
        Controls.Add(linkDocs);
        Controls.Add(linkGitHub);
        Controls.Add(lblLicense);
        Controls.Add(lblAuthor);
        Controls.Add(lblVersion);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AboutForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "About";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblVersion;
    private Label lblAuthor;
    private Label lblLicense;
    private LinkLabel linkGitHub;
    private LinkLabel linkDocs;
    private LinkLabel linkEsDe;
    private ThemedButton btnOk;
}
