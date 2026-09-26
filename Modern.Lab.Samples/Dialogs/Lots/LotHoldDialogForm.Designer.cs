namespace Modern.Lab.Samples
{
    public partial class LotHoldDialogForm
    {
        private System.ComponentModel.IContainer components;

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null) { this.components.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.sourceCard = new Modern.Lab.WinForms.Controls.Layout.ModernGroupBox();
            this.fieldSource = new Modern.Lab.WinForms.Controls.Display.ModernFieldList();
            this.inputPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblCode = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.cboCode = new Modern.Lab.WinForms.Controls.Selection.ModernComboBox();
            this.lblEngineer = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.cboEngineer = new Modern.Lab.WinForms.Controls.Selection.ModernComboBox();
            this.lblEngineerName = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.lblDescription = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.txtDescription = new Modern.Lab.WinForms.Controls.Input.ModernTextBox();
            this.lblStatus = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.btnOk = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnCancel = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.sourceCard.SuspendLayout();
            this.inputPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            this.sourceCard.Controls.Add(this.fieldSource);
            this.sourceCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.sourceCard.Padding = new System.Windows.Forms.Padding(12, 40, 12, 8);
            this.sourceCard.Height = 152;
            this.sourceCard.Text = "Lot";
            this.sourceCard.TitleBar = true;
            this.fieldSource.Columns = 4;
            this.fieldSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fieldSource.TabIndex = 0;
            this.inputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.inputPanel.ColumnCount = 3;
            this.inputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.inputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.inputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputPanel.RowCount = 4;
            this.inputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.inputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.inputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.inputPanel.Controls.Add(this.lblCode, 0, 0);
            this.inputPanel.Controls.Add(this.cboCode, 1, 0);
            this.inputPanel.SetColumnSpan(this.cboCode, 2);
            this.inputPanel.Controls.Add(this.lblEngineer, 0, 1);
            this.inputPanel.Controls.Add(this.cboEngineer, 1, 1);
            this.inputPanel.Controls.Add(this.lblEngineerName, 2, 1);
            this.inputPanel.Controls.Add(this.lblDescription, 0, 2);
            this.inputPanel.Controls.Add(this.txtDescription, 1, 2);
            this.inputPanel.SetColumnSpan(this.txtDescription, 2);
            this.inputPanel.Controls.Add(this.lblStatus, 1, 3);
            this.inputPanel.SetColumnSpan(this.lblStatus, 2);
            this.inputPanel.TabIndex = 1;
            this.lblCode.Text = "Hold Code";
            this.lblCode.Required = true;
            this.lblCode.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Label;
            this.lblCode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCode.Size = new System.Drawing.Size(112, 24);
            this.lblEngineer.Text = "Engineer";
            this.lblEngineer.Required = true;
            this.lblEngineer.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Label;
            this.lblEngineer.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEngineer.Size = new System.Drawing.Size(112, 24);
            this.lblEngineerName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEngineerName.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            this.lblDescription.Text = "Description";
            this.lblDescription.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Label;
            this.lblDescription.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.lblDescription.Size = new System.Drawing.Size(112, 32);
            this.cboCode.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cboCode.Height = 32;
            this.cboCode.TabIndex = 0;
            this.cboCode.Enabled = false;
            this.cboCode.PlaceholderText = "Select a reason code";
            this.cboCode.SelectedIndexChanged += new System.EventHandler(this.OnSelectionChanged);
            this.cboEngineer.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cboEngineer.Height = 32;
            this.cboEngineer.TabIndex = 1;
            this.cboEngineer.Enabled = false;
            this.cboEngineer.PlaceholderText = "Select an engineer";
            this.cboEngineer.SelectedIndexChanged += new System.EventHandler(this.OnSelectionChanged);
            this.txtDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDescription.Multiline = true;
            this.txtDescription.PlaceholderText = "Description (optional)";
            this.txtDescription.TabIndex = 2;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Text = "Loading reason codes and users…";
            this.buttonPanel.Controls.Add(this.btnOk);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Size = new System.Drawing.Size(728, 48);
            this.buttonPanel.TabIndex = 2;
            this.btnOk.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Execute;
            this.btnOk.Location = new System.Drawing.Point(520, 12);
            this.btnOk.Size = new System.Drawing.Size(104, 32);
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnOk.Text = "Hold";
            this.btnOk.TabIndex = 0;
            this.btnOk.Enabled = false;
            this.btnOk.Click += new System.EventHandler(this.OnOkClick);
            this.btnCancel.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Subtle;
            this.btnCancel.Location = new System.Drawing.Point(632, 12);
            this.btnCancel.Size = new System.Drawing.Size(96, 32);
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.TabIndex = 1;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 450);
            this.Padding = new System.Windows.Forms.Padding(16);
            this.Controls.Add(this.inputPanel);
            this.Controls.Add(this.sourceCard);
            this.Controls.Add(this.buttonPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hold";
            this.Name = "LotHoldDialogForm";
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnFormClosed);
            this.sourceCard.ResumeLayout(false);
            this.inputPanel.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Modern.Lab.WinForms.Controls.Layout.ModernGroupBox sourceCard;
        private Modern.Lab.WinForms.Controls.Display.ModernFieldList fieldSource;
        private System.Windows.Forms.TableLayoutPanel inputPanel;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblCode;
        private Modern.Lab.WinForms.Controls.Selection.ModernComboBox cboCode;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblEngineer;
        private Modern.Lab.WinForms.Controls.Selection.ModernComboBox cboEngineer;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblEngineerName;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblDescription;
        private Modern.Lab.WinForms.Controls.Input.ModernTextBox txtDescription;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblStatus;
        private System.Windows.Forms.Panel buttonPanel;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnOk;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnCancel;
    }
}
