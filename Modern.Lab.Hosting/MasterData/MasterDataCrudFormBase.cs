using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.Hosting.MasterData
{
    public abstract class MasterDataCrudFormBase : ModernFormBase
    {
        private const string ChannelList = "list";

        private enum ListState
        {
            Loading,
            Ready,
            ReadyEmpty,
            Blocked,
            Failed,
            Closed
        }

        private enum WriteKind
        {
            New,
            Save,
            Delete
        }

        private MasterDataCrudDefinition definition;
        private MasterDataCrudView view;
        private string[] keyColumns;
        private TableContract listContract;
        private ListState listState = ListState.Loading;
        private TableResponse itemList;
        private bool blockedNoticeShowing;
        private string[] pendingSelectKey;
        private int selectedIndex = -1;
        private bool restoringSelection;
        private bool writeLocked;
        private bool keywordWasEnabled;
        private bool searchWasEnabled;
        private bool gridWasEnabled;

        protected virtual MasterDataCrudView CreateView()
        {
            return new MasterDataCrudView
            {
                Keyword = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernTextBox>("txtKeyword")
                        ?? this.FindOnly<Modern.Lab.WinForms.Controls.Input.ModernTextBox>(),
                SearchButton = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernButton>("btnSearch"),
                Grid = this.FindOnly<Modern.Lab.WinForms.Controls.Data.ModernDataGrid>(),
                EditorCard = this.FindByName<Modern.Lab.WinForms.Controls.Layout.ModernGroupBox>("editorCard"),
                Editor = this.FindOnly<ModernPropertyGrid>(),
                NewButton = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernButton>("btnNew"),
                CancelButton = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernButton>("btnCancel"),
                SaveButton = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernButton>("btnSave"),
                DeleteButton = this.FindByName<Modern.Lab.WinForms.Controls.Input.ModernButton>("btnDelete"),
                ActionMenu = this.FindMenu("menuActions"),
                NewMenuItem = this.FindMenuItem("miNew"),
                CancelMenuItem = this.FindMenuItem("miCancel"),
                SaveMenuItem = this.FindMenuItem("miSave"),
                DeleteMenuItem = this.FindMenuItem("miDelete")
            };
        }

        protected void InitializeCrud(MasterDataCrudDefinition definition)
        {
            this.InitializeCrud(definition, this.CreateView());
        }

        private T FindByName<T>(string name) where T : Control
        {
            foreach (Control candidate in Descendants(this))
            {
                T typed = candidate as T;

                if (typed != null && candidate.Name == name)
                {
                    return typed;
                }
            }

            return null;
        }

        private T FindOnly<T>() where T : Control
        {
            T found = null;

            foreach (Control candidate in Descendants(this))
            {
                T typed = candidate as T;

                if (typed == null)
                {
                    continue;
                }

                if (found != null)
                {
                    return null;
                }

                found = typed;
            }

            return found;
        }

        private ContextMenuStrip FindMenu(string name)
        {
            foreach (Component component in this.MenuComponents())
            {
                ContextMenuStrip menu = component as ContextMenuStrip;

                if (menu != null && menu.Name == name)
                {
                    return menu;
                }
            }

            return null;
        }

        private ToolStripMenuItem FindMenuItem(string name)
        {
            foreach (Component component in this.MenuComponents())
            {
                ContextMenuStrip menu = component as ContextMenuStrip;

                if (menu == null)
                {
                    continue;
                }

                foreach (ToolStripItem item in menu.Items)
                {
                    ToolStripMenuItem typed = item as ToolStripMenuItem;

                    if (typed != null && typed.Name == name)
                    {
                        return typed;
                    }
                }
            }

            return null;
        }

        private IEnumerable<Component> MenuComponents()
        {
            System.Reflection.FieldInfo field = null;
            Type type = this.GetType();

            while (type != null && field == null)
            {
                field = type.GetField(
                        "components",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                type = type.BaseType;
            }

            IContainer container = field == null ? null : field.GetValue(this) as IContainer;

            if (container == null)
            {
                return new Component[0];
            }

            List<Component> components = new List<Component>();

            foreach (Component component in container.Components)
            {
                components.Add(component);
            }

            return components;
        }

        private static IEnumerable<Control> Descendants(Control root)
        {
            List<Control> found = new List<Control>();
            Collect(root, found);
            return found;
        }

        private static void Collect(Control parent, List<Control> found)
        {
            foreach (Control child in parent.Controls)
            {
                found.Add(child);
                Collect(child, found);
            }
        }

        private static void EnsureComplete(MasterDataCrudView view)
        {
            List<string> missing = new List<string>();

            if (view.Grid == null) { missing.Add("그리드 (ModernDataGrid 하나)"); }
            if (view.EditorCard == null) { missing.Add("editorCard (ModernGroupBox)"); }
            if (view.Editor == null) { missing.Add("속성 편집기 (ModernPropertyGrid 하나)"); }
            if (view.NewButton == null) { missing.Add("btnNew"); }
            if (view.CancelButton == null) { missing.Add("btnCancel"); }
            if (view.SaveButton == null) { missing.Add("btnSave"); }
            if (view.DeleteButton == null) { missing.Add("btnDelete"); }
            if (view.ActionMenu == null) { missing.Add("menuActions (ContextMenuStrip)"); }
            if (view.NewMenuItem == null) { missing.Add("miNew"); }
            if (view.CancelMenuItem == null) { missing.Add("miCancel"); }
            if (view.SaveMenuItem == null) { missing.Add("miSave"); }
            if (view.DeleteMenuItem == null) { missing.Add("miDelete"); }

            if (missing.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                    "CRUD 화면 배선이 비었다: " + string.Join(" · ", missing.ToArray())
                            + " — 디자이너 이름을 규칙대로 붙이거나 CreateView()를 재정의할 것.");
        }

        protected void InitializeCrud(MasterDataCrudDefinition definition, MasterDataCrudView view)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            EnsureComplete(view);

            this.definition = definition;
            this.view = view;
            this.keyColumns = ResolveKeyColumns(definition);
            this.listContract = new TableContract(definition.ListTableId).Key(this.keyColumns);

            this.InitializeModernForm();
            this.view.ActionMenu.Renderer = new Modern.Lab.WinForms.Rendering.ModernMenuRenderer();
            this.DeferredResize = true;
            this.RegisterFindShortcut(this.view.Grid);

            string keyText = this.CompositeKey ? string.Join(",", this.keyColumns) : this.keyColumns[0];
            this.view.Grid.RowKeyMember = keyText;
            this.view.Editor.KeyColumns = keyText;
            this.view.Editor.ParameterNameStyle = ParameterNameStyle.PascalCase;
            this.view.Editor.ValueChanged += this.OnEditorValueChanged;

            this.UpdateActionState();
        }

        // 복합 키(KeyColumns)가 없으면 단일 키(KeyColumn)를 그대로 쓴다 — 기존 화면의 값·동작이 바뀌지 않는다.
        private static string[] ResolveKeyColumns(MasterDataCrudDefinition definition)
        {
            List<string> columns = new List<string>();

            foreach (string column in definition.KeyColumns ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(column))
                {
                    columns.Add(column.Trim());
                }
            }

            if (columns.Count == 0)
            {
                return new string[] { definition.KeyColumn };
            }

            if (!string.IsNullOrWhiteSpace(definition.KeyColumn))
            {
                throw new InvalidOperationException(
                        "CRUD 정의에 KeyColumn 과 KeyColumns 가 함께 있다 — 단일 키는 KeyColumn, 복합 키는 KeyColumns 하나만 채울 것.");
            }

            return columns.ToArray();
        }

        private bool CompositeKey
        {
            get { return this.keyColumns.Length > 1; }
        }

        protected ModernPropertyGrid CrudEditor
        {
            get { return this.view.Editor; }
        }

        protected virtual void LoadLookupSources()
        {
        }

        protected virtual DataTable RequestLookupItems(string requestName, object[] requestFields)
        {
            throw new InvalidOperationException(
                    "콤보 조회 전문이 없다: " + requestName + " — 화면의 .Server.cs 에서 RequestLookupItems 를 재정의할 것.");
        }

        /// <summary>목록을 다시 조회한다. 저장·삭제가 진행 중이면 아무것도 하지 않는다 — 조회 시작이 편집기를 비워
        /// 쓰기 실패 뒤 입력을 잃지 않게 한다(검색·Enter·화면 코드의 직접 호출 모두).</summary>
        protected void ReloadItems()
        {
            if (this.ActionInProgress)
            {
                return;
            }

            string keyword = this.view.Keyword == null ? string.Empty : this.view.Keyword.Text.Trim();

            this.listState = ListState.Loading;
            this.UpdateActionState();

            this.OnListLoading();

            this.LoadAsync(ChannelList,
                    () => this.RequestItems(keyword),
                    this.BindItems,
                    this.AfterListSettled);
        }

        protected virtual void OnListLoading()
        {
        }

        /// <summary>
        /// 목록을 닫고 쓰기를 막는다 — 상위 선택이 풀린 화면이 쓴다. 진행 중이던 목록 응답은 늦게 와도 버린다.
        /// 표와 편집기를 비우는 것은 화면이 한다(<see cref="OnListLoading"/>과 같은 방식).
        /// </summary>
        protected void CloseItems()
        {
            this.InvalidateChannel(ChannelList);
            this.pendingSelectKey = null;
            this.itemList = null;
            this.listState = ListState.Closed;
            this.HideBlockedNotice();
            this.UpdateActionState();
        }

        /// <summary>폼이 조회 상태에서 정하는 신규 입력 값(컬럼 → 값, 예: 선택한 상위 키). 기본은 없음.
        /// 고정 기본값은 <c>.Grid.cs</c>에서 <c>CrudEditor.DefineDefaultValue</c>로 선언하고, 같은 컬럼이면 이 값이 이긴다.
        /// New와 선택 없는 목록 반영에서 쓰이고, 신규 중 Cancel은 이 값으로 되돌린다.</summary>
        protected virtual IDictionary<string, string> NewItemDefaults()
        {
            return null;
        }

        private void BeginNewItem()
        {
            this.view.Editor.BeginNew(this.NewItemDefaults());
        }

        /// <summary>목록 선택이 바뀌면 화면이 부른다. 저장·삭제 중에는 선택을 전송 당시 행으로 되돌리고 편집기를 바꾸지 않는다.</summary>
        protected void HandleSelectionChanged()
        {
            if (this.restoringSelection)
            {
                return;
            }

            if (this.ActionInProgress)
            {
                this.restoringSelection = true;

                try
                {
                    this.view.Grid.SelectedIndex = this.selectedIndex;
                }
                finally
                {
                    this.restoringSelection = false;
                }

                return;
            }

            this.selectedIndex = this.view.Grid.SelectedIndex;
            DataRow row = SelectedRow(this.view.Grid.SelectedItem);

            if (row != null && this.ListOpen)
            {
                this.view.Editor.LoadRow(row);
            }

            this.UpdateActionState();
        }

        protected void RefreshCrudActionState()
        {
            this.UpdateActionState();
        }

        protected void OnFormLoad(object sender, EventArgs e)
        {
            this.LoadLookupSources();
            this.ReloadItems();
        }

        protected void OnSearchClick(object sender, EventArgs e)
        {
            this.ReloadItems();
        }

        protected void OnKeywordEnterPressed(object sender, EventArgs e)
        {
            this.ReloadItems();
        }

        protected void OnNewClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.New))
            {
                return;
            }

            this.BeginNewItem();
            this.view.Editor.FocusFirstEditor();
            this.UpdateActionState();
        }

        protected void OnCancelClick(object sender, EventArgs e)
        {
            if (!this.CanCancel)
            {
                return;
            }

            DataRow row = SelectedRow(this.view.Grid.SelectedItem);

            if (this.view.Editor.IsNew && row != null)
            {
                this.view.Editor.LoadRow(row);
            }
            else
            {
                this.view.Editor.RevertEdits();
            }

            this.UpdateActionState();
        }

        protected void OnSaveClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.Save) || !this.ValidateRequired())
            {
                return;
            }

            object[] requestFields = this.view.Editor.ToRequestFields();
            string[] key = this.ReadKeyValues();
            bool isNew = this.view.Editor.IsNew;

            this.RunAction(
                    () => isNew ? this.InsertItem(requestFields) : this.UpdateItem(requestFields),
                    reply => this.AfterSaved(reply, key, isNew),
                    isNew ? "Inserting " + this.EntityNameLower + "…" : "Updating " + this.EntityNameLower + "…");
        }

        protected void OnDeleteClick(object sender, EventArgs e)
        {
            if (!this.CanWrite(WriteKind.Delete))
            {
                return;
            }

            string[] key = this.ReadKeyValues();

            if (!this.Confirm(
                    "Delete " + this.EntityNameLower + " '" + DisplayKey(key) + "'?",
                    "Delete " + this.definition.EntityName))
            {
                return;
            }

            this.RunAction(
                    () => this.DeleteItem(key),
                    this.AfterDeleted,
                    "Deleting " + this.EntityNameLower + "…");
        }

        protected virtual void OnActionMenuOpening(object sender, CancelEventArgs e)
        {
            this.UpdateActionState();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && this.CanCancel && !this.view.Grid.IsFindPanelOpen)
            {
                this.OnCancelClick(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private string EntityNameLower
        {
            get { return this.definition.EntityName.ToLowerInvariant(); }
        }

        private bool ListOpen
        {
            get { return this.listState == ListState.Ready || this.listState == ListState.ReadyEmpty; }
        }

        private bool CanCancel
        {
            get
            {
                if (!this.ListOpen || this.ActionInProgress)
                {
                    return false;
                }

                return this.view.Editor.IsDirty
                        || (this.view.Editor.IsNew && this.view.Grid.SelectedItem != null);
            }
        }

        private bool SelectedRowValid
        {
            get
            {
                DataRow row = SelectedRow(this.view.Grid.SelectedItem);

                if (row == null || this.itemList == null || this.itemList.Table == null)
                {
                    return false;
                }

                int index = this.itemList.Table.Rows.IndexOf(row);
                return index >= 0 && this.itemList.IsRowValid(index);
            }
        }

        /// <summary>목록 조회 전문 — 화면의 <c>.Server.cs</c>가 정의한다(★ 교체 지점). <paramref name="keyword"/>는
        /// 조회 카드에 <c>txtKeyword</c>가 있을 때 그 값이다. <b>백그라운드 스레드에서 불린다</b> — 조건이 다른 화면은
        /// 컨트롤을 여기서 읽지 말고 UI 스레드에서 도는 <see cref="OnListLoading"/>에서 필드에 담아 둔 뒤 그 값을 쓴다.</summary>
        protected abstract DataTable RequestItems(string keyword);

        /// <summary>등록 전문 — <paramref name="requestFields"/>는 편집기 컬럼 전부의 이름/값 쌍(자동 생성).</summary>
        protected abstract DataActionResult InsertItem(object[] requestFields);

        /// <summary>수정 전문 — <paramref name="requestFields"/>는 편집기 컬럼 전부의 이름/값 쌍(자동 생성).</summary>
        protected abstract DataActionResult UpdateItem(object[] requestFields);

        /// <summary>단일 키 삭제 전문 — <paramref name="key"/>는 편집기의 키 값. 단일 키 화면이 재정의한다.</summary>
        protected virtual DataActionResult DeleteItem(string key)
        {
            throw new InvalidOperationException(
                    "삭제 전문이 없다 — 화면의 .Server.cs 에서 DeleteItem 을 재정의할 것.");
        }

        /// <summary>삭제 전문 — <paramref name="keyValues"/>는 정의의 키 컬럼 순서대로 편집기의 키 값이다.
        /// 기본은 단일 키 <see cref="DeleteItem(string)"/>으로 넘긴다. 복합 키 화면은 이것을 재정의한다.</summary>
        protected virtual DataActionResult DeleteItem(string[] keyValues)
        {
            return this.DeleteItem(keyValues.Length == 0 ? string.Empty : keyValues[0]);
        }

        /// <summary><c>MethodCommand</c> 쌍을 앞에 붙인 이름/값 배열 — <c>.Server.cs</c>가 전문을 조립할 때 쓴다.</summary>
        protected static object[] WithMethodCommand(string methodCommand, object[] fields)
        {
            int fieldCount = fields == null ? 0 : fields.Length;
            object[] result = new object[fieldCount + 2];
            result[0] = "MethodCommand";
            result[1] = methodCommand;

            if (fieldCount > 0)
            {
                Array.Copy(fields, 0, result, 2, fieldCount);
            }

            return result;
        }

        private void BindItems(DataTable table)
        {
            DataTable received = table ?? new DataTable();

            this.itemList = TableResponse.Read(
                    ResponseKind.Data, received, ColumnAliasCatalog.Empty, this.listContract);
            this.listState = this.ListStateOf(this.itemList);

            if (this.listState == ListState.Blocked)
            {
                this.pendingSelectKey = null;
                this.view.Editor.SetSchema(new DataTable());
                this.view.Editor.BeginNew();
                this.view.Grid.DataSource = received;
                this.ShowBlockedNotice();
                this.UpdateActionState();
                return;
            }

            this.HideBlockedNotice();

            DataTable bound = this.itemList.Table;
            this.view.Editor.SetSchema(bound);
            this.view.Grid.DataSource = bound;

            if (HasKey(this.pendingSelectKey))
            {
                this.SelectByKey(bound, this.pendingSelectKey);
            }

            this.pendingSelectKey = null;

            if (this.view.Grid.SelectedItem == null)
            {
                this.BeginNewItem();
            }
            else
            {
                this.view.Editor.LoadRow(SelectedRow(this.view.Grid.SelectedItem));
            }

            this.UpdateActionState();
        }

        private void AfterListSettled(bool current)
        {
            if (!current || this.listState != ListState.Loading)
            {
                return;
            }

            this.pendingSelectKey = null;
            this.itemList = TableResponse.Read(
                    ResponseKind.Failed,
                    null,
                    ColumnAliasCatalog.Empty,
                    this.listContract,
                    string.Empty,
                    this.definition.EntityName + " list could not be loaded.");
            this.listState = ListState.Failed;
            this.UpdateActionState();
        }

        private ListState ListStateOf(TableResponse response)
        {
            if (response.State == TableResponseState.Failed)
            {
                return ListState.Failed;
            }

            if (response.ReceivedColumns.Count == 0
                    || response.State == TableResponseState.MissingRequired
                    || this.MissingEditorColumns(response).Length > 0
                    || this.ReservedEditorColumns(response).Length > 0)
            {
                return ListState.Blocked;
            }

            return response.State == TableResponseState.Empty ? ListState.ReadyEmpty : ListState.Ready;
        }

        private string[] MissingEditorColumns(TableResponse response)
        {
            List<string> missing = new List<string>();
            string[] required = this.definition.RequiredEditorColumns ?? new string[0];

            foreach (string column in required)
            {
                bool found = false;

                foreach (string received in response.ReceivedColumns)
                {
                    if (string.Equals(received, column, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missing.Add(column);
                }
            }

            return missing.ToArray();
        }

        private string[] ReservedEditorColumns(TableResponse response)
        {
            List<string> reserved = new List<string>();

            foreach (string received in response.ReceivedColumns)
            {
                string parameterName = ParameterNameConverter.Convert(
                        received, this.view.Editor.ParameterNameStyle);

                if (string.Equals(parameterName, "MethodCommand", StringComparison.OrdinalIgnoreCase))
                {
                    reserved.Add(received);
                }
            }

            return reserved.ToArray();
        }

        private bool CanWrite(WriteKind kind)
        {
            if (!this.ListOpen || this.ActionInProgress)
            {
                return false;
            }

            if (kind == WriteKind.New)
            {
                return true;
            }

            if (this.view.Editor.IsNew)
            {
                return kind == WriteKind.Save;
            }

            return this.SelectedRowValid;
        }

        private void ShowBlockedNotice()
        {
            List<string> details = new List<string>();
            List<string> missing = new List<string>(ToArray(this.itemList.MissingColumns));
            missing.AddRange(this.MissingEditorColumns(this.itemList));

            if (missing.Count > 0)
            {
                details.Add("missing " + string.Join(", ", missing.ToArray()));
            }

            string[] reserved = this.ReservedEditorColumns(this.itemList);

            if (reserved.Length > 0)
            {
                details.Add("reserved " + string.Join(", ", reserved));
            }

            string text = this.itemList.ReceivedColumns.Count == 0
                    ? this.definition.EntityName + " list did not match the contract (no columns) — writes are disabled."
                    : this.definition.EntityName + " list did not match the contract ("
                            + string.Join("; ", details.ToArray()) + ") — writes are disabled.";

            this.blockedNoticeShowing = true;
            this.ShowStickyNotice(text, Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
        }

        private void HideBlockedNotice()
        {
            if (!this.blockedNoticeShowing)
            {
                return;
            }

            this.blockedNoticeShowing = false;
            this.HideNotice();
        }

        private static string[] ToArray(IList<string> values)
        {
            string[] result = new string[values.Count];
            values.CopyTo(result, 0);
            return result;
        }

        private void SelectByKey(DataTable table, string[] key)
        {
            if (table == null || key.Length != this.keyColumns.Length)
            {
                return;
            }

            foreach (string column in this.keyColumns)
            {
                if (!table.Columns.Contains(column))
                {
                    return;
                }
            }

            for (int i = 0; i < table.Rows.Count; i = i + 1)
            {
                if (this.RowHasKey(table.Rows[i], key))
                {
                    this.view.Grid.SelectedIndex = i;
                    return;
                }
            }
        }

        private bool RowHasKey(DataRow row, string[] key)
        {
            for (int i = 0; i < this.keyColumns.Length; i = i + 1)
            {
                if (!string.Equals(
                        Convert.ToString(row[this.keyColumns[i]]),
                        key[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 저장·삭제·재선택에 쓰는 키 값(정의의 키 컬럼 순서). 단일 키는 편집기 키의 앞뒤 공백을 지운다(기존 동작).
        /// 복합 키는 신규만 공백을 지우고, 기존 행은 조회한 행의 원값을 그대로 쓴다 — 서버가 공백을 구별해도
        /// 같은 행을 가리킨다. 단일 키 화면이 원값을 써야 하면 재정의한다. UI 스레드에서 불린다.
        /// </summary>
        protected virtual string[] ReadKeyValues()
        {
            if (!this.CompositeKey)
            {
                return new string[] { this.view.Editor.ReadKey().Trim() };
            }

            DataRow stored = this.view.Editor.IsNew ? null : SelectedRow(this.view.Grid.SelectedItem);
            string[] values = new string[this.keyColumns.Length];

            for (int i = 0; i < this.keyColumns.Length; i = i + 1)
            {
                string column = this.keyColumns[i];
                values[i] = stored != null && stored.Table.Columns.Contains(column)
                        ? Convert.ToString(stored[column])
                        : this.view.Editor.ReadValue(column).Trim();
            }

            return values;
        }

        private static string DisplayKey(string[] key)
        {
            return string.Join(" / ", key);
        }

        private static bool HasKey(string[] key)
        {
            if (key == null || key.Length == 0)
            {
                return false;
            }

            foreach (string value in key)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
            }

            return true;
        }

        private static DataRow SelectedRow(object item)
        {
            DataRowView view = item as DataRowView;

            if (view != null)
            {
                return view.Row;
            }

            return item as DataRow;
        }

        private void OnEditorValueChanged(object sender, EventArgs e)
        {
            this.UpdateActionState();
        }

        private bool ValidateRequired()
        {
            string[] missing = this.view.Editor.MissingRequired();

            if (missing.Length == 0)
            {
                return true;
            }

            List<string> captions = new List<string>();

            foreach (string column in missing)
            {
                captions.Add(this.view.Editor.CaptionOf(column));
            }

            this.ShowToast(
                    "Required: " + string.Join(", ", captions.ToArray()),
                    Modern.Lab.Controls.Wpf.Display.ToastKind.Warning);
            this.view.Editor.FocusFirstEditor();

            return false;
        }

        private void AfterSaved(DataActionResult reply, string[] selectKey, bool isNew)
        {
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Done.");
            this.pendingSelectKey = this.SavedItemKey(reply, selectKey, isNew);
            this.ReloadItems();
        }

        /// <summary>
        /// 저장 성공 뒤 재조회에서 다시 선택할 키. 기본: 복합 키는 저장한 키, 단일 키는 등록·수정 모두 서버가 돌려준 키를
        /// 먼저 쓰고 없으면 저장한 키를 쓴다. 서버 반환 키 대신 조회 원값으로 찾아야 하는 화면만 재정의한다.
        /// </summary>
        protected virtual string[] SavedItemKey(DataActionResult reply, string[] savedKey, bool isNew)
        {
            return this.CompositeKey
                    ? savedKey
                    : new string[] { ReturnedKey(reply) ?? savedKey[0] };
        }

        private void AfterDeleted(DataActionResult reply)
        {
            this.ShowToast(reply.Message.Length > 0 ? reply.Message : "Done.");
            this.pendingSelectKey = null;
            this.ReloadItems();
        }

        private static string ReturnedKey(DataActionResult reply)
        {
            if (reply == null || reply.Data.Length == 0)
            {
                return null;
            }

            string[] values = reply.Values;

            return values.Length > 0 && values[0].Trim().Length > 0 ? values[0].Trim() : null;
        }

        /// <summary>
        /// 저장·삭제가 시작되면 조회어·조회 버튼·목록·편집기를 잠그고, 끝나면(성공·실패·예외) 잠그기 전 상태로 되돌린다.
        /// 화면에만 있는 조회 입력(상위 선택 콤보 등)은 <see cref="OnWriteLockChanged"/>에서 같이 잠근다.
        /// </summary>
        protected override void OnActionStateChanged(bool running)
        {
            base.OnActionStateChanged(running);

            if (this.view == null || running == this.writeLocked)
            {
                return;
            }

            this.writeLocked = running;

            if (running)
            {
                this.keywordWasEnabled = this.view.Keyword != null && this.view.Keyword.Enabled;
                this.searchWasEnabled = this.view.SearchButton != null && this.view.SearchButton.Enabled;
                this.gridWasEnabled = this.view.Grid.Enabled;
            }

            if (this.view.Keyword != null)
            {
                this.view.Keyword.Enabled = !running && this.keywordWasEnabled;
            }

            if (this.view.SearchButton != null)
            {
                this.view.SearchButton.Enabled = !running && this.searchWasEnabled;
            }

            this.view.Grid.Enabled = !running && this.gridWasEnabled;
            this.UpdateActionState();
            this.OnWriteLockChanged(running);
        }

        /// <summary>쓰기 중 잠금이 걸리거나(<paramref name="locked"/> true) 풀린 직후 불린다. 기본은 아무것도 하지 않는다.</summary>
        protected virtual void OnWriteLockChanged(bool locked)
        {
        }

        private void UpdateActionState()
        {
            this.view.Editor.Enabled = this.ListOpen && !this.ActionInProgress;
            this.view.NewButton.Enabled = this.CanWrite(WriteKind.New);
            this.view.CancelButton.Enabled = this.CanCancel;
            this.view.SaveButton.Enabled = this.CanWrite(WriteKind.Save);
            this.view.DeleteButton.Enabled = this.CanWrite(WriteKind.Delete);
            this.view.NewMenuItem.Enabled = this.view.NewButton.Enabled;
            this.view.CancelMenuItem.Enabled = this.view.CancelButton.Enabled;
            this.view.SaveMenuItem.Enabled = this.view.SaveButton.Enabled;
            this.view.DeleteMenuItem.Enabled = this.view.DeleteButton.Enabled;
            this.view.EditorCard.Text = this.view.Editor.IsNew
                    ? this.definition.EntityName + " — New"
                    : this.definition.EntityName + " — " + this.EditorKeyText();
        }

        private string EditorKeyText()
        {
            if (!this.CompositeKey)
            {
                return this.view.Editor.ReadKey();
            }

            string[] values = new string[this.keyColumns.Length];

            for (int i = 0; i < this.keyColumns.Length; i = i + 1)
            {
                values[i] = this.view.Editor.ReadValue(this.keyColumns[i]);
            }

            return DisplayKey(values);
        }
    }
}
