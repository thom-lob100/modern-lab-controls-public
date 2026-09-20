using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Modern.Lab.Controls.Wpf.Common;
using Modern.Lab.Controls.Wpf.Input;

namespace Modern.Lab.Controls.Wpf.Data
{
    /// <summary>
    /// ModernDataGridControl의 컬럼 값 필터 컨트롤러 — AllowColumnFilters가
    /// 켜지면 텍스트/배지 컬럼에 GridFilterAttach.Filterable을 설정해 헤더
    /// 템플릿의 깔때기 버튼(헤더 셀 맨 오른쪽 고정, 정렬 글리프 바로 다음)이
    /// 나타나고, 클릭 시 그 컬럼의 고유 값 체크리스트 팝업이 열린다. 팝업의
    /// 검색 입력은 초성 매칭으로 체크리스트를 좁히고 선택도 그 결과로 맞춘다
    /// (별도 제안 드롭다운 없음).
    /// 체크는 팝업 안에서만 모이고 **Apply를 눌러야 반영**된다(엑셀식) —
    /// 즉시 반영하면 페이징 화면의 재바인딩이 팝업을 닫아 여러 값을 이어서
    /// 체크할 수 없기 때문이다. Apply 없이 닫으면 변경은 버려진다. 필터가
    /// 걸린 컬럼은 깔때기가 액센트색(IsActive)으로 칠해진다.
    ///
    /// 필터는 데이터 소스를 바꾸지 않고 **뷰에만** 적용한다:
    /// - DataTable/DataView 소스(BindingListCollectionView)는 CustomFilter
    ///   식으로, 그 외 IList 소스는 Filter 조건자로 거른다.
    /// - 상태바 행 수/빈 안내(EmptyText)는 뷰 기준이라 자동으로 따라온다.
    /// - 선택 상태는 컬럼 이름 기준으로 유지되어 DataSource 재할당(재조회)
    ///   후에도 같은 필터가 다시 적용된다.
    /// </summary>
    internal sealed class GridFilterController
    {
        // 팝업 체크리스트에 올리는 고유 값 상한 — 넘치면 앞쪽만 보여준다.
        private const int maxDistinctValues = 500;

        private readonly DataGrid grid;
        private readonly FrameworkElement resourceSource;
        private readonly Func<IEnumerable> itemsSourceGetter;

        // 컬럼 이름 → 선택된 값 집합. 항목이 없으면 그 컬럼은 필터 없음(전체).
        private readonly Dictionary<string, HashSet<string>> selections =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        // 필터 대상 컬럼 → 데이터 컬럼 이름 (팝업 대상 해석 + 활성 표시 갱신용).
        private readonly Dictionary<DataGridColumn, string> filterColumns =
                new Dictionary<DataGridColumn, string>();

        // 재사용 팝업과 현재 팝업이 다루는 컬럼/체크박스 목록.
        private Popup popup;
        private string popupColumn;
        private StackPanel popupList;
        private CheckBox popupAll;
        private ModernTextBoxControl popupSearch;
        private List<string> popupValues;
        private bool suppressPopupEvents;
        /// <summary>소유자가 직접 거르는 모드 — 켜면 뷰 필터를 설치하지 않는다.</summary>
        internal bool ExternalFiltering;

        private ICollectionView filteredView;
        private Predicate<object> ownedFilter;
        private ViewFilter filterLease;

        // 편집 중인 외부 뷰는 Filter 교체가 금지된다. 연결만 끊어도 그리드는 수거될 수 있어야 한다.
        private sealed class ViewFilter
        {
            internal GridFilterController Owner;

            internal bool Matches(object item)
            {
                return this.Owner == null || this.Owner.MatchesAllFilters(item);
            }
        }

        // 팝업 안에서만 유지되는 편집 중 선택 — Apply를 눌러야 selections에
        // 확정된다 (null = 전체 선택 = 필터 없음).
        private HashSet<string> popupPending;

        /// <summary>값 필터 상태가 바뀔 때(Apply 확정) 발생한다 — 페이징
        /// 화면이 전체 결과에 같은 필터를 적용해 페이지를 재계산할 때 쓴다.</summary>
        internal event EventHandler FiltersChanged;

        /// <summary>팝업 고유 값의 원천 — 페이징 화면은 페이지 조각 대신 전체
        /// 결과(DataTable 등)를 지정한다. null이면 현재 ItemsSource에서 모은다.</summary>
        internal object ValueSource;

