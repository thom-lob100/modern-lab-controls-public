using System.Windows.Forms;

using Modern.Lab.MasterData.Controls;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Layout;

namespace Modern.Lab.MasterData
{
    public sealed class MasterDataCrudView
    {
        public ModernTextBox Keyword { get; set; }

        public ModernDataGrid Grid { get; set; }

        public ModernGroupBox EditorCard { get; set; }

        public ModernPropertyGrid Editor { get; set; }

        public ModernButton NewButton { get; set; }

        public ModernButton CancelButton { get; set; }

        public ModernButton SaveButton { get; set; }

        public ModernButton DeleteButton { get; set; }

        public ContextMenuStrip ActionMenu { get; set; }

        public ToolStripMenuItem NewMenuItem { get; set; }

        public ToolStripMenuItem CancelMenuItem { get; set; }

        public ToolStripMenuItem SaveMenuItem { get; set; }

        public ToolStripMenuItem DeleteMenuItem { get; set; }
    }
}
