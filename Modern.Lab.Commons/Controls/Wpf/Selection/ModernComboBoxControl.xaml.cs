using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Modern.Lab.Controls.Wpf.Common;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Controls.Wpf.Input;

namespace Modern.Lab.Controls.Wpf.Selection
{
    /// <summary>
    /// 모던 드롭다운 선택기.
    /// - ItemsSource / DisplayMemberPath / SelectedValuePath: 바인딩 표면
    /// - SelectedItem / SelectedValue: 현재 선택 (양방향)
    /// - IsEditable: 검색형 콤보 — 입력하면 목록이 필터링된다
    ///   (한국어 초성 매칭 포함)
    /// - Placeholder: 아무것도 선택/입력되지 않은 동안 표시되는 힌트
    /// - SelectionChanged: 선택이 바뀔 때 발생
    ///
    /// 내부 ComboBox는 ItemsSource의 내부 필터링 스냅숏에 바인딩되어 편집 가능
    /// 모드가 입력 중에 필터링할 수 있다. 소스 컬렉션 변경
    /// (ObservableCollection / IBindingList)은 감시되어 다시 반영된다.
    /// </summary>
    public partial class ModernComboBoxControl : UserControl, IHostDisposalAware
    {
        /// <summary>표시할 항목 목록. 임의의 IEnumerable (DataView, IList, ...).</summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>현재 선택된 항목. 기본적으로 양방향 바인딩.</summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(ModernComboBoxControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>선택된 항목의 값(SelectedValuePath 기준). 기본적으로 양방향 바인딩.</summary>
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(
                "SelectedValue",
                typeof(object),
                typeof(ModernComboBoxControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>각 항목의 표시 텍스트에 사용되는 멤버 경로.</summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(string.Empty, OnDisplayMemberPathChanged));

        /// <summary>드롭다운 항목의 글자색을 결정하는 멤버 경로 (항목의 색 hex를
        /// 담은 필드 이름). 비우면 기본 텍스트색. 항목마다 상태를 색으로 구분할 때 쓴다.</summary>
        public static readonly DependencyProperty ItemColorPathProperty =
            DependencyProperty.Register(
                "ItemColorPath",
                typeof(string),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(string.Empty, OnItemColorPathChanged));

        /// <summary>
        /// SelectedValue에 사용되는 멤버 경로. <b>선택 이후에 지정해도</b> 그 시점에
        /// SelectedValue가 새 경로로 다시 계산된다(원본 ListControl과 같은 순서 내성 —
        /// DataSource를 먼저 할당하고 ValueMember를 나중에 주는 코드가 흔하다).
        /// </summary>
        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(
                "SelectedValuePath",
                typeof(string),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(string.Empty, OnSelectedValuePathChanged));

        /// <summary>아무것도 선택되지 않은 동안 표시되는 힌트 텍스트.</summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                "Placeholder",
                typeof(string),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(string.Empty));

        /// <summary>검색형 콤보: 입력하면 목록이 필터링된다.</summary>
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(
                "IsEditable",
                typeof(bool),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(false, OnIsEditableChanged));

        /// <summary>
        /// 편집 가능 콤보에서 입력 즉시 대문자/소문자로 강제 변환 (기본 Normal).
        /// Lot ID처럼 대문자만 존재하는 코드 입력용 — ModernTextBox와 같은 축이며,
        /// 내부 편집 TextBox의 WPF 기본 기능(CharacterCasing)에 위임한다.
        /// </summary>
        public static readonly DependencyProperty CharacterCasingProperty =
            DependencyProperty.Register(
                "CharacterCasing",
                typeof(CharacterCasing),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(CharacterCasing.Normal, OnCharacterCasingChanged));