        internal GridFilterController(
                DataGrid grid, FrameworkElement resourceSource, Func<IEnumerable> itemsSourceGetter)
        {
            this.grid = grid;
            this.resourceSource = resourceSource;
            this.itemsSourceGetter = itemsSourceGetter;
        }

        /// <summary>행이 현재 컬럼 필터를 전부 통과하는지 판정한다 — 페이징
        /// 화면이 전체 결과를 직접 거를 때 쓴다 (필터 없으면 항상 true).</summary>
        internal bool Matches(object item)
        {
            return this.selections.Count == 0 || this.MatchesAllFilters(item);
        }

        /// <summary>값이 걸린 컬럼(활성 필터)이 하나라도 있는지.</summary>
        internal bool HasActiveFilters
        {
            get { return this.selections.Count > 0; }
        }

        /// <summary>
        /// 모든 컬럼의 값 필터를 한 번에 해제한다 — 우클릭 메뉴 "Clear Filters".
        /// 팝업의 Clear를 전 컬럼에 적용한 것과 동일하며, 깔때기 활성 표시도
        /// 함께 걷고 FiltersChanged를 1회 알린다.
        /// </summary>
        internal void ClearAllFilters()
        {
            if (this.selections.Count == 0)
            {
                return;
            }

            List<string> cleared = new List<string>(this.selections.Keys);
            this.selections.Clear();

            if (this.popup != null)
            {
                this.popup.IsOpen = false;
            }

            foreach (string columnName in cleared)
            {
                this.UpdateActiveFlags(columnName);
            }

            this.ApplyFilters();
            this.RaiseFiltersChanged();
        }

