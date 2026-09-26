using System;

namespace Modern.Lab.WinForms.Controls.Display
{
    /// <summary>
    /// <see cref="ModernFieldList"/>의 필드 하나 — 캡션과 값을 읽어 올
    /// 컬럼/속성 이름의 쌍. 폼이 DefineFields로 배치를 정하고, SetRow가
    /// Member 이름으로 행에서 값을 꺼낸다.
    ///
    /// ColumnSpan을 주면 그 필드가 여러 열을 차지한다 — 제품명·설명처럼
    /// 값이 긴 필드를 한 줄 전체로 넓히는 용도다.
    ///
    /// IsLink를 켜면 값이 링크(액센트색, 마우스 오버 시 밑줄·손 커서)로 그려지고
    /// 클릭하면 <see cref="ModernFieldList.FieldLinkClick"/>가 난다 — 그리드의
    /// <c>GridColumnKind.Link</c> 컬럼과 같은 역할(의뢰 번호 → 의뢰서 팝업 등).
    ///
    /// IsBadge를 켜면 값 자리에 상태 배지가 놓인다 — 색은 값에서 유도되므로
    /// (<see cref="ModernStatusBadge.ColorValue"/>) 같은 값이면 그리드 배지와 색이 맞는다.
    ///
    /// 캡션을 생략하면(멤버만 주는 생성자) 그리드 컬럼과 같은 용어사전
    /// (<see cref="Modern.Lab.Controls.Wpf.Data.GridCaptionCatalog"/> →
    /// <c>ModernDataGridColumn.CaptionResolver</c> → 폴백 "EQP_ID" → "Eqp Id")에서
    /// 캡션을 읽는다 — 그리드 헤더와 상세 카드의 어휘가 한 사전에서 나온다.
    /// </summary>
    public sealed class ModernFieldDefinition
    {
        /// <summary>
        /// 캡션을 용어사전에서 읽는 필드를 만든다 (한 열 차지, 2026-08-29 추가) —
        /// <c>new ModernDataGridColumn("EQP_ID")</c>와 같은 규칙.
        /// </summary>
        /// <param name="member">값을 읽어 올 컬럼/속성 이름 — 캡션도 이 이름으로 해석한다.</param>
        public ModernFieldDefinition(string member)
            : this(ResolveCaption(member), member, 1)
        {
        }

        /// <summary>캡션을 용어사전에서 읽고 열 병합 폭을 지정해 필드를 만든다 (2026-08-29 추가).</summary>
        /// <param name="member">값을 읽어 올 컬럼/속성 이름 — 캡션도 이 이름으로 해석한다.</param>
        /// <param name="columnSpan">차지할 열 수 (1 이상).</param>
        public ModernFieldDefinition(string member, int columnSpan)
            : this(ResolveCaption(member), member, columnSpan)
        {
        }

        /// <summary>필드를 만든다 (한 열 차지).</summary>
        /// <param name="caption">캡션(위에 표시되는 작은 회색 텍스트).</param>
        /// <param name="member">값을 읽어 올 컬럼/속성 이름.</param>
        public ModernFieldDefinition(string caption, string member)
            : this(caption, member, 1)
        {
        }

        /// <summary>열 병합 폭을 지정해 필드를 만든다.</summary>
        /// <param name="caption">캡션(위에 표시되는 작은 회색 텍스트).</param>
        /// <param name="member">값을 읽어 올 컬럼/속성 이름.</param>
        /// <param name="columnSpan">차지할 열 수 (1 이상). 남은 열보다 크면 줄의 끝까지 채운다.</param>
        public ModernFieldDefinition(string caption, string member, int columnSpan)
        {
            this.Caption = caption ?? string.Empty;
            this.Member = member ?? string.Empty;
            this.ColumnSpan = Math.Max(1, columnSpan);
            this.Format = Modern.Lab.Controls.Wpf.Data.AutoColumns.IsTime(this.Member)
                    ? Modern.Lab.Controls.Wpf.Data.AutoColumns.TimeFormat
                    : null;
        }

