namespace ESDEUpdater;

partial class AdvancedForm
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
        chkRememberExclusions = new ThemedCheckBox();
        lblInfo = new Label();
        lstItems = new CheckedListBox();
        lblCount = new Label();
        btnRestoreDefaults = new ThemedButton();
        btnOk = new ThemedButton();
        btnCancel = new ThemedButton();
        SuspendLayout();
        // 
        // chkRememberExclusions
        // 
        chkRememberExclusions.AutoSize = true;
        chkRememberExclusions.Location = new Point(14, 14);
        chkRememberExclusions.Name = "chkRememberExclusions";
        chkRememberExclusions.Size = new Size(280, 19);
        chkRememberExclusions.TabIndex = 0;
        chkRememberExclusions.Text = "Remember excluded folders and files (across sessions)";
        chkRememberExclusions.UseVisualStyleBackColor = true;
        // 
        // lblInfo
        // 
        lblInfo.AutoSize = true;
        lblInfo.Location = new Point(14, 44);
        lblInfo.MaximumSize = new Size(412, 0);
        lblInfo.Name = "lblInfo";
        lblInfo.Size = new Size(386, 30);
        lblInfo.TabIndex = 1;
        lblInfo.Text = "Checked items are kept during an update: they are never deleted and never overwritten. Required items are locked and always kept.";
        // 
        // lstItems
        // 
        lstItems.DrawMode = DrawMode.OwnerDrawFixed;
        lstItems.FormattingEnabled = true;
        lstItems.IntegralHeight = false;
        lstItems.ItemHeight = 18;
        lstItems.Location = new Point(14, 84);
        lstItems.Name = "lstItems";
        lstItems.Size = new Size(412, 348);
        lstItems.TabIndex = 2;
        lstItems.ScrollAlwaysVisible = true;
        lstItems.DrawItem += LstItems_DrawItem;
        lstItems.ItemCheck += LstItems_ItemCheck;
        // 
        // btnRestoreDefaults
        // 
        btnRestoreDefaults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnRestoreDefaults.Location = new Point(14, 468);
        btnRestoreDefaults.Name = "btnRestoreDefaults";
        btnRestoreDefaults.Size = new Size(130, 27);
        btnRestoreDefaults.TabIndex = 3;
        btnRestoreDefaults.Text = "Restore Defaults";
        btnRestoreDefaults.UseVisualStyleBackColor = true;
        btnRestoreDefaults.Click += BtnRestoreDefaults_Click;
        // 
        // lblCount
        // 
        lblCount.AutoSize = true;
        lblCount.Location = new Point(14, 440);
        lblCount.Name = "lblCount";
        lblCount.Size = new Size(60, 15);
        lblCount.TabIndex = 3;
        lblCount.Text = "item count";
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.Location = new Point(240, 468);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(90, 27);
        btnOk.TabIndex = 4;
        btnOk.Text = "OK";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += BtnOk_Click;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.Location = new Point(336, 468);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(90, 27);
        btnCancel.TabIndex = 5;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += BtnCancel_Click;
        // 
        // AdvancedForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 507);
        Controls.Add(lblCount);
        Controls.Add(lstItems);
        Controls.Add(lblInfo);
        Controls.Add(chkRememberExclusions);
        Controls.Add(btnRestoreDefaults);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AdvancedForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Advanced — Excluded Items";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ThemedCheckBox chkRememberExclusions;
    private Label lblInfo;
    private CheckedListBox lstItems;
    private Label lblCount;
    private ThemedButton btnRestoreDefaults;
    private ThemedButton btnOk;
    private ThemedButton btnCancel;
}