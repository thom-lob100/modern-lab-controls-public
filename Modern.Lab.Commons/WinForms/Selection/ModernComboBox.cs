using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Modern.Lab.WinForms.Controls.Hosting;

namespace Modern.Lab.WinForms.Controls.Selection
{
    /// <summary>
    /// System.Windows.Forms.ComboBox의 드롭인 대체 컨트롤
    /// (WPF ModernComboBoxControl을 ElementHost로 호스팅).
    ///
    /// 호환 멤버: DataSource(DataTable/DataView/IList/IEnumerable),
    /// DisplayMember, ValueMember, SelectedValue, SelectedItem, SelectedIndex,
    /// Items, SelectedIndexChanged, Enabled, DropDownStyle(DropDownList =
    /// 선택 전용; DropDown/Simple = 입력하면 한국어 초성 매칭으로 목록이
    /// 필터링되는 검색형 콤보), CharacterCasing(편집 가능 콤보에서 입력 즉시
    /// 대문자/소문자 강제 — ModernTextBox와 같은 의미).
    ///
    /// 계약 동작 (docs/design-notes.md 6-1절):
    /// - SelectedValue는 DataSource보다 먼저 할당해도 된다; 값이 보류되었다가
    ///   데이터가 도착하면 적용된다(규칙 3).
    /// - DataSource를 할당하면 보류 값이 없을 때 첫 행이 선택되어 WinForms
    ///   ComboBox 동작과 일치하며, 할당 한 번당 SelectedIndexChanged가
    ///   정확히 한 번 발생한다.
    /// - null/빈 데이터는 빈 목록으로 렌더링되며 절대 예외를 던지지 않는다.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public class ModernComboBox : WpfElementHostBase<Modern.Lab.Controls.Wpf.Selection.ModernComboBoxControl>
    {
        // DataSource가 할당되지 않았을 때 사용하는 Items 컬렉션 (combo.Items.Add(...)).
        private readonly ObservableCollection<object> manualItems;

        private object dataSource;
        private object pendingSelectedValue;
        private bool hasPendingSelectedValue;
        private bool suppressSelectionChanged;

        // 디자인 타임 WPF 생성이 실패한 경우(Wpf == null)에도 속성 그리드가
        // 동작하도록 하는 폴백 저장소.
        private string fallbackDisplayMember;
        private string fallbackValueMember;
        private string fallbackItemColorPath;
        private string fallbackPlaceholder;
        private ComboBoxStyle fallbackDropDownStyle;
        private bool fallbackRequired;
        private bool fallbackHighlight;
        private CharacterCasing fallbackCharacterCasing;
        private string fallbackAllowedCharacters = string.Empty;

        // 자동완성 3종(WinForms 호환) — 편집 가능 여부(DropDownStyle)와는 별개 축이다.
        private AutoCompleteMode autoCompleteMode;
        private AutoCompleteSource autoCompleteSource;
        private AutoCompleteStringCollection autoCompleteCustomSource;

        /// <summary>선택이 바뀔 때 발생한다(WinForms 호환 이름).</summary>
        public event EventHandler SelectedIndexChanged;

        /// <summary>드롭다운 목록이 열리기 직전에 발생한다(WinForms ComboBox.DropDown 호환).</summary>
        public event EventHandler DropDown;

        /// <summary>드롭다운 목록이 닫힌 직후에 발생한다(WinForms ComboBox.DropDownClosed 호환).</summary>
        public event EventHandler DropDownClosed;

        /// <summary>적절한 기본 크기로 컨트롤을 생성한다.</summary>
        public ModernComboBox()
        {
            this.Size = new Size(200, 32);
            this.manualItems = new ObservableCollection<object>();
            this.fallbackDisplayMember = string.Empty;
            this.fallbackValueMember = string.Empty;
            this.fallbackItemColorPath = string.Empty;
            this.fallbackPlaceholder = string.Empty;
            this.fallbackRequired = false;

            // 기본값은 System.Windows.Forms.ComboBox와 동일: DropDown(편집 가능) +
            // AutoCompleteMode.None(입력해도 목록을 좁히지 않는 자유 입력).
            this.fallbackDropDownStyle = ComboBoxStyle.DropDown;
            this.autoCompleteMode = AutoCompleteMode.None;
            this.autoCompleteSource = AutoCompleteSource.None;
            this.fallbackCharacterCasing = CharacterCasing.Normal;

            if (this.Wpf != null)
            {
                this.Wpf.ItemsSource = this.manualItems;
                this.Wpf.IsEditable = true;
                this.Wpf.SelectionChanged += this.OnWpfSelectionChanged;
                this.Wpf.DropDownOpened += this.OnWpfDropDownOpened;
                this.Wpf.DropDownClosedEvent += this.OnWpfDropDownClosed;

                // 늦은 Items.Add에서 보류 SelectedValue를 재시도하기 위한 구독.
                // WPF 컨트롤 쪽 구독(ItemsSource 할당 시)이 먼저 등록되므로, 이
                // 핸들러가 돌 때는 내부 필터 목록이 이미 새 항목을 반영한 뒤다.
                // 컬렉션은 래퍼가 소유하므로 별도 해제는 필요 없다.
                this.manualItems.CollectionChanged += this.OnManualItemsChanged;
            }

            this.ApplyAutoComplete();
        }

        /// <summary>
        /// 데이터 소스: DataTable, DataView, IList 또는 임의의 IEnumerable.
        /// 할당하면 선택이 초기화되고, 보류 중인 SelectedValue가 있으면 적용되며
        /// (없으면 첫 행 선택), SelectedIndexChanged가 한 번 발생한다.
        /// null을 할당하면 수동 Items 컬렉션으로 폴백한다.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object DataSource
        {
            get
            {
                return this.dataSource;
            }
            set
            {
                this.dataSource = value;

                if (this.Wpf == null)
                {
                    return;
                }

                this.suppressSelectionChanged = true;

                try
                {
                    this.Wpf.SelectedItem = null;
                    this.Wpf.ItemsSource = DataSourceConverter.ToItemsSource(value) ?? this.manualItems;

                    if (this.hasPendingSelectedValue)
                    {
                        this.Wpf.SelectedValue = this.pendingSelectedValue;
                        this.pendingSelectedValue = null;
                        this.hasPendingSelectedValue = false;
                    }
                    else if (this.Wpf.SelectedItem == null)
                    {
                        // WinForms ComboBox와 동일: 기본적으로 첫 행을 선택한다.
                        this.SelectFirstItemIfAny();
                    }
                }
                finally
                {
                    this.suppressSelectionChanged = false;
                }

                this.RaiseSelectedIndexChanged();
            }
        }

        /// <summary>필수 입력 필드 표시 — 필드 왼쪽에 빨간 세로 바를 그린다.</summary>
        [Category("Modern")]
        [Description("Required marker (red vertical bar on the left of the field)")]
        [DefaultValue(false)]
        public bool Required
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.Required;
                }

