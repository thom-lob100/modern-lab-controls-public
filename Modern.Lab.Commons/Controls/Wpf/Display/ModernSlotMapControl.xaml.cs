using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Modern.Lab.Controls.Wpf.Common;

namespace Modern.Lab.Controls.Wpf.Display
{
    /// <summary>
    /// 슬롯 맵 — 캐리어(운반체)의 수납 구조를 실물 단면처럼 그리는 표시/선택
    /// 컨트롤. 구획(SlotMapSection) 단위로 셀 격자를 그린다.
    ///
    /// 셀 표현 — **자리(번호)는 고정, 웨이퍼(ID)은 움직인다**를 시각으로 나눈다.
    /// 구획의 Kind(SlotMapSectionKind)가 실물 표현을 정한다:
    /// - WaferEdge: 카세트 단면 — 레일 사이 얇은 웨이퍼 바(채움), 빈 슬롯은
    ///   레일 홈, 미리보기는 점선 바 (FOUP).
    /// - PinStub: 핀 스텁 탑 뷰 — 원판 위 사각 칩, 빈 스텁은 핀 자국만 (STUB).
    /// - LamellaPost: 포스트+라멜라 — 삽입 위치(Top/Left/Right)가 라멜라가
    ///   붙는 위치 자체로 표현된다 (LCC).
    /// - Generic(기본): 번호 칩 + 웨이퍼 토큰(단일) / 핑거 도트 미니 행(복합) —
    ///   기존 화면 호환용.
    /// 상태(채움/빈/미리보기/선택)는 전부 형태 차(도형 유무·점선·링)로
    /// 인코딩해 테마와 무관하게 읽힌다.
    ///
    /// 상호작용:
    /// - 채워진 셀 또는 미리보기 셀 클릭 = CellClicked로 키를 알린다. 화면은
    ///   SetClickKeys로 클릭 선택을, SetSelectedKeys로 스테이징 상태를 각각 표시한다.
    /// - EnableDragOut = 채워진 셀을 끌 수 있다 (선택된 셀을 끌면 선택 전체가
    ///   함께 간다). AcceptDrops = 드롭을 받아 WafersDropped(키들 + 앵커 셀
    ///   키)를 발생시킨다 — 앵커는 "이 자리부터 채워 달라"는 의도이며 검증과
    ///   실제 이동은 화면(서버 호출)이 한다.
    /// - SetPreview(구획별 웨이퍼 수) = "들어갈 빈 자리" 하이라이트. 부족하면
    ///   구획 집계가 빨간 "need n more"가 된다.
    ///
    /// 데이터는 SetSections로 통째로 준다(재조회 = 재구성) — 셀 시각 트리는
    /// 코드가 만들고 색은 전부 토큰에서 읽는다.
    /// </summary>
    public partial class ModernSlotMapControl : UserControl
    {
        // 드래그 페이로드의 DataObject 형식 이름.
        private const string dragDataFormat = "ModernLab.SlotMapKeys";

        /// <summary>셀 클릭 선택 허용 여부 — 대상(읽기 전용) 맵은 끈다.</summary>
        public static readonly DependencyProperty AllowSelectionProperty =
            DependencyProperty.Register(
                "AllowSelection", typeof(bool), typeof(ModernSlotMapControl),
                new PropertyMetadata(true));

        /// <summary>채워진 셀 드래그 시작 허용 여부 (원본 맵용, 기본 false).</summary>
        public static readonly DependencyProperty EnableDragOutProperty =
            DependencyProperty.Register(
                "EnableDragOut", typeof(bool), typeof(ModernSlotMapControl),
                new PropertyMetadata(false));

        /// <summary>드롭 수용 여부 (대상 맵용, 기본 false) — 켜면 AllowDrop이
        /// 함께 켜지고 WafersDropped가 발생한다.</summary>
        public static readonly DependencyProperty AcceptDropsProperty =
            DependencyProperty.Register(
                "AcceptDrops", typeof(bool), typeof(ModernSlotMapControl),
                new PropertyMetadata(false, OnAcceptDropsChanged));

        /// <summary>선택이 바뀔 때 발생한다 (재구성/프로그램 선택 해제 등).</summary>
        public event EventHandler SelectionChanged;

        /// <summary>채움 셀을 클릭할 때 발생한다 — 선택 상태는 바꾸지 않고
        /// 클릭된 키만 알린다. 선택 표시는 화면이 SetClickKeys로 관리한다.</summary>
        public event EventHandler<SlotMapCellEventArgs> CellClicked;

        /// <summary>드롭을 받을 때 발생한다 — 끌려온 셀 키들과 앵커 셀 키.</summary>
        public event EventHandler<SlotMapDropEventArgs> WafersDropped;

        /// <summary>맵에서 오른쪽 클릭할 때 발생한다 — 커서 아래 채움 셀의 키
        /// (셀 밖이면 빈 문자열). 화면이 이동 컨텍스트 메뉴를 띄우는 데 쓴다.</summary>
        public event EventHandler<SlotMapCellEventArgs> CellRightClick;

        // 셀 시각 요소 묶음 — 선택/미리보기 상태 변경 시 다시 칠할 대상.
        // Kind(구획의 실물 표현)에 따라 쓰는 필드가 다르다:
        // - Generic     : Token/LabelChip/LabelText/WaferText (+복합이면 Dot*/MarkerEdges)
        // - WaferEdge   : LabelText(눈금 번호)/Ring/Bar/WaferText
        // - PinStub     : Ring(원)/Disc/PinDot/Chip/WaferText(캡션)
        // - LamellaPost : Token(프레임)/LabelChip/LabelText/Posts/Lamellas/
        //                 LetterTexts/SubWaferTexts(ID 행, ON 모드)
        private sealed class CellVisual
        {
            internal SlotMapCell Cell;
            internal SlotMapSectionKind Kind;
            internal Border Outer;
            internal Border Token;
            internal Border LabelChip;
            internal TextBlock LabelText;
            internal TextBlock WaferText;
            internal List<Border> DotBorders;
            internal List<TextBlock> DotTexts;
            internal List<TextBlock> SubWaferTexts;
            internal List<Border> MarkerEdges;

            // WaferEdge/PinStub — 선택 링(도형 바깥 액센트 윤곽).
            internal System.Windows.Shapes.Shape Ring;

            // WaferEdge — 웨이퍼 바(채움/홈/미리보기를 한 도형이 겸한다).
            internal System.Windows.Shapes.Rectangle Bar;

            // PinStub — 스텁 원판 / 핀 자국 / 칩 / 칩 안 ID 축약 글자.
            internal System.Windows.Shapes.Ellipse Disc;
            internal System.Windows.Shapes.Ellipse PinDot;
            internal System.Windows.Shapes.Rectangle Chip;
            internal TextBlock ChipText;

            // LamellaPost — 핑거당 포스트/라멜라/눈금 글자.
            internal List<System.Windows.Shapes.Rectangle> Posts;
            internal List<System.Windows.Shapes.Rectangle> Lamellas;
            internal List<TextBlock> LetterTexts;
        }

        private SlotMapSection[] sections;
        private readonly List<List<CellVisual>> sectionVisuals = new List<List<CellVisual>>();
        private readonly List<TextBlock> sectionCountTexts = new List<TextBlock>();
        private readonly HashSet<string> selectedKeys = new HashSet<string>(StringComparer.Ordinal);

        // 클릭으로 강조한 셀들 — 스테이징(selectedKeys)과 별개로 경고색
        // 외곽선 + 같은 색 번호 배지로 표시한다. SetClickKeys로 다중 선택을 전달한다.
        private readonly HashSet<string> clickKeys = new HashSet<string>(StringComparer.Ordinal);

        // 미리보기 맵 — 자리 키("SLOT|7" / "STUB|3" / "LCC|3|A") → 들어올 웨이퍼
        // ID. 해당 자리가 비어 있으면 "→ ID"로 표기한다. 화면(폼)이 서버 배치
        // 계획을 그대로 받아 주므로 미리보기와 실제 이동 결과가 일치한다.
        private System.Collections.Generic.Dictionary<string, string> previewMap;

        // 미리보기 LCC 핑거의 삽입 위치 — 자리 키 → Top/Left/Right. 웨이퍼 ID
        // 맵과 분리해 기존 SetPreview 호출과 호환하면서 방향을 함께 표시한다.
        private System.Collections.Generic.Dictionary<string, string> previewMarkerMap;

        // 복합 셀(LCC 등)의 하위 웨이퍼 ID 표시 방식 — 켬: 핑거당 웨이퍼 ID 행,
        // 끔: 포스트/도트만(ID는 호버 툴팁으로). SubCells가 있는 구획 헤더의
        // "Lamella ID" 스위치로 토글한다(단일 셀 FOUP 슬롯/STUB은 무관).
        private bool showSubCellWaferIds = true;

        // 드래그 시작 판정 — 마우스 다운 셀과 좌표를 기억해 두고, 이동 거리가
        // 시스템 임계값을 넘으면 드래그로, 그대로 떼면 클릭(선택 토글)으로 본다.
        private CellVisual pressCandidate;
        private Point pressPoint;

        public ModernSlotMapControl()
        {
            this.InitializeComponent();
            this.PreviewMouseRightButtonUp += this.OnMapRightButtonUp;
        }

        // 맵 오른쪽 클릭 — 커서 아래 채움 셀 키를 실어 CellRightClick를 발생시킨다
        // (셀 밖이면 빈 문자열). 화면이 이동 컨텍스트 메뉴를 띄운다.
        private void OnMapRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            EventHandler<SlotMapCellEventArgs> handler = this.CellRightClick;

            if (handler == null)
            {
                return;
            }

            CellVisual visual = FindCellVisual(e.OriginalSource as DependencyObject);
            string key = visual != null && visual.Cell.Filled ? visual.Cell.Key : string.Empty;
            handler(this, new SlotMapCellEventArgs(key));
            e.Handled = true;
        }

        /// <summary>셀 클릭 선택 허용 여부 (기본 true).</summary>
        public bool AllowSelection
        {
            get { return (bool)this.GetValue(AllowSelectionProperty); }
            set { this.SetValue(AllowSelectionProperty, value); }
        }

        /// <summary>채워진 셀 드래그 시작 허용 여부 (기본 false).</summary>
        public bool EnableDragOut
        {
            get { return (bool)this.GetValue(EnableDragOutProperty); }
            set { this.SetValue(EnableDragOutProperty, value); }
        }