        private void RaiseFiltersChanged()
        {
            if (this.FiltersChanged != null)
            {
                this.FiltersChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>컬럼 재구성 시 컬럼 매핑을 비운다 (선택 상태는 유지).</summary>
        internal void OnColumnsRebuilt()
        {
            this.filterColumns.Clear();

            if (this.popup != null)
            {
                this.popup.IsOpen = false;
            }
        }

        /// <summary>데이터 소스가 바뀌면 유지 중인 선택을 새 뷰에 다시 적용한다.</summary>
        internal void OnItemsSourceChanged()
        {
            this.ReleaseOwnedFilter();
            this.ApplyFilters();
        }

        internal void Detach()
        {
            this.ReleaseOwnedFilter();
            if (this.popup != null)
            {
                this.popup.IsOpen = false;
            }
            this.ValueSource = null;
        }

        // 다른 소비자가 교체한 필터는 건드리지 않고 자신이 설치한 조건자만 해제한다.
        private void ReleaseOwnedFilter()
        {
            ICollectionView view = this.filteredView;
            Predicate<object> filter = this.ownedFilter;
            if (this.filterLease != null)
            {
                this.filterLease.Owner = null;
                this.filterLease = null;
            }
            this.filteredView = null;
            this.ownedFilter = null;
            IEditableCollectionView editable = view as IEditableCollectionView;
            bool editing = editable != null && (editable.IsAddingNew || editable.IsEditingItem);
            if (view != null && filter != null && view.Filter == filter && !editing)
            {
                view.Filter = null;
            }
        }

        /// <summary>
        /// 컬럼을 값 필터 대상으로 등록한다 — 체크박스/버튼 컬럼은 대상이
        /// 아니므로 건너뛴다. 깔때기 버튼 자체는 헤더 템플릿에 있고, 여기서는
        /// Filterable 첨부 속성으로 표시를 켜고 활성 상태를 되비춘다.
        /// </summary>
        internal void AttachHeader(DataGridColumn column, ModernDataGridColumn definition)
        {
            if (definition.Kind == GridColumnKind.CheckBox || definition.Kind == GridColumnKind.Button)
            {
                return;
            }

            this.filterColumns[column] = definition.DataPropertyName;
            GridFilterAttach.SetFilterable(column, true);
            this.UpdateActiveFlags(definition.DataPropertyName);
        }

        // 필터 활성 여부를 컬럼 첨부 속성(IsActive)에 되비춘다 — 헤더 템플릿이
        // 이 값으로 깔때기를 액센트색으로 칠해 걸린 컬럼이 한눈에 보인다.
        private void UpdateActiveFlags(string columnName)
        {
            bool active = this.selections.ContainsKey(columnName);

            foreach (KeyValuePair<DataGridColumn, string> entry in this.filterColumns)
            {
                if (entry.Value == columnName)
                {
                    GridFilterAttach.SetIsActive(entry.Key, active);
                }
            }
        }

        // ===== 팝업 =====

        /// <summary>헤더 깔때기 버튼 클릭 — 그 컬럼의 값 체크리스트 팝업을 연다.</summary>
        internal void OpenPopupFor(DataGridColumn column, UIElement anchor)
        {
            string columnName;

            if (column == null || !this.filterColumns.TryGetValue(column, out columnName))
            {
                return;
            }

            if (this.popup == null)
            {
                this.popup = new Popup();
                this.popup.StaysOpen = false;
                this.popup.AllowsTransparency = true;
                this.popup.Placement = PlacementMode.Bottom;
            }

            if (this.popup.IsOpen && this.popupColumn == columnName)
            {
                this.popup.IsOpen = false;
                return;
            }

            this.popupColumn = columnName;
            this.popup.Child = this.BuildPopupContent(columnName);
            this.popup.PlacementTarget = anchor;
            this.popup.IsOpen = true;
        }

        // 팝업 내용: [초성 검색 + ✕닫기] + "(All)" + 고유 값 체크리스트(스크롤)
        // + Apply/Clear. 검색은 목록을 좁히면서 선택도 그 결과로 맞추고(제안
        // 드롭다운 없음), 체크 변경은 Apply를 눌러야 반영된다. Clear는 필터를
        // 즉시 해제하고 닫는 단축, ✕는 변경을 버리고 닫는다.
        private UIElement BuildPopupContent(string columnName)
        {
            List<string> values = this.CollectDistinctValues(columnName);
            HashSet<string> selected;
            this.selections.TryGetValue(columnName, out selected);
            this.popupValues = values;
            this.popupPending = selected == null
                    ? null
                    : new HashSet<string>(selected, StringComparer.Ordinal);

            this.suppressPopupEvents = true;

            this.popupSearch = new ModernTextBoxControl();
            this.popupSearch.Placeholder = "Search values";
            this.popupSearch.TextChanged += this.OnPopupSearchTextChanged;

            // ✕ 닫기(취소) — 변경을 버리고 닫는다 (바깥 클릭과 동일). 검색줄
            // 오른쪽의 컴팩트 아이콘 버튼으로 공간을 아낀다.
            ModernButtonControl cancel = new ModernButtonControl();
            cancel.Text = "✕";
            cancel.Kind = ButtonKind.Subtle;
            cancel.FontSizeOverride = 12d;
            cancel.HeightOverride = 26d;
            cancel.Width = 26d;
            cancel.Margin = new Thickness(6d, 0d, 0d, 0d);
            cancel.VerticalAlignment = VerticalAlignment.Center;
            cancel.Click += this.OnCancelClick;

            Grid searchRow = new Grid();
            searchRow.Margin = new Thickness(0d, 0d, 0d, 8d);
            searchRow.ColumnDefinitions.Add(new ColumnDefinition());
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(cancel, 1);
            searchRow.Children.Add(this.popupSearch);
            searchRow.Children.Add(cancel);

            this.popupAll = new CheckBox();
            this.popupAll.Content = "(All)";
            this.popupAll.FontWeight = FontWeights.SemiBold;
            this.popupAll.Margin = new Thickness(2d, 2d, 2d, 6d);
            this.popupAll.IsChecked = selected == null;
            this.popupAll.Checked += this.OnAllToggled;
            this.popupAll.Unchecked += this.OnAllToggled;

            this.popupList = new StackPanel();
            this.RebuildPopupValueItems();

            this.suppressPopupEvents = false;

            ScrollViewer scroll = new ScrollViewer();
            scroll.Content = this.popupList;
            scroll.MaxHeight = 240d;
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            // Apply(확정, Primary) + Clear(필터 즉시 해제 후 닫기, Secondary —
            // 테두리형이라 Surface 팝업 위에서 형태가 살아 Primary와 짝이 맞는다).
            // 글자는 체크리스트와 같은 12px, 높이 26으로 팝업 밀도에 맞춘다.
            ModernButtonControl apply = new ModernButtonControl();
            apply.Text = "Apply";
            apply.Kind = ButtonKind.Primary;
            apply.FontSizeOverride = 12d;
            apply.HeightOverride = 26d;
            apply.Margin = new Thickness(0d, 8d, 4d, 0d);
            apply.Click += this.OnApplyClick;

            ModernButtonControl clear = new ModernButtonControl();
            clear.Text = "Clear";
            clear.Kind = ButtonKind.Secondary;
            clear.FontSizeOverride = 12d;
            clear.HeightOverride = 26d;
            clear.Margin = new Thickness(4d, 8d, 0d, 0d);
            clear.Click += this.OnClearClick;

            Grid buttons = new Grid();
            buttons.ColumnDefinitions.Add(new ColumnDefinition());
            buttons.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(clear, 1);
            buttons.Children.Add(apply);
            buttons.Children.Add(clear);

            StackPanel layout = new StackPanel();
            layout.Children.Add(searchRow);
            layout.Children.Add(this.popupAll);
            layout.Children.Add(new Separator());
            layout.Children.Add(scroll);
            layout.Children.Add(buttons);

            Border card = new Border();
            card.Background = (Brush)this.resourceSource.FindResource("Brush.Surface");
            card.BorderBrush = (Brush)this.resourceSource.FindResource("Brush.Border");
            card.BorderThickness = new Thickness(1d);
            card.CornerRadius = (CornerRadius)this.resourceSource.FindResource("Radius.Sm");
            card.Padding = new Thickness(10d, 8d, 10d, 8d);
            card.MinWidth = 170d;
            card.MaxWidth = 260d;
            card.Child = layout;

            System.Windows.Documents.TextElement.SetForeground(
                    card, (Brush)this.resourceSource.FindResource("Brush.TextPrimary"));
            return card;
        }

        // 검색 입력이 바뀌면 보이는 고유 값 목록을 좁히고 **선택도 그 결과로
        // 맞춘다** — 초성 매칭. 좁혀 놓고 체크는 "(All)"로 두면 Apply가 전체를
        // 내보내서 검색이 아무 일도 하지 않는다(2026-09-20). 검색어를 지우면
        // 다시 전체 선택으로 돌아간다. 제안 드롭다운은 쓰지 않는다(목록 자체가
        // 좁혀지는데 드롭다운이 겹치면 혼란).
        private void OnPopupSearchTextChanged(object sender, EventArgs e)
        {
            string keyword = this.popupSearch == null ? string.Empty : this.popupSearch.Text;

            if (keyword.Trim().Length == 0)
            {
                this.popupPending = null;
            }
            else
            {
                HashSet<string> matched = new HashSet<string>(StringComparer.Ordinal);

                foreach (string value in this.popupValues)
                {
                    if (HangulTextMatcher.Contains(value, keyword))
                    {
                        matched.Add(value);
                    }
                }

                this.popupPending = matched.Count == this.popupValues.Count ? null : matched;
            }

            this.suppressPopupEvents = true;
            this.RebuildPopupValueItems();
            this.popupAll.IsChecked = this.popupPending == null;
            this.suppressPopupEvents = false;
        }

        private void RebuildPopupValueItems()
        {
            string keyword = this.popupSearch == null ? string.Empty : this.popupSearch.Text;
            int matchCount = 0;

            this.popupList.Children.Clear();

            foreach (string value in this.popupValues)
            {
                if (!HangulTextMatcher.Contains(value, keyword))
                {
                    continue;
                }

                CheckBox item = new CheckBox();
                item.Content = value.Length == 0 ? "(Blanks)" : value;
                item.Tag = value;
                item.Margin = new Thickness(2d);
                item.IsChecked = this.popupPending == null || this.popupPending.Contains(value);
                item.Checked += this.OnValueToggled;
                item.Unchecked += this.OnValueToggled;
                this.popupList.Children.Add(item);
                matchCount = matchCount + 1;
            }

            if (matchCount == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "No matching values";
                empty.Margin = new Thickness(2d);
                empty.Foreground = (Brush)this.resourceSource.FindResource("Brush.TextSecondary");
                this.popupList.Children.Add(empty);
            }
        }

        // "(All)": 체크 = 전체 선택, 해제 = 아무 값도 선택 안 함 — 편집 중
        // 상태(popupPending)만 바꾸고 반영은 Apply가 한다.
        private void OnAllToggled(object sender, RoutedEventArgs e)
        {
            if (this.suppressPopupEvents)
            {
                return;
            }

            bool all = this.popupAll.IsChecked == true;
            this.suppressPopupEvents = true;

            foreach (object child in this.popupList.Children)
            {
                CheckBox item = child as CheckBox;

                if (item != null)
                {
                    item.IsChecked = all;
                }
            }

            this.suppressPopupEvents = false;

            this.popupPending = all ? null : new HashSet<string>(StringComparer.Ordinal);
        }

        // 값 체크 변경: 검색으로 일부 값만 보일 때도 보이지 않는 값의 선택
        // 상태는 유지한다 — 편집 중 상태(popupPending)만 갱신하고 반영은
        // Apply가 한다. 전체 고유 값이 체크되면 "필터 없음"(null)으로 접는다.
        private void OnValueToggled(object sender, RoutedEventArgs e)
        {
            if (this.suppressPopupEvents)
            {
                return;
            }

            HashSet<string> selected = this.popupPending == null
                    ? new HashSet<string>(this.popupValues, StringComparer.Ordinal)
                    : new HashSet<string>(this.popupPending, StringComparer.Ordinal);

            foreach (object child in this.popupList.Children)
            {
                CheckBox item = child as CheckBox;

                if (item == null)
                {
                    continue;
                }

                if (item.IsChecked == true)
                {
                    selected.Add((string)item.Tag);
                }
                else
                {
                    selected.Remove((string)item.Tag);
                }
            }

            bool allChecked = selected.Count == this.popupValues.Count;
            this.popupPending = allChecked ? null : selected;

            this.suppressPopupEvents = true;
            this.popupAll.IsChecked = allChecked;
            this.suppressPopupEvents = false;
        }

        // Apply — 팝업에서 모은 선택을 확정하고 한 번에 반영한다.
        private void OnApplyClick(object sender, RoutedEventArgs e)
        {
            if (this.popupPending == null)
            {
                this.selections.Remove(this.popupColumn);
            }
            else
            {
                this.selections[this.popupColumn] =
                        new HashSet<string>(this.popupPending, StringComparer.Ordinal);
            }

            this.ApplyFilters();
            this.UpdateActiveFlags(this.popupColumn);
            this.RaiseFiltersChanged();
            this.popup.IsOpen = false;
        }

        // Clear — 이 컬럼의 필터를 **즉시 해제**하고 닫는다 ("(All) 체크 +
        // Apply"와 같은 결과의 단축 버튼).
        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            this.selections.Remove(this.popupColumn);
            this.ApplyFilters();
            this.UpdateActiveFlags(this.popupColumn);
            this.RaiseFiltersChanged();
            this.popup.IsOpen = false;
        }

        // ✕(Cancel) — 편집 중 변경을 버리고 닫는다 (바깥 클릭으로 닫는 것과 동일).
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.popup.IsOpen = false;
        }

