using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Controls.Wpf.Display;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.WinForms.Controls.Display;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.Hosting.MasterData
{
    /// <summary>
    /// 표 스키마로 편집기를 스스로 구성하는 속성 편집기.
    ///
    /// 서버가 SELECT한 컬럼을 <b>그 순서대로</b> "라벨 + 편집 컨트롤" 행으로 전부 뿌린다.
    /// 컬럼 수가 늘면 패널 폭에 맞춰 열을 나눠 흘려 넣는다(<see cref="MinColumnWidth"/>마다 한 열).
    /// 기본 편집기는 텍스트박스이고, 폼이 <see cref="DefineCombo"/>·<see cref="DefineToggle"/>로
    /// 지정한 컬럼만 콤보박스·스위치가 된다. 라벨은 캡션 사전(<see cref="GridCaptionCatalog.Resolve"/>)을 쓴다.
    ///
    /// 콤보 항목은 전문으로 채운다 — <see cref="DefineCombo"/>에 전문 이름·필드를 적고, 폼이
    /// <see cref="FetchComboSources"/>(작업 스레드) → <see cref="ApplyComboSources"/>(UI 스레드)를
    /// 자기 <c>LoadAsync</c>로 돌린다. 편집기는 서버를 모르고 폼은 컬럼 종류만 안다.
    /// </summary>
    [Designer("System.Windows.Forms.Design.ControlDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ModernPropertyGrid : Panel
    {
        private const int ColumnGap = 16;

        private int editorHeight = 28;
        private int multilineHeight = 72;
        private int rowGap = 4;

        private readonly TableLayoutPanel layout;
        private readonly List<FieldEditor> editors = new List<FieldEditor>();
        private readonly HashSet<string> keyColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> readOnlyColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> requiredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FieldDefinition> definitions =
                new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DataTable> comboSources =
                new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> defaultValues =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, string> baseline =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string keyColumnText = string.Empty;
        private string[] schemaColumns = new string[0];
        private int labelWidth = 140;
        private int minColumnWidth = 420;
        private int layoutColumns = 1;
        private bool isNew = true;
        private ParameterNameStyle parameterNameStyle = ParameterNameStyle.PascalCase;

        /// <summary>편집 값이 바뀔 때 발생한다(어느 컬럼이든).</summary>
        public event EventHandler ValueChanged;

        public ModernPropertyGrid()
        {
            this.AutoScroll = true;
            this.BackColor = Color.Transparent;
            this.Padding = new Padding(4, 4, 12, 4);

            this.layout = new TableLayoutPanel();
            this.layout.AutoSize = true;
            this.layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.layout.Dock = DockStyle.Top;
            this.layout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            this.layout.BackColor = Color.Transparent;

            this.Controls.Add(this.layout);
        }

        // ===== 설정 =====

        /// <summary>
        /// 키 컬럼 이름(쉼표 구분). 새 행에서는 편집할 수 있고, 기존 행을 불러오면 읽기 전용이 된다.
        /// 폼이 이름을 알아야 하는 유일한 컬럼이다.
        /// </summary>
        [Category("Modern")]
        [Description("Key column names (comma separated). Editable only for a new row.")]
        [DefaultValue("")]
        public string KeyColumns
        {
            get
            {
                return this.keyColumnText;
            }
            set
            {
                this.keyColumnText = value ?? string.Empty;
                this.keyColumns.Clear();

                foreach (string part in this.keyColumnText.Split(','))
                {
                    string name = part.Trim();

                    if (name.Length > 0)
                    {
                        this.keyColumns.Add(name);
                    }
                }

                this.ApplyKeyState();
            }
        }

        /// <summary>라벨 열 너비(px).</summary>
        [Category("Modern")]
        [Description("Width of each label column in pixels.")]
        [DefaultValue(140)]
        public int LabelWidth
        {
            get
            {
                return this.labelWidth;
            }
            set
            {
                this.labelWidth = Math.Max(60, value);
                this.Rebuild();
            }
        }

        /// <summary>한 열(라벨+편집기)이 차지할 최소 폭. 패널 폭을 이 값으로 나눈 만큼 열이 생긴다.</summary>
        [Category("Modern")]
        [Description("Minimum width of one label+editor column; the panel width divided by this gives the column count.")]
        [DefaultValue(420)]
        public int MinColumnWidth
        {
            get
            {
                return this.minColumnWidth;
            }
            set
            {
                this.minColumnWidth = Math.Max(200, value);
                this.RelayoutIfColumnsChanged();
            }
        }

        /// <summary>한 줄 입력칸 높이(px). 줄이면 같은 화면에 더 많은 행이 들어간다.</summary>
        [Category("Modern")]
        [Description("Height of a single-line editor in pixels.")]
        [DefaultValue(28)]
        public int EditorHeight
        {
            get { return this.editorHeight; }
            set { this.editorHeight = Math.Max(20, value); this.Arrange(this.layoutColumns); }
        }

        /// <summary>여러 줄 입력칸 높이(px).</summary>
        [Category("Modern")]
        [Description("Height of a multiline editor in pixels.")]
        [DefaultValue(72)]
        public int MultilineHeight
        {
            get { return this.multilineHeight; }
            set { this.multilineHeight = Math.Max(40, value); this.Arrange(this.layoutColumns); }
        }

        /// <summary>행과 행 사이 간격(px).</summary>
        [Category("Modern")]
        [Description("Vertical gap between rows in pixels.")]
        [DefaultValue(4)]
        public int RowGap
        {
            get { return this.rowGap; }
            set { this.rowGap = Math.Max(0, value); this.Arrange(this.layoutColumns); }
        }

        /// <summary>
        /// <see cref="ToRequestFields"/>가 컬럼 이름을 전문 파라미터 이름으로 바꾸는 방식.
        /// 기본은 PascalCase(<c>PROD_ID</c> → <c>ProdId</c>). 예외 표는 없다 — 규칙과 다른 이름이 필요하면 컬럼 쪽을 맞춘다.
        /// </summary>
        [Category("Modern")]
        [Description("How column names become request parameter names (PROD_ID -> ProdId).")]
        [DefaultValue(ParameterNameStyle.PascalCase)]
        public ParameterNameStyle ParameterNameStyle
        {
            get { return this.parameterNameStyle; }
            set { this.parameterNameStyle = value; }
        }

        /// <summary>새 행 입력 중이면 true, 기존 행을 불러온 상태면 false.</summary>
        [Browsable(false)]
        public bool IsNew
        {
            get { return this.isNew; }
        }

        /// <summary>현재 편집기가 나열한 컬럼 이름(스키마 순서).</summary>
        [Browsable(false)]
        public string[] Columns
        {
            get { return (string[])this.schemaColumns.Clone(); }
        }

        // ===== 컬럼 종류 지정 (폼이 아는 몇 개만) =====

        /// <summary>
        /// 이 컬럼을 콤보박스로 만들고, 항목은 <paramref name="requestName"/> 전문(<paramref name="requestFields"/>는
        /// 이름/값 쌍)으로 채운다. 응답 표의 <paramref name="valueMember"/>가 저장 값, <paramref name="displayMember"/>가 표시 글자다.
        /// </summary>
        public void DefineCombo(
                string column, string requestName, string valueMember, string displayMember, params object[] requestFields)
        {
            FieldDefinition definition = new FieldDefinition(EditorKind.Combo);
            definition.RequestName = requestName;
            definition.RequestFields = requestFields ?? new object[0];
            definition.ValueMember = string.IsNullOrEmpty(valueMember) ? "CODE" : valueMember;
            definition.DisplayMember = string.IsNullOrEmpty(displayMember) ? definition.ValueMember : displayMember;

            this.Define(column, definition);
        }

        /// <summary>이 컬럼을 콤보박스로 만들고, 항목은 폼이 직접 넘긴 표로 채운다(서버 조회가 없는 고정 목록용).</summary>
        public void DefineCombo(string column, DataTable items, string valueMember, string displayMember)
        {
            FieldDefinition definition = new FieldDefinition(EditorKind.Combo);
            definition.ValueMember = string.IsNullOrEmpty(valueMember) ? "CODE" : valueMember;
            definition.DisplayMember = string.IsNullOrEmpty(displayMember) ? definition.ValueMember : displayMember;

            this.comboSources[column] = items;
            this.Define(column, definition);
        }

        /// <summary>이 컬럼을 Y/N 스위치로 만든다.</summary>
        public void DefineToggle(string column)
        {
            this.Define(column, new FieldDefinition(EditorKind.Toggle));
        }

        /// <summary>이 컬럼을 여러 줄 텍스트로 만든다(한 행을 통째로 차지한다).</summary>
        public void DefineMultiline(string column)
        {
            this.Define(column, new FieldDefinition(EditorKind.MultilineText));
        }

        /// <summary>이 컬럼을 날짜 선택기로 만든다(값은 yyyy-MM-dd).</summary>
        public void DefineDate(string column)
        {
            this.Define(column, new FieldDefinition(EditorKind.Date));
        }

        /// <summary>이 컬럼을 숫자 입력으로 만든다.</summary>
        public void DefineNumber(string column, int decimalPlaces)
        {
            FieldDefinition definition = new FieldDefinition(EditorKind.Number);
            definition.DecimalPlaces = Math.Max(0, decimalPlaces);
            this.Define(column, definition);
        }

        /// <summary>
        /// 새 행 입력(<see cref="BeginNew()"/>)을 시작할 때 이 컬럼에 채울 값. 기존 행을 싣는 <see cref="LoadRow"/>에는
        /// 적용하지 않는다 — 조회 값이 비어 있어도 덮지 않는다. 응답에 없는 컬럼이면 무시된다.
        /// </summary>
        public void DefineDefaultValue(string column, string value)
        {
            if (string.IsNullOrEmpty(column))
            {
                return;
            }

            this.defaultValues[column] = value ?? string.Empty;
        }

        /// <summary>
        /// 이 컬럼들을 읽기 전용으로 만든다. 값은 보이지만 고칠 수 없고, 저장 전문에는 그대로 실린다.
        /// 입력칸은 테마의 읽기 전용(비활성) 색으로 그려진다. 키 컬럼을 넣으면 "신규일 때만 열린다"는
        /// 규칙을 덮어 신규에서도 잠기고 필수 검사에서도 빠진다 — 서버가 PK를 만드는 화면용이다.
        /// </summary>
        public void DefineReadOnly(params string[] columns)
        {
            foreach (string column in columns ?? new string[0])
            {
                if (!string.IsNullOrEmpty(column))
                {
                    this.readOnlyColumns.Add(column);
                }
            }

            this.ApplyKeyState();
        }

        /// <summary>이 컬럼들을 필수 입력으로 표시한다(라벨 별표 + 입력칸 빨간 바). 키 컬럼은 지정 없이 필수다.</summary>
        public void DefineRequired(params string[] columns)
        {
            foreach (string column in columns ?? new string[0])
            {
                if (!string.IsNullOrEmpty(column))
                {
                    this.requiredColumns.Add(column);
                }
            }

            foreach (FieldEditor editor in this.editors)
            {
                editor.SetRequired(this.IsRequired(editor.Column));
            }
        }

        /// <summary>
        /// 값이 비어 있는 필수 컬럼 이름들(스키마 순서). 비어 있으면 저장해도 된다.
        /// 읽기 전용 컬럼은 사용자가 채울 수 없으므로 필수라도 세지 않는다(서버가 만드는 키 등).
        /// </summary>
        public string[] MissingRequired()
        {
            List<string> missing = new List<string>();

            foreach (FieldEditor editor in this.editors)
            {
                if (editor.IsRequired && !editor.ReadOnly && editor.Read().Trim().Length == 0)
                {
                    missing.Add(editor.Column);
                }
            }

            return missing.ToArray();
        }

        /// <summary>컬럼의 표시 캡션 — 사전에 있으면 그것, 없으면 Humanize 결과.</summary>
        public string CaptionOf(string column)
        {
            return GridCaptionCatalog.Resolve(column) ?? GridCaptionCatalog.Humanize(column);
        }

        private bool IsRequired(string column)
        {
            return this.keyColumns.Contains(column) || this.requiredColumns.Contains(column);
        }

        /// <summary>지정을 지워 기본 텍스트박스로 되돌린다.</summary>
        public void DefineText(string column)
        {
            if (this.definitions.Remove(column))
            {
                this.Rebuild();
            }
        }

        private void Define(string column, FieldDefinition definition)
        {
            if (string.IsNullOrEmpty(column))
            {
                return;
            }

            this.definitions[column] = definition;

            if (this.schemaColumns.Length > 0)
            {
                this.Rebuild();
            }
        }

        // ===== 콤보 항목 채우기 =====

        /// <summary>
        /// 전문으로 선언된 콤보들의 항목을 한 번에 가져온다. <paramref name="send"/>는 (전문 이름, 필드 쌍) → 표이고,
        /// 보통 폼의 <c>RequestFields(action, fields).Table</c>이다. 작업 스레드에서 불러도 된다 — UI를 건드리지 않는다.
        /// 같은 전문·같은 필드는 한 번만 보낸다.
        /// </summary>
        public Dictionary<string, DataTable> FetchComboSources(Func<string, object[], DataTable> send)
        {
            Dictionary<string, DataTable> result = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);

            if (send == null)
            {
                return result;
            }

            Dictionary<string, DataTable> byRequest = new Dictionary<string, DataTable>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, FieldDefinition> entry in this.definitions)
            {
                FieldDefinition definition = entry.Value;

                if (definition.Kind != EditorKind.Combo || string.IsNullOrEmpty(definition.RequestName))
                {
                    continue;
                }

                string signature = definition.RequestSignature();
                DataTable table;

                if (!byRequest.TryGetValue(signature, out table))
                {
                    table = send(definition.RequestName, definition.RequestFields) ?? new DataTable();
                    byRequest[signature] = table;
                }

                result[entry.Key] = table;
            }

            return result;
        }

        /// <summary><see cref="FetchComboSources"/> 결과를 콤보에 싣는다. UI 스레드에서 부른다. 현재 편집 값은 유지된다.</summary>
        public void ApplyComboSources(Dictionary<string, DataTable> sources)
        {
            if (sources == null)
            {
                return;
            }

            foreach (KeyValuePair<string, DataTable> entry in sources)
            {
                this.comboSources[entry.Key] = entry.Value;

                FieldEditor editor = this.Find(entry.Key);

                if (editor != null && editor.Kind == EditorKind.Combo)
                {
                    // 바인딩이 첫 항목을 고르더라도 원래 값(빈 값 포함)을 다시 쓴다 — 항목 도착이 값을 바꾸지 않는다.
                    string kept = editor.Read();
                    editor.BindCombo(entry.Value);
                    editor.Write(kept);
                }
            }
        }

        // ===== 스키마 · 값 =====

        /// <summary>
        /// 표의 컬럼 순서대로 편집기를 구성한다. 컬럼 목록이 이전과 같으면 다시 만들지 않는다
        /// (조회 새로고침마다 깜빡이지 않도록).
        /// </summary>
        public void SetSchema(DataTable table)
        {
            string[] names = ColumnNames(table);

            if (SameNames(names, this.schemaColumns))
            {
                return;
            }

            this.schemaColumns = names;
            this.Rebuild();
        }

        /// <summary>기존 행을 편집기에 싣는다. 키 컬럼은 읽기 전용이 된다.</summary>
        public void LoadRow(DataRow row)
        {
            if (row == null)
            {
                this.BeginNew();
                return;
            }

            foreach (FieldEditor editor in this.editors)
            {
                editor.Write(row.Table.Columns.Contains(editor.Column)
                        ? Convert.ToString(row[editor.Column], CultureInfo.InvariantCulture)
                        : string.Empty);
            }

            this.isNew = false;
            this.ApplyKeyState();
            this.CaptureBaseline();
        }

        /// <summary>새 행 입력을 시작한다. 값을 비우고 <see cref="DefineDefaultValue"/>의 기본값을 채운 뒤 키 컬럼을 편집 가능하게 한다.</summary>
        public void BeginNew()
        {
            this.BeginNew(null);
        }

        /// <summary>
        /// 기본값을 채운 새 행 입력을 시작한다. <see cref="DefineDefaultValue"/>로 선언한 값을 먼저 채우고
        /// <paramref name="defaults"/>(폼이 조회 상태에서 정하는 값)를 그 위에 채운다 — 같은 컬럼이면 이쪽이 이긴다.
        /// 스키마에 없는 컬럼은 건너뛴다. 기본값이 되돌리기 기준이 되므로 신규 중 <see cref="RevertEdits"/>는 이 값으로 돌아간다.
        /// </summary>
        public void BeginNew(IDictionary<string, string> defaults)
        {
            this.ClearValues();
            this.WriteDefaults(this.defaultValues);
            this.WriteDefaults(defaults);
            this.isNew = true;
            this.ApplyKeyState();
            this.CaptureBaseline();
        }

        private void WriteDefaults(IDictionary<string, string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> value in values)
            {
                FieldEditor editor = this.Find(value.Key);

                if (editor != null)
                {
                    editor.Write(value.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// 마지막으로 불러온 상태(<see cref="LoadRow"/> · <see cref="BeginNew"/>) 이후 값이 하나라도 바뀌었나.
        /// 폼은 이것으로 되돌리기 버튼을 열고 닫는다 — 꺼진 버튼이 "잃을 것이 없다"는 표시가 되므로
        /// 확인 다이얼로그를 두지 않는다.
        /// </summary>
        [Browsable(false)]
        public bool IsDirty
        {
            get
            {
                foreach (FieldEditor editor in this.editors)
                {
                    if (!string.Equals(editor.Read(), this.BaselineOf(editor.Column), StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 편집 값을 마지막으로 불러온 상태로 되돌린다. 신규 여부와 키 잠금은 그대로다 —
        /// 신규 입력을 버리고 목록 선택으로 돌아가는 것은 폼이 <see cref="LoadRow"/>로 한다.
        /// </summary>
        public void RevertEdits()
        {
            foreach (FieldEditor editor in this.editors)
            {
                editor.Write(this.BaselineOf(editor.Column));
            }
        }

        private string BaselineOf(string column)
        {
            string original;
            return this.baseline.TryGetValue(column, out original) ? original : string.Empty;
        }

        private void CaptureBaseline()
        {
            this.baseline = this.ReadValues();
        }

        /// <summary>모든 편집기 값을 비운다(스위치는 N, 콤보는 선택 없음 — 첫 항목 기본값은 두지 않는다).</summary>
        public void ClearValues()
        {
            foreach (FieldEditor editor in this.editors)
            {
                editor.Write(string.Empty);
            }
        }

        /// <summary>편집기 값을 컬럼 이름 → 문자열로 읽는다(스키마 순서 유지).</summary>
        public Dictionary<string, string> ReadValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (FieldEditor editor in this.editors)
            {
                values[editor.Column] = editor.Read();
            }

            return values;
        }

        /// <summary>
        /// 편집 값 전부를 전문 파라미터 글자로 내놓는다 — <c>ProdId=PROD-0001 ProdNm=[300mm Bare Wafer] UseYn=Y …</c>.
        /// 이름은 <see cref="ParameterNameStyle"/>로 바뀌고(<c>PROD_ID</c> → <c>ProdId</c>), 공백이 든 값은
        /// <c>[…]</c>로 감싸져 있다. 순서는 스키마 순서다. <b>서버로 보낼 때는 이 글자를 쓰지 않는다</b> —
        /// 원문 문자열을 넘기는 <c>Request(string)</c>은 값을 감싸 주지 않으므로 <see cref="ToRequestFields"/>를
        /// 그대로 넘긴다. 이 속성은 사람이 읽는 진단·검사용이다.
        /// </summary>
        [Browsable(false)]
        public string RequestText
        {
            get { return ServerMessageFormat.BuildRequestText(string.Empty, this.ToRequestFields()).TrimStart(); }
        }

        /// <summary>
        /// 서버로 보내는 재료 — 이름/값이 번갈아 오는 배열. 폼은
        /// <c>RequestFields("ProductAction", fields)</c>로 넘기고, 값 감싸기는 그
        /// 조립 경로(<c>ServerMessageFormat.FormatValue</c>)가 맡는다. 원문 문자열을 직접 만들어
        /// <c>Request(string)</c>으로 보내면 감싸기가 적용되지 않는다.
        /// </summary>
        public object[] ToRequestFields()
        {
            List<object> pairs = new List<object>();

            foreach (FieldEditor editor in this.editors)
            {
                pairs.Add(this.ParameterNameOf(editor.Column));
                pairs.Add(editor.Read());
            }

            return pairs.ToArray();
        }

        /// <summary>컬럼 이름을 현재 <see cref="ParameterNameStyle"/>로 바꾼 전문 파라미터 이름.</summary>
        public string ParameterNameOf(string column)
        {
            return ParameterNameConverter.Convert(column, this.parameterNameStyle);
        }

        /// <summary>컬럼 하나의 편집 값. 없는 컬럼이면 빈 문자열.</summary>
        public string ReadValue(string column)
        {
            FieldEditor editor = this.Find(column);
            return editor == null ? string.Empty : editor.Read();
        }

        /// <summary>키 컬럼 값들을 순서대로 이어 붙인 문자열(단일 키면 그 값 그대로).</summary>
        public string ReadKey()
        {
            List<string> parts = new List<string>();

            foreach (FieldEditor editor in this.editors)
            {
                if (editor.IsKey)
                {
                    parts.Add(editor.Read());
                }
            }

            return string.Join(",", parts.ToArray());
        }

        /// <summary>첫 편집 가능한 편집기에 포커스를 준다.</summary>
        public void FocusFirstEditor()
        {
            foreach (FieldEditor editor in this.editors)
            {
                if (editor.Control.Enabled && !editor.ReadOnly)
                {
                    editor.Control.Focus();
                    return;
                }
            }
        }

        // ===== 배치 =====

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.RelayoutIfColumnsChanged();
        }

        private int ComputeColumns()
        {
            int usable = this.ClientSize.Width - this.Padding.Horizontal;
            return Math.Max(1, usable / Math.Max(1, this.minColumnWidth));
        }

        private void RelayoutIfColumnsChanged()
        {
            int columns = this.ComputeColumns();

            if (columns != this.layoutColumns && this.editors.Count > 0)
            {
                this.Arrange(columns);
            }
        }

        private void Rebuild()
        {
            Dictionary<string, string> kept = this.ReadValues();
            bool keptNew = this.isNew;

            this.SuspendLayout();
            this.layout.SuspendLayout();

            this.layout.Controls.Clear();

            foreach (FieldEditor old in this.editors)
            {
                old.Control.Dispose();
                old.Label.Dispose();
            }

            this.editors.Clear();

            foreach (string column in this.schemaColumns)
            {
                this.editors.Add(this.CreateEditor(column));
            }

            this.layout.ResumeLayout(false);
            this.ResumeLayout(false);

            this.Arrange(this.ComputeColumns());

            foreach (FieldEditor editor in this.editors)
            {
                string value;
                editor.Write(kept.TryGetValue(editor.Column, out value) ? value : string.Empty);
            }

            this.isNew = keptNew;
            this.ApplyKeyState();

            // 컬럼이 바뀌면 옛 기준값에 없는 컬럼이 생기고 없어진 컬럼이 남는다. 그대로 두면 값이
            // 그대로인데도 되돌리기가 열려 있는 것처럼 보인다. 그렇다고 통째로 다시 잡으면 **고치던
            // 값이 조용히 기준이 되어** 되돌릴 것이 사라진다(코덱스 리뷰 2026-09-13). 살아남은 컬럼의
            // 기준은 지키고, 새로 생긴 컬럼만 현재 값으로 채운다.
            Dictionary<string, string> rebuilt =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (FieldEditor editor in this.editors)
            {
                string original;
                rebuilt[editor.Column] = this.baseline.TryGetValue(editor.Column, out original)
                        ? original
                        : editor.Read();
            }

            this.baseline = rebuilt;
        }

        private void Arrange(int columns)
        {
            if (this.layout == null)
            {
                return;
            }

            this.layoutColumns = Math.Max(1, columns);

            this.SuspendLayout();
            this.layout.SuspendLayout();

            this.layout.Controls.Clear();
            this.layout.ColumnStyles.Clear();
            this.layout.RowStyles.Clear();
            this.layout.ColumnCount = this.layoutColumns * 2;
            this.layout.RowCount = 0;

            float editorPercent = 100F / this.layoutColumns;

            for (int i = 0; i < this.layoutColumns; i = i + 1)
            {
                this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, this.labelWidth));
                this.layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, editorPercent));
            }

            int row = 0;
            int slot = 0;

            foreach (FieldEditor editor in this.editors)
            {
                bool fullRow = editor.Kind == EditorKind.MultilineText;
                int height = fullRow ? this.multilineHeight : this.editorHeight;

                if (fullRow && slot > 0)
                {
                    row = row + 1;
                    slot = 0;
                }

                if (slot == 0)
                {
                    this.layout.RowCount = row + 1;
                    this.layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height + this.rowGap));
                }

                int labelColumn = slot * 2;
                int rightGap = slot == this.layoutColumns - 1 || fullRow ? 0 : ColumnGap;

                editor.Label.Margin = new Padding(0, this.rowGap / 2, 8, this.rowGap / 2);
                editor.Label.Dock = DockStyle.Fill;

                editor.Control.Margin = new Padding(0, this.rowGap / 2, rightGap, this.rowGap / 2);
                editor.Control.Height = height;
                editor.Control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

                this.layout.Controls.Add(editor.Label, labelColumn, row);
                this.layout.Controls.Add(editor.Control, labelColumn + 1, row);

                if (fullRow)
                {
                    this.layout.SetColumnSpan(editor.Control, this.layoutColumns * 2 - 1);
                    row = row + 1;
                    slot = 0;
                }
                else
                {
                    slot = slot + 1;

                    if (slot == this.layoutColumns)
                    {
                        slot = 0;
                        row = row + 1;
                    }
                }
            }

            this.layout.ResumeLayout(true);
            this.ResumeLayout(true);
        }

        private FieldEditor CreateEditor(string column)
        {
            FieldDefinition definition;

            if (!this.definitions.TryGetValue(column, out definition))
            {
                definition = FieldDefinition.Text;
            }

            bool isKey = this.keyColumns.Contains(column);

            ModernLabel label = new ModernLabel();
            label.Kind = LabelKind.Label;
            label.BackColor = Color.Transparent;
            label.Text = this.CaptionOf(column);

            Control control;

            switch (definition.Kind)
            {
                case EditorKind.Combo:
                    ModernComboBox combo = new ModernComboBox();
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    combo.ValueMember = definition.ValueMember;
                    combo.DisplayMember = definition.DisplayMember;
                    combo.SelectedIndexChanged += this.OnEditorChanged;
                    control = combo;
                    break;

                case EditorKind.Toggle:
                    ModernToggleSwitch toggle = new ModernToggleSwitch();
                    toggle.Text = "N";
                    toggle.CheckedChanged += this.OnToggleChanged;
                    control = toggle;
                    break;

                case EditorKind.Date:
                    ModernDatePicker date = new ModernDatePicker();
                    date.PlaceholderText = "yyyy-MM-dd";
                    date.ValueChanged += this.OnEditorChanged;
                    control = date;
                    break;

                case EditorKind.Number:
                    ModernNumericTextBox number = new ModernNumericTextBox();
                    number.DecimalPlaces = definition.DecimalPlaces;
                    number.ValueChanged += this.OnEditorChanged;
                    control = number;
                    break;

                case EditorKind.MultilineText:
                    ModernTextBox multiline = new ModernTextBox();
                    multiline.Multiline = true;
                    multiline.TextChanged += this.OnEditorChanged;
                    control = multiline;
                    break;

                default:
                    ModernTextBox text = new ModernTextBox();
                    text.TextChanged += this.OnEditorChanged;
                    control = text;
                    break;
            }

            FieldEditor editor = new FieldEditor(column, definition.Kind, isKey, label, control);
            editor.SetRequired(this.IsRequired(column));

            if (definition.Kind == EditorKind.Combo)
            {
                DataTable items;
                editor.BindCombo(this.comboSources.TryGetValue(column, out items) ? items : null);
            }

            return editor;
        }

        private void ApplyKeyState()
        {
            foreach (FieldEditor editor in this.editors)
            {
                editor.SetReadOnly(this.readOnlyColumns.Contains(editor.Column) || (editor.IsKey && !this.isNew));
            }
        }

        private FieldEditor Find(string column)
        {
            foreach (FieldEditor editor in this.editors)
            {
                if (string.Equals(editor.Column, column, StringComparison.OrdinalIgnoreCase))
                {
                    return editor;
                }
            }

            return null;
        }

        private void OnToggleChanged(object sender, EventArgs e)
        {
            ModernToggleSwitch toggle = sender as ModernToggleSwitch;

            if (toggle != null)
            {
                toggle.Text = toggle.Checked ? "Y" : "N";
            }

            this.OnEditorChanged(sender, e);
        }

        private void OnEditorChanged(object sender, EventArgs e)
        {
            EventHandler handler = this.ValueChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static string[] ColumnNames(DataTable table)
        {
            if (table == null)
            {
                return new string[0];
            }

            string[] names = new string[table.Columns.Count];

            for (int i = 0; i < table.Columns.Count; i = i + 1)
            {
                names[i] = table.Columns[i].ColumnName;
            }

            return names;
        }

        private static bool SameNames(string[] left, string[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i = i + 1)
            {
                if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private enum EditorKind
        {
            Text,
            MultilineText,
            Number,
            Date,
            Toggle,
            Combo
        }

        private sealed class FieldDefinition
        {
            internal static readonly FieldDefinition Text = new FieldDefinition(EditorKind.Text);

            internal FieldDefinition(EditorKind kind)
            {
                this.Kind = kind;
                this.RequestFields = new object[0];
                this.ValueMember = "CODE";
                this.DisplayMember = "NAME";
            }

            internal EditorKind Kind { get; private set; }

            internal string RequestName { get; set; }

            internal object[] RequestFields { get; set; }

            internal string ValueMember { get; set; }

            internal string DisplayMember { get; set; }

            internal int DecimalPlaces { get; set; }

            internal string RequestSignature()
            {
                List<string> parts = new List<string>();
                parts.Add(this.RequestName ?? string.Empty);

                foreach (object field in this.RequestFields)
                {
                    parts.Add(Convert.ToString(field, CultureInfo.InvariantCulture) ?? string.Empty);
                }

                return string.Join("|", parts.ToArray());
            }
        }

        private sealed class FieldEditor
        {
            internal FieldEditor(string column, EditorKind kind, bool isKey, ModernLabel label, Control control)
            {
                this.Column = column;
                this.Kind = kind;
                this.IsKey = isKey;
                this.Label = label;
                this.Control = control;
            }

            internal string Column { get; private set; }

            internal EditorKind Kind { get; private set; }

            internal bool IsKey { get; private set; }

            internal ModernLabel Label { get; private set; }

            internal Control Control { get; private set; }

            internal bool ReadOnly { get; private set; }

            internal bool IsRequired { get; private set; }

            internal void SetRequired(bool required)
            {
                this.IsRequired = required;
                this.Label.Required = required;

                ModernTextBox text = this.Control as ModernTextBox;
                if (text != null)
                {
                    text.Required = required;
                    return;
                }

                ModernComboBox combo = this.Control as ModernComboBox;
                if (combo != null)
                {
                    combo.Required = required;
                    return;
                }

                ModernDatePicker date = this.Control as ModernDatePicker;
                if (date != null)
                {
                    date.Required = required;
                    return;
                }

                ModernNumericTextBox number = this.Control as ModernNumericTextBox;
                if (number != null)
                {
                    number.Required = required;
                }
            }

            internal void BindCombo(DataTable items)
            {
                ModernComboBox combo = this.Control as ModernComboBox;

                if (combo != null)
                {
                    combo.DataSource = items;
                }
            }

            internal void SetReadOnly(bool readOnly)
            {
                this.ReadOnly = readOnly;

                ModernTextBox text = this.Control as ModernTextBox;

                if (text != null)
                {
                    text.ReadOnly = readOnly;
                    return;
                }

                this.Control.Enabled = !readOnly;
            }

            // 콤보에 실은 원래 값. 항목이 아직 없거나 값이 항목 코드에 없어 선택이 비어 있을 때
            // Read가 이 값을 그대로 내놓는다 — 서버가 준 값은 항목 도착 순서와 무관하게 보존된다
            // (상태 계약 product-master.md 표 ②, 리뷰 2026-09-12 P1-2).
            private string comboValue = string.Empty;

            internal string Read()
            {
                switch (this.Kind)
                {
                    case EditorKind.Combo:
                        ModernComboBox source = (ModernComboBox)this.Control;

                        if (source.SelectedIndex < 0)
                        {
                            return this.comboValue;
                        }

                        object selected = source.SelectedValue;
                        return selected == null ? this.comboValue : Convert.ToString(selected, CultureInfo.InvariantCulture);

                    case EditorKind.Toggle:
                        return ((ModernToggleSwitch)this.Control).Checked ? "Y" : "N";

                    case EditorKind.Date:
                        DateTime? date = ((ModernDatePicker)this.Control).Value;
                        return date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

                    case EditorKind.Number:
                        decimal? number = ((ModernNumericTextBox)this.Control).Value;
                        return number.HasValue ? number.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

                    default:
                        return ((ModernTextBox)this.Control).Text ?? string.Empty;
                }
            }

            internal void Write(string value)
            {
                string text = value ?? string.Empty;

                switch (this.Kind)
                {
                    case EditorKind.Combo:
                        // 빈 값은 선택 없음이다 — 첫 항목으로 바꾸지 않는다. 코드에 없는 값도 선택 없음으로
                        // 보이되 comboValue 로 남아 저장 전문에 원래 값 그대로 실린다.
                        ModernComboBox combo = (ModernComboBox)this.Control;
                        this.comboValue = text;
                        combo.SelectedIndex = -1;

                        if (text.Length > 0)
                        {
                            combo.SelectedValue = text;
                        }

                        break;

                    case EditorKind.Toggle:
                        ((ModernToggleSwitch)this.Control).Checked =
                                string.Equals(text.Trim(), "Y", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(text.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase);
                        break;

                    case EditorKind.Date:
                        ((ModernDatePicker)this.Control).Value = ParseDate(text);
                        break;

                    case EditorKind.Number:
                        decimal parsed;
                        ((ModernNumericTextBox)this.Control).Value =
                                decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
                                        ? parsed : (decimal?)null;
                        break;

                    default:
                        ((ModernTextBox)this.Control).Text = text;
                        break;
                }
            }

            private static DateTime? ParseDate(string text)
            {
                string trimmed = (text ?? string.Empty).Trim();

                if (trimmed.Length == 0)
                {
                    return null;
                }

                DateTime value;
                string[] formats = new string[] { "yyyy-MM-dd", "yyyyMMdd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd" };

                if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                {
                    return value;
                }

                if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                {
                    return value;
                }

                return null;
            }
        }
    }
}