        /// <summary>편집 입력에 허용할 문자. 빈 문자열이면 제한하지 않는다.</summary>
        public static readonly DependencyProperty AllowedCharactersProperty =
            DependencyProperty.Register("AllowedCharacters", typeof(string), typeof(ModernComboBoxControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// 편집 가능 콤보에서 <b>입력이 목록을 좁힐지</b> 여부 (기본 true).
        ///
        /// WinForms ComboBox는 "편집 가능"(DropDownStyle)과 "자동완성"
        /// (AutoCompleteMode)이 별개 축이다 — 편집은 되지만 목록은 그대로인
        /// 조합(DropDown + AutoCompleteMode.None)이 정상적으로 존재한다.
        /// 래퍼(ModernComboBox)가 AutoCompleteMode를 이 값으로 옮겨 그 조합을
        /// 그대로 재현한다. false면 타이핑해도 목록을 좁히지 않고 드롭다운도
        /// 자동으로 열지 않는다(자유 입력 콤보).
        /// </summary>
        public static readonly DependencyProperty FilterOnTypingProperty =
            DependencyProperty.Register(
                "FilterOnTyping",
                typeof(bool),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(true, OnFilterOnTypingChanged));

        /// <summary>
        /// 입력한 글자 뒤에 <b>남은 글자를 옅게 겹쳐</b> 보여 줄지 여부 (기본 false).
        /// WinForms의 <c>AutoCompleteMode.Append</c> 계열에 대응한다.
        ///
        /// 원본은 입력란 텍스트에 나머지를 실제로 덧붙이지만, 이 컨트롤은 텍스트를
        /// 건드리지 않고 <b>겹쳐 그리기만</b> 한다 — 한글 IME는 조합 중에 앞 글자가
        /// 계속 바뀌므로("그" → "글" → "그루") 그 시점에 버퍼를 수정하면 조합이
        /// 깨지기 때문이다. 그래서 <b>한글 완성문자에서도 영문과 똑같이</b> 힌트가
        /// 뜨고, 자음/모음만 친 상태에서는 접두 일치가 없어 자연히 뜨지 않는다.
        ///
        /// 힌트는 <c>Tab</c> 또는 <c>→</c>(캐럿이 끝일 때)로 확정한다.
        /// </summary>
        public static readonly DependencyProperty AppendCompletionProperty =
            DependencyProperty.Register(
                "AppendCompletion",
                typeof(bool),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(false, OnAppendCompletionChanged));

        /// <summary>필수 입력 필드 표시 — 필드 왼쪽에 빨간 세로 바를 그린다.</summary>
        public static readonly DependencyProperty RequiredProperty =
            DependencyProperty.Register(
                "Required",
                typeof(bool),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(false));

        /// <summary>강조 표시 — 주목이 필요한 핵심 선택 필드에 액센트색
        /// 테두리를 덧그린다 (Required의 빨간 바와 별개로 함께 쓸 수 있다).</summary>
        public static readonly DependencyProperty HighlightProperty =
            DependencyProperty.Register(
                "Highlight",
                typeof(bool),
                typeof(ModernComboBoxControl),
                new PropertyMetadata(false));

        private readonly ObservableCollection<object> filteredItems;
        private TextBox editableTextBox;
        private bool isRebuildingItems;
        private bool isRaisingDropDown;

        // 선택 확정 직후 편집란에 다시 쓰일 것으로 **예상되는 텍스트**.
        // 그 텍스트가 실제로 들어오면 한 번만 필터링을 건너뛰고 지운다.
        //
        // 예전에는 bool 플래그였는데, 예상한 재작성이 오지 않으면 플래그가 남아
        // **사용자의 첫 키 입력을 삼켰다**(폼을 열자마자 첫 글자를 쳐도 아무 일도
        // 일어나지 않던 원인). 값으로 비교하면 남아 있어도 사용자가 친 글자와
        // 다르므로 그냥 통과한다.
        private string expectedSelectionText;

        // 드롭다운 항목 클릭 한 번의 문맥 — 마우스 다운(프리뷰)에서 기억해 업에서 판정한다.
        private ComboBoxItem clickedDropDownItem;
        private object selectionBeforeDropDownClick;

        // 멀티컬럼 드롭다운 구성. null이면 기존 단일 컬럼(DisplayMemberPath) 모드.
        private List<ModernDataGridColumn> dropDownColumns;

        /// <summary>선택이 바뀔 때 발생한다.</summary>
        public event EventHandler SelectionChanged;

        /// <summary>드롭다운 목록이 열리기 직전에 발생한다(WinForms ComboBox.DropDown 대응).</summary>
        public event EventHandler DropDownOpened;

        /// <summary>드롭다운 목록이 닫힌 직후에 발생한다(WinForms ComboBox.DropDownClosed 대응).</summary>
        public event EventHandler DropDownClosedEvent;

        public ModernComboBoxControl()
        {
            this.filteredItems = new ObservableCollection<object>();
            this.InitializeComponent();
            this.InnerComboBox.ItemsSource = this.filteredItems;
            this.InnerComboBox.DropDownOpened += this.OnDropDownOpened;
            this.InnerComboBox.DropDownClosed += this.OnDropDownClosed;

            // 드롭다운 항목 클릭 감시 — 같은 항목 재선택도 WinForms처럼 알린다
            // (아래 "같은 항목 재선택" 절). handledEventsToo: ComboBoxItem이 MouseUp을
            // 처리(Handled)한 뒤에도 조상인 이 콤보에서 받아야 한다.
            this.InnerComboBox.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
                    new MouseButtonEventHandler(this.OnDropDownPreviewMouseUp), true);
            this.InnerComboBox.AddHandler(UIElement.MouseLeftButtonUpEvent,
                    new MouseButtonEventHandler(this.OnDropDownMouseUp), true);

            this.UpdatePlaceholderVisibility();
        }

        // 요구 높이 계약 — 자동 배치(무한 높이)에서는 설계 높이(32), 유한
        // 슬롯에서는 min(설계, 가용). 근거: Common/FieldSlotHeight.cs.
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            double preferred = (double)this.FindResource("Size.ControlHeight");
            double height = Modern.Lab.Controls.Wpf.Common.FieldSlotHeight.Resolve(
                    constraint.Height, preferred);
            System.Windows.Size desired = base.MeasureOverride(
                    new System.Windows.Size(constraint.Width, height));

            return new System.Windows.Size(desired.Width, height);
        }

