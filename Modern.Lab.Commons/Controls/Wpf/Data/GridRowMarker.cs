using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Modern.Lab.Controls.Wpf.Data
{
    /// <summary>
    /// 셀의 측정 크기에 참여하지 않는 모서리 표식. 입력은 원래 셀로 통과시킨다.
    /// 글자색은 주황 계열(Brush.WarningText)이다 — 선택 행의 파란 배경·글자와 색상이 달라야 선택된 행 위에서도 구분된다.
    /// </summary>
    internal sealed class GridRowMarker : FrameworkElement
    {
        private ModernDataGridControl owner;
        private DataGridCell cell;
        private string text = string.Empty;

        public GridRowMarker()
        {
            this.IsHitTestVisible = false;
            this.ClipToBounds = true;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
            this.DataContextChanged += this.OnDataContextChanged;
        }

        internal string Text { get { return this.text; } }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (this.owner == null || this.text.Length == 0) { return; }
            Typeface typeface = new Typeface((FontFamily)this.FindResource("Font.Family"), FontStyles.Normal,
                    (FontWeight)this.FindResource("Font.Weight.GridMarker"), FontStretches.Normal);
            FormattedText label = new FormattedText(this.text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    typeface, (double)this.FindResource("Font.Size.GridMarker"),
                    (Brush)this.FindResource("Brush.WarningText"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            Thickness inset = (Thickness)this.FindResource("Pad.GridMarker");
            drawingContext.PushClip(new RectangleGeometry(new Rect(this.RenderSize)));
            drawingContext.DrawText(label, new Point(inset.Left, inset.Top));
            drawingContext.Pop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this;
            while (parent != null)
            {
                if (this.cell == null) { this.cell = parent as DataGridCell; }
                this.owner = parent as ModernDataGridControl;
                if (this.owner != null) { break; }
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (this.owner != null) { this.owner.RowMarkersChanged += this.OnMarkersChanged; }
            this.Refresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.owner != null) { this.owner.RowMarkersChanged -= this.OnMarkersChanged; }
            this.owner = null;
            this.cell = null;
            this.text = string.Empty;
            this.BeginAnimation(OpacityProperty, null);
            this.InvalidateVisual();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.Refresh();
        }

        private void OnMarkersChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void Refresh()
        {
            this.BeginAnimation(OpacityProperty, null);
            this.text = this.owner == null || this.cell == null ? string.Empty
                    : this.owner.MarkerText(this.DataContext, this.cell.Column);
            this.InvalidateVisual();
            if (this.text.Length == 0 || !this.owner.BeginMarkerPulse(this.DataContext)
                    || !SystemParameters.ClientAreaAnimation) { return; }
            DoubleAnimation pulse = new DoubleAnimation
            {
                From = (double)this.FindResource("Opacity.GridMarkerPulse"),
                To = 1,
                Duration = TimeSpan.FromMilliseconds((double)this.FindResource("Duration.GridMarkerPulse")),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                FillBehavior = FillBehavior.Stop
            };
            this.BeginAnimation(OpacityProperty, pulse);
        }
    }
}
