using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Modern.Lab.Theming;
using Modern.Lab.WinForms.Rendering;

namespace Modern.Lab.WinForms.Controls.Display
{
    /// <summary>
    /// 단건 상세 필드 목록 — Lot 정보 카드처럼 "한 건의 레코드"를 캡션/값
    /// 쌍으로 보여준다. 레거시 "괘선 상세 표"(캡션 셀 회색 배경 +
    /// TableLayoutPanel + 라벨 2N개, 또는 ModernDetailTable)의 모던 대안으로,
    /// **캡션(작은 회색) 위 + 값(SemiBold) 아래**의 스택형 배치를 쓴다 —
    /// 괘선과 셀 배경 없이 여백과 굵기로 위계를 만든다 (DevExpress Detail
    /// View / Fluent 설정 화면의 관례).
    ///
    /// 사용법 (수동형 데이터 계약 — 폼이 조회해서 넣는다):
    ///   fieldLotInfo.Columns = 2;
    ///   fieldLotInfo.DefineFields(
    ///       new ModernFieldDefinition("Product", "MODEL_ID"), ...);
    ///   fieldLotInfo.SetRow(row);      // DataRow — 필드의 Member 컬럼을 읽는다
    ///   fieldLotInfo.ClearValues();    // 전부 "-"
    ///
    /// 배지 필드(IsBadge, 2026-09-15): 값 자리에 ModernStatusBadge가 놓인다 — 배경색은 값에서
    /// 유도되므로(ColorValue) 같은 값이면 그리드 배지와 색이 맞고, BadgeAccentValues로 강조할 값을,
    /// BadgeSpinValues로 테두리가 도는 값을 선언한다. 값이 비면 배지 대신 "-"를 그린다.
    ///
    /// 링크 필드(IsLink, 2026-08-29): 값이 액센트색으로 그려지고 마우스 오버 시 밑줄·손 커서,
    /// 클릭하면 FieldLinkClick(Member, Value)가 난다 — 그리드의 Link 컬럼과 같은 역할이라
    /// 의뢰 번호처럼 표에서 링크인 값이 상세 카드에서도 같은 팝업을 연다. 빈 값("-")은 링크가 아니다.
    ///
    /// 표시 전용이라 ModernLabel과 같은 GDI+ 직접 그리기 컨트롤이다 —
    /// 폼당 라벨 2N개(WPF 섬 2N개)를 쓰던 자리를 컨트롤 1개로 줄인다.
    /// 값이 비면 "-"로 표기한다. 행 높이는 컨트롤 Size를 행 수로 등분한다.
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ModernFieldList : Control
    {
        // 캡션과 값 사이 간격/캡션 줄 높이 — 스택형 쌍의 내부 리듬.
        private const int captionHeight = 17;

        // 셀 좌우 여백.
        private const int cellPadding = 2;

        // 배지 필드의 배지 높이 — 값 영역이 이보다 낮으면 값 영역에 맞춘다.
        private const int badgeHeight = 24;

        private readonly List<ModernFieldDefinition> definitions;
        private readonly List<string> values;

        // 배지 필드의 자식 배지 — 배지가 아닌 자리는 null이다. 배지 시각을 여기서 다시
        // 그리지 않고 ModernStatusBadge를 그대로 얹는다(동작의 출처는 하나).
        private readonly List<ModernStatusBadge> badges;

        // 배지 필드가 하나라도 있는지 — 없으면 동기화 자체를 건너뛴다(기존 화면 비용 0).
        private bool hasBadges;

        // 지금 자식 배지가 어떤 정의로 만들어졌는지 — 정의가 그대로면 다시 만들지 않는다.
        private readonly List<string> badgeKeys;

        private int columns;
        private double fontWidthRatio;

        // 마우스가 올라가 있는 링크 필드 — 없으면 -1. 밑줄과 손 커서의 기준.
        private int hoverIndex = -1;

        /// <summary>적절한 기본 크기로 컨트롤을 생성한다.</summary>
        public ModernFieldList()
        {
            this.SetStyle(
                    ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                    true);

            this.definitions = new List<ModernFieldDefinition>();
            this.values = new List<string>();
            this.badges = new List<ModernStatusBadge>();
            this.badgeKeys = new List<string>();
            this.columns = 2;
            this.fontWidthRatio = 0d;

            this.Size = new Size(400, 160);
            this.TabStop = false;
            this.BackColor = Color.Transparent;
        }

        /// <summary>
        /// 링크 필드(<see cref="ModernFieldDefinition.IsLink"/>)의 값을 클릭했을 때 — 인자에 Member와 값.
        /// 값이 비어("-") 있으면 나지 않는다.
        /// </summary>
        [Category("Modern")]
        [Description("Raised when the value of a link field (IsLink) is clicked")]
        public event EventHandler<ModernFieldLinkClickEventArgs> FieldLinkClick;

        /// <summary>열 수 (기본 2). 필드는 왼쪽→오른쪽, 위→아래로 채워진다.</summary>
        [Category("Modern")]
        [Description("Number of columns — fields flow left to right, top to bottom")]
        [DefaultValue(2)]
        public int Columns
        {
            get { return this.columns; }
            set
            {
                this.columns = Math.Max(1, value);
                this.SyncBadges();
                this.Invalidate();
            }
        }

        /// <summary>
        /// 장평(글자 가로 비율) 재정의. 0 = 전역(ModernTheme.FontWidthRatio) 사용.
        /// </summary>
        [Category("Modern")]
        [Description("Font width ratio override — 0 = use global, allowed 0.8~1.2")]
        [DefaultValue(0d)]
        public double FontWidthRatio
        {
            get { return this.fontWidthRatio; }
            set
            {
                this.fontWidthRatio = value;
                this.SyncBadges();
                this.Invalidate();
            }
        }

        /// <summary>
        /// 기존 `.Designer.cs`가 직렬화해 둔 `xxx.Child = null;` 라인이 계속
        /// 컴파일되도록 남겨 둔 무동작 속성 (drop-in 계약).
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object Child
        {
            get { return null; }
            set { }
        }

        /// <summary>색은 토큰이 결정한다 — 속성 그리드에서 숨긴다.</summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; }
        }