        // ===== 값 수집 + 필터 적용 =====

        // 컬럼의 고유 값 목록 — 다른 컬럼 필터와 무관하게 원본 전체에서 모은다.
        private List<string> CollectDistinctValues(string columnName)
        {
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            List<string> values = new List<string>();
            IEnumerable items = this.ResolveValueItems();

            if (items != null)
            {
                foreach (object item in items)
                {
                    string value = ReadValue(item, columnName);

                    if (unique.Add(value))
                    {
                        values.Add(value);

                        if (values.Count >= maxDistinctValues)
                        {
                            break;
                        }
                    }
                }
            }

            values.Sort(StringComparer.OrdinalIgnoreCase);
            return values;
        }

        // 고유 값을 모을 행 목록을 정한다 — ValueSource(전체 결과)가 지정돼
        // 있으면 그것을, 아니면 현재 ItemsSource를 쓴다. DataView는 CustomFilter가
        // 이미 걸려 있을 수 있으므로 원본 테이블에서 모은다.
        private IEnumerable ResolveValueItems()
        {
            object source = this.ValueSource;

            if (source == null)
            {
                source = this.itemsSourceGetter();
            }

            DataTable table = source as DataTable;

            if (table != null)
            {
                return table.Rows;
            }

            DataView view = source as DataView;

            if (view != null)
            {
                return view.Table.Rows;
            }

            return source as IEnumerable;
        }

