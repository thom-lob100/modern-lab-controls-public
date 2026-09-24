using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Controls.Wpf.Input;
using Modern.Lab.Hosting.MasterData;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Display;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Layout;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.MasterData
{
    public partial class CommonCodeForm
    {
        private System.ComponentModel.IContainer components;
        private CodePane editorPane;
        private ModernComboBox cmbCommonType;
        private ModernButton btnRefreshTypes;
        private ModernLabel lblTitle;
        private ContextMenuStrip menuActions;
        private ToolStripMenuItem miNew;
        private ToolStripMenuItem miCancel;
        private ToolStripMenuItem miSave;
        private ToolStripSeparator sepDelete;
        private ToolStripMenuItem miDelete;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            string title = this.manageTypes ? "Common Type Master" : "Common Code Master";
            this.editorPane = CreatePane(this.manageTypes ? "Common Type" : "Common Code");
            this.editorPane.Keyword.EnterPressed += new System.EventHandler(this.OnKeywordEnterPressed);
            this.editorPane.Search.Click += new System.EventHandler(this.OnSearchClick);
            this.editorPane.Grid.SelectionChanged += new System.EventHandler(this.OnItemSelectionChanged);
            this.editorPane.New.Click += new System.EventHandler(this.OnNewClick);
            this.editorPane.Cancel.Click += new System.EventHandler(this.OnCancelClick);
            this.editorPane.Save.Click += new System.EventHandler(this.OnSaveClick);
            this.editorPane.Delete.Click += new System.EventHandler(this.OnDeleteClick);
            this.menuActions = new ContextMenuStrip(this.components) { Name = "menuActions" };
            this.miNew = new ToolStripMenuItem { Name = "miNew", Text = "New" };
            this.miCancel = new ToolStripMenuItem { Name = "miCancel", Text = "Cancel" };
            this.miSave = new ToolStripMenuItem { Name = "miSave", Text = "Save" };
            this.sepDelete = new ToolStripSeparator { Name = "sepDelete" };
            this.miDelete = new ToolStripMenuItem { Name = "miDelete", Text = "Delete" };
            this.menuActions.Items.AddRange(new ToolStripItem[] { this.miNew, this.miCancel, this.miSave, this.sepDelete, this.miDelete });
            this.menuActions.Opening += new System.ComponentModel.CancelEventHandler(this.OnActionMenuOpening);
            this.miNew.Click += new System.EventHandler(this.OnNewClick);
            this.miCancel.Click += new System.EventHandler(this.OnCancelClick);
            this.miSave.Click += new System.EventHandler(this.OnSaveClick);
            this.miDelete.Click += new System.EventHandler(this.OnDeleteClick);
            this.editorPane.Grid.ContextMenuStrip = this.menuActions;
            this.lblTitle = new ModernLabel
            {
                Dock = DockStyle.Top, Height = 40, Kind = LabelKind.Title, TitleBar = true,
                Text = title + " — Session demo - changes reset on restart"
            };
            ModernCardPanel typeBar = new ModernCardPanel
            {
                Dock = DockStyle.Top, Height = 56, Visible = !this.manageTypes, Padding = new Padding(12, 8, 12, 8)
            };
            ModernLabel typeLabel = new ModernLabel
            {
                Text = "Common Type", Location = new Point(12, 12), Size = new Size(112, 32), Kind = LabelKind.Label
            };
            this.cmbCommonType = new ModernComboBox
            {
                Name = "cmbCommonType", Location = new Point(128, 12), Size = new Size(450, 32),
                DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.Suggest,
                AutoCompleteSource = AutoCompleteSource.ListItems, DisplayMember = "DISPLAY_NAME", ValueMember = "COMMON_TYP",
                PlaceholderText = "Type a common type or name", Required = true
            };
            this.btnRefreshTypes = new ModernButton
            {
                Name = "btnRefreshTypes", Text = "Refresh Types", Kind = ButtonKind.Secondary,
                Location = new Point(590, 12), Size = new Size(136, 32)
            };
            typeBar.Controls.AddRange(new Control[] { typeLabel, this.cmbCommonType, this.btnRefreshTypes });
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1400, 800);
            this.MinimumSize = new Size(1000, 650);
            this.Padding = new Padding(12);
            this.Text = title;
            this.Name = "CommonCodeForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Controls.Add(this.editorPane.Root);
            this.Controls.Add(typeBar);
            this.Controls.Add(this.lblTitle);
            this.ResumeLayout(false);
        }

        private static CodePane CreatePane(string title)
        {
            CodePane pane = new CodePane();
            pane.Root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            ModernCardPanel search = new ModernCardPanel { Dock = DockStyle.Top, Height = 56 };
            ModernLabel keywordLabel = new ModernLabel
            {
                Text = "Keyword", Location = new Point(12, 12), Size = new Size(72, 32), Kind = LabelKind.Label
            };
            pane.Keyword = new ModernTextBox
            {
                Name = "txtKeyword", Location = new Point(88, 12), Size = new Size(280, 32),
                PlaceholderText = "Identifier or keyword", ShowClearButton = true
            };
            pane.Search = CreateButton("btnSearch", "Search", ButtonKind.Primary);
            pane.Search.Location = new Point(376, 12);
            pane.Search.Size = new Size(104, 32);
            search.Controls.AddRange(new Control[] { keywordLabel, pane.Keyword, pane.Search });
            ModernSplitContainer split = new ModernSplitContainer
            {
                Dock = DockStyle.Fill, Size = new Size(1376, 620), KeepRatio = true,
                SplitterWidth = 8, SplitterDistance = 820, Panel2MinSize = 470
            };
            ModernGroupBox listCard = new ModernGroupBox
            {
                Text = title + " List", Dock = DockStyle.Fill, Padding = new Padding(6, 40, 6, 6), TitleBar = true
            };
            pane.Grid = new ModernDataGrid
            {
                Name = "gridItems", Dock = DockStyle.Fill, ReadOnly = true, AutoFitColumns = true,
                ShowStatusBar = true, StatusCountFormat = "{0:N0} items"
            };
            listCard.Controls.Add(pane.Grid);
            pane.Card = new ModernGroupBox
            {
                Name = "editorCard", Text = title, Dock = DockStyle.Fill, Padding = new Padding(12, 44, 12, 8), TitleBar = true
            };
            pane.Editor = new ModernPropertyGrid
            {
                Name = "propertyGrid", Dock = DockStyle.Fill, LabelWidth = 150, TabIndex = 0
            };
            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
                Padding = new Padding(0, 8, 0, 0), Margin = new Padding(0), TabIndex = 1
            };
            pane.New = CreateButton("btnNew", "New", ButtonKind.Secondary);
            pane.Cancel = CreateButton("btnCancel", "Cancel", ButtonKind.Secondary);
            pane.Save = CreateButton("btnSave", "Save", ButtonKind.Execute);
            pane.Delete = CreateButton("btnDelete", "Delete", ButtonKind.Danger);
            pane.New.TabIndex = 0;
            pane.Cancel.TabIndex = 1;
            pane.Save.TabIndex = 2;
            pane.Delete.TabIndex = 3;
            actions.Controls.AddRange(new Control[] { pane.Delete, pane.Save, pane.Cancel, pane.New });
            pane.Card.Controls.Add(pane.Editor);
            pane.Card.Controls.Add(actions);
            split.Panel1.Controls.Add(listCard);
            split.Panel2.Controls.Add(pane.Card);
            Panel gap = new Panel { Dock = DockStyle.Top, Height = 8 };
            pane.Root.Controls.Add(split);
            pane.Root.Controls.Add(gap);
            pane.Root.Controls.Add(search);
            return pane;
        }

        private static ModernButton CreateButton(string name, string text, ButtonKind kind)
        {
            return new ModernButton { Name = name, Text = text, Kind = kind, Size = new Size(100, 34), Margin = new Padding(8, 0, 0, 0) };
        }

        private sealed class CodePane
        {
            internal Panel Root;
            internal ModernGroupBox Card;
            internal ModernDataGrid Grid;
            internal ModernPropertyGrid Editor;
            internal ModernTextBox Keyword;
            internal ModernButton Search;
            internal ModernButton New;
            internal ModernButton Cancel;
            internal ModernButton Save;
            internal ModernButton Delete;
        }
    }
}
