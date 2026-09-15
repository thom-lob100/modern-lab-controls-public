namespace Modern.Lab.MasterData
{
    public partial class MasterDataMenuForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel titlePanel;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblTitle;
        private System.Windows.Forms.Panel spTitle;
        private Modern.Lab.WinForms.Controls.Layout.ModernCardPanel menuCard;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnProduct;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnFlow;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnOper;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnPfo;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnAreaBay;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblHint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.titlePanel = new System.Windows.Forms.Panel();
            this.lblTitle = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.spTitle = new System.Windows.Forms.Panel();
            this.menuCard = new Modern.Lab.WinForms.Controls.Layout.ModernCardPanel();
            this.btnProduct = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnFlow = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnOper = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnPfo = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnAreaBay = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.lblHint = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.titlePanel.SuspendLayout();
            this.menuCard.SuspendLayout();
            this.SuspendLayout();
            this.titlePanel.Controls.Add(this.lblTitle);
            this.titlePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titlePanel.Location = new System.Drawing.Point(12, 12);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(396, 28);
            this.titlePanel.TabIndex = 0;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Title;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(396, 28);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Master Data";
            this.lblTitle.TitleBar = true;
            this.spTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.spTitle.Location = new System.Drawing.Point(12, 40);
            this.spTitle.Name = "spTitle";
            this.spTitle.Size = new System.Drawing.Size(396, 8);
            this.spTitle.TabIndex = 1;
            this.menuCard.Controls.Add(this.btnProduct);
            this.menuCard.Controls.Add(this.btnFlow);
            this.menuCard.Controls.Add(this.btnOper);
            this.menuCard.Controls.Add(this.btnPfo);
            this.menuCard.Controls.Add(this.btnAreaBay);
            this.menuCard.Controls.Add(this.lblHint);
            this.menuCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.menuCard.Location = new System.Drawing.Point(12, 48);
            this.menuCard.Name = "menuCard";
            this.menuCard.Padding = new System.Windows.Forms.Padding(16, 16, 16, 16);
            this.menuCard.Size = new System.Drawing.Size(396, 364);
            this.menuCard.TabIndex = 2;
            this.btnProduct.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnProduct.Location = new System.Drawing.Point(20, 20);
            this.btnProduct.Name = "btnProduct";
            this.btnProduct.Size = new System.Drawing.Size(356, 44);
            this.btnProduct.TabIndex = 0;
            this.btnProduct.Text = "Product Master";
            this.btnProduct.Click += new System.EventHandler(this.OnProductClick);
            this.btnFlow.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnFlow.Location = new System.Drawing.Point(20, 76);
            this.btnFlow.Name = "btnFlow";
            this.btnFlow.Size = new System.Drawing.Size(356, 44);
            this.btnFlow.TabIndex = 1;
            this.btnFlow.Text = "Flow Master";
            this.btnFlow.Click += new System.EventHandler(this.OnFlowClick);
            this.btnOper.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnOper.Location = new System.Drawing.Point(20, 132);
            this.btnOper.Name = "btnOper";
            this.btnOper.Size = new System.Drawing.Size(356, 44);
            this.btnOper.TabIndex = 2;
            this.btnOper.Text = "Oper Master";
            this.btnOper.Click += new System.EventHandler(this.OnOperClick);
            this.btnPfo.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnPfo.Location = new System.Drawing.Point(20, 188);
            this.btnPfo.Name = "btnPfo";
            this.btnPfo.Size = new System.Drawing.Size(356, 44);
            this.btnPfo.TabIndex = 3;
            this.btnPfo.Text = "PFO Node";
            this.btnPfo.Click += new System.EventHandler(this.OnPfoClick);
            this.btnAreaBay.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnAreaBay.Location = new System.Drawing.Point(20, 244);
            this.btnAreaBay.Name = "btnAreaBay";
            this.btnAreaBay.Size = new System.Drawing.Size(356, 44);
            this.btnAreaBay.TabIndex = 4;
            this.btnAreaBay.Text = "Area / Bay Master";
            this.btnAreaBay.Click += new System.EventHandler(this.OnAreaBayClick);
            this.lblHint.BackColor = System.Drawing.Color.Transparent;
            this.lblHint.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Helper;
            this.lblHint.Location = new System.Drawing.Point(20, 304);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(356, 36);
            this.lblHint.TabIndex = 5;
            this.lblHint.Text = "Each screen loads its own list. Screens that cannot reach their server stay read-only.";
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 424);
            this.Controls.Add(this.menuCard);
            this.Controls.Add(this.spTitle);
            this.Controls.Add(this.titlePanel);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(436, 463);
            this.Name = "MasterDataMenuForm";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Master Data";
            this.titlePanel.ResumeLayout(false);
            this.menuCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