                return this.fallbackRequired;
            }
            set
            {
                this.fallbackRequired = value;

                if (this.Wpf != null)
                {
                    this.Wpf.Required = value;
                }

                this.InvalidateDesignTimePreview();
            }
        }

        /// <summary>
        /// 강조 표시 — 주목이 필요한 핵심 선택 필드에 액센트색 테두리를
        /// 덧그린다 (Required의 빨간 바와 별개로 함께 쓸 수 있다).
        /// </summary>
        [Category("Modern")]
        [Description("Highlight (accent border) — for key fields that need attention")]
        [DefaultValue(false)]
        public bool Highlight
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.Highlight;
                }

                return this.fallbackHighlight;
            }
            set
            {
                this.fallbackHighlight = value;

                if (this.Wpf != null)
                {
                    this.Wpf.Highlight = value;
                }
            }
        }

        /// <summary>
        /// 아무것도 선택/입력되지 않은 동안 표시되는 힌트 텍스트.
        /// 일관된 API를 위해 ModernTextBox와 같은 속성 이름을 쓴다.
        /// </summary>
        [Category("Modern")]
        [Description("Hint text shown while nothing is selected or entered")]
        [Localizable(true)]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.Placeholder;
                }

                return this.fallbackPlaceholder;
            }
            set
            {
                this.fallbackPlaceholder = value;

                if (this.Wpf != null)
                {
                    this.Wpf.Placeholder = value;
                }

                this.InvalidateDesignTimePreview();
            }
        }

        /// <summary>
        /// 선택 스타일(WinForms 호환 이름과 기본값). DropDown(기본, 편집 가능)은
        /// 입력하는 동안 바인딩된 목록을 필터링하고(초성 검색 포함) 텍스트를
        /// 지우면 선택도 지워진다; DropDownList는 선택 전용; Simple은
        /// DropDown처럼 동작한다.
        /// </summary>
        [Category("Modern")]
        [Description("DropDown (default) / Simple = filter the list by typing (search style) / DropDownList = selection only")]
        [DefaultValue(ComboBoxStyle.DropDown)]
        public ComboBoxStyle DropDownStyle
        {
            get
            {
                return this.fallbackDropDownStyle;
            }
            set
            {
                this.fallbackDropDownStyle = value;

                if (this.Wpf != null)
                {
                    this.Wpf.IsEditable = value != ComboBoxStyle.DropDownList;
                }

                this.InvalidateDesignTimePreview();
            }
        }

        /// <summary>
        /// 편집 가능 콤보(DropDown/Simple)에서 입력 즉시 대문자/소문자 강제 변환
        /// (ModernTextBox와 같은 이름과 의미). 기본 Normal = 변환 없음.
        /// Lot ID처럼 대문자만 존재하는 코드 입력 콤보에 쓴다 —
        /// DropDownList(선택 전용)에서는 입력란이 없어 효과가 없다.
        /// </summary>
        [Category("Modern")]
        [Description("Force upper/lower case as typed in an editable combo — for code/ID input")]
        [DefaultValue(CharacterCasing.Normal)]
        public CharacterCasing CharacterCasing
        {
            get
            {
                if (this.Wpf != null)
                {
                    return ToWinFormsCasing(this.Wpf.CharacterCasing);
                }

                return this.fallbackCharacterCasing;
            }
            set
            {
                this.fallbackCharacterCasing = value;

                if (this.Wpf != null)
                {
                    this.Wpf.CharacterCasing = ToWpfCasing(value);
                }
            }
        }

        /// <summary>편집 입력의 허용 문자. 빈 문자열이면 제한하지 않는다.</summary>
        [Category("Modern")]
        [Description("Characters allowed in editable input; empty means unrestricted")]
        [DefaultValue("")]
        public string AllowedCharacters
        {
            get { return this.Wpf != null ? this.Wpf.AllowedCharacters : this.fallbackAllowedCharacters; }
            set
            {
                this.fallbackAllowedCharacters = value ?? string.Empty;
                if (this.Wpf != null) { this.Wpf.AllowedCharacters = this.fallbackAllowedCharacters; }
            }
        }

        // WinForms와 WPF의 CharacterCasing enum은 값 순서가 다르다
        // (WinForms: Normal/Upper/Lower, WPF: Normal/Lower/Upper) — 명시 매핑.
        private static System.Windows.Controls.CharacterCasing ToWpfCasing(CharacterCasing casing)
        {
            if (casing == CharacterCasing.Upper)
            {
                return System.Windows.Controls.CharacterCasing.Upper;
            }

            if (casing == CharacterCasing.Lower)
            {
                return System.Windows.Controls.CharacterCasing.Lower;
            }

            return System.Windows.Controls.CharacterCasing.Normal;
        }

        private static CharacterCasing ToWinFormsCasing(System.Windows.Controls.CharacterCasing casing)
        {
            if (casing == System.Windows.Controls.CharacterCasing.Upper)
            {
                return CharacterCasing.Upper;
            }

            if (casing == System.Windows.Controls.CharacterCasing.Lower)
            {
                return CharacterCasing.Lower;
            }

            return CharacterCasing.Normal;
        }

        /// <summary>
        /// 자동완성 동작(WinForms 호환 이름) — 기본 <c>None</c>.
        ///
        /// <b>편집 가능 여부와는 별개 축</b>이다: 타이핑을 허용할지는
        /// <see cref="DropDownStyle"/>이 정하고, 타이핑이 <b>목록을 좁힐지</b>를
        /// 이 속성이 정한다(원본 ComboBox와 같은 구조).
        ///
        /// - <c>None</c>: 아무것도 하지 않는다(자유 입력).
        /// - <c>Suggest</c>: 입력한 글자로 <b>목록을 좁히고 드롭다운을 연다</b>.
        /// - <c>Append</c>: 목록은 그대로 두고, 입력 뒤에 <b>남은 글자를 옅게 겹쳐</b>
        ///   보여 준다(<c>Tab</c> 또는 <c>→</c>로 확정). 원본처럼 입력란 텍스트를
        ///   실제로 바꾸지는 않는다 — 한글 IME 조합 중에 버퍼를 건드리면 조합이
        ///   깨지기 때문이며, 덕분에 <b>한글 완성문자에서도 영문과 똑같이</b> 뜬다.
        /// - <c>SuggestAppend</c>: 둘 다.
        ///
        /// <see cref="DropDownStyle"/>이 <c>DropDownList</c>(선택 전용)면 이 값은
        /// 효과가 없다 — 원본은 그 조합에서도 목록을 띄우지만, 이 컨트롤은 입력란
        /// 자체가 없기 때문이다.
        /// </summary>
        [Category("Modern")]
        [Description("Auto-complete mode — None keeps the full list, Suggest modes narrow it as you type (editability is DropDownStyle)")]
        [DefaultValue(AutoCompleteMode.None)]
        public AutoCompleteMode AutoCompleteMode
        {
            get
            {
                return this.autoCompleteMode;
            }
            set
            {
                this.autoCompleteMode = value;
                this.ApplyAutoComplete();
            }
        }

        /// <summary>
        /// 자동완성 후보의 원본(WinForms 호환 이름) — 기본 <c>None</c>.
        ///
        /// 이 컨트롤은 <b>항상 목록(ListItems)에서</b> 후보를 찾는다. 콤보의 후보는
        /// 곧 그 콤보에 바인딩된 항목이기 때문이다. 그래서 이 속성은
        /// <b>레거시 코드가 그대로 컴파일되도록 값을 보관</b>하는 역할이며,
        /// <c>ListItems</c> / <c>CustomSource</c> / <c>None</c> 어느 값이어도
        /// 목록 기준 필터링으로 동작한다.
        ///
        /// <c>FileSystem</c>·<c>HistoryList</c> 같은 OS 제공 원본은 지원하지 않는다
        /// (파일 경로 입력이 필요하면 <c>ModernTextBox</c>의 자동완성을 쓴다).
        /// </summary>
        [Category("Modern")]
        [Description("Auto-complete source — the value is kept, but candidates always come from ListItems")]
        [DefaultValue(AutoCompleteSource.None)]
        public AutoCompleteSource AutoCompleteSource
        {
            get
            {
                return this.autoCompleteSource;
            }
            set
            {
                this.autoCompleteSource = value;
                this.ApplyAutoComplete();
            }
        }

        /// <summary>
        /// 사용자 지정 자동완성 후보(WinForms 호환 이름과 타입) — <b>보관 전용</b>.
        ///
        /// 콤보의 후보는 바인딩된 항목이므로 이 컬렉션을 후보로 쓰지 않는다.
        /// 레거시 폼의 <c>AutoCompleteCustomSource.AddRange(...)</c> 줄이 그대로
        /// 컴파일되고 값이 왕복하도록 두는 것이 목적이다. 후보를 바꾸려면
        /// <see cref="DataSource"/>(또는 <see cref="Items"/>)를 바꾼다.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AutoCompleteStringCollection AutoCompleteCustomSource
        {
            get
            {
                return this.autoCompleteCustomSource;
            }
            set
            {
                this.autoCompleteCustomSource = value;
            }
        }

        // 자동완성 설정을 WPF 컨트롤의 두 축으로 옮긴다:
        //  · FilterOnTyping   = 입력이 목록을 좁히고 드롭다운을 여는가 (Suggest)
        //  · AppendCompletion = 입력 뒤에 남은 글자를 옅게 겹쳐 보이는가 (Append)
        // 순서에 관대함: Mode/Source를 어떤 순서로 할당해도 결과가 같다(계약 규칙 3).
        private void ApplyAutoComplete()
        {
            if (this.Wpf == null)
            {
                return;
            }

            bool suggest = this.autoCompleteMode == AutoCompleteMode.Suggest
                    || this.autoCompleteMode == AutoCompleteMode.SuggestAppend;
            bool append = this.autoCompleteMode == AutoCompleteMode.Append
                    || this.autoCompleteMode == AutoCompleteMode.SuggestAppend;

            this.Wpf.FilterOnTyping = suggest;
            this.Wpf.AppendCompletion = append;
        }

        /// <summary>표시 텍스트로 사용되는 컬럼/속성 이름(WinForms 호환).</summary>
        [Category("Modern")]
        [Description("Column/property name used as the display text")]
        [DefaultValue("")]
        public string DisplayMember
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.DisplayMemberPath;
                }

                return this.fallbackDisplayMember;
            }
            set
            {
                this.fallbackDisplayMember = value;

                if (this.Wpf != null)
                {
                    this.Wpf.DisplayMemberPath = value;
                }
            }
        }

        /// <summary>드롭다운 항목 글자색을 결정하는 컬럼/속성 이름 (색 hex 필드).
        /// 비우면 기본색. 항목마다 상태를 색으로 구분할 때 쓴다.</summary>
        [Category("Modern")]
        [Description("Column/property name holding the drop-down item text color (hex)")]
        [DefaultValue("")]
        public string ItemColorPath
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.ItemColorPath;
                }

                return this.fallbackItemColorPath;
            }
            set
            {
                this.fallbackItemColorPath = value;

                if (this.Wpf != null)
                {
                    this.Wpf.ItemColorPath = value;
                }
            }
        }

        /// <summary>값으로 사용되는 컬럼/속성 이름(WinForms 호환).</summary>
        [Category("Modern")]
        [Description("Column/property name used as SelectedValue")]
        [DefaultValue("")]
        public string ValueMember
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.SelectedValuePath;
                }

                return this.fallbackValueMember;
            }
            set
            {
                this.fallbackValueMember = value;

                if (this.Wpf != null)
                {
                    this.Wpf.SelectedValuePath = value;
                }
            }
        }

        /// <summary>
        /// 선택된 항목의 값. DataSource보다 먼저 할당해도 된다(값이 보류되었다가
        /// 데이터가 도착하면 적용된다 — 계약 규칙 3).
        ///
        /// 값은 <b>읽는 시점에</b> 현재 <see cref="ValueMember"/>로 해석한다 —
        /// <c>DataSource</c>를 먼저 할당하고 <c>ValueMember</c>를 나중에 주는
        /// 코드에서도 원본 <c>ListControl</c>처럼 값이 나온다.
        /// <c>ValueMember</c>가 비어 있으면 원본과 같이 항목 자체를 돌려준다.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedValue
        {
            get
            {
                if (this.Wpf == null)
                {
                    return null;
                }

                object item = this.Wpf.SelectedItem;

                if (item == null)
                {
                    return this.Wpf.SelectedValue;
                }

                string member = this.ValueMember;

                if (string.IsNullOrEmpty(member))
                {
                    return item;
                }

                return Modern.Lab.Controls.Wpf.Common.MemberPathReader.Read(item, member);
            }
            set
            {
                if (this.Wpf == null)
                {
                    return;
                }

                if (this.HasBoundItems())
                {
                    this.Wpf.SelectedValue = value;
                    return;
                }

                // 수동 Items 컬렉션에서도 즉시 적용한다 (Items.Add 후 SelectedValue —
                // 문서화된 사용법). 일치 항목이 아직 없으면 보류해 두었다가
                // DataSource 할당 또는 늦은 Items.Add(OnManualItemsChanged)에서
                // 적용한다 — 계약 규칙 3의 순서 내성.
                this.Wpf.SelectedValue = value;

                if (value == null || this.Wpf.SelectedItem != null)
                {
                    this.pendingSelectedValue = null;
                    this.hasPendingSelectedValue = false;
                }
                else
                {
                    this.Wpf.SelectedValue = null;
                    this.pendingSelectedValue = value;
                    this.hasPendingSelectedValue = true;
                }
            }
        }

        /// <summary>현재 선택된 항목(DataTable 소스의 경우 DataRowView).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.SelectedItem;
                }

                return null;
            }
            set
            {
                if (this.Wpf != null)
                {
                    this.Wpf.SelectedItem = value;
                }
            }
        }

        /// <summary>선택된 항목의 인덱스(아무것도 선택되지 않았으면 -1).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.SelectedIndex;
                }

                return -1;
            }
            set
            {
                if (this.Wpf != null)
                {
                    this.Wpf.SelectedIndex = value;
                }
            }
        }

        /// <summary>
        /// 수동 항목 컬렉션 (combo.Items.Add(...)). DataSource가 할당되지 않은
        /// 동안에만 사용되며, DataSource가 우선한다.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList Items
        {
            get { return this.manualItems; }
        }

        /// <summary>
        /// 현재 선택의 표시 텍스트(WinForms ComboBox.Text). setter는
        /// DropDown/Simple 스타일에서 편집 가능 텍스트를 쓰며, DropDownList에서는
        /// 아무 동작도 하지 않는다 — SelectedValue/SelectedIndex로 선택할 것.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get
            {
                if (this.Wpf != null)
                {
                    return this.Wpf.SelectionText;
                }

                return string.Empty;
            }
            set
            {
                if (this.Wpf != null)
                {
                    this.Wpf.SetEditableText(value);
                }
            }
        }

        /// <summary>
        /// 드롭다운을 멀티컬럼(코드+명칭 등)으로 구성한다. 그리드와 동일한
        /// ModernDataGridColumn 정의를 재사용하며, 헤더 행이 표시되고 검색형
        /// 콤보의 타이핑 필터는 모든 컬럼(코드 포함)을 대상으로 동작한다.
        /// 필드의 선택 텍스트는 계속 DisplayMember(명칭)를 따른다.
        /// DataSource 할당 전에 호출한다.
        /// </summary>
        public void ConfigureDropDownColumns(params Modern.Lab.Controls.Wpf.Data.ModernDataGridColumn[] columns)
        {
            if (this.Wpf != null)
            {
                this.Wpf.ApplyDropDownColumns(columns);
            }
        }

        // 수동 Items에 항목이 늦게 추가되면 보류 중인 SelectedValue를 재시도한다.
        // 일치 항목이 나타날 때까지 보류를 유지한다.
        private void OnManualItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.Wpf == null || !this.hasPendingSelectedValue || this.HasBoundItems())
            {
                return;
            }

            this.Wpf.SelectedValue = this.pendingSelectedValue;

            if (this.Wpf.SelectedItem != null)
            {
                this.pendingSelectedValue = null;
                this.hasPendingSelectedValue = false;
            }
            else
            {
                this.Wpf.SelectedValue = null;
            }
        }

        private bool HasBoundItems()
        {
            return this.Wpf != null &&
                   this.Wpf.ItemsSource != null &&
                   !object.ReferenceEquals(this.Wpf.ItemsSource, this.manualItems);
        }

        private void SelectFirstItemIfAny()
        {
            IEnumerator enumerator = this.Wpf.ItemsSource.GetEnumerator();

            if (enumerator.MoveNext())
            {
                this.Wpf.SelectedItem = enumerator.Current;
            }
        }

        private void OnWpfSelectionChanged(object sender, EventArgs e)
        {
            if (!this.suppressSelectionChanged)
            {
                this.RaiseSelectedIndexChanged();
            }
        }

        private void OnWpfDropDownOpened(object sender, EventArgs e)
        {
            if (this.DropDown != null)
            {
                this.DropDown(this, EventArgs.Empty);
            }
        }

        private void OnWpfDropDownClosed(object sender, EventArgs e)
        {
            if (this.DropDownClosed != null)
            {
                this.DropDownClosed(this, EventArgs.Empty);
            }
        }

        private void RaiseSelectedIndexChanged()
        {
            if (this.SelectedIndexChanged != null)
            {
                this.SelectedIndexChanged(this, EventArgs.Empty);
            }
        }
    }
}