        /// <summary>표시할 항목 목록.</summary>
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>필수 입력 필드 표시(필드 왼쪽 빨간 세로 바).</summary>
        public bool Required
        {
            get { return (bool)this.GetValue(RequiredProperty); }
            set { this.SetValue(RequiredProperty, value); }
        }

        /// <summary>강조 표시 — 액센트색 테두리로 필드에 주목을 준다.</summary>
        public bool Highlight
        {
            get { return (bool)this.GetValue(HighlightProperty); }
            set { this.SetValue(HighlightProperty, value); }
        }

        /// <summary>현재 선택된 항목.</summary>
        public object SelectedItem
        {
            get { return this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        /// <summary>선택된 항목의 값(SelectedValuePath 기준).</summary>
        public object SelectedValue
        {
            get { return this.GetValue(SelectedValueProperty); }
            set { this.SetValue(SelectedValueProperty, value); }
        }

        /// <summary>각 항목의 표시 텍스트에 사용되는 멤버 경로.</summary>
        public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        /// <summary>드롭다운 항목 글자색을 결정하는 멤버 경로 (색 hex 필드 이름).</summary>
        public string ItemColorPath
        {
            get { return (string)this.GetValue(ItemColorPathProperty); }
            set { this.SetValue(ItemColorPathProperty, value); }
        }

        /// <summary>SelectedValue에 사용되는 멤버 경로.</summary>
        public string SelectedValuePath
        {
            get { return (string)this.GetValue(SelectedValuePathProperty); }
            set { this.SetValue(SelectedValuePathProperty, value); }
        }

        /// <summary>아무것도 선택되지 않은 동안 표시되는 힌트 텍스트.</summary>
        public string Placeholder
        {
            get { return (string)this.GetValue(PlaceholderProperty); }
            set { this.SetValue(PlaceholderProperty, value); }
        }

        /// <summary>검색형 콤보: 입력하면 목록이 필터링된다.</summary>
        public bool IsEditable
        {
            get { return (bool)this.GetValue(IsEditableProperty); }
            set { this.SetValue(IsEditableProperty, value); }
        }

        /// <summary>편집 가능 콤보에서 입력 즉시 대문자/소문자 강제 변환 (기본 Normal).</summary>
        public CharacterCasing CharacterCasing
        {
            get { return (CharacterCasing)this.GetValue(CharacterCasingProperty); }
            set { this.SetValue(CharacterCasingProperty, value); }
        }

        /// <summary>편집 입력에 허용할 문자. 목록에서 선택한 표시값은 변형하지 않는다.</summary>
        public string AllowedCharacters
        {
            get { return (string)this.GetValue(AllowedCharactersProperty); }
            set { this.SetValue(AllowedCharactersProperty, value); }
        }

        /// <summary>입력한 글자 뒤에 남은 글자를 옅게 겹쳐 보여 줄지 여부 (기본 false).</summary>
        public bool AppendCompletion
        {
            get { return (bool)this.GetValue(AppendCompletionProperty); }
            set { this.SetValue(AppendCompletionProperty, value); }
        }

        /// <summary>편집 가능 콤보에서 입력이 목록을 좁힐지 여부 (기본 true).</summary>
        public bool FilterOnTyping
        {
            get { return (bool)this.GetValue(FilterOnTypingProperty); }
            set { this.SetValue(FilterOnTypingProperty, value); }
        }

        /// <summary>선택된 항목의 인덱스(아무것도 선택되지 않았으면 -1).</summary>
        public int SelectedIndex
        {
            get { return this.InnerComboBox.SelectedIndex; }
            set { this.InnerComboBox.SelectedIndex = value; }
        }

        /// <summary>현재 선택의 표시 텍스트(내부 ComboBox 텍스트).</summary>
        public string SelectionText
        {
            get { return this.InnerComboBox.Text; }
        }

        /// <summary>편집 가능 텍스트를 설정한다(검색형 콤보 전용).</summary>
        public void SetEditableText(string value)
        {
            if (this.IsEditable)
            {
                this.InnerComboBox.Text = value ?? string.Empty;
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            control.DetachSourceListeners(e.OldValue);
            control.AttachSourceListeners(e.NewValue);
            control.RebuildFilteredItems(null);
        }

        // 대소문자 강제는 내부 편집 TextBox의 WPF 기본 기능(CharacterCasing)에
        // 위임한다 — 입력 시점에 변환되므로 필터링에는 이미 변환된 텍스트가
        // 들어온다. 에디터가 아직 템플릿에서 잡히기 전이면 Loaded에서 다시 적용된다.
        private static void OnCharacterCasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            if (control.editableTextBox != null)
            {
                control.editableTextBox.CharacterCasing = (CharacterCasing)e.NewValue;
            }
        }

        // 필터링을 끄면 좁혀져 있던 목록을 전체로 되돌린다 (켤 때는 다음 입력이 좁힌다).
        private static void OnFilterOnTypingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            if (!control.FilterOnTyping)
            {
                control.RebuildFilteredItems(null);
            }
        }

        // 값 경로가 바뀌면 현재 선택의 값을 새 경로로 다시 계산해 내보낸다.
        // (경로가 비어 있던 동안 SelectedValue에는 항목 자체가 실려 있었다.)
        private static void OnSelectedValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;
            object item = control.InnerComboBox.SelectedItem;

