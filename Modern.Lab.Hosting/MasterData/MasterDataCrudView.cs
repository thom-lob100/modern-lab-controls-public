using System.Windows.Forms;

using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Layout;

namespace Modern.Lab.Hosting.MasterData
{
    public sealed class MasterDataCrudView
    {
        public ModernTextBox Keyword { get; set; }

        /// <summary>조회 버튼(선택). 있으면 쓰기 중에 잠긴다.</summary>
        public ModernButton SearchButton { get; set; }

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
