using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Modern.Lab.Controls.Wpf.Display
{
    /// <summary>
    /// 메시지 다이얼로그의 기술 상세 박스 — 본문 박스와 같은 생김새로 상세 원문을 보여 주고
    /// 박스 안 복사 아이콘을 둔다. 창·복사 내용·피드백 시간은 호스트 폼
    /// (<c>Modern.Lab.WinForms.Controls.Dialogs.ModernMessageDialog</c>)이 정한다.
    /// </summary>
    internal partial class ModernMessageDetailsControl : UserControl
    {
        private const string CopyGlyphText = "";
        private const string CopiedGlyphText = "";

        internal event EventHandler CopyRequested;

        internal ModernMessageDetailsControl()
        {
            this.InitializeComponent();
        }

        /// <summary>상세 원문.</summary>
        internal string Details
        {
            get { return this.DetailsText.Text; }
            set { this.DetailsText.Text = value ?? string.Empty; }
        }

        internal void ShowCopyConfirmation()
        {
            this.CopyGlyph.Text = CopiedGlyphText;
            Brush accent = this.TryFindResource("Brush.Accent") as Brush;

            if (accent != null)
            {
                this.CopyGlyph.Foreground = accent;
            }
        }

        internal void ResetCopyConfirmation()
        {
            this.CopyGlyph.Text = CopyGlyphText;
            Brush foreground = this.TryFindResource("Brush.TextSecondary") as Brush;

            if (foreground != null)
            {
                this.CopyGlyph.Foreground = foreground;
            }
        }

        private void OnCopyButtonClick(object sender, RoutedEventArgs e)
        {
            EventHandler handler = this.CopyRequested;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