        // 행(DataRow/DataRowView/POCO)에서 컬럼 값을 문자열로 읽는다.
        private static string ReadValue(object item, string columnName)
        {
            DataRow row = item as DataRow;
            DataRowView rowView = item as DataRowView;

            if (rowView != null)
            {
                row = rowView.Row;
            }

            if (row != null)
            {
                if (!row.Table.Columns.Contains(columnName))
                {
                    return string.Empty;
                }

                object cell = row[columnName];
                return cell == null || cell == DBNull.Value ? string.Empty : cell.ToString();
            }

            if (item == null)
            {
                return string.Empty;
            }

            // 일반 객체는 멤버 경로로 읽는다 — 점 경로("Row.QTY")도 해석되므로
            // 트리리스트의 노드처럼 원본 행을 품은 항목도 값이 나온다.
            object value = MemberPathReader.Read(item, columnName);
            return value == null ? string.Empty : value.ToString();
        }

        // 현재 선택을 뷰에 적용한다 — DataView는 CustomFilter 식, 그 외는
        // Filter 조건자. 소스 자체는 건드리지 않는다.
        internal void ApplyFilters()
        {
            // 소유자가 스스로 거르는 경우(트리리스트 — 걸린 노드의 조상을 남겨야
            // 계보가 끊기지 않는다)에는 뷰 필터를 걸지 않는다. 선택 상태와
            // FiltersChanged 알림만 쓰고 무엇을 감출지는 소유자가 정한다.
            if (this.ExternalFiltering)
            {
                return;
            }

            IEnumerable source = this.itemsSourceGetter();

            if (source == null)
            {
                return;
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(source);
            BindingListCollectionView bindingView = view as BindingListCollectionView;

            if (bindingView != null)
            {
                if (bindingView.CanCustomFilter)
                {
                    bindingView.CustomFilter = this.BuildDataViewFilter();
                }

                return;
            }

            if (view == null || !view.CanFilter)
            {
                return;
            }

            if (this.filterLease != null)
            {
                this.filterLease.Owner = null;
            }
            this.filterLease = new ViewFilter { Owner = this };
            this.ownedFilter = this.selections.Count == 0
                    ? (Predicate<object>)null
                    : new Predicate<object>(this.filterLease.Matches);
            this.filteredView = view;
            view.Filter = this.ownedFilter;
            view.Refresh();
        }

        private bool MatchesAllFilters(object item)
        {
            foreach (KeyValuePair<string, HashSet<string>> entry in this.selections)
            {
                if (!entry.Value.Contains(ReadValue(item, entry.Key)))
                {
                    return false;
                }
            }

            return true;
        }

        // DataView CustomFilter 식 — 컬럼별 "값 중 하나" 절을 AND로 잇는다.
        private string BuildDataViewFilter()
        {
            if (this.selections.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder filter = new StringBuilder();

            foreach (KeyValuePair<string, HashSet<string>> entry in this.selections)
            {
                if (filter.Length > 0)
                {
                    filter.Append(" AND ");
                }

                filter.Append(BuildColumnClause(entry.Key, entry.Value));
            }

            return filter.ToString();
        }

        // 한 컬럼의 값 절 — 타입 컬럼(int 등)도 문자열로 변환해 비교하고,
        // 빈 값 선택은 NULL/빈 문자열을 함께 포함한다. 아무 값도 선택하지
        // 않았으면 모두 숨긴다.
        private static string BuildColumnClause(string columnName, HashSet<string> values)
        {
            string safeColumn = "[" + columnName.Replace("]", "]]") + "]";
            string asText = "CONVERT(" + safeColumn + ", 'System.String')";
            StringBuilder clause = new StringBuilder("(");
            bool first = true;
            bool includeBlank = false;

            foreach (string value in values)
            {
                if (value.Length == 0)
                {
                    includeBlank = true;
                    continue;
                }

                if (!first)
                {
                    clause.Append(" OR ");
                }

                clause.Append(asText).Append(" = '").Append(value.Replace("'", "''")).Append("'");
                first = false;
            }

            if (includeBlank)
            {
                if (!first)
                {
                    clause.Append(" OR ");
                }

                clause.Append(safeColumn).Append(" IS NULL OR ").Append(asText).Append(" = ''");
                first = false;
            }

            if (first)
            {
                clause.Append("1 = 0");
            }

            clause.Append(")");
            return clause.ToString();
        }
    }
}