        /// <summary>드롭 수용 여부 (기본 false).</summary>
        public bool AcceptDrops
        {
            get { return (bool)this.GetValue(AcceptDropsProperty); }
            set { this.SetValue(AcceptDropsProperty, value); }
        }

        private static void OnAcceptDropsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernSlotMapControl control = (ModernSlotMapControl)d;
            control.AllowDrop = (bool)e.NewValue;

            if ((bool)e.NewValue)
            {
                control.DragOver -= control.OnMapDragOver;
                control.Drop -= control.OnMapDrop;
                control.DragOver += control.OnMapDragOver;
                control.Drop += control.OnMapDrop;
            }
        }

        /// <summary>현재 선택된 셀 키 목록 (없으면 빈 배열).</summary>
        public string[] SelectedKeys
        {
            get
            {
                string[] keys = new string[this.selectedKeys.Count];
                this.selectedKeys.CopyTo(keys);
                return keys;
            }
        }

        /// <summary>구획들을 통째로 다시 그린다 — 재조회 반영 경로. 기존
        /// 선택과 미리보기는 초기화된다.</summary>
        public void SetSections(SlotMapSection[] newSections)
        {
            bool hadSelection = this.selectedKeys.Count > 0;

            this.sections = newSections;
            this.selectedKeys.Clear();
            this.clickKeys.Clear();
            this.previewMap = null;
            this.previewMarkerMap = null;
            this.RebuildVisualTree();

            if (hadSelection)
            {
                this.RaiseSelectionChanged();
            }
        }

        /// <summary>지정한 키들만 선택 상태로 만든다 — 이벤트를 발생시키지
        /// 않는다(이동 후 도착지에서 "방금 옮긴 웨이퍼"을 강조하는 용도).</summary>
        public void SetSelectedKeys(string[] keys)
        {
            this.selectedKeys.Clear();

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        this.selectedKeys.Add(key);
                    }
                }
            }

            this.RefreshAllVisuals();
        }

        /// <summary>클릭 강조 셀 하나를 지정한다 (스테이징과 다른 색). null이면 없음.
        /// 기존 단일 선택 호출부를 위한 호환 진입점이며 이벤트를 발생시키지 않는다.</summary>
        public void SetClickKey(string key)
        {
            this.clickKeys.Clear();

            if (!string.IsNullOrEmpty(key))
            {
                this.clickKeys.Add(key);
            }

            this.RefreshAllVisuals();
        }

        /// <summary>클릭 강조 셀들을 지정한다. 채움 색을 바꾸지 않고 고대비
        /// 경고색 외곽선과 **같은 색 슬롯 번호 배지**를 표시하며 이벤트를
        /// 발생시키지 않는다.</summary>
        public void SetClickKeys(string[] keys)
        {
            this.clickKeys.Clear();

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        this.clickKeys.Add(key);
                    }
                }
            }

            this.RefreshAllVisuals();
        }

        /// <summary>선택을 모두 해제한다.</summary>
        public void ClearSelection()
        {
            if (this.selectedKeys.Count == 0)
            {
                return;
            }

            this.selectedKeys.Clear();
            this.RefreshAllVisuals();
            this.RaiseSelectionChanged();
        }

        /// <summary>"들어갈 자리" 미리보기 — 자리 키("SLOT|7"/"STUB|3"/
        /// "LCC|3|A") → 들어올 웨이퍼 ID 맵을 준다 (null = 해제). 그 자리가 비어
        /// 있으면 "→ ID"로 표기·하이라이트하고, 계획된 자리가 부족하면 집계에
        /// 빨간 부족분(need n more)을 표기한다.</summary>
        public void SetPreview(System.Collections.Generic.Dictionary<string, string> map)
        {
            this.previewMap = map;
            this.previewMarkerMap = null;
            this.RefreshAllVisuals();
        }

        /// <summary>미리보기 LCC 핑거의 삽입 위치 맵(자리 키 → Top/Left/Right).
        /// null이면 미리보기 방향 표기를 해제한다.</summary>
        public void SetPreviewMarkers(System.Collections.Generic.Dictionary<string, string> markers)
        {
            this.previewMarkerMap = markers;
            this.RefreshAllVisuals();
        }

        // ===== 시각 트리 구성 =====

        private void RebuildVisualTree()
        {
            this.SectionHost.Children.Clear();
            this.sectionVisuals.Clear();
            this.sectionCountTexts.Clear();
            this.pressCandidate = null;

            if (this.sections == null)
            {
                return;
            }

            for (int index = 0; index < this.sections.Length; index++)
            {
                this.SectionHost.Children.Add(this.BuildSection(this.sections[index]));
            }

            this.RefreshAllVisuals();
        }

        private UIElement BuildSection(SlotMapSection section)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(0d, 0d, 0d, 8d);

            // 구획 머리 — 제목(좌) + 채움 집계(우, 미리보기/부족분 포함).
            Grid header = new Grid();
            header.Margin = new Thickness(2d, 0d, 2d, 4d);
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock title = new TextBlock();
            title.Text = section.Title;
            title.FontSize = (double)this.FindResource("Font.Size.Label");
            title.FontWeight = FontWeights.SemiBold;
            title.Foreground = (Brush)this.FindResource("Brush.TextSecondary");
            title.VerticalAlignment = VerticalAlignment.Center;

            // SubCells가 있는 구획(LCC 등)은 제목 옆에 "칩 ID" 스위치를 둔다 —
            // 끄면 하위 웨이퍼 ID 글씨 없이(도트/삽입위치/색만) 보여 준다.
            if (SectionHasSubCells(section))
            {
                StackPanel titleRow = new StackPanel();
                titleRow.Orientation = Orientation.Horizontal;
                titleRow.Children.Add(title);

                Modern.Lab.Controls.Wpf.Input.ModernToggleSwitchControl idSwitch =
                        new Modern.Lab.Controls.Wpf.Input.ModernToggleSwitchControl();
                idSwitch.Text = "Lamella Id";
                idSwitch.IsChecked = this.showSubCellWaferIds;
                idSwitch.VerticalAlignment = VerticalAlignment.Center;
                idSwitch.Margin = new Thickness(10d, 0d, 0d, 0d);
                idSwitch.CheckedChanged += this.OnSubCellIdSwitchChanged;
                titleRow.Children.Add(idSwitch);

                header.Children.Add(titleRow);
            }
            else
            {
                header.Children.Add(title);
            }

            TextBlock count = new TextBlock();
            count.FontSize = (double)this.FindResource("Font.Size.Label");
            count.Foreground = (Brush)this.FindResource("Brush.TextSecondary");
            Grid.SetColumn(count, 1);
            header.Children.Add(count);
            this.sectionCountTexts.Add(count);

            panel.Children.Add(header);

            // 셀 격자 — Columns 열의 UniformGrid (1 = 세로 사다리).
            UniformGrid grid = new UniformGrid();
            grid.Columns = Math.Max(1, section.Columns);

            List<CellVisual> visuals = new List<CellVisual>();

            foreach (SlotMapCell cell in section.Cells)
            {
                CellVisual visual = this.BuildCell(cell, section);
                visuals.Add(visual);
                grid.Children.Add(visual.Outer);
            }

            this.sectionVisuals.Add(visuals);
            panel.Children.Add(grid);
            return panel;
        }

        // 구획에 복합(하위 자리) 셀이 하나라도 있는가 — LCC 구획 판정.
        private static bool SectionHasSubCells(SlotMapSection section)
        {
            if (section.Cells == null)
            {
                return false;
            }

            foreach (SlotMapCell cell in section.Cells)
            {
                if (cell.SubCells != null)
                {
                    return true;
                }
            }

            return false;
        }

        // "Lamella ID" 스위치 토글 — 하위 웨이퍼 ID 표시 방식을 바꾼다. 켜면
        // 핑거당 미니 행(도트 + 웨이퍼 ID), 끄면 A~E 배지를 크게 한 줄로만 보여
        // 준다. 레이아웃이 다르므로 시각 트리를 다시 구성한다(상태는 유지).
        private void OnSubCellIdSwitchChanged(object sender, EventArgs e)
        {
            Modern.Lab.Controls.Wpf.Input.ModernToggleSwitchControl idSwitch =
                    sender as Modern.Lab.Controls.Wpf.Input.ModernToggleSwitchControl;

            if (idSwitch == null)
            {
                return;
            }

            this.showSubCellWaferIds = idSwitch.IsChecked;
            this.RebuildVisualTree();
        }

        // 셀 빌더 디스패처 — 구획의 실물 표현(Kind)에 맞는 전용 빌더로 보낸다.
        // 셀 모양이 표현과 안 맞으면(예: WaferEdge에 복합 셀) 기본 상자 표현으로
        // 안전하게 내려간다.
        private CellVisual BuildCell(SlotMapCell cell, SlotMapSection section)
        {
            if (section.Kind == SlotMapSectionKind.WaferEdge && cell.SubCells == null)
            {
                return this.BuildWaferCell(cell);
            }

            if (section.Kind == SlotMapSectionKind.PinStub && cell.SubCells == null)
            {
                return this.BuildStubCell(cell, section.CellFontSize);
            }

            if (section.Kind == SlotMapSectionKind.LamellaPost && cell.SubCells != null)
            {
                return this.BuildLamellaCell(cell, section.CellFontSize);
            }

            return this.BuildGenericCell(cell, section.CellFontSize);
        }

        // 공용 바깥 틀 — 마우스(클릭/드래그/우클릭) 히트 테스트의 단위. Tag에
        // CellVisual을 실어 FindCellVisual이 어느 표현에서든 셀을 찾게 한다.
        private Border BuildOuter(CellVisual visual)
        {
            Border outer = new Border();
            outer.Background = Brushes.Transparent;
            outer.CornerRadius = new CornerRadius(6d);
            outer.Margin = new Thickness(1d);
            outer.Tag = visual;
            outer.MouseLeftButtonDown += this.OnCellMouseDown;
            outer.MouseLeftButtonUp += this.OnCellMouseUp;
            outer.MouseMove += this.OnCellMouseMove;
            visual.Outer = outer;
            return outer;
        }

        // ===== WaferEdge — 카세트 단면(웨이퍼 에지 뷰) 셀 =====

        // 한 행 = [슬롯 번호 눈금 | 레일 | 웨이퍼 바 | 레일]. 셀 세로 여백을
        // 없애 레일 조각이 행마다 이어져 하나의 레일로 보인다. 바 도형 하나가
        // 채움(Lot색 바 + ID)/빈 홈(가는 줄)/미리보기(점선 바)를 상태에
        // 따라 겸한다 — 명도 차가 아닌 형태 차라 어느 테마에서든 읽힌다.
        private CellVisual BuildWaferCell(SlotMapCell cell)
        {
            CellVisual visual = new CellVisual();
            visual.Cell = cell;
            visual.Kind = SlotMapSectionKind.WaferEdge;

            Border outer = this.BuildOuter(visual);
            outer.Margin = new Thickness(1d, 0d, 1d, 0d);
            outer.CornerRadius = new CornerRadius(0d);

            Grid row = new Grid();
            row.Height = 20d;
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24d) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7d) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7d) });

            // 슬롯 번호 눈금 — 평소에는 칩이 투명해 숫자만 보이고, 선택되면
            // 번호 자체가 배지로 채워진다. 칩 폭은 번호 칸(24) 안에 들어가게
            // 줄여 레일 위치가 밀리지 않는다.
            TextBlock number;
            Border numberChip = this.BuildNumberChip(cell.Label, 9d, out number);
            numberChip.MinWidth = 18d;
            numberChip.Padding = new Thickness(2d, 0d, 2d, 0d);
            numberChip.HorizontalAlignment = HorizontalAlignment.Right;
            numberChip.Margin = new Thickness(0d, 0d, 4d, 0d);
            row.Children.Add(numberChip);
            visual.LabelChip = numberChip;
            visual.LabelText = number;

            // 좌우 레일 조각 — 행 전체 높이를 채워 위아래 행과 이어진다.
            Brush railBrush = (Brush)this.FindResource("Brush.Border");
            System.Windows.Shapes.Rectangle railLeft = BuildRailSegment(railBrush);
            Grid.SetColumn(railLeft, 1);
            row.Children.Add(railLeft);

            System.Windows.Shapes.Rectangle railRight = BuildRailSegment(railBrush);
            Grid.SetColumn(railRight, 3);
            row.Children.Add(railRight);

            // 바 구역 — 선택 링(바깥) + 웨이퍼 바 + ID 글자.
            Grid barZone = new Grid();
            Grid.SetColumn(barZone, 2);
            row.Children.Add(barZone);

            // 선택 링은 **웨이퍼 바를 밀착해 감싼다.** 예전에는 barZone 전체를
            // 감쌌는데(Margin 1 + 세로 Stretch) 바는 좌우 4 DIU 안쪽이라,
            // 링과 바 사이 좌우 끝에 카드 배경이 1~2px 드러났다 — 카드가 흰
            // 배경(Brush.Surface)이면 선택할 때만 흰 틈이 보여 결함처럼 읽혔다.
            // 링 사각형을 바 경계선 위로 올려(좌우 3 = 바 4 - 1, 높이 16 = 바 14 + 2)
            // 2px 스트로크가 바 테두리를 덮게 하면 틈이 생기지 않는다.
            System.Windows.Shapes.Rectangle ring = new System.Windows.Shapes.Rectangle();
            ring.RadiusX = 8d;
            ring.RadiusY = 8d;
            ring.StrokeThickness = 2d;
            ring.Margin = new Thickness(3d, 0d, 3d, 0d);
            ring.Height = 16d;
            ring.VerticalAlignment = VerticalAlignment.Center;
            ring.IsHitTestVisible = false;
            ring.Visibility = Visibility.Collapsed;
            barZone.Children.Add(ring);
            visual.Ring = ring;

            System.Windows.Shapes.Rectangle bar = new System.Windows.Shapes.Rectangle();
            bar.RadiusX = 7d;
            bar.RadiusY = 7d;
            bar.Height = 14d;
            bar.Margin = new Thickness(4d, 0d, 4d, 0d);
            bar.VerticalAlignment = VerticalAlignment.Center;
            barZone.Children.Add(bar);
            visual.Bar = bar;

            TextBlock wafer = new TextBlock();
            wafer.FontSize = 9.5d;
            wafer.FontWeight = FontWeights.SemiBold;
            wafer.TextTrimming = TextTrimming.CharacterEllipsis;
            wafer.HorizontalAlignment = HorizontalAlignment.Center;
            wafer.VerticalAlignment = VerticalAlignment.Center;
            wafer.IsHitTestVisible = false;
            barZone.Children.Add(wafer);
            visual.WaferText = wafer;

            outer.Child = row;
            outer.ToolTip = !string.IsNullOrEmpty(cell.ToolTip)
                    ? cell.ToolTip
                    : (string.IsNullOrEmpty(cell.WaferId) ? null : (object)cell.WaferId);
            return visual;
        }

        // 레일 조각 하나 (5px 폭, 행 높이 전체).
        private static System.Windows.Shapes.Rectangle BuildRailSegment(Brush fill)
        {
            System.Windows.Shapes.Rectangle rail = new System.Windows.Shapes.Rectangle();
            rail.Width = 5d;
            rail.Fill = fill;
            rail.HorizontalAlignment = HorizontalAlignment.Center;
            rail.IsHitTestVisible = false;
            return rail;
        }

        // ===== PinStub — 핀 스텁 탑 뷰 셀 =====

        // 원형 금속 스텁 위에 사각 칩을 올린 모습 — 칩의 유무가 곧 채움/빈
        // 구분이다(색이 아닌 형태 차). 캡션(번호 · 웨이퍼 ID)은 원판 아래.
        private CellVisual BuildStubCell(SlotMapCell cell, double cellFontSize)
        {
            CellVisual visual = new CellVisual();
            visual.Cell = cell;
            visual.Kind = SlotMapSectionKind.PinStub;

            Border outer = this.BuildOuter(visual);

            // 내용은 셀 폭을 다 쓴다 — 캡션의 웨이퍼 ID가 쓸 폭이 원판(62)에
            // 갇히면 안 된다. 원판만 가운데로 고정한다.
            StackPanel content = new StackPanel();
            content.Margin = new Thickness(0d, 4d, 0d, 4d);
            content.HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid discZone = new Grid();
            discZone.Width = 62d;
            discZone.Height = 62d;
            discZone.HorizontalAlignment = HorizontalAlignment.Center;

            System.Windows.Shapes.Ellipse ring = new System.Windows.Shapes.Ellipse();
            ring.StrokeThickness = 2d;
            ring.IsHitTestVisible = false;
            ring.Visibility = Visibility.Collapsed;
            discZone.Children.Add(ring);
            visual.Ring = ring;

            System.Windows.Shapes.Ellipse disc = new System.Windows.Shapes.Ellipse();
            disc.Width = 54d;
            disc.Height = 54d;
            disc.StrokeThickness = 1.5d;
            discZone.Children.Add(disc);
            visual.Disc = disc;

            System.Windows.Shapes.Ellipse pin = new System.Windows.Shapes.Ellipse();
            pin.Width = 5d;
            pin.Height = 5d;
            discZone.Children.Add(pin);
            visual.PinDot = pin;

            System.Windows.Shapes.Rectangle chip = new System.Windows.Shapes.Rectangle();
            chip.Width = 30d;
            chip.Height = 22d;
            chip.RadiusX = 3d;
            chip.RadiusY = 3d;
            chip.StrokeThickness = 1.5d;
            chip.Visibility = Visibility.Collapsed;
            discZone.Children.Add(chip);
            visual.Chip = chip;

            // 칩 안 ID 축약 — 웨이퍼 ID의 끝 4자리만 새긴다 (전체 ID는 캡션/툴팁).
            TextBlock chipText = new TextBlock();
            chipText.FontSize = 8.5d;
            chipText.FontWeight = FontWeights.SemiBold;
            chipText.HorizontalAlignment = HorizontalAlignment.Center;
            chipText.VerticalAlignment = VerticalAlignment.Center;
            chipText.IsHitTestVisible = false;
            discZone.Children.Add(chipText);
            visual.ChipText = chipText;

            content.Children.Add(discZone);

            // 캡션 줄 = [번호 배지 | 웨이퍼 ID]. 번호를 캡션 글자에서 떼어내
            // 칩으로 세운 이유는 선택 표시를 걸 자리가 필요해서다. 줄은 셀 폭을
            // 그대로 받고(UniformGrid가 셀 폭을 정한다) 남는 폭을 ID가 가져간다 —
            // 원판 위 칩은 끝 4자리만 새기므로 전체 ID를 보여 주는 것은 이 캡션이다.
            double captionSize = cellFontSize > 0d ? cellFontSize - 1d : 10d;

            Grid captionRow = new Grid();
            captionRow.HorizontalAlignment = HorizontalAlignment.Stretch;
            captionRow.Margin = new Thickness(2d, 3d, 2d, 0d);
            captionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            captionRow.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock number;
            Border numberChip = this.BuildNumberChip(cell.Label, captionSize, out number);
            numberChip.MinWidth = 20d;
            numberChip.Padding = new Thickness(2d, 0d, 2d, 0d);
            captionRow.Children.Add(numberChip);
            visual.LabelChip = numberChip;
            visual.LabelText = number;

            TextBlock caption = new TextBlock();
            caption.FontSize = captionSize;
            caption.TextAlignment = TextAlignment.Left;
            caption.VerticalAlignment = VerticalAlignment.Center;
            caption.Margin = new Thickness(3d, 0d, 0d, 0d);
            caption.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(caption, 1);
            captionRow.Children.Add(caption);
            content.Children.Add(captionRow);
            visual.WaferText = caption;

            outer.Child = content;
            outer.ToolTip = !string.IsNullOrEmpty(cell.ToolTip)
                    ? cell.ToolTip
                    : (string.IsNullOrEmpty(cell.WaferId) ? null : (object)cell.WaferId);
            return visual;
        }

        // ===== LamellaPost — 포스트 + 라멜라 셀 =====

        // 베이스에서 솟은 포스트(핑거)에 라멜라 칩이 붙는다 — 삽입 위치
        // (Top/Left/Right)가 라멜라가 붙는 **위치 자체**라 테마와 무관하게
        // 형태로 읽힌다. A~E 눈금은 베이스 아래 고정이라 절대 가려지지 않는다.
        // "Lamella ID" 스위치가 켜져 있으면 그 아래 핑거당 웨이퍼 ID 행을 붙인다.
        private CellVisual BuildLamellaCell(SlotMapCell cell, double cellFontSize)
        {
            CellVisual visual = new CellVisual();
            visual.Cell = cell;
            visual.Kind = SlotMapSectionKind.LamellaPost;
            visual.Posts = new List<System.Windows.Shapes.Rectangle>();
            visual.Lamellas = new List<System.Windows.Shapes.Rectangle>();
            visual.LetterTexts = new List<TextBlock>();
            visual.SubWaferTexts = new List<TextBlock>();

            double chipSize = cellFontSize > 0d ? cellFontSize - 1d : 10d;

            Border outer = this.BuildOuter(visual);

            Border frame = new Border();
            frame.CornerRadius = new CornerRadius(4d);
            frame.BorderThickness = new Thickness(1.5d);
            frame.Padding = new Thickness(4d, 2d, 4d, 3d);
            visual.Token = frame;

            StackPanel content = new StackPanel();

            TextBlock label;
            Border labelChip = this.BuildNumberChip(cell.Label, chipSize, out label);
            labelChip.HorizontalAlignment = HorizontalAlignment.Left;
            labelChip.Margin = new Thickness(0d, 0d, 0d, 2d);
            content.Children.Add(labelChip);
            visual.LabelChip = labelChip;
            visual.LabelText = label;

            // 포스트 구역(핑거 수만큼 등분)과 A~E 눈금 행.
            Grid postZone = new Grid();
            postZone.Height = 34d;

            Grid letterRow = new Grid();
            letterRow.Margin = new Thickness(0d, 1d, 0d, 0d);

            for (int index = 0; index < cell.SubCells.Count; index++)
            {
                postZone.ColumnDefinitions.Add(new ColumnDefinition());
                letterRow.ColumnDefinitions.Add(new ColumnDefinition());

                // 핑거 폭은 홀수(27)로 둔다 — 포스트(5)/측면 라멜라(7)/상단
                // 라멜라(15)와 조합하면 모든 도형의 좌표가 정수 픽셀에 떨어져,
                // Left/Right 라멜라와 포스트의 간격이 렌더링에서도 항상 동일하다
                // (짝수 폭 + 중앙 정렬은 반픽셀 경계에 걸려 셀마다 안티앨리어싱
                // 편차가 생기고, 좌우 간격이 달라 보이는 착시를 키운다).
                Grid finger = new Grid();
                finger.Width = 27d;
                finger.HorizontalAlignment = HorizontalAlignment.Center;
                Grid.SetColumn(finger, index);
                postZone.Children.Add(finger);

                System.Windows.Shapes.Rectangle post = new System.Windows.Shapes.Rectangle();
                post.Width = 5d;
                post.Height = 20d;
                post.RadiusX = 1.5d;
                post.RadiusY = 1.5d;
                post.HorizontalAlignment = HorizontalAlignment.Center;
                post.VerticalAlignment = VerticalAlignment.Bottom;
                finger.Children.Add(post);
                visual.Posts.Add(post);

                System.Windows.Shapes.Rectangle lamella = new System.Windows.Shapes.Rectangle();
                lamella.RadiusX = 1.5d;
                lamella.RadiusY = 1.5d;
                lamella.StrokeThickness = 1.2d;
                lamella.HorizontalAlignment = HorizontalAlignment.Center;
                lamella.VerticalAlignment = VerticalAlignment.Bottom;
                lamella.Visibility = Visibility.Collapsed;
                finger.Children.Add(lamella);
                visual.Lamellas.Add(lamella);

                TextBlock letter = new TextBlock();
                letter.Text = cell.SubCells[index].Name;
                letter.FontSize = 9d;
                letter.TextAlignment = TextAlignment.Center;
                letter.HorizontalAlignment = HorizontalAlignment.Center;
                Grid.SetColumn(letter, index);
                letterRow.Children.Add(letter);
                visual.LetterTexts.Add(letter);
            }

            content.Children.Add(postZone);

            // 베이스 바 — 포스트가 서 있는 바닥(반원 링의 단면).
            System.Windows.Shapes.Rectangle baseBar = new System.Windows.Shapes.Rectangle();
            baseBar.Height = 4d;
            baseBar.RadiusX = 2d;
            baseBar.RadiusY = 2d;
            baseBar.Margin = new Thickness(2d, 0d, 2d, 0d);
            baseBar.Fill = (Brush)this.FindResource("Brush.Border");
            content.Children.Add(baseBar);

            content.Children.Add(letterRow);

            // ON 모드 — 핑거당 웨이퍼 ID 행 ("A · LM-001-03").
            if (this.showSubCellWaferIds)
            {
                foreach (SlotMapSubCell sub in cell.SubCells)
                {
                    TextBlock idRow = new TextBlock();
                    idRow.FontSize = 9d;
                    idRow.Margin = new Thickness(2d, 1d, 0d, 0d);
                    idRow.TextTrimming = TextTrimming.CharacterEllipsis;
                    content.Children.Add(idRow);
                    visual.SubWaferTexts.Add(idRow);
                }
            }

            frame.Child = content;
            outer.Child = frame;
            outer.ToolTip = this.BuildSubToolTip(cell);
            return visual;
        }

        // 라멜라를 포스트의 삽입 위치에 붙인다 — Top: 포스트 머리 위(가로),
        // Left/Right: 포스트 윗부분 옆(세로). HorizontalAlignment.Center 기준
        // 좌우 마진 차로 옆 부착 오프셋을 만든다. 위치가 곧 삽입 방향 표현이다.
        // 좌표 검증(핑거 폭 27 기준): 포스트 x [11,16], Left 라멜라 [4,11],
        // Right 라멜라 [16,23] — 양쪽 다 간격 0(접함)으로 완전 대칭이고 전부
        // 정수 픽셀이라 렌더링 편차가 없다.
        private static void PlaceLamella(System.Windows.Shapes.Rectangle lamella, string marker)
        {
            if (marker == "Left")
            {
                lamella.Width = 7d;
                lamella.Height = 12d;
                lamella.Margin = new Thickness(0d, 0d, 12d, 8d);
            }
            else if (marker == "Right")
            {
                lamella.Width = 7d;
                lamella.Height = 12d;
                lamella.Margin = new Thickness(12d, 0d, 0d, 8d);
            }
            else
            {
                // Top(기본) — 포스트 머리 위에 가로로 얹는다. 폭은 포스트와
                // 같은 홀수(15)로 두어 중심이 포스트 중심과 일치한다.
                lamella.Width = 15d;
                lamella.Height = 7d;
                lamella.Margin = new Thickness(0d, 0d, 0d, 20d);
            }
        }

        // ===== Generic — 기본 상자 표현 셀 (Kind 미지정 호환 경로) =====

        private CellVisual BuildGenericCell(SlotMapCell cell, double cellFontSize)
        {
            CellVisual visual = new CellVisual();
            visual.Cell = cell;

            // 웨이퍼 ID / 번호 칩 글자 크기 — 구획이 지정하면 그 값, 아니면 기본.
            double waferSize = cellFontSize > 0d ? cellFontSize : 11d;
            double chipSize = cellFontSize > 0d ? cellFontSize - 1d : 10d;

            Border outer = this.BuildOuter(visual);

            if (cell.SubCells == null)
            {
                // 단일 수납 — [고정 번호 칩 | 웨이퍼 토큰] 구조. 번호는 자리에
                // 붙어 있고, ID가 적힌 토큰만 상태(채움/빈/미리보기)를 갖는다.
                Grid content = new Grid();
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                content.ColumnDefinitions.Add(new ColumnDefinition());

                TextBlock label;
                Border labelChip = this.BuildNumberChip(cell.Label, chipSize, out label);
                labelChip.Margin = new Thickness(0d, 0d, 6d, 0d);
                content.Children.Add(labelChip);
                visual.LabelChip = labelChip;
                visual.LabelText = label;

                Border token = new Border();
                token.CornerRadius = new CornerRadius(4d);
                token.BorderThickness = new Thickness(1.5d);
                token.Padding = new Thickness(8d, 2d, 8d, 2d);
                token.MinHeight = waferSize + 8d;
                Grid.SetColumn(token, 1);
                visual.Token = token;

                TextBlock wafer = new TextBlock();
                wafer.FontSize = waferSize;
                wafer.FontWeight = FontWeights.SemiBold;
                wafer.TextTrimming = TextTrimming.CharacterEllipsis;
                wafer.VerticalAlignment = VerticalAlignment.Center;
                token.Child = wafer;
                visual.WaferText = wafer;

                content.Children.Add(token);
                outer.Child = content;
                outer.ToolTip = !string.IsNullOrEmpty(cell.ToolTip)
                        ? cell.ToolTip
                        : (string.IsNullOrEmpty(cell.WaferId) ? null : (object)cell.WaferId);
                return visual;
            }

            // 복합 수납 — 번호 눈금 + 핑거 표현. "Lamella ID" 스위치에 따라
            // 두 레이아웃: 켬 = 핑거당 미니 행(도트 + 웨이퍼 ID), 끔 = A~E 배지를
            // 크게 한 줄(5개)로.
            Border frame = new Border();
            frame.CornerRadius = new CornerRadius(4d);
            frame.BorderThickness = new Thickness(1.5d);
            frame.Padding = new Thickness(4d, 2d, 4d, 2d);
            visual.Token = frame;

            StackPanel content2 = new StackPanel();

            TextBlock label2;
            Border labelChip2 = this.BuildNumberChip(cell.Label, chipSize, out label2);
            labelChip2.HorizontalAlignment = HorizontalAlignment.Left;
            labelChip2.Margin = new Thickness(0d, 0d, 0d, 2d);
            content2.Children.Add(labelChip2);
            visual.LabelChip = labelChip2;
            visual.LabelText = label2;

            visual.DotBorders = new List<Border>();
            visual.DotTexts = new List<TextBlock>();
            visual.SubWaferTexts = new List<TextBlock>();
            visual.MarkerEdges = new List<Border>();

            if (this.showSubCellWaferIds)
            {
                // 켬 — 핑거당 미니 행: [이름 도트 | 웨이퍼 ID].
                foreach (SlotMapSubCell sub in cell.SubCells)
                {
                    Grid row = new Grid();
                    row.Margin = new Thickness(0d, 0d, 0d, 1d);
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition());

                    Border dot;
                    TextBlock letter;
                    Border markerEdge;
                    Grid dotGrid = this.BuildFingerDotGrid(
                            sub, 15d, 8d, out dot, out letter, out markerEdge);
                    row.Children.Add(dotGrid);

                    TextBlock subWafer = new TextBlock();
                    subWafer.FontSize = 9d;
                    subWafer.TextTrimming = TextTrimming.CharacterEllipsis;
                    subWafer.VerticalAlignment = VerticalAlignment.Center;
                    subWafer.Margin = new Thickness(4d, 0d, 0d, 0d);
                    Grid.SetColumn(subWafer, 1);
                    row.Children.Add(subWafer);

                    content2.Children.Add(row);
                    visual.DotBorders.Add(dot);
                    visual.DotTexts.Add(letter);
                    visual.SubWaferTexts.Add(subWafer);
                    visual.MarkerEdges.Add(markerEdge);
                }
            }
            else
            {
                // 끔 — A~E 배지를 크게 한 줄로 가운데 정렬해 나열(웨이퍼 ID 없음).
                StackPanel badges = new StackPanel();
                badges.Orientation = Orientation.Horizontal;
                badges.HorizontalAlignment = HorizontalAlignment.Center;
                badges.Margin = new Thickness(0d, 1d, 0d, 1d);

                foreach (SlotMapSubCell sub in cell.SubCells)
                {
                    Border dot;
                    TextBlock letter;
                    Border markerEdge;
                    Grid dotGrid = this.BuildFingerDotGrid(
                            sub, 22d, 11d, out dot, out letter, out markerEdge);
                    dotGrid.Margin = new Thickness(1d, 0d, 1d, 0d);
                    badges.Children.Add(dotGrid);
                    visual.DotBorders.Add(dot);
                    visual.DotTexts.Add(letter);
                    visual.MarkerEdges.Add(markerEdge);
                    // 이 레이아웃엔 웨이퍼 ID 글씨가 없다(SubWaferTexts 비움).
                }

                content2.Children.Add(badges);
            }

            frame.Child = content2;
            outer.Child = frame;
            outer.ToolTip = this.BuildSubToolTip(cell);
            return visual;
        }

        // 핑거 도트(이름 글자) + 삽입 위치 테두리 하이라이트를 담은 Grid를
        // 만든다 — 미니 행(작게)과 A~E 배지 한 줄(크게) 두 레이아웃이 크기만
        // 달리해 공유한다.
        private Grid BuildFingerDotGrid(
                SlotMapSubCell sub, double dotSize, double letterSize,
                out Border dot, out TextBlock letter,
                out Border markerEdge)
        {
            Grid dotGrid = new Grid();

            dot = new Border();
            dot.Width = dotSize;
            dot.Height = dotSize;
            dot.CornerRadius = new CornerRadius(3d);
            dot.BorderThickness = new Thickness(1d);
            dot.VerticalAlignment = VerticalAlignment.Center;

            letter = new TextBlock();
            letter.Text = sub.Name;
            letter.FontSize = letterSize;
            letter.HorizontalAlignment = HorizontalAlignment.Center;
            letter.VerticalAlignment = VerticalAlignment.Center;
            dot.Child = letter;
            dotGrid.Children.Add(dot);

            // 삽입 위치는 도트 안이 아니라 **해당 변의 테두리를 액센트로 굵게**
            // 표시한다 — 내부 공간을 침범하지 않아 이름 글자가 항상 정중앙에
            // 보인다. 빈 핑거의 미리보기에서도 갱신할 수 있게 항상 만들어 둔다.
            markerEdge = new Border();
            markerEdge.Width = dotSize;
            markerEdge.Height = dotSize;
            markerEdge.CornerRadius = new CornerRadius(3d);
            markerEdge.Background = null;
            markerEdge.VerticalAlignment = VerticalAlignment.Center;
            markerEdge.IsHitTestVisible = false;
            this.ConfigureMarkerEdge(markerEdge, sub.Marker, false);
            dotGrid.Children.Add(markerEdge);

            return dotGrid;
        }

        // 삽입 위치(Top/Left/Right)를 도트의 **해당 변 테두리만 액센트로 굵게**
        // 그려 표시한다 — 도트와 같은 크기/모서리의 투명 Border를 겹치고 그
        // 변의 두께만 준다. 실제 수납은 진하게, 우측 미리보기는 같은 액센트를
        // 옅은 투명도로 낮춰(빈 자리 위에서 과하지 않게) 좌·우 맵의 표현을
        // 일관되게 유지한다.
        private void ConfigureMarkerEdge(Border edge, string marker, bool preview)
        {
            edge.BorderBrush = (Brush)this.FindResource("Brush.Accent");
            edge.Opacity = preview ? 0.45d : 1d;
            edge.Visibility = string.IsNullOrEmpty(marker) ? Visibility.Collapsed : Visibility.Visible;

            if (string.IsNullOrEmpty(marker))
            {
                edge.BorderThickness = new Thickness(0d);
                return;
            }

            double thickness = 2d;

            if (marker == "Top")
            {
                edge.BorderThickness = new Thickness(0d, thickness, 0d, 0d);
            }
            else if (marker == "Left")
            {
                edge.BorderThickness = new Thickness(thickness, 0d, 0d, 0d);
            }
            else
            {
                edge.BorderThickness = new Thickness(0d, 0d, thickness, 0d);
            }
        }

        // 고정 자리 번호 칩 — 파랑 틴트 배경 + Info 텍스트의 작은 배지로
        // 번호에 포인트를 준다. 자리(번호)는 상태와 무관하게 항상 같은
        // 모습이라 "자리는 고정, 웨이퍼이 움직인다"가 읽힌다.
        private Border BuildNumberChip(string label, double fontSize, out TextBlock text)
        {
            // 고정 높이 칩 + 좌우 패딩만. 숫자는 Grid 중앙 정렬로 위아래 정확히
            // 가운데 오게 한다 (TextBlock의 기본 라인 여백으로 위로 치우치던 것 보정).
            Border chip = new Border();
            chip.CornerRadius = new CornerRadius(3d);
            chip.Background = (Brush)this.FindResource("Brush.InfoBackground");
            chip.MinWidth = 22d;
            chip.Height = fontSize + 8d;
            chip.Padding = new Thickness(3d, 0d, 3d, 0d);
            chip.VerticalAlignment = VerticalAlignment.Center;
            chip.SnapsToDevicePixels = true;

            text = new TextBlock();
            text.Text = label;
            text.FontSize = fontSize;
            text.FontWeight = FontWeights.SemiBold;
            text.Foreground = (Brush)this.FindResource("Brush.InfoText");
            text.TextAlignment = TextAlignment.Center;
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Padding = new Thickness(0d);
            text.LineHeight = fontSize + 2d;
            text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            chip.Child = text;

            return chip;
        }

        // 번호 배지 — 선택 상태를 슬롯 번호 자체에 색면으로 채운다. 링만으로는
        // 바/칩이 Lot 색으로 차 있을 때 테두리 한 겹이 그 색에 묻혀 "선택했는데
        // 표시가 안 난다"로 읽혔다. 배지는 네 구획이 같은 규칙을 쓴다 —
        // 클릭 선택은 링과 같은 클릭 색, 스테이징·미리보기는 액센트,
        // 그 밖은 구획이 넘긴 평상시 색이다.
        private void ApplyNumberBadge(CellVisual visual, bool staged, bool clicked,
                bool previewed, Brush restBackground, Brush restForeground)
        {
            if (visual.LabelChip == null || visual.LabelText == null)
            {
                return;
            }

            if (!staged && !clicked && !previewed)
            {
                visual.LabelChip.Background = restBackground;
                visual.LabelText.Foreground = restForeground;
                return;
            }

            // 스테이징·미리보기는 액센트 **한 단계 진한 색**을 쓴다. 기본 액센트 위
            // 유도 글자는 4.64:1 로 기준을 겨우 넘어, 9~10px 짜리 작은 번호에는
            // 여유가 없었다(사용자 보고 2026-09-15). 진한 쪽은 7.1:1 이고 색 계열이
            // 같아 "이동 예정"이라는 의미는 그대로다.
            Brush badge = clicked
                    ? (Brush)this.FindResource("Brush.Warning")
                    : (Brush)this.FindResource("Brush.AccentPressed");
            visual.LabelChip.Background = badge;
            visual.LabelText.Foreground = this.DeriveBadgeForeground(badge);
        }

        // 배지 글자색은 **배지 배경에서 유도한다.** 고정 토큰을 쓰면 테마가 배경을
        // 바꿀 때 대비가 따라오지 않는다 — 경고색(#D97706) 위 흰 글자는 기본 테마에서
        // 3.19:1이라 작은 번호가 가장 안 읽히는 조합이 됐다. DeriveForeground가
        // 4.5:1을 보장하므로(못 미치면 검정·흰색 중 대비가 큰 쪽) 테마를 새로 더해도
        // 이 자리는 따로 손볼 것이 없다.
        private Brush DeriveBadgeForeground(Brush background)
        {
            SolidColorBrush solid = background as SolidColorBrush;

            if (solid == null)
            {
                return (Brush)this.FindResource("Brush.OnAccent");
            }

            SolidColorBrush derived = new SolidColorBrush(
                    ChipColorHelper.DeriveForeground(solid.Color));
            derived.Freeze();

            return derived;
        }

        // 복합 셀(LCC) 툴팁 — 채워진 핑거를 **정렬된 미니 표**로 보여 준다
        // (평문 대신 컬럼이 맞는 표: Finger / Wafer ID / Insert / Lot). 핑거가
        // 여러 개인 TRAY LCC에서 내용이 한눈에 정돈돼 보인다. 채움이 없으면 null.
        private object BuildSubToolTip(SlotMapCell cell)
        {
            System.Collections.Generic.List<SlotMapSubCell> filled =
                    new System.Collections.Generic.List<SlotMapSubCell>();

            foreach (SlotMapSubCell sub in cell.SubCells)
            {
                if (!string.IsNullOrEmpty(sub.WaferId))
                {
                    filled.Add(sub);
                }
            }

            if (filled.Count == 0)
            {
                return null;
            }

            Brush textPrimary = (Brush)this.FindResource("Brush.TextPrimary");
            Brush textSecondary = (Brush)this.FindResource("Brush.TextSecondary");

            Grid grid = new Grid();

            for (int c = 0; c < 4; c++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            for (int r = 0; r < filled.Count + 2; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // 제목 — "LCC {번호}" (전 컬럼에 걸침).
            TextBlock title = new TextBlock();
            title.Text = "LCC " + cell.Label;
            title.FontWeight = FontWeights.SemiBold;
            title.Foreground = textPrimary;
            title.Margin = new Thickness(0d, 0d, 0d, 4d);
            Grid.SetRow(title, 0);
            Grid.SetColumnSpan(title, 4);
            grid.Children.Add(title);

            // 헤더 행.
            string[] headers = new string[] { "Finger", "Wafer Id", "Insert", "Lot" };

            for (int c = 0; c < headers.Length; c++)
            {
                grid.Children.Add(this.BuildTipCell(headers[c], 1, c, true, textPrimary, textSecondary));
            }

            // 데이터 행 — 핑거마다 한 줄.
            for (int i = 0; i < filled.Count; i++)
            {
                SlotMapSubCell sub = filled[i];
                int row = i + 2;
                grid.Children.Add(this.BuildTipCell(sub.Name, row, 0, false, textPrimary, textSecondary));
                grid.Children.Add(this.BuildTipCell(sub.WaferId, row, 1, false, textPrimary, textSecondary));
                grid.Children.Add(this.BuildTipCell(sub.Marker ?? string.Empty, row, 2, false, textPrimary, textSecondary));
                grid.Children.Add(this.BuildTipCell(sub.Detail ?? string.Empty, row, 3, false, textPrimary, textSecondary));
            }

            return grid;
        }

        // 툴팁 표의 셀 하나(헤더/데이터) — 컬럼 사이 간격을 둬 정렬되게 한다.
        private TextBlock BuildTipCell(
                string text, int row, int column, bool header, Brush primary, Brush secondary)
        {
            TextBlock cell = new TextBlock();
            cell.Text = text;
            cell.FontSize = 11d;
            cell.FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal;
            cell.Foreground = header ? secondary : primary;
            cell.Margin = new Thickness(0d, 1d, column < 3 ? 14d : 0d, 1d);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, column);
            return cell;
        }

        // ===== 선택 + 드래그 시작 =====

        // 마우스 다운은 클릭/드래그 공용 시작점 — 여기서는 후보만 기억한다.
        private void OnCellMouseDown(object sender, MouseButtonEventArgs e)
        {
            Border outer = sender as Border;
            CellVisual visual = outer != null ? outer.Tag as CellVisual : null;

            if (visual == null || string.IsNullOrEmpty(visual.Cell.Key)
                    || (!visual.Cell.Filled
                            && (!this.AllowSelection || !this.CellHasPreview(visual.Cell))))
            {
                return;
            }

            if (!this.AllowSelection && !this.EnableDragOut)
            {
                return;
            }

            this.pressCandidate = visual;
            this.pressPoint = e.GetPosition(this);
            e.Handled = true;
        }

        // 임계 거리를 넘게 끌면 드래그 시작 — 끌린 셀이 선택에 포함돼 있으면
        // 선택 전체가 페이로드가 되고, 아니면 그 셀 하나만 간다.
        private void OnCellMouseMove(object sender, MouseEventArgs e)
        {
            if (this.pressCandidate == null || !this.EnableDragOut
                    || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point position = e.GetPosition(this);

            if (Math.Abs(position.X - this.pressPoint.X) < SystemParameters.MinimumHorizontalDragDistance
                    && Math.Abs(position.Y - this.pressPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            string[] payload = this.selectedKeys.Contains(this.pressCandidate.Cell.Key)
                    ? this.SelectedKeys
                    : new string[] { this.pressCandidate.Cell.Key };

            this.pressCandidate = null;

            DataObject data = new DataObject(dragDataFormat, payload);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
        }

        // 드래그로 넘어가지 않고 같은 셀에서 떼면 클릭 — 선택 상태는 바꾸지
        // 않고 CellClicked만 알린다 (클릭 선택 표시는 화면이 SetClickKeys로 관리).
        private void OnCellMouseUp(object sender, MouseButtonEventArgs e)
        {
            Border outer = sender as Border;
            CellVisual visual = outer != null ? outer.Tag as CellVisual : null;

            if (visual == null || !object.ReferenceEquals(visual, this.pressCandidate))
            {
                this.pressCandidate = null;
                return;
            }

            this.pressCandidate = null;

            if (!this.AllowSelection)
            {
                return;
            }

            EventHandler<SlotMapCellEventArgs> handler = this.CellClicked;

            if (handler != null)
            {
                handler(this, new SlotMapCellEventArgs(visual.Cell.Key));
            }

            e.Handled = true;
        }

        private void RaiseSelectionChanged()
        {
            EventHandler handler = this.SelectionChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        // ===== 드롭 수용 =====

        private void OnMapDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(dragDataFormat)
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnMapDrop(object sender, DragEventArgs e)
        {
            string[] keys = e.Data.GetData(dragDataFormat) as string[];

            if (keys == null || keys.Length == 0)
            {
                return;
            }

            // 놓은 지점의 셀(앵커) — 셀 밖이면 빈 문자열(앞에서부터 채움).
            CellVisual anchor = FindCellVisual(e.OriginalSource as DependencyObject);
            string anchorKey = anchor != null ? anchor.Cell.Key : string.Empty;

            EventHandler<SlotMapDropEventArgs> handler = this.WafersDropped;

            if (handler != null)
            {
                handler(this, new SlotMapDropEventArgs(keys, anchorKey));
            }

            e.Handled = true;
        }

        // 드롭 지점의 시각 요소에서 셀(Outer Border의 Tag)을 찾는다.
        private static CellVisual FindCellVisual(DependencyObject element)
        {
            while (element != null)
            {
                Border border = element as Border;

                if (border != null && border.Tag is CellVisual)
                {
                    return (CellVisual)border.Tag;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        // ===== 상태 칠하기 =====

        // 단일 셀의 미리보기 키 = 자리 키. 하위(LCC 핑거) 미리보기 키 = "셀키|핑거".
        private static string SubPreviewKey(string cellKey, string subName)
        {
            return cellKey + "|" + subName;
        }

        // 이 구획에서 미리보기 맵이 지정한(=이 구획 자리로 계획된) 웨이퍼 수와,
        // 그중 실제로 빈 자리에 놓일 수(previewed)를 센다. 지정 자리가 이미 차
        // 있으면 놓을 수 없어 부족분(shortage)이 된다.
        private void CountSectionPreview(int section, out int requested, out int previewed)
        {
            requested = 0;
            previewed = 0;

            if (this.previewMap == null || this.previewMap.Count == 0)
            {
                return;
            }

            foreach (CellVisual visual in this.sectionVisuals[section])
            {
                SlotMapCell cell = visual.Cell;

                if (string.IsNullOrEmpty(cell.Key))
                {
                    continue;
                }

                if (cell.SubCells == null)
                {
                    if (this.previewMap.ContainsKey(cell.Key))
                    {
                        requested = requested + 1;

                        if (!cell.Filled)
                        {
                            previewed = previewed + 1;
                        }
                    }

                    continue;
                }

                foreach (SlotMapSubCell sub in cell.SubCells)
                {
                    if (!this.previewMap.ContainsKey(SubPreviewKey(cell.Key, sub.Name)))
                    {
                        continue;
                    }

                    requested = requested + 1;

                    if (string.IsNullOrEmpty(sub.WaferId))
                    {
                        previewed = previewed + 1;
                    }
                }
            }
        }

        private void RefreshAllVisuals()
        {
            for (int section = 0; section < this.sectionVisuals.Count; section++)
            {
                foreach (CellVisual visual in this.sectionVisuals[section])
                {
                    this.ApplyCellVisual(visual);
                }

                this.UpdateSectionCount(section);
            }
        }

        // 구획 집계 — "채움 / 용량"에 미리보기(+n)와 부족분(need n more)을 덧붙인다.
        private void UpdateSectionCount(int section)
        {
            if (section >= this.sectionCountTexts.Count || this.sections == null)
            {
                return;
            }

            int filled = 0;
            int capacity = 0;

            foreach (CellVisual visual in this.sectionVisuals[section])
            {
                filled = filled + visual.Cell.WaferCount;
                capacity = capacity + visual.Cell.WaferCount + visual.Cell.EmptyWaferCount;
            }

            int requested;
            int previewed;
            this.CountSectionPreview(section, out requested, out previewed);
            int shortage = requested - previewed;

            TextBlock count = this.sectionCountTexts[section];
            string text = filled.ToString("N0") + " / " + capacity.ToString("N0");

            if (requested > 0)
            {
                text = text + "  ·  +" + previewed.ToString("N0");
            }

            if (shortage > 0)
            {
                text = text + "  ·  need " + shortage.ToString("N0") + " more";
                count.Foreground = (Brush)this.FindResource("Brush.ErrorText");
                count.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                count.Foreground = (Brush)this.FindResource("Brush.TextSecondary");
                count.FontWeight = FontWeights.Normal;
            }

            count.Text = text;
        }

        // 색 배경 위 글자색 — 배경(색 문자열)에서 대비색을 유도한다(배지와
        // 같은 규칙). 색이 없으면 fallback(테마 기본 텍스트색)을 쓴다.
        // 테마 무관: 배경이 밝으면 어두운 글자, 어두우면 밝은 글자가 나온다.
        private Brush DeriveTextBrush(string color, Brush fallback)
        {
            System.Windows.Media.Color parsed;

            if (ChipColorHelper.TryParseColor(color, out parsed))
            {
                SolidColorBrush brush = new SolidColorBrush(ChipColorHelper.DeriveForeground(parsed));
                brush.Freeze();
                return brush;
            }

            return fallback;
        }

        // 셀의 강조 상태 3종 — staged(스테이징) / clicked(클릭) / 둘의 결합.
        private void GetCellState(SlotMapCell cell, out bool staged, out bool clicked)
        {
            bool hasKey = !string.IsNullOrEmpty(cell.Key);
            staged = hasKey && this.selectedKeys.Contains(cell.Key);
            clicked = hasKey
                    && (cell.Filled || this.CellHasPreview(cell))
                    && this.clickKeys.Contains(cell.Key);
        }

        // WaferEdge 칠하기 — 채움(Lot색 웨이퍼 바 + ID) / 빈 홈(가는 줄) /
        // 미리보기(점선 바 + "→ ID"). 선택은 바 바깥 링(스테이징 액센트 /
        // 클릭 경고색) + 같은 색 번호 배지 두 겹이다 — 링만으로는 Lot 색 채움에
        // 묻힌다. 나머지 상태는 형태 차라 테마 무관.
        private void ApplyWaferVisual(CellVisual visual)
        {
            SlotMapCell cell = visual.Cell;
            bool staged;
            bool clicked;
            this.GetCellState(cell, out staged, out clicked);

            bool interactive = (this.AllowSelection || this.EnableDragOut)
                    && (cell.Filled || this.CellHasPreview(cell))
                    && !string.IsNullOrEmpty(cell.Key);
            visual.Outer.Cursor = interactive ? Cursors.Hand : null;

            Brush accent = (Brush)this.FindResource("Brush.Accent");
            Brush clickAccent = (Brush)this.FindResource("Brush.Warning");
            Brush borderBrush = (Brush)this.FindResource("Brush.Border");
            Brush borderSubtle = (Brush)this.FindResource("Brush.BorderSubtle");
            Brush info = (Brush)this.FindResource("Brush.InfoBackground");
            Brush textPrimary = (Brush)this.FindResource("Brush.TextPrimary");
            Brush textSecondary = (Brush)this.FindResource("Brush.TextSecondary");

            string incoming = this.SingleCellPreview(cell);
            bool previewed = !string.IsNullOrEmpty(incoming);

            // 슬롯 번호 눈금 — 평상시엔 칩이 투명해 숫자만 보이고, 선택되면
            // 번호가 배지로 채워진다.
            this.ApplyNumberBadge(visual, staged, clicked, previewed, Brushes.Transparent, textSecondary);
            visual.LabelText.FontWeight = staged || clicked || previewed
                    ? FontWeights.SemiBold
                    : FontWeights.Normal;

            visual.Ring.Visibility = staged || clicked ? Visibility.Visible : Visibility.Collapsed;
            visual.Ring.Stroke = clicked ? clickAccent : accent;

            if (cell.Filled)
            {
                Brush custom = ChipColorHelper.TryCreateBrush(cell.Color);
                visual.Bar.Height = 14d;
                visual.Bar.Fill = custom != null
                        ? custom
                        : (Brush)this.FindResource("Brush.SelectedBackground");

                // 선택/스테이징 시에는 바 자체의 회색 테두리를 없앤다. 그대로 두면
                // 채움과 선택 링 사이에 밝은 1px 선이 끼어 "흰 틈"처럼 보인다.
                // 링 색으로 덮지 않고 없애는 이유: 랏 색이 액센트와 같을 때
                // 링까지 같은 색이 되어 선택 표시가 사라진다.
                bool ringed = staged || clicked;
                visual.Bar.Stroke = ringed ? null : borderBrush;
                visual.Bar.StrokeThickness = ringed ? 0d : 1d;
                visual.Bar.StrokeDashArray = null;
                visual.WaferText.Text = cell.WaferId;
                visual.WaferText.Foreground = this.DeriveTextBrush(cell.Color, textPrimary);
            }
            else if (previewed)
            {
                visual.Bar.Height = 14d;
                visual.Bar.Fill = info;
                visual.Bar.Stroke = accent;
                visual.Bar.StrokeThickness = 1.5d;
                visual.Bar.StrokeDashArray = new DoubleCollection(new double[] { 3d, 2d });
                visual.WaferText.Text = clicked ? "✓ " + incoming : "→ " + incoming;
                visual.WaferText.Foreground = accent;
            }
            else
            {
                // 빈 슬롯 — 레일 사이 가는 홈만 남긴다.
                visual.Bar.Height = 3d;
                visual.Bar.Fill = borderSubtle;
                visual.Bar.Stroke = null;
                visual.Bar.StrokeThickness = 0d;
                visual.Bar.StrokeDashArray = null;
                visual.WaferText.Text = string.Empty;
            }
        }

        // PinStub 칠하기 — 칩의 유무가 채움/빈 구분이다. 빈 스텁은 핀 자국만
        // 있는 맨 원판, 미리보기는 점선 칩 + 캡션 "→ ID".
        private void ApplyStubVisual(CellVisual visual)
        {
            SlotMapCell cell = visual.Cell;
            bool staged;
            bool clicked;
            this.GetCellState(cell, out staged, out clicked);

            bool interactive = (this.AllowSelection || this.EnableDragOut)
                    && (cell.Filled || this.CellHasPreview(cell))
                    && !string.IsNullOrEmpty(cell.Key);
            visual.Outer.Cursor = interactive ? Cursors.Hand : null;

            Brush accent = (Brush)this.FindResource("Brush.Accent");
            Brush clickAccent = (Brush)this.FindResource("Brush.Warning");
            Brush borderBrush = (Brush)this.FindResource("Brush.Border");
            Brush borderSubtle = (Brush)this.FindResource("Brush.BorderSubtle");
            Brush neutral = (Brush)this.FindResource("Brush.NeutralBackground");
            Brush info = (Brush)this.FindResource("Brush.InfoBackground");
            Brush textPrimary = (Brush)this.FindResource("Brush.TextPrimary");
            Brush textSecondary = (Brush)this.FindResource("Brush.TextSecondary");

            string incoming = this.SingleCellPreview(cell);
            bool previewed = !string.IsNullOrEmpty(incoming);

            visual.Ring.Visibility = staged || clicked ? Visibility.Visible : Visibility.Collapsed;
            visual.Ring.Stroke = clicked ? clickAccent : accent;

            // 번호 배지 — 원판 위 칩은 Lot 색이라 선택 색을 얹을 수 없다.
            // 캡션의 번호가 선택을 드러내는 자리다.
            this.ApplyNumberBadge(visual, staged, clicked, previewed, Brushes.Transparent, textSecondary);

            visual.Disc.Fill = neutral;
            visual.Disc.Stroke = cell.Filled || previewed ? borderBrush : borderSubtle;

            if (cell.Filled)
            {
                Brush custom = ChipColorHelper.TryCreateBrush(cell.Color);
                visual.PinDot.Visibility = Visibility.Collapsed;
                visual.Chip.Visibility = Visibility.Visible;
                visual.Chip.Fill = custom != null
                        ? custom
                        : (Brush)this.FindResource("Brush.SelectedBackground");
                visual.Chip.Stroke = borderBrush;
                visual.Chip.StrokeDashArray = null;
                // 칩 안에는 ID 끝 4자리만 — 글자색은 칩 배경(Lot 색)에서
                // 대비색을 유도한다. 전체 ID는 아래 캡션과 툴팁이 보여 준다.
                visual.ChipText.Text = TailChars(cell.WaferId, 4);
                visual.ChipText.Foreground = this.DeriveTextBrush(cell.Color, textPrimary);
                visual.WaferText.Text = cell.WaferId;
                visual.WaferText.Foreground = textPrimary;
            }
            else if (previewed)
            {
                visual.PinDot.Visibility = Visibility.Collapsed;
                visual.Chip.Visibility = Visibility.Visible;
                visual.Chip.Fill = info;
                visual.Chip.Stroke = accent;
                visual.Chip.StrokeDashArray = new DoubleCollection(new double[] { 3d, 2d });
                // 들어올 칩도 끝 4자리를 새긴다 — 점선 + 액센트 글자라 실수납과 구분된다.
                visual.ChipText.Text = clicked ? "✓" : TailChars(incoming, 4);
                visual.ChipText.Foreground = accent;
                visual.WaferText.Text = (clicked ? "✓ " : "→ ") + incoming;
                visual.WaferText.Foreground = accent;
            }
            else
            {
                visual.Chip.Visibility = Visibility.Collapsed;
                visual.PinDot.Visibility = Visibility.Visible;
                visual.PinDot.Fill = borderBrush;
                visual.ChipText.Text = string.Empty;
                visual.WaferText.Text = string.Empty;
                visual.WaferText.Foreground = textSecondary;
            }
        }

        // 웨이퍼 ID의 끝 n자리 — 칩 안 축약 표기용 (예: "TH10001.01-C001" → "C001").
        private static string TailChars(string text, int count)
        {
            string trimmed = (text ?? string.Empty).Trim();
            return trimmed.Length <= count ? trimmed : trimmed.Substring(trimmed.Length - count);
        }

        // LamellaPost 칠하기 — 핑거마다 채움(Lot색 라멜라가 삽입 위치에
        // 붙음) / 미리보기(점선 라멜라) / 빈 포스트. 프레임/번호 칩은 Generic
        // 복합 셀과 같은 규칙을 따라 화면 전체의 강조 문법이 일관된다.
        private void ApplyLamellaVisual(CellVisual visual)
        {
            SlotMapCell cell = visual.Cell;
            bool staged;
            bool clicked;
            this.GetCellState(cell, out staged, out clicked);
            bool selected = staged || clicked;

            bool interactive = (this.AllowSelection || this.EnableDragOut)
                    && (cell.Filled || this.CellHasPreview(cell))
                    && !string.IsNullOrEmpty(cell.Key);
            visual.Outer.Cursor = interactive ? Cursors.Hand : null;

            Brush accent = (Brush)this.FindResource("Brush.Accent");
            Brush clickAccent = (Brush)this.FindResource("Brush.Warning");
            Brush borderBrush = (Brush)this.FindResource("Brush.Border");
            Brush borderSubtle = (Brush)this.FindResource("Brush.BorderSubtle");
            Brush surface = (Brush)this.FindResource("Brush.Surface");
            Brush info = (Brush)this.FindResource("Brush.InfoBackground");
            Brush textPrimary = (Brush)this.FindResource("Brush.TextPrimary");
            Brush textSecondary = (Brush)this.FindResource("Brush.TextSecondary");
            Brush textDisabled = (Brush)this.FindResource("Brush.DisabledText");

            bool previewed = this.CellHasPreview(cell);
            Brush emphasis = clicked ? clickAccent : accent;
            visual.LabelText.Text = clicked && previewed ? "✓ " + cell.Label : cell.Label;

            visual.Token.Background = previewed ? info : surface;
            visual.Token.BorderBrush = selected || previewed
                    ? emphasis
                    : (cell.Filled ? borderBrush : borderSubtle);

            // 번호 배지 — Generic과 같은 강조 규칙.
            this.ApplyNumberBadge(visual, staged, clicked, previewed, info,
                    (Brush)this.FindResource("Brush.InfoText"));

            for (int index = 0; index < cell.SubCells.Count; index++)
            {
                SlotMapSubCell sub = cell.SubCells[index];
                System.Windows.Shapes.Rectangle post = visual.Posts[index];
                System.Windows.Shapes.Rectangle lamella = visual.Lamellas[index];
                TextBlock letter = visual.LetterTexts[index];
                TextBlock idRow = index < visual.SubWaferTexts.Count
                        ? visual.SubWaferTexts[index]
                        : null;

                string subIncoming = null;

                if (string.IsNullOrEmpty(sub.WaferId) && this.previewMap != null)
                {
                    this.previewMap.TryGetValue(SubPreviewKey(cell.Key, sub.Name), out subIncoming);
                }

                if (!string.IsNullOrEmpty(sub.WaferId))
                {
                    Brush custom = ChipColorHelper.TryCreateBrush(
                            string.IsNullOrEmpty(sub.Color) ? cell.Color : sub.Color);
                    post.Fill = borderBrush;
                    lamella.Visibility = Visibility.Visible;
                    PlaceLamella(lamella, (sub.Marker ?? string.Empty).Trim());
                    lamella.Fill = custom != null
                            ? custom
                            : (Brush)this.FindResource("Brush.SelectedBackground");
                    lamella.Stroke = borderBrush;
                    lamella.StrokeDashArray = null;
                    letter.Foreground = textSecondary;

                    if (idRow != null)
                    {
                        idRow.Text = sub.Name + " · " + sub.WaferId;
                        idRow.Foreground = textPrimary;
                    }
                }
                else if (!string.IsNullOrEmpty(subIncoming))
                {
                    post.Fill = borderBrush;
                    lamella.Visibility = Visibility.Visible;
                    PlaceLamella(lamella, this.SubPreviewMarker(cell.Key, sub.Name));
                    lamella.Fill = info;
                    lamella.Stroke = accent;
                    lamella.StrokeDashArray = new DoubleCollection(new double[] { 3d, 2d });
                    letter.Foreground = accent;

                    if (idRow != null)
                    {
                        idRow.Text = sub.Name + " · → " + subIncoming;
                        idRow.Foreground = accent;
                    }
                }
                else
                {
                    post.Fill = borderSubtle;
                    lamella.Visibility = Visibility.Collapsed;
                    letter.Foreground = textDisabled;

                    if (idRow != null)
                    {
                        idRow.Text = string.Empty;
                    }
                }
            }
        }

        private void ApplyCellVisual(CellVisual visual)
        {
            // 실물 표현 구획은 전용 칠하기로 — Generic만 아래 공통 경로를 탄다.
            if (visual.Kind == SlotMapSectionKind.WaferEdge)
            {
                this.ApplyWaferVisual(visual);
                return;
            }

            if (visual.Kind == SlotMapSectionKind.PinStub)
            {
                this.ApplyStubVisual(visual);
                return;
            }

            if (visual.Kind == SlotMapSectionKind.LamellaPost)
            {
                this.ApplyLamellaVisual(visual);
                return;
            }

            SlotMapCell cell = visual.Cell;
            // staged는 액센트 채움, clicked는 채움색을 건드리지 않는 약한
            // 외곽선으로 서로 독립적으로 표시한다.
            bool staged;
            bool clicked;
            this.GetCellState(cell, out staged, out clicked);
            bool selected = staged || clicked;
            bool interactive = (this.AllowSelection || this.EnableDragOut)
                    && (cell.Filled || this.CellHasPreview(cell))
                    && !string.IsNullOrEmpty(cell.Key);

            visual.Outer.Cursor = interactive ? Cursors.Hand : null;

            Brush accent = (Brush)this.FindResource("Brush.Accent");
            // 클릭 선택은 스테이징 액센트와 확실히 구분되는 경고색 외곽선으로 보인다.
            Brush clickAccent = (Brush)this.FindResource("Brush.Warning");
            Brush borderBrush = (Brush)this.FindResource("Brush.Border");
            Brush borderSubtle = (Brush)this.FindResource("Brush.BorderSubtle");
            Brush surface = (Brush)this.FindResource("Brush.Surface");
            Brush neutral = (Brush)this.FindResource("Brush.NeutralBackground");
            Brush info = (Brush)this.FindResource("Brush.InfoBackground");
            Brush textPrimary = (Brush)this.FindResource("Brush.TextPrimary");
            Brush textDisabled = (Brush)this.FindResource("Brush.DisabledText");

            // 이 셀에 걸린 미리보기 존재 여부(번호 칩 하이라이트 판정용).
            bool previewed = this.CellHasPreview(cell);

            // 계획된 웨이퍼은 옅은 표면 틴트를 쓴다. LCC도 단일 슬롯과 같은
            // 액센트 실선 테두리를 쓰므로 표시 문법이 일관된다.
            visual.Outer.Background = previewed ? info : Brushes.Transparent;

            // 클릭 선택은 채움색을 바꾸지 않고 대비가 높은 색 테두리를 덧대며,
            // 번호 배지도 같은 색으로 채운다. 스테이징/미리보기는 액센트를 유지한다.
            Brush emphasis = clicked ? clickAccent : accent;
            visual.LabelText.Text = clicked && previewed ? "✓ " + cell.Label : cell.Label;

            // 번호 배지 — 클릭 선택도 스테이징과 같은 자리에서 색으로 말한다.
            this.ApplyNumberBadge(visual, staged, clicked, previewed, info,
                    (Brush)this.FindResource("Brush.InfoText"));

            if (cell.SubCells == null)
            {
                string incoming = this.SingleCellPreview(cell);

                // 단일 수납 토큰 — 채움(색 바)/빈 틀/미리보기 세 상태.
                if (cell.Filled)
                {
                    Brush custom = ChipColorHelper.TryCreateBrush(cell.Color);
                    visual.Token.Background = custom != null
                            ? custom
                            : (Brush)this.FindResource("Brush.SelectedBackground");
                    visual.Token.BorderBrush = selected ? emphasis : borderBrush;
                    // 글자는 토큰 배경(Lot 색)에서 대비색을 유도한다 —
                    // 다크 테마에서도 밝은 배경 위에 어두운 글자가 나온다.
                    visual.WaferText.Foreground = this.DeriveTextBrush(cell.Color, textPrimary);
                    visual.WaferText.Text = cell.WaferId;
                }
                else if (!string.IsNullOrEmpty(incoming))
                {
                    // 들어올 웨이퍼 ID를 그대로 보여준다 — "이 자리에 이게 온다".
                    visual.Token.Background = info;
                    visual.Token.BorderBrush = accent;
                    visual.WaferText.Foreground = accent;
                    visual.WaferText.Text = clicked ? "✓ " + incoming : "→ " + incoming;
                }
                else
                {
                    visual.Token.Background = neutral;
                    visual.Token.BorderBrush = borderSubtle;
                    visual.WaferText.Text = string.Empty;
                }

                return;
            }

            // 복합 수납 — 틀은 중립(미니 행이 상태를 나타냄), 선택만 테두리로.
            visual.Token.Background = surface;
            visual.Token.BorderBrush = selected || previewed
                    ? emphasis
                    : (cell.Filled ? borderBrush : borderSubtle);

            for (int index = 0; index < cell.SubCells.Count; index++)
            {
                SlotMapSubCell sub = cell.SubCells[index];
                Border dot = visual.DotBorders[index];
                TextBlock letter = visual.DotTexts[index];
                Border markerEdge = visual.MarkerEdges[index];
                // 배지(한 줄) 레이아웃엔 웨이퍼 ID 글씨가 없다 — subWafer은 null일 수 있다.
                TextBlock subWafer = index < visual.SubWaferTexts.Count
                        ? visual.SubWaferTexts[index]
                        : null;

                string subIncoming = null;

                if (string.IsNullOrEmpty(sub.WaferId) && this.previewMap != null)
                {
                    this.previewMap.TryGetValue(SubPreviewKey(cell.Key, sub.Name), out subIncoming);
                }

                if (!string.IsNullOrEmpty(sub.WaferId))
                {
                    // 하위 자리 색 → 셀 색 → 토큰 기본색 순 폴백 (Lot별 구분).
                    Brush custom = ChipColorHelper.TryCreateBrush(
                            string.IsNullOrEmpty(sub.Color) ? cell.Color : sub.Color);
                    dot.Background = custom != null
                            ? custom
                            : (Brush)this.FindResource("Brush.SelectedBackground");
                    dot.BorderBrush = borderBrush;
                    this.ConfigureMarkerEdge(markerEdge, sub.Marker, false);
                    // 도트 글자는 도트 배경(Lot 색)에서 대비색 유도. 옆의
                    // 웨이퍼 ID 텍스트는 셀 표면(Surface) 위라 기본 텍스트색.
                    letter.Foreground = this.DeriveTextBrush(
                            string.IsNullOrEmpty(sub.Color) ? cell.Color : sub.Color, textPrimary);

                    if (subWafer != null)
                    {
                        subWafer.Foreground = textPrimary;
                        subWafer.Text = sub.WaferId;
                    }
                }
                else if (!string.IsNullOrEmpty(subIncoming))
                {
                    // 들어올 칩을 계획된 핑거 자리에 표시한다(배지는 글자 강조).
                    dot.Background = info;
                    dot.BorderBrush = accent;
                    letter.Foreground = accent;
                    this.ConfigureMarkerEdge(
                            markerEdge, this.SubPreviewMarker(cell.Key, sub.Name), true);

                    if (subWafer != null)
                    {
                        subWafer.Foreground = accent;
                        subWafer.Text = "→ " + subIncoming;
                    }
                }
                else
                {
                    dot.Background = neutral;
                    dot.BorderBrush = borderSubtle;
                    letter.Foreground = textDisabled;
                    this.ConfigureMarkerEdge(markerEdge, string.Empty, false);

                    if (subWafer != null)
                    {
                        subWafer.Foreground = textDisabled;
                        subWafer.Text = string.Empty;
                    }
                }
            }
        }

        // 이 셀의 빈 자리에 걸린 미리보기가 하나라도 있는가.
        private bool CellHasPreview(SlotMapCell cell)
        {
            if (this.previewMap == null || this.previewMap.Count == 0 || string.IsNullOrEmpty(cell.Key))
            {
                return false;
            }

            if (cell.SubCells == null)
            {
                return !cell.Filled && this.previewMap.ContainsKey(cell.Key);
            }

            foreach (SlotMapSubCell sub in cell.SubCells)
            {
                if (string.IsNullOrEmpty(sub.WaferId)
                        && this.previewMap.ContainsKey(SubPreviewKey(cell.Key, sub.Name)))
                {
                    return true;
                }
            }

            return false;
        }

        // 단일 셀의 미리보기 유입 ID (없으면 null).
        private string SingleCellPreview(SlotMapCell cell)
        {
            if (this.previewMap == null || cell.Filled || string.IsNullOrEmpty(cell.Key))
            {
                return null;
            }

            string incoming;
            this.previewMap.TryGetValue(cell.Key, out incoming);
            return incoming;
        }

        // 복합 셀의 미리보기 삽입 위치 (없으면 빈 문자열).
        private string SubPreviewMarker(string cellKey, string subName)
        {
            if (this.previewMarkerMap == null || string.IsNullOrEmpty(cellKey))
            {
                return string.Empty;
            }

            string marker;
            this.previewMarkerMap.TryGetValue(SubPreviewKey(cellKey, subName), out marker);
            return marker ?? string.Empty;
        }
    }
}