        /// <summary>타이포그래피는 타입 램프 토큰이 결정한다 — 속성 그리드에서 숨긴다.</summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override Font Font
        {
            get { return base.Font; }
            set { base.Font = value; }
        }

        /// <summary>
        /// 필드 배치를 정의한다 — 캡션과 값을 읽어 올 컬럼 이름의 쌍.
        /// 값은 전부 "-"로 초기화된다.
        /// </summary>
        public void DefineFields(params ModernFieldDefinition[] fields)
        {
            this.definitions.Clear();
            this.values.Clear();

            if (fields != null)
            {
                foreach (ModernFieldDefinition field in fields)
                {
                    if (field != null)
                    {
                        this.definitions.Add(field);
                        this.values.Add(string.Empty);
                    }
                }
            }

            this.RebuildBadges();
            this.SyncBadges();
            this.Invalidate();
        }

        /// <summary>
        /// 한 건의 행에서 각 필드의 Member 컬럼 값을 읽어 채운다.
        /// 없는 컬럼/빈 값은 "-"로 표기한다 (예외 없음 — 계약 룰 3).
        /// </summary>
        public void SetRow(DataRow row)
        {
            // 정의가 없으면 행이 속한 표가 정한다 — 화면은 SetRow만 부르면 되고,
            // 골라 보여 줄 때만 DefineFields로 선언한다 (규칙은 AutoColumns).
            if (this.definitions.Count == 0 && row != null && row.Table != null)
            {
                string[] members = Modern.Lab.Controls.Wpf.Data.AutoColumns.MembersOf(row.Table);
                ModernFieldDefinition[] fields = new ModernFieldDefinition[members.Length];

                for (int index = 0; index < members.Length; index++)
                {
                    fields[index] = new ModernFieldDefinition(members[index]);
                }

                this.DefineFields(fields);
            }

            for (int index = 0; index < this.definitions.Count; index++)
            {
                this.values[index] = ReadCell(row, this.definitions[index].Member);
            }

            this.SyncBadges();
            this.Invalidate();
        }

