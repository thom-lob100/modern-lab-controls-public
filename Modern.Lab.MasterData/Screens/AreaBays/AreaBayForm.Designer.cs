namespace Modern.Lab.MasterData
{
    public partial class AreaBayForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel titlePanel;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblTitle;
        private System.Windows.Forms.Panel spTitle;
        private Modern.Lab.WinForms.Controls.Layout.ModernCardPanel searchCard;
        private Modern.Lab.WinForms.Controls.Display.ModernLabel lblKeyword;
        private System.Windows.Forms.Label lblDemoHint;
        private Modern.Lab.WinForms.Controls.Input.ModernTextBox txtKeyword;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnSearch;
        private System.Windows.Forms.Panel gapSearch;
        private Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer splitMain;
        private Modern.Lab.WinForms.Controls.Layout.ModernGroupBox listCard;
        private Modern.Lab.WinForms.Controls.Data.ModernDataGrid gridLocations;
        private Modern.Lab.WinForms.Controls.Layout.ModernGroupBox editorCard;
        private Modern.Lab.Hosting.MasterData.ModernPropertyGrid propertyGrid;
        private System.Windows.Forms.FlowLayoutPanel actionPanel;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnDelete;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnSave;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnCancel;
        private Modern.Lab.WinForms.Controls.Input.ModernButton btnNew;
        private System.Windows.Forms.ContextMenuStrip menuActions;
        private System.Windows.Forms.ToolStripMenuItem miNew;
        private System.Windows.Forms.ToolStripMenuItem miCancel;
        private System.Windows.Forms.ToolStripMenuItem miSave;
        private System.Windows.Forms.ToolStripSeparator sepDelete;
        private System.Windows.Forms.ToolStripMenuItem miDelete;

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
            this.components = new System.ComponentModel.Container();
            this.titlePanel = new System.Windows.Forms.Panel();
            this.lblTitle = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.spTitle = new System.Windows.Forms.Panel();
            this.searchCard = new Modern.Lab.WinForms.Controls.Layout.ModernCardPanel();
            this.lblDemoHint = new System.Windows.Forms.Label();
            this.lblKeyword = new Modern.Lab.WinForms.Controls.Display.ModernLabel();
            this.txtKeyword = new Modern.Lab.WinForms.Controls.Input.ModernTextBox();
            this.btnSearch = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.gapSearch = new System.Windows.Forms.Panel();
            this.splitMain = new Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer();
            this.listCard = new Modern.Lab.WinForms.Controls.Layout.ModernGroupBox();
            this.gridLocations = new Modern.Lab.WinForms.Controls.Data.ModernDataGrid();
            this.editorCard = new Modern.Lab.WinForms.Controls.Layout.ModernGroupBox();
            this.propertyGrid = new Modern.Lab.Hosting.MasterData.ModernPropertyGrid();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnDelete = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnSave = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnCancel = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.btnNew = new Modern.Lab.WinForms.Controls.Input.ModernButton();
            this.menuActions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miNew = new System.Windows.Forms.ToolStripMenuItem();
            this.miCancel = new System.Windows.Forms.ToolStripMenuItem();
            this.miSave = new System.Windows.Forms.ToolStripMenuItem();
            this.sepDelete = new System.Windows.Forms.ToolStripSeparator();
            this.miDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.titlePanel.SuspendLayout();
            this.searchCard.SuspendLayout();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.listCard.SuspendLayout();
            this.editorCard.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.menuActions.SuspendLayout();
            this.SuspendLayout();
            this.titlePanel.Controls.Add(this.lblTitle);
            this.titlePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titlePanel.Location = new System.Drawing.Point(12, 12);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(1376, 28);
            this.titlePanel.TabIndex = 0;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Title;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1376, 28);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Area / Bay Master";
            this.lblTitle.TitleBar = true;
            this.spTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.spTitle.Location = new System.Drawing.Point(12, 40);
            this.spTitle.Name = "spTitle";
            this.spTitle.Size = new System.Drawing.Size(1376, 8);
            this.spTitle.TabIndex = 1;
            this.searchCard.Controls.Add(this.lblKeyword);
            this.searchCard.Controls.Add(this.txtKeyword);
            this.searchCard.Controls.Add(this.btnSearch);
            this.searchCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.searchCard.Location = new System.Drawing.Point(12, 48);
            this.searchCard.Name = "searchCard";
            this.searchCard.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.searchCard.Size = new System.Drawing.Size(1376, 56);
            this.searchCard.TabIndex = 2;
            this.lblKeyword.BackColor = System.Drawing.Color.Transparent;
            this.lblKeyword.Kind = Modern.Lab.Controls.Wpf.Display.LabelKind.Label;
            this.lblKeyword.Location = new System.Drawing.Point(12, 12);
            this.lblKeyword.Name = "lblKeyword";
            this.lblKeyword.Size = new System.Drawing.Size(72, 32);
            this.lblKeyword.TabIndex = 0;
            this.lblKeyword.Text = "Keyword";
            this.txtKeyword.Location = new System.Drawing.Point(88, 12);
            this.txtKeyword.Name = "txtKeyword";
            this.txtKeyword.PlaceholderText = "Code, name, Fab or type";
            this.txtKeyword.ShowClearButton = true;
            this.txtKeyword.Size = new System.Drawing.Size(280, 32);
            this.txtKeyword.TabIndex = 1;
            this.btnSearch.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Primary;
            this.btnSearch.Location = new System.Drawing.Point(376, 12);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(104, 32);
            this.btnSearch.TabIndex = 2;
            this.btnSearch.Text = "Search";
            this.lblDemoHint.AutoSize = true;
            this.lblDemoHint.Location = new System.Drawing.Point(500, 20);
            this.lblDemoHint.Name = "lblDemoHint";
            this.lblDemoHint.Text = "Session demo - changes reset on restart";
            this.searchCard.Controls.Add(this.lblDemoHint);
            this.gapSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.gapSearch.Location = new System.Drawing.Point(12, 104);
            this.gapSearch.Name = "gapSearch";
            this.gapSearch.Size = new System.Drawing.Size(1376, 8);
            this.gapSearch.TabIndex = 3;
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.KeepRatio = true;
            this.splitMain.Location = new System.Drawing.Point(12, 112);
            this.splitMain.Name = "splitMain";
            this.splitMain.Panel1.Controls.Add(this.listCard);
            this.splitMain.Panel2.Controls.Add(this.editorCard);
            this.splitMain.Size = new System.Drawing.Size(1376, 676);
            this.splitMain.SplitterDistance = 820;
            this.splitMain.SplitterWidth = 8;
            this.splitMain.TabIndex = 4;
            this.splitMain.TabStop = false;
            this.listCard.Controls.Add(this.gridLocations);
            this.listCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listCard.Location = new System.Drawing.Point(0, 0);
            this.listCard.Name = "listCard";
            this.listCard.Padding = new System.Windows.Forms.Padding(6, 40, 6, 6);
            this.listCard.Size = new System.Drawing.Size(820, 676);
            this.listCard.TabIndex = 0;
            this.listCard.Text = "Areas and Bays";
            this.listCard.TitleBar = true;
            this.gridLocations.AutoFitColumns = true;
            this.gridLocations.EnableColumnVirtualization = true;
            this.gridLocations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLocations.EmptyText = "No areas or bays";
            this.gridLocations.Location = new System.Drawing.Point(8, 40);
            this.gridLocations.Name = "gridLocations";
            this.gridLocations.ContextMenuStrip = this.menuActions;
            this.gridLocations.ReadOnly = true;
            this.gridLocations.ShowStatusBar = true;
            this.gridLocations.Size = new System.Drawing.Size(804, 628);
            this.gridLocations.StatusCountFormat = "{0:N0} locations";
            this.gridLocations.TabIndex = 0;
            this.gridLocations.SelectionChanged += new System.EventHandler(this.OnLocationSelectionChanged);
            this.gridLocations.Child = null;
            this.editorCard.Controls.Add(this.propertyGrid);
            this.editorCard.Controls.Add(this.actionPanel);
            this.editorCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorCard.Location = new System.Drawing.Point(0, 0);
            this.editorCard.Name = "editorCard";
            this.editorCard.Padding = new System.Windows.Forms.Padding(12, 44, 12, 8);
            this.editorCard.Size = new System.Drawing.Size(548, 676);
            this.editorCard.TabIndex = 0;
            this.editorCard.Text = "Area / Bay";
            this.editorCard.TitleBar = true;
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.LabelWidth = 150;
            this.propertyGrid.Location = new System.Drawing.Point(14, 44);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(520, 572);
            this.propertyGrid.TabIndex = 0;
            this.actionPanel.Controls.Add(this.btnDelete);
            this.actionPanel.Controls.Add(this.btnSave);
            this.actionPanel.Controls.Add(this.btnCancel);
            this.actionPanel.Controls.Add(this.btnNew);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.actionPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.actionPanel.Location = new System.Drawing.Point(14, 616);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.actionPanel.Size = new System.Drawing.Size(520, 50);
            this.actionPanel.TabIndex = 1;
            this.btnDelete.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Danger;
            this.btnDelete.Location = new System.Drawing.Point(420, 8);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, 34);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "Delete";
            this.btnSave.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Execute;
            this.btnSave.Location = new System.Drawing.Point(312, 8);
            this.btnSave.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 34);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save";
            this.btnCancel.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnCancel.Location = new System.Drawing.Point(204, 8);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 34);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnNew.Kind = Modern.Lab.Controls.Wpf.Input.ButtonKind.Secondary;
            this.btnNew.Location = new System.Drawing.Point(96, 8);
            this.btnNew.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(100, 34);
            this.btnNew.TabIndex = 0;
            this.btnNew.Text = "New";
            this.menuActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miNew,
            this.miCancel,
            this.miSave,
            this.sepDelete,
            this.miDelete});
            this.menuActions.Name = "menuActions";
            this.menuActions.Size = new System.Drawing.Size(153, 98);
            this.miNew.Name = "miNew";
            this.miNew.Size = new System.Drawing.Size(152, 22);
            this.miNew.Text = "New";
            this.miCancel.Name = "miCancel";
            this.miCancel.Size = new System.Drawing.Size(152, 22);
            this.miCancel.Text = "Cancel";
            this.miSave.Name = "miSave";
            this.miSave.Size = new System.Drawing.Size(152, 22);
            this.miSave.Text = "Save";
            this.sepDelete.Name = "sepDelete";
            this.sepDelete.Size = new System.Drawing.Size(149, 6);
            this.miDelete.Name = "miDelete";
            this.miDelete.Size = new System.Drawing.Size(152, 22);
            this.miDelete.Text = "Delete";
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1400, 800);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.gapSearch);
            this.Controls.Add(this.searchCard);
            this.Controls.Add(this.spTitle);
            this.Controls.Add(this.titlePanel);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "AreaBayForm";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Area / Bay Master";
            this.titlePanel.ResumeLayout(false);
            this.searchCard.ResumeLayout(false);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.ResumeLayout(false);
            this.listCard.ResumeLayout(false);
            this.editorCard.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.menuActions.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