            if (item == null)
            {
                return;
            }

            string path = control.SelectedValuePath;

            control.SelectedValue = string.IsNullOrEmpty(path)
                    ? item
                    : MemberPathReader.Read(item, path);
        }

        // 힌트를 끄면 지금 떠 있는 것부터 지운다.
        private static void OnAppendCompletionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            if (!control.AppendCompletion)
            {
                control.HideCompletionHint();
            }
        }

        private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            control.UpdatePlaceholderVisibility();
            control.ApplyMultiColumnVisuals();
        }

        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ModernComboBoxControl control = (ModernComboBoxControl)d;

            // 멀티컬럼 모드에서는 내부 DisplayMemberPath 바인딩이 해제되어 있으므로
            // 편집/필드 텍스트가 따라가도록 TextSearch 경로를 갱신한다 (순서 내성).
            if (control.dropDownColumns != null)
            {
                TextSearch.SetTextPath(control.InnerComboBox, (string)e.NewValue ?? string.Empty);
            }
        }

        // 소스 컬렉션 변경을 감시하여(수동 Items 컬렉션은 ObservableCollection,
        // DataView는 IBindingList) 뒤늦게 추가된 항목도 직접 ItemsSource에
        // 바인딩했을 때처럼 나타나게 한다.
        private void AttachSourceListeners(object source)
        {
            INotifyCollectionChanged observable = source as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged += this.OnSourceCollectionChanged;
                return;
            }

            IBindingList bindingList = source as IBindingList;

            if (bindingList != null)
            {
                bindingList.ListChanged += this.OnSourceListChanged;
            }
        }

        private void DetachSourceListeners(object source)
        {
            INotifyCollectionChanged observable = source as INotifyCollectionChanged;

            if (observable != null)
            {
                observable.CollectionChanged -= this.OnSourceCollectionChanged;
                return;
            }

            IBindingList bindingList = source as IBindingList;

            if (bindingList != null)
            {
                bindingList.ListChanged -= this.OnSourceListChanged;
            }
        }

        // 호스트 Dispose 시 현재 소스 구독을 해제한다 — 공유/장수명 데이터
        // 소스가 닫힌 폼과 래퍼를 계속 참조하지 않게 한다.
        void IHostDisposalAware.OnHostDisposing()
        {
            this.DetachSourceListeners(this.ItemsSource);
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RebuildFilteredItems(null);
        }

        private void OnSourceListChanged(object sender, ListChangedEventArgs e)
        {
            this.RebuildFilteredItems(null);
        }

        // ItemsSource로부터 내부 목록을 다시 만들며, 표시 텍스트가 필터와 매칭되는
        // 항목만 남긴다(한국어 인식). 선택과 입력 텍스트를 보존하고, 재구성 중에는
        // SelectionChanged 잡음을 억제한다.
        private void RebuildFilteredItems(string filterText)
        {
            this.isRebuildingItems = true;

            try
            {
                object previousSelection = this.InnerComboBox.SelectedItem;
                string previousEditorText = this.editableTextBox != null ? this.editableTextBox.Text : null;
                int previousCaret = this.editableTextBox != null ? this.editableTextBox.CaretIndex : 0;

                this.filteredItems.Clear();

                IEnumerable source = this.ItemsSource;

                if (source != null)
                {
                    foreach (object item in source)
                    {
                        if (string.IsNullOrEmpty(filterText) || this.MatchesFilter(item, filterText))
                        {
                            this.filteredItems.Add(item);
                        }
                    }
                }

                if (previousSelection != null && this.filteredItems.Contains(previousSelection))
                {
                    this.InnerComboBox.SelectedItem = previousSelection;
                }

                // 선택을 비우거나 복원하면 편집 가능 텍스트가 다시 쓰이므로,
                // 사용자가 입력한 텍스트(와 캐럿)를 되돌려 놓는다. 이때 동기적으로
                // 발생하는 TextChanged는 isRebuildingItems로 무시된다.
                if (this.editableTextBox != null && previousEditorText != null &&
                    !string.Equals(this.editableTextBox.Text, previousEditorText, StringComparison.Ordinal))
                {
                    this.editableTextBox.Text = previousEditorText;
                    this.editableTextBox.CaretIndex = Math.Min(previousCaret, previousEditorText.Length);
                }
            }
            finally
            {
                this.isRebuildingItems = false;
            }

            if (!this.isRaisingDropDown && filterText == null && this.filteredItems.Count == 0 && this.InnerComboBox.IsDropDownOpen)
            {
                this.InnerComboBox.IsDropDownOpen = false;
            }

            this.UpdatePlaceholderVisibility();
        }

        // 항목이 멀티컬럼 구성일 때는 모든 컬럼의 텍스트를 대상으로,
        // 아니면 DisplayMemberPath 텍스트만 대상으로 매칭한다(초성 검색 포함).
        private bool MatchesFilter(object item, string filterText)
        {
            if (this.dropDownColumns == null)
            {
                return HangulTextMatcher.Contains(
                    MemberPathReader.ReadDisplayText(item, this.DisplayMemberPath), filterText);
            }

            foreach (ModernDataGridColumn column in this.dropDownColumns)
            {
                if (HangulTextMatcher.Contains(
                    MemberPathReader.ReadDisplayText(item, column.DataPropertyName), filterText))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 드롭다운을 멀티컬럼 행(코드+명칭 등)으로 구성한다. 헤더 행이 표시되고
        /// 검색 필터는 모든 컬럼을 대상으로 동작한다. 필드에 표시되는 선택
        /// 텍스트는 계속 DisplayMemberPath(명칭)를 따른다. null/빈 목록이면
        /// 아무것도 하지 않는다(구성 후 단일 컬럼으로 되돌리는 것은 미지원).
        /// </summary>
        public void ApplyDropDownColumns(IList<ModernDataGridColumn> columns)
        {
            if (columns == null || columns.Count == 0)
            {
                return;
            }

            this.dropDownColumns = new List<ModernDataGridColumn>(columns);

            // DisplayMemberPath와 ItemTemplate은 동시에 쓸 수 없으므로 내부
            // 바인딩을 해제하고, 선택 텍스트는 TextSearch 경로로 대신 공급한다.
            this.InnerComboBox.ClearValue(ItemsControl.DisplayMemberPathProperty);
            TextSearch.SetTextPath(this.InnerComboBox, this.DisplayMemberPath ?? string.Empty);
            this.InnerComboBox.ItemTemplate = this.BuildMultiColumnRowTemplate();

            this.ApplyMultiColumnVisuals();
        }

        private static double EffectiveColumnWidth(ModernDataGridColumn column)
        {
            return column.Width > 0d ? column.Width : 120d;
        }

        // 멀티컬럼 행 템플릿: 컬럼 폭이 고정된 TextBlock들의 가로 나열.
        private DataTemplate BuildMultiColumnRowTemplate()
        {
            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            foreach (ModernDataGridColumn column in this.dropDownColumns)
            {
                FrameworkElementFactory cell = new FrameworkElementFactory(typeof(TextBlock));
                cell.SetBinding(TextBlock.TextProperty, new Binding(column.DataPropertyName));
                cell.SetValue(FrameworkElement.WidthProperty, EffectiveColumnWidth(column));
                cell.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
                cell.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                cell.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

                if (column.TextAlignment == GridTextAlignment.Center)
                {
                    cell.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                }
                else if (column.TextAlignment == GridTextAlignment.Right)
                {
                    cell.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                }

                panel.AppendChild(cell);
            }

            DataTemplate template = new DataTemplate();
            template.VisualTree = panel;
            return template;
        }

        private static void OnItemColorPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ModernComboBoxControl)d).ApplyItemColor();
        }

        // 항목 글자색 스타일을 적용/해제한다. 경로가 지정되면 기본 항목 스타일에
        // Foreground 바인딩(색 hex → Brush)을 덧대고, 비우면 기본 스타일로 되돌린다.
        private void ApplyItemColor()
        {
            Style baseStyle = (Style)this.FindResource("ModernComboBoxItemStyle");

            if (string.IsNullOrEmpty(this.ItemColorPath))
            {
                this.InnerComboBox.ItemContainerStyle = baseStyle;
                return;
            }

            Style style = new Style(typeof(ComboBoxItem), baseStyle);
            Binding binding = new Binding(this.ItemColorPath);
            binding.Converter = new HexToBrushConverter();
            style.Setters.Add(new Setter(Control.ForegroundProperty, binding));
            this.InnerComboBox.ItemContainerStyle = style;
        }

        // 색 hex 문자열을 Brush로 변환한다 (빈 값/실패는 기본 텍스트색으로 폴백).
        private sealed class HexToBrushConverter : IValueConverter
        {
            public object Convert(
                    object value, Type targetType, object parameter,
                    System.Globalization.CultureInfo culture)
            {
                System.Windows.Media.Brush brush =
                        ChipColorHelper.TryCreateBrush(value == null ? null : value.ToString());
                return brush != null ? (object)brush : DependencyProperty.UnsetValue;
            }

            public object ConvertBack(
                    object value, Type targetType, object parameter,
                    System.Globalization.CultureInfo culture)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        // 헤더 행: 항목 행과 같은 폭 배치, SemiBold 캡션.
        private StackPanel BuildHeaderRow()
        {
            StackPanel header = new StackPanel();
            header.Orientation = Orientation.Horizontal;
            header.Margin = new Thickness(12, 6, 12, 6);

            foreach (ModernDataGridColumn column in this.dropDownColumns)
            {
                TextBlock caption = new TextBlock();
                caption.Text = column.HeaderText;
                caption.Width = EffectiveColumnWidth(column);
                caption.Margin = new Thickness(0, 0, 12, 0);
                caption.FontWeight = FontWeights.SemiBold;
                caption.VerticalAlignment = VerticalAlignment.Center;

                if (column.TextAlignment == GridTextAlignment.Center)
                {
                    caption.TextAlignment = TextAlignment.Center;
                }
                else if (column.TextAlignment == GridTextAlignment.Right)
                {
                    caption.TextAlignment = TextAlignment.Right;
                }

                header.Children.Add(caption);
            }

            return header;
        }

        // 템플릿 내부 요소(헤더 호스트, 필드 표시)를 멀티컬럼 모드에 맞게 갱신한다.
        // 템플릿이 아직 적용되지 않았으면 Loaded에서 다시 호출된다.
        private void ApplyMultiColumnVisuals()
        {
            if (this.dropDownColumns == null || this.InnerComboBox.Template == null)
            {
                return;
            }

            Border headerHost = this.InnerComboBox.Template.FindName("DropDownHeaderHost", this.InnerComboBox) as Border;

            if (headerHost == null)
            {
                return;
            }

            headerHost.Child = this.BuildHeaderRow();
            headerHost.Visibility = Visibility.Visible;

            // 필드에는 멀티컬럼 행 대신 선택 텍스트(명칭)만 보여준다.
            ContentPresenter contentSite = this.InnerComboBox.Template.FindName("ContentSite", this.InnerComboBox) as ContentPresenter;
            TextBlock selectionText = this.InnerComboBox.Template.FindName("MultiColumnSelectionText", this.InnerComboBox) as TextBlock;

            if (contentSite != null)
            {
                contentSite.Visibility = Visibility.Collapsed;
            }

            if (selectionText != null)
            {
                selectionText.Visibility = this.IsEditable ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // 템플릿에서 편집 가능 텍스트 영역을 얻어 입력을 후킹한다.
        // Loaded는 호스트 폼이 데이터를 이미 바인딩한 뒤 발생하므로,
        // 여기서 실제 에디터 상태를 기준으로 placeholder를 다시 평가하고
        // 멀티컬럼 시각 요소도 마저 적용한다.
        private void InnerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            this.ApplyMultiColumnVisuals();
            TextBox editor = this.InnerComboBox.Template.FindName("PART_EditableTextBox", this.InnerComboBox) as TextBox;

            if (!object.ReferenceEquals(editor, this.editableTextBox))
            {
                if (this.editableTextBox != null)
                {
                    this.editableTextBox.TextChanged -= this.OnEditableTextChanged;
                    this.editableTextBox.PreviewKeyDown -= this.OnEditablePreviewKeyDown;
                    this.editableTextBox.LostKeyboardFocus -= this.OnEditableLostFocus;
                }

                this.editableTextBox = editor;

                if (this.editableTextBox != null)
                {
                    this.editableTextBox.TextChanged += this.OnEditableTextChanged;
                    this.editableTextBox.PreviewKeyDown += this.OnEditablePreviewKeyDown;
                    this.editableTextBox.LostKeyboardFocus += this.OnEditableLostFocus;

                    // 에디터가 템플릿에서 늦게 잡히므로 대소문자 강제를 여기서 적용한다.
                    this.editableTextBox.CharacterCasing = this.CharacterCasing;
                }
            }

            this.UpdatePlaceholderVisibility();
        }

        // IME 조합 중에도 발생하므로, 첫 자음 입력부터 목록이 필터링되고
        // placeholder가 숨겨진다.
        private void OnEditableTextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdatePlaceholderVisibility();

            if (!this.IsEditable || this.isRebuildingItems)
            {
                return;
            }

            if (this.expectedSelectionText != null)
            {
                bool matchesSelectionRewrite = string.Equals(
                        this.editableTextBox.Text, this.expectedSelectionText, StringComparison.Ordinal);

                this.expectedSelectionText = null;

                if (matchesSelectionRewrite)
                {
                    return;
                }
            }

            if (this.EnforceAllowedCharacters() || !this.InnerComboBox.IsKeyboardFocusWithin)
            {
                return;
            }

            string typed = this.editableTextBox.Text;

            // 인라인 힌트는 목록 필터링과 별개 축이다 — Append(목록은 그대로 두고
            // 힌트만) 조합을 그대로 재현하기 위해 필터링보다 먼저 갱신한다.
            this.UpdateCompletionHint(typed);

            // 자동완성이 꺼진 자유 입력 콤보(WinForms의 DropDown +
            // AutoCompleteMode.None)는 타이핑해도 목록을 좁히거나 열지 않는다.
            if (!this.FilterOnTyping)
            {
                return;
            }

            if (string.IsNullOrEmpty(typed))
            {
                // 텍스트를 지우면 선택도 지워진다(빈 값은 "전체"를 의미).
                if (this.InnerComboBox.SelectedItem != null)
                {
                    this.InnerComboBox.SelectedItem = null;
                    // 선택 해제는 텍스트 재작성을 예상하지만 텍스트가 이미
                    // 비어 있으므로 기대값을 지운다.
                    this.expectedSelectionText = null;
                }

                this.RebuildFilteredItems(null);
                this.InnerComboBox.IsDropDownOpen = false;
                return;
            }

            int selectionStart = this.editableTextBox.SelectionStart;
            int selectionLength = this.editableTextBox.SelectionLength;
            this.RebuildFilteredItems(typed);
            this.InnerComboBox.IsDropDownOpen = this.filteredItems.Count > 0;
            // 입력으로 팝업을 열 때 WPF가 고른 전체 텍스트 대신 입력 중 선택 영역을 유지한다.
            this.editableTextBox.Select(Math.Min(selectionStart, typed.Length),
                    Math.Min(selectionLength, typed.Length - Math.Min(selectionStart, typed.Length)));
        }

        // 셰브런으로 드롭다운을 다시 열 때는 전체 목록이 보여야 한다.
        // 또한 오래된 suppress 플래그를 지운다(텍스트를 바꾸지 않은 선택이
        // 다음 키 입력의 필터링을 삼켜버릴 수 있기 때문).
        private void OnDropDownClosed(object sender, EventArgs e)
        {
            if (this.IsEditable)
            {
                this.RebuildFilteredItems(null);
                this.expectedSelectionText = null;
            }

            if (this.DropDownClosedEvent != null)
            {
                this.DropDownClosedEvent(this, EventArgs.Empty);
            }
        }

        // 드롭다운이 열리기 직전 알림 — 타이핑으로 필터가 자동으로 여는 경우도
        // 포함한다(원본 WinForms는 자동 개폐가 없어 이 구분이 없었다; 사용자가
        // 여는 것과 같은 계약으로 둔다 — 원본과 가장 가까운 동작).
        private void OnDropDownOpened(object sender, EventArgs e)
        {
            this.isRaisingDropDown = true;
            try
            {
                if (this.DropDownOpened != null)
                {
                    this.DropDownOpened(this, EventArgs.Empty);
                }
            }
            finally
            {
                this.isRaisingDropDown = false;
            }

            // 지연 로딩 이벤트가 목록을 채울 기회를 준 뒤 빈 팝업을 닫는다.
            if (this.filteredItems.Count == 0)
            {
                this.InnerComboBox.IsDropDownOpen = false;
            }
        }

        private bool EnforceAllowedCharacters()
        {
            string allowed = this.AllowedCharacters;
            string text = this.editableTextBox.Text;
            if (string.IsNullOrEmpty(allowed) || text.Length == 0
                    || (this.InnerComboBox.SelectedItem != null && string.Equals(text,
                        MemberPathReader.ReadDisplayText(this.InnerComboBox.SelectedItem, this.DisplayMemberPath),
                        StringComparison.Ordinal)))
            {
                return false;
            }

            int start = this.editableTextBox.SelectionStart;
            int end = start + this.editableTextBox.SelectionLength;
            int filteredStart = 0;
            int filteredEnd = 0;
            StringBuilder filtered = new StringBuilder(text.Length);
            for (int index = 0; index < text.Length; index++)
            {
                if (allowed.IndexOf(text[index]) >= 0)
                {
                    filtered.Append(text[index]);
                    if (index < start) { filteredStart++; }
                    if (index < end) { filteredEnd++; }
                }
            }
            if (filtered.Length == text.Length) { return false; }

            this.editableTextBox.Text = filtered.ToString();
            this.editableTextBox.Select(filteredStart, filteredEnd - filteredStart);
            return true;
        }

        private void UpdatePlaceholderVisibility()
        {
            bool empty;

            if (this.IsEditable)
            {
                string editorText = this.editableTextBox != null ? this.editableTextBox.Text : this.InnerComboBox.Text;
                empty = string.IsNullOrEmpty(editorText);
            }
            else
            {
                empty = this.InnerComboBox.SelectedItem == null;
            }

            this.PlaceholderOverlay.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        }

        // ===== 인라인 자동완성 힌트(고스트 텍스트) =====

        // 입력한 글자로 시작하는 첫 항목을 찾아 남은 부분을 옅게 겹쳐 보여 준다.
        // 텍스트 버퍼는 건드리지 않는다(IME 조합 안전).
        private void UpdateCompletionHint(string typed)
        {
            if (!this.AppendCompletion || !this.IsEditable || string.IsNullOrEmpty(typed))
            {
                this.HideCompletionHint();
                return;
            }

            // 캐럿이 끝이 아닐 때(문장 중간 편집)는 뒤에 붙는 힌트가 방해만 된다.
            if (this.editableTextBox != null && this.editableTextBox.CaretIndex != typed.Length)
            {
                this.HideCompletionHint();
                return;
            }

            string completion = this.FindPrefixCompletion(typed);

            if (completion.Length == 0)
            {
                this.HideCompletionHint();
                return;
            }

            // 투명 Run으로 입력 글자만큼 자리를 만들고, 그 뒤에 남은 글자를 그린다.
            this.CompletionTypedRun.Text = typed;
            this.CompletionHintRun.Text = completion;
            this.CompletionOverlay.Visibility = Visibility.Visible;
        }

        // 입력 글자로 시작하는 첫 항목의 "남은 글자". 없으면 빈 문자열.
        // 초성 검색(ㄱ → 개발팀)은 접두 일치가 아니므로 힌트를 만들지 않는다 —
        // 자음/모음만 친 상태에서 엉뚱한 꼬리가 붙지 않게 하는 규칙이기도 하다.
        private string FindPrefixCompletion(string typed)
        {
            IEnumerable source = this.ItemsSource;

            if (source == null)
            {
                return string.Empty;
            }

            foreach (object item in source)
            {
                string text = MemberPathReader.ReadDisplayText(item, this.DisplayMemberPath);

                if (text.Length <= typed.Length)
                {
                    continue;
                }

                if (text.StartsWith(typed, StringComparison.CurrentCultureIgnoreCase))
                {
                    return text.Substring(typed.Length);
                }
            }

            return string.Empty;
        }

        private void HideCompletionHint()
        {
            if (this.CompletionOverlay == null)
            {
                return;
            }

            this.CompletionOverlay.Visibility = Visibility.Collapsed;
            this.CompletionTypedRun.Text = string.Empty;
            this.CompletionHintRun.Text = string.Empty;
        }

        // Tab / → (캐럿이 끝) 로 힌트를 확정한다. 이때 처음으로 텍스트를 쓴다 —
        // 사용자가 명시적으로 수락한 시점이라 IME 조합과 겹치지 않는다.
        private void OnEditablePreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 사용자가 키를 눌렀다면 다음 텍스트 변경은 사용자 입력이다 —
            // 선택 재작성 기대값이 남아 있어도 여기서 버린다.
            this.expectedSelectionText = null;

            if (this.CompletionOverlay == null
                    || this.CompletionOverlay.Visibility != Visibility.Visible
                    || this.editableTextBox == null)
            {
                return;
            }

            bool accept = e.Key == Key.Tab
                    || (e.Key == Key.Right && this.editableTextBox.CaretIndex == this.editableTextBox.Text.Length);

            if (!accept)
            {
                return;
            }

            string completed = this.editableTextBox.Text + this.CompletionHintRun.Text;

            this.HideCompletionHint();
            this.editableTextBox.Text = completed;
            this.editableTextBox.CaretIndex = completed.Length;

            e.Handled = true;
        }

        private void OnEditableLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            this.HideCompletionHint();
        }

        private void InnerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.isRebuildingItems)
            {
                return;
            }

            this.NotifySelectionChanged();
        }

        // 선택 알림의 단일 경로 — WPF SelectionChanged와 같은 항목 재선택이 함께 쓴다.
        private void NotifySelectionChanged()
        {
            // 선택을 확정하면 편집 가능 텍스트가 항목의 표시 텍스트로 다시 쓰인다;
            // 이 변경이 필터링을 다시 트리거해서는 안 된다.
            if (this.IsEditable)
            {
                this.expectedSelectionText = MemberPathReader.ReadDisplayText(
                        this.InnerComboBox.SelectedItem, this.DisplayMemberPath);
                this.HideCompletionHint();
            }

            this.UpdatePlaceholderVisibility();

            if (this.SelectionChanged != null)
            {
                this.SelectionChanged(this, EventArgs.Empty);
            }
        }

        // ===== 같은 항목 재선택 — WinForms CBN_SELCHANGE 호환 (2026-08-26 현장 보고) =====
        //
        // DataSource 할당은 첫 행을 자동 선택한다(WinForms 호환). 사용자가 드롭다운에서
        // 바로 그 항목을 고르면 WPF ComboBox는 선택이 안 바뀌었다고 보고 SelectionChanged를
        // 내지 않는다 — 그러나 WinForms ComboBox는 사용자가 목록에서 고르면 같은
        // 항목이어도 SelectedIndexChanged를 낸다(네이티브 CBN_SELCHANGE). 회사 폼의
        // 핸들러가 "첫 항목만 안 된다"고 보였던 원인. 클릭 전 선택을 기억해 두고,
        // 마우스 업 뒤에도 같은 항목이면 직접 알린다 — 다른 항목이면 WPF가 이미
        // 알렸으므로 중복 알림은 없다.

        private void OnDropDownPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.clickedDropDownItem = ItemsControl.ContainerFromElement(
                    this.InnerComboBox, e.OriginalSource as DependencyObject) as ComboBoxItem;
            this.selectionBeforeDropDownClick = this.clickedDropDownItem != null
                    ? this.InnerComboBox.SelectedItem
                    : null;
        }

        private void OnDropDownMouseUp(object sender, MouseButtonEventArgs e)
        {
            ComboBoxItem item = this.clickedDropDownItem;
            object before = this.selectionBeforeDropDownClick;
            this.clickedDropDownItem = null;
            this.selectionBeforeDropDownClick = null;

            if (item == null || before == null)
            {
                return;
            }

            object picked = this.InnerComboBox.ItemContainerGenerator.ItemFromContainer(item);

            if (picked == null || picked == DependencyProperty.UnsetValue)
            {
                return;
            }

            if (!object.Equals(picked, before) || !object.Equals(picked, this.InnerComboBox.SelectedItem))
            {
                return;
            }

            if (this.IsEditable)
            {
                // 타이핑으로 좁힌 뒤 같은 항목을 고른 경우 — 편집 텍스트를 항목 표시로
                // 되돌린다(WPF는 선택이 안 바뀌면 텍스트도 다시 쓰지 않는다).
                string display = MemberPathReader.ReadDisplayText(picked, this.DisplayMemberPath);
                this.expectedSelectionText = display;
                this.InnerComboBox.Text = display;
            }

            this.NotifySelectionChanged();
        }
    }
}