        /// <summary>특정 필드의 값을 직접 지정한다 (행에 없는 파생 값용).</summary>
        public void SetValue(string member, string value)
        {
            for (int index = 0; index < this.definitions.Count; index++)
            {
                if (string.Equals(this.definitions[index].Member, member, StringComparison.Ordinal))
                {
                    this.values[index] = value ?? string.Empty;
                }
            }

            this.SyncBadges();
            this.Invalidate();
        }

        /// <summary>모든 값을 "-"로 되돌린다 (선택 해제/초기화).</summary>
        public void ClearValues()
        {
            for (int index = 0; index < this.values.Count; index++)
            {
                this.values[index] = string.Empty;
            }

            this.SyncBadges();
            this.Invalidate();
        }

        private static string ReadCell(DataRow row, string member)
        {
            if (row == null || string.IsNullOrEmpty(member)
                    || !row.Table.Columns.Contains(member) || row.IsNull(member))
            {
                return string.Empty;
            }

            return Convert.ToString(row[member]).Trim();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (this.definitions.Count == 0 || this.Width <= 0 || this.Height <= 0)
            {
                return;
            }

            Graphics graphics = e.Graphics;
            double ratio = ModernTheme.ResolveFontWidthRatio(this.fontWidthRatio);
            Rectangle[] valueRects = this.ValueRectangles();

            Color captionColor = Token("Brush.TextSecondary", Color.FromArgb(96, 96, 96));
            Color valueColor = this.Enabled
                ? Token("Brush.TextPrimary", Color.Black)
                : Token("Brush.DisabledText", Color.FromArgb(161, 161, 161));
            Color linkColor = this.Enabled
                ? Token("Brush.Accent", ModernTheme.Accent)
                : valueColor;
            Color linkHoverColor = this.Enabled
                ? Token("Brush.AccentHover", linkColor)
                : valueColor;

            const TextFormatFlags flags = TextFormatFlags.NoPadding
                    | TextFormatFlags.SingleLine
                    | TextFormatFlags.NoPrefix
                    | TextFormatFlags.EndEllipsis;

            // 캡션은 본문 크기 Regular 회색, 값은 같은 크기 SemiBold — 크기가
            // 아니라 굵기·색으로 위계를 만든다 (Fluent Body / Body Strong).
            // 링크 값은 액센트색 + 항상 밑줄 — 마우스를 올리기 전에도 클릭하면 팝업이
            // 열린다는 것을 알 수 있어야 한다(그리드 Link 컬럼·ModernLabel Hyperlink와 같은 체감).
            // 호버는 색만 한 단계 진하게(AccentHover) 바꾸고 밑줄은 그대로 둔다.
            using (Font captionFont = ModernFonts.Create(ModernFonts.BodyDiu, false, FontStyle.Regular))
            using (Font valueFont = ModernFonts.Create(ModernFonts.BodyDiu, true, FontStyle.Regular))
            using (Font linkFont = ModernFonts.Create(ModernFonts.BodyDiu, true, FontStyle.Underline))
            {
                for (int index = 0; index < this.definitions.Count; index++)
                {
                    Rectangle valueRect = valueRects[index];
                    Rectangle captionRect = new Rectangle(
                            valueRect.Left, valueRect.Top - captionHeight, valueRect.Width, captionHeight);

                    ScaledTextRenderer.DrawText(
                            graphics, this.definitions[index].Caption, captionFont,
                            captionRect, captionColor, flags | TextFormatFlags.Bottom, ratio);

                    // 배지 필드의 값은 자식 배지가 그린다 — 캡션만 여기서 그리고 넘어간다.
                    if (this.IsBadgeAt(index))
                    {
                        continue;
                    }

                    string value = this.values[index];
                    bool link = this.IsLinkAt(index);

                    Color textColor = valueColor;

                    if (link)
                    {
                        textColor = index == this.hoverIndex ? linkHoverColor : linkColor;
                    }

                    ScaledTextRenderer.DrawText(
                            graphics, value.Length > 0 ? value : "-",
                            link ? linkFont : valueFont,
                            valueRect, textColor, flags | TextFormatFlags.Top, ratio);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.UpdateHover(this.HitLink(e.Location));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.UpdateHover(-1);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int index = this.HitLink(e.Location);

            if (index < 0)
            {
                return;
            }

            EventHandler<ModernFieldLinkClickEventArgs> handler = this.FieldLinkClick;

            if (handler != null)
            {
                handler(this, new ModernFieldLinkClickEventArgs(
                        this.definitions[index].Member, this.values[index]));
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.SyncBadges();
            this.UpdateHover(-1);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.SyncBadges();
        }

        // 링크로 동작하는 필드인지 — IsLink이고 값이 있고(빈 "-"는 링크가 아니다) 컨트롤이 활성일 때.
        private bool IsLinkAt(int index)
        {
            return this.Enabled
                    && this.definitions[index].IsLink
                    && !this.IsBadgeAt(index)
                    && this.values[index].Length > 0;
        }

        // 좌표가 어느 링크 필드의 값 영역에 있는지 — 없으면 -1.
        private int HitLink(Point location)
        {
            if (this.definitions.Count == 0 || this.Width <= 0 || this.Height <= 0)
            {
                return -1;
            }

            Rectangle[] valueRects = this.ValueRectangles();

            for (int index = 0; index < this.definitions.Count; index++)
            {
                if (this.IsLinkAt(index) && valueRects[index].Contains(location))
                {
                    return index;
                }
            }

            return -1;
        }

        private void UpdateHover(int index)
        {
            if (index == this.hoverIndex)
            {
                return;
            }

            this.hoverIndex = index;
            this.Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            this.Invalidate();
        }

        // 각 필드의 값 영역 — 그리기와 히트테스트가 같은 배치를 쓴다. 열 병합을 고려해
        // (열 위치, 행, 폭)을 먼저 정한다: 남은 열보다 넓은 필드는 다음 줄로 내리고 줄 끝까지 채운다.
        private Rectangle[] ValueRectangles()
        {
            int count = this.definitions.Count;
            int[] cellColumn = new int[count];
            int[] cellRow = new int[count];
            int[] cellSpan = new int[count];

            int cursorColumn = 0;
            int cursorRow = 0;

            for (int index = 0; index < count; index++)
            {
                int span = Math.Min(this.columns, this.definitions[index].ColumnSpan);

                if (cursorColumn > 0 && cursorColumn + span > this.columns)
                {
                    cursorColumn = 0;
                    cursorRow = cursorRow + 1;
                }

                cellColumn[index] = cursorColumn;
                cellRow[index] = cursorRow;
                cellSpan[index] = Math.Min(span, this.columns - cursorColumn);

                cursorColumn = cursorColumn + cellSpan[index];

                if (cursorColumn >= this.columns)
                {
                    cursorColumn = 0;
                    cursorRow = cursorRow + 1;
                }
            }

            int rows = cursorColumn > 0 ? cursorRow + 1 : cursorRow;
            int rowHeight = this.Height / Math.Max(1, rows);
            Rectangle[] rects = new Rectangle[count];

            for (int index = 0; index < count; index++)
            {
                int left = this.Width * cellColumn[index] / this.columns;
                int right = this.Width * (cellColumn[index] + cellSpan[index]) / this.columns;
                int top = cellRow[index] * rowHeight;

                rects[index] = new Rectangle(
                        left + cellPadding, top + captionHeight,
                        right - left - cellPadding * 2,
                        Math.Max(0, rowHeight - captionHeight));
            }

            return rects;
        }

        // 배지로 그려지는 필드인지 — IsBadge이고 값이 있을 때. 값이 비면 배지 대신 "-"를 그린다.
        private bool IsBadgeAt(int index)
        {
            return this.hasBadges
                    && index < this.badges.Count
                    && this.badges[index] != null
                    && this.values[index].Length > 0;
        }

        // 정의가 바뀌면 자식 배지를 다시 만든다. 값에 따라 달라지는 것(Text·ColorValue)은
        // SyncBadges가 맡고, 정의에 매인 것(강조·회전 값 목록)만 여기서 준다.
        private void RebuildBadges()
        {
            string[] keys = new string[this.definitions.Count];

            for (int index = 0; index < this.definitions.Count; index++)
            {
                ModernFieldDefinition field = this.definitions[index];

                keys[index] = field.IsBadge
                        ? field.Member + "|" + (field.BadgeAccentValues ?? string.Empty)
                                + "|" + (field.BadgeSpinValues ?? string.Empty)
                        : string.Empty;
            }

            // 조회마다 FieldDefinitions.Apply 를 부르는 것이 이 카드의 표준 사용법이다.
            // 정의가 그대로인데 자식을 버리고 다시 만들면 회전 위상이 0으로 튀고 핸들이
            // 매 갱신마다 다시 생긴다 — 같은 지문이면 쓰던 배지를 그대로 둔다.
            if (this.SameBadgeKeys(keys))
            {
                return;
            }

            foreach (ModernStatusBadge existing in this.badges)
            {
                if (existing != null)
                {
                    this.Controls.Remove(existing);
                    existing.Dispose();
                }
            }

            this.badges.Clear();
            this.hasBadges = false;

            foreach (ModernFieldDefinition field in this.definitions)
            {
                ModernStatusBadge badge = null;

                if (field.IsBadge)
                {
                    badge = new ModernStatusBadge();
                    badge.TabStop = false;
                    badge.Visible = false;

                    // 생성자 기본 Text("Status")를 지운다 — 첫 값이 마침 "Status"면
                    // 아래 SyncBadges 의 중복 대입 가드에 걸려 ColorValue 가 비게 된다.
                    badge.Text = string.Empty;
                    badge.AccentValues = field.BadgeAccentValues ?? string.Empty;
                    badge.SpinValues = field.BadgeSpinValues ?? string.Empty;
                    this.Controls.Add(badge);
                    this.hasBadges = true;
                }

                this.badges.Add(badge);
            }

            this.badgeKeys.Clear();
            this.badgeKeys.AddRange(keys);
        }

        // 지금 자식 배지가 이 정의로 만들어진 것인지 — 지문이 하나라도 다르면 다시 만든다.
        private bool SameBadgeKeys(string[] keys)
        {
            if (this.badgeKeys.Count != keys.Length || this.badges.Count != keys.Length)
            {
                return false;
            }

            for (int index = 0; index < keys.Length; index++)
            {
                if (!string.Equals(this.badgeKeys[index], keys[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        // 값과 배치를 자식 배지에 옮긴다. 색은 값에서 유도되므로(ColorValue) 같은 값이면
        // 그리드 배지와 색이 맞는다. 값이 비면 배지를 숨겨 "-"가 보이게 둔다.
        private void SyncBadges()
        {
            if (!this.hasBadges || this.badges.Count != this.definitions.Count
                    || this.Width <= 0 || this.Height <= 0)
            {
                return;
            }

            Rectangle[] valueRects = this.ValueRectangles();

            for (int index = 0; index < this.badges.Count; index++)
            {
                ModernStatusBadge badge = this.badges[index];

                if (badge == null)
                {
                    continue;
                }

                string value = this.values[index];

                if (value.Length == 0)
                {
                    badge.Visible = false;
                    continue;
                }

                // 같은 값을 다시 넣지 않는다 — Text 대입이 회전 판정과 다시 그리기를 부른다.
                if (!string.Equals(badge.Text, value, StringComparison.Ordinal))
                {
                    badge.Text = value;
                    badge.ColorValue = value;
                }

                Rectangle valueRect = valueRects[index];

                badge.FontWidthRatio = this.fontWidthRatio;
                badge.Enabled = this.Enabled;
                badge.Bounds = new Rectangle(
                        valueRect.Left,
                        valueRect.Top,
                        valueRect.Width,
                        Math.Min(valueRect.Height, badgeHeight));
                badge.Visible = true;
            }
        }

        private static Color Token(string key, Color fallback)
        {
            return ModernTokenColors.Get(key, fallback);
        }
    }
}