        /// <summary>캡션(위에 표시되는 작은 회색 텍스트).</summary>
        public string Caption { get; private set; }

        /// <summary>값을 읽어 올 컬럼/속성 이름.</summary>
        public string Member { get; private set; }

        /// <summary>차지할 열 수 (기본 1).</summary>
        public int ColumnSpan { get; private set; }

        /// <summary>
        /// 값을 링크로 그릴지 (기본 false, 2026-08-29 추가). 켜면 값이 액센트색이고 클릭 시
        /// <see cref="ModernFieldList.FieldLinkClick"/>가 난다. 값이 비어("-") 있으면 링크가 아니다.
        /// </summary>
        public bool IsLink { get; set; }

        /// <summary>
        /// 값을 상태 배지로 그릴지 (기본 false, 2026-09-15 추가). 켜면 값 자리에
        /// <see cref="ModernStatusBadge"/>가 놓이고 배경색은 값에서 유도된다
        /// (<see cref="ModernStatusBadge.ColorValue"/> — 그리드/트리 배지의 <c>BadgeAutoColor</c>와
        /// 같은 규칙이라 같은 값이면 표와 카드의 색이 맞는다). 값이 비면 배지 대신 "-"를 그린다.
        /// 해시 자동색은 값을 구분해 줄 뿐 의미를 주지 않으므로, 눈에 띄어야 하는 값은
        /// <see cref="BadgeAccentValues"/>로 따로 뺀다.
        ///
        /// 이 값은 <see cref="ModernFieldList.DefineFields"/> 시점에 읽힌다 — 카드에 넘긴 뒤에 켜면
        /// 반영되지 않는다. 매번 읽는 <see cref="IsLink"/>와 다른 점이다.
        /// </summary>
        public bool IsBadge { get; set; }

        /// <summary>
        /// 진한 오류 채움 + 테두리로 강조할 값 목록 (세미콜론/쉼표 구분, 2026-09-15 추가).
        /// <see cref="ModernStatusBadge.AccentValues"/>에 그대로 전달하므로 그리드 배지의
        /// <c>BadgeAccentValues</c>와 같은 문법이다. <see cref="IsBadge"/>일 때만 쓰인다.
        /// </summary>
        public string BadgeAccentValues { get; set; }

        /// <summary>
        /// 배지 테두리 회전(코멧 빛띠)을 켤 값 목록 (세미콜론/쉼표 구분, 2026-09-15 추가).
        /// <see cref="ModernStatusBadge.SpinValues"/>에 그대로 전달하므로 그리드 컬럼의
        /// <c>BadgeSpinValues</c>와 같은 문법이다. <see cref="IsBadge"/>일 때만 쓰인다.
        /// </summary>
        public string BadgeSpinValues { get; set; }

        /// <summary>
        /// 값 표시 형식 (2026-09-26 추가) — 그리드 컬럼의 <c>ModernDataGridColumn.Format</c>과 같은 문법·규칙이다
        /// (<c>"N0"</c>, <c>"yyyy-MM-dd"</c> 등). 서버가 날짜·숫자를 문자열로 보내도 해석해 적용하고,
        /// 해석이나 형식 적용에 실패하면 원래 값을 그대로 보여 준다. 이름이 <c>_TM</c>으로 끝나는 필드는
        /// 그리드와 같이 <c>yyyy-MM-dd HH:mm:ss</c>가 기본값이다. 비우면 서식 없이 표시한다.
        /// <see cref="ModernFieldList.SetRow"/>가 값을 읽을 때마다 적용된다.
        /// </summary>
        public string Format { get; set; }

        private static string ResolveCaption(string member)
        {
            return Modern.Lab.Controls.Wpf.Data.ModernDataGridColumn.ResolveCaption(member ?? string.Empty);
        }
    }
}
