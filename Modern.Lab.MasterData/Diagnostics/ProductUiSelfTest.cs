using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Modern.Lab.Hosting;
using Modern.Lab.Hosting.Messaging;
using Modern.Lab.MasterData.Controls;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.MasterData.Diagnostics
{
    public static class ProductUiSelfTest
    {
        private static readonly StringBuilder report = new StringBuilder();
        private static int passCount;
        private static int failCount;
        private static string lastDialog = string.Empty;

        private const string SchemaMarker = "__schema_only__";
        private const string ListMethod = "MethodCommand=SelectProducts";
        private const string ComboMethodPrefix = "MethodCommand=Get";

        private static readonly string[] SchemaColumns = new string[]
        {
            "PROD_ID", "PRODUCT_NAME", "PRODUCT_TYPE", "UNIT", "USE_YN", "DESCRIPTION"
        };

        public static void Run()
        {
            Form pump = new Form();
            IntPtr forceHandle = pump.Handle;
            GC.KeepAlive(forceHandle);

            pump.BeginInvoke(new MethodInvoker(async delegate
            {
                try
                {
                    await RunAllAsync();
                }
                catch (Exception exception)
                {
                    Line("HARNESS 예외: " + exception);
                    failCount++;
                }
                finally
                {
                    Finish();
                    Application.ExitThread();
                }
            }));

            Application.Run();
        }

        private static async Task RunAllAsync()
        {
            TestSharedCrudSurface();
            await TestLoadingGate();
            await TestBlocked("0컬럼 응답", "[]", "no columns");
            await TestBlocked("PROD_ID 누락 응답",
                    Json(Row("PRODUCT_NAME", "No key column", "PRODUCT_TYPE", "WAFER")), "PROD_ID");
            await TestBlocked("PRODUCT_NAME 누락 응답",
                    Json(Row("PROD_ID", "P-1", "PRODUCT_TYPE", "WAFER")), "PRODUCT_NAME");
            await TestReadyEmptySchema();
            await TestFailedKeepsList();
            await TestRecoveryFromBlocked();
            await TestSaveAndReselect();
            await TestDisqualifiedRow();
            await TestReservedMethodCommand();
            await TestComboValuesListFirst();
            await TestComboValuesCombosFirst();
            await TestComboFailureKeepsValues();
            await TestCancel();
            await TestActionMenu();
            await TestEscape();
        }

        private static void TestSharedCrudSurface()
        {
            MethodInfo initialize = typeof(MasterDataCrudFormBase).GetMethod(
                    "InitializeCrud", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] definitionProperties = typeof(MasterDataCrudDefinition).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] viewProperties = typeof(MasterDataCrudView).GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);

            Check("공통 CRUD 형식 — 외부 업무 폼이 상속하고 초기화할 수 있다",
                    typeof(MasterDataCrudFormBase).IsPublic
                            && typeof(MasterDataCrudDefinition).IsPublic
                            && typeof(MasterDataCrudView).IsPublic
                            && initialize != null
                            && initialize.IsFamily
                            && definitionProperties.Length == 9
                            && viewProperties.Length == 13
                            && PublicReadWriteProperties(definitionProperties)
                            && PublicReadWriteProperties(viewProperties),
                    "base=" + typeof(MasterDataCrudFormBase).IsPublic
                            + " definition=" + typeof(MasterDataCrudDefinition).IsPublic
                            + " view=" + typeof(MasterDataCrudView).IsPublic
                            + " properties=" + definitionProperties.Length + "/" + viewProperties.Length
                            + " initialize=" + (initialize == null ? "missing" : initialize.Attributes.ToString()));
        }

        private static bool PublicReadWriteProperties(PropertyInfo[] properties)
        {
            if (properties.Length == 0)
            {
                return false;
            }

            foreach (PropertyInfo property in properties)
            {
                MethodInfo getter = property.GetGetMethod();
                MethodInfo setter = property.GetSetMethod();

                if (getter == null || setter == null || !getter.IsPublic || !setter.IsPublic)
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task TestLoadingGate()
        {
            using (ProbeForm form = Open(null, false, true))
            {
                await Task.Delay(500);

                Check("Loading — 목록 응답 전에는 New·Save·Delete 가 닫히고 편집기가 잠긴다",
                        !New(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled && !Grid(form).Enabled,
                        ButtonStates(form));

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check("Loading — Save·Delete 핸들러가 요청을 조립하지 않는다", form.WriteRequests.Length == 0,
                        string.Join(" | ", form.WriteRequests));

                form.ListGate.Set();
                await WaitIdleAsync(form);

                Check("Ready — 정상 응답 뒤 New·Save 가 열린다", New(form).Enabled && Save(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestBlocked(string title, string json, string expectedDetail)
        {
            using (ProbeForm form = Open(ListReply(QueryReply(json)), true, true))
            {
                await WaitIdleAsync(form);

                Check("Blocked — " + title + ": 스티키 알림에 사유가 적힌다",
                        form.NoticeShowing && form.NoticeText.Contains("did not match") && form.NoticeText.Contains(expectedDetail),
                        form.NoticeText);
                Check("Blocked — " + title + ": 세 버튼 닫힘 · 편집기 비움",
                        !New(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled && Grid(form).Columns.Length == 0,
                        ButtonStates(form) + " columns=" + Grid(form).Columns.Length);

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check("Blocked — " + title + ": 핸들러가 요청을 조립하지 않는다", form.WriteRequests.Length == 0,
                        string.Join(" | ", form.WriteRequests));
            }
        }

        private static async Task TestReadyEmptySchema()
        {
            using (ProbeForm form = Open(ListReply(SchemaOnlyReply()), true, true))
            {
                await WaitIdleAsync(form);

                Check("ReadyEmpty — 스키마 있는 0행은 New·Save 열림 · Delete 닫힘 · 편집기 New",
                        New(form).Enabled && Save(form).Enabled && !Delete(form).Enabled && Grid(form).IsNew && !form.NoticeShowing,
                        ButtonStates(form));

                WriteEditor(Grid(form), "PRODUCT_NAME", "Gate Test");
                WriteEditor(Grid(form), "PRODUCT_TYPE", "WAFER");
                Invoke(form, "OnSaveClick");
                await WaitIdleAsync(form);

                string insert = FirstContaining(form.WriteRequests, "MethodCommand=InsertProduct");
                Check("ReadyEmpty — Save 는 InsertProduct 한 건이고 ProdId 는 비어 간다",
                        insert != null && insert.StartsWith(
                                "ProductAction MethodCommand=InsertProduct ", StringComparison.Ordinal)
                                && insert.Contains("ProdId= ") && insert.Contains("ProductName=[Gate Test]")
                                && insert.Contains("ProductType=WAFER") && form.WriteRequests.Length == 1,
                        insert ?? "(없음)");
            }
        }

        private static async Task TestFailedKeepsList()
        {
            using (ProbeForm form = Open(null, true, true))
            {
                await WaitIdleAsync(form);
                object before = ProductsGrid(form).DataSource;
                DataTable beforeTable = before as DataTable;
                int rowsBefore = beforeTable == null ? 0 : beforeTable.Rows.Count;

                form.Reply = ListReply(RejectedReply());
                lastDialog = string.Empty;
                Invoke(form, "LoadProducts");
                await WaitIdleAsync(form);

                Check("Failed — 오류 응답 뒤 세 버튼 닫힘 · 직전 목록 유지",
                        !New(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled
                                && object.ReferenceEquals(ProductsGrid(form).DataSource, before) && rowsBefore > 0,
                        ButtonStates(form) + " rows=" + rowsBefore + " dialog=" + lastDialog);

                Invoke(form, "OnSaveClick");
                Check("Failed — Save 핸들러가 요청을 조립하지 않는다", form.WriteRequests.Length == 0,
                        string.Join(" | ", form.WriteRequests));

                form.Reply = null;
                Invoke(form, "LoadProducts");
                await WaitIdleAsync(form);

                Check("Failed → Ready — 다음 정상 응답이 쓰기를 다시 연다", New(form).Enabled && Save(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestRecoveryFromBlocked()
        {
            using (ProbeForm form = Open(ListReply(QueryReply("[]")), true, true))
            {
                await WaitIdleAsync(form);
                bool blocked = form.NoticeShowing && !New(form).Enabled;

                form.Reply = null;
                Invoke(form, "LoadProducts");
                await WaitIdleAsync(form);

                Check("Blocked → Ready — 정상 응답이 오면 버튼이 열리고 스티키 알림이 내려간다",
                        blocked && New(form).Enabled && Save(form).Enabled && !form.NoticeShowing,
                        ButtonStates(form) + " notice=" + form.NoticeShowing);
            }
        }

        private static async Task TestSaveAndReselect()
        {
            using (ProbeForm form = Open(null, true, true))
            {
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "PROD-1002", 5000);

                Check("Ready — 기존 행 선택 뒤 Save·Delete 열림", Save(form).Enabled && Delete(form).Enabled && !Grid(form).IsNew,
                        ButtonStates(form) + " key=" + Grid(form).ReadKey());

                WriteEditor(Grid(form), "PRODUCT_NAME", "200mm Test Wafer v2");
                Invoke(form, "OnSaveClick");
                await WaitIdleAsync(form);

                string update = FirstContaining(form.WriteRequests, "MethodCommand=UpdateProduct");
                Check("Ready — Save 는 UpdateProduct ProdId=<키> 한 건이고 재조회 뒤 그 키가 선택된다",
                        update != null && update.StartsWith(
                                "ProductAction MethodCommand=UpdateProduct ", StringComparison.Ordinal)
                                && update.Contains("ProdId=PROD-1002 ")
                                && update.Contains("ProductName=[200mm Test Wafer v2]")
                                && form.WriteRequests.Length == 1
                                && Grid(form).ReadKey() == "PROD-1002" && !Grid(form).IsNew,
                        (update ?? "(없음)") + " key=" + Grid(form).ReadKey());

                Invoke(form, "OnNewClick");
                Invoke(form, "OnDeleteClick");
                Check("Ready — New 상태에서 Delete 는 닫혀 있고 요청이 없다",
                        Grid(form).IsNew && !Delete(form).Enabled && FirstContaining(form.WriteRequests, "MethodCommand=DeleteProduct") == null,
                        ButtonStates(form));
            }
        }

        private static async Task TestDisqualifiedRow()
        {
            string json = Json(
                    Row("PROD_ID", string.Empty, "PRODUCT_NAME", "No key", "PRODUCT_TYPE", "WAFER", "UNIT", "EA", "USE_YN", "Y", "DESCRIPTION", string.Empty),
                    Row("PROD_ID", "P-OK", "PRODUCT_NAME", "Ok", "PRODUCT_TYPE", "WAFER", "UNIT", "EA", "USE_YN", "Y", "DESCRIPTION", string.Empty));

            using (ProbeForm form = Open(ListReply(QueryReply(json)), true, true))
            {
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => !Grid(form).IsNew && Grid(form).ReadValue("PRODUCT_NAME") == "No key", 5000);

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check("결격 행 — 키 없는 행을 선택하면 Save·Delete 닫힘 · 요청 없음",
                        !Save(form).Enabled && !Delete(form).Enabled && form.WriteRequests.Length == 0 && New(form).Enabled,
                        ButtonStates(form) + " " + string.Join(" | ", form.WriteRequests));

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "P-OK", 5000);

                Check("결격 행 — 유효한 행으로 옮기면 Save·Delete 열림", Save(form).Enabled && Delete(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestReservedMethodCommand()
        {
            string json = Json(Row(
                    "PROD_ID", "P-METHOD",
                    "PRODUCT_NAME", "Reserved field",
                    "PRODUCT_TYPE", "WAFER",
                    "UNIT", "EA",
                    "USE_YN", "Y",
                    "DESCRIPTION", string.Empty,
                    "METHOD_COMMAND", "InjectedMethod"));

            using (ProbeForm form = Open(ListReply(QueryReply(json)), true, true))
            {
                await WaitIdleAsync(form);
                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");

                Check("Blocked — 예약 필드 METHOD_COMMAND은 스티키 알림에 컬럼 이름을 적고 세 버튼을 닫는다",
                        form.NoticeShowing && form.NoticeText.Contains("METHOD_COMMAND")
                                && !New(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled,
                        ButtonStates(form) + " notice=" + form.NoticeText);
                Check("Blocked — 예약 필드 METHOD_COMMAND은 쓰기 요청을 조립하지 않는다",
                        form.WriteRequests.Length == 0,
                        string.Join(" | ", form.WriteRequests));
            }
        }

        private static string ComboRowsJson()
        {
            return Json(
                    Row("PROD_ID", "P-EMPTY", "PRODUCT_NAME", "Empty type", "PRODUCT_TYPE", string.Empty, "UNIT", "ZZZ", "USE_YN", "Y", "DESCRIPTION", string.Empty),
                    Row("PROD_ID", "P-OK", "PRODUCT_NAME", "Known type", "PRODUCT_TYPE", "WAFER", "UNIT", "EA", "USE_YN", "Y", "DESCRIPTION", string.Empty));
        }

        private static async Task TestComboValuesListFirst()
        {
            using (ProbeForm form = Open(ListReply(QueryReply(ComboRowsJson())), true, false))
            {
                await WaitUntilAsync(() => Grid(form).Columns.Length > 0, 10000);
                await Task.Delay(200);

                ProductsGrid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "P-EMPTY", 5000);

                CheckPreserved(form, "목록→콤보 · 항목 도착 전");
                Check("목록→콤보 — 콤보 항목이 오기 전에도 목록이 Ready 면 Save 가 열려 있다(값이 보존되므로)",
                        Save(form).Enabled, ButtonStates(form));

                form.ComboGate.Set();
                await WaitIdleAsync(form);

                CheckPreserved(form, "목록→콤보 · 항목 도착 후");
                Check("Ready — PRODUCT_TYPE 가 빈 기존 행도 Save 가 열린다(입력 필수는 행 결격 사유가 아니다)",
                        Save(form).Enabled && Delete(form).Enabled && !Grid(form).IsNew, ButtonStates(form));
                Check("목록→콤보 — 빈 값·모르는 코드는 선택 없음으로 보인다",
                        Combo(form, "PRODUCT_TYPE").SelectedIndex < 0 && Combo(form, "UNIT").SelectedIndex < 0,
                        ComboStates(form));

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "P-OK", 5000);

                Check("목록→콤보 — 정상 코드는 그 항목이 선택되고 값이 그 코드다",
                        Combo(form, "PRODUCT_TYPE").SelectedIndex >= 0 && Grid(form).ReadValue("PRODUCT_TYPE") == "WAFER"
                                && Combo(form, "UNIT").SelectedIndex >= 0 && Grid(form).ReadValue("UNIT") == "EA" && Save(form).Enabled,
                        ComboStates(form));

                Invoke(form, "OnNewClick");
                Check("New — 콤보는 선택 없음이고 PRODUCT_TYPE 이 필수 누락에 든다",
                        Combo(form, "PRODUCT_TYPE").SelectedIndex < 0 && Grid(form).ReadValue("PRODUCT_TYPE").Length == 0
                                && Array.IndexOf(Grid(form).MissingRequired(), "PRODUCT_TYPE") >= 0,
                        ComboStates(form) + " missing=" + string.Join(",", Grid(form).MissingRequired()));
            }
        }

        private static async Task TestComboValuesCombosFirst()
        {
            using (ProbeForm form = Open(ListReply(QueryReply(ComboRowsJson())), false, true))
            {
                await WaitUntilAsync(() => ((IDictionary)Field(Grid(form), "comboSources")).Count >= 2, 10000);

                form.ListGate.Set();
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "P-EMPTY", 5000);

                CheckPreserved(form, "콤보→목록");
                Check("콤보→목록 — 빈 값·모르는 코드는 선택 없음으로 보인다",
                        Combo(form, "PRODUCT_TYPE").SelectedIndex < 0 && Combo(form, "UNIT").SelectedIndex < 0,
                        ComboStates(form));
            }
        }

        private static async Task TestComboFailureKeepsValues()
        {
            Func<string, string> reply = delegate(string request)
            {
                if (request.Contains(ListMethod))
                {
                    return QueryReply(ComboRowsJson());
                }

                return RejectedReply();
            };

            using (ProbeForm form = Open(reply, true, true))
            {
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "P-EMPTY", 5000);

                CheckPreserved(form, "콤보 조회 실패");
                Check("콤보 조회 실패 — 목록 상태는 Ready 그대로라 Save 가 열려 있다", Save(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestCancel()
        {
            using (ProbeForm form = Open(null, true, true))
            {
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "PROD-1002", 5000);

                string original = Grid(form).ReadValue("MAKER");

                Check("Cancel — 불러온 그대로면 닫혀 있다", !Cancel(form).Enabled && !Grid(form).IsDirty, ButtonStates(form));

                WriteEditor(Grid(form), "MAKER", original + " (edited)");

                Check("Cancel — 값을 고치면 열린다", Cancel(form).Enabled && Grid(form).IsDirty, ButtonStates(form));

                Invoke(form, "OnCancelClick");

                Check("Cancel — 기존 행은 원래 값으로 돌아가고 버튼이 다시 닫힌다 · 요청 없음",
                        Grid(form).ReadValue("MAKER") == original && !Cancel(form).Enabled && !Grid(form).IsDirty
                                && form.WriteRequests.Length == 0 && Grid(form).ReadKey() == "PROD-1002" && !Grid(form).IsNew,
                        "maker=" + Grid(form).ReadValue("MAKER") + " " + ButtonStates(form));

                Invoke(form, "OnNewClick");

                Check("Cancel — New 상태에서는 고친 것이 없어도 열린다",
                        Grid(form).IsNew && Cancel(form).Enabled, ButtonStates(form));

                Invoke(form, "OnCancelClick");

                Check("Cancel — New 를 취소하면 선택 행으로 돌아간다",
                        !Grid(form).IsNew && Grid(form).ReadKey() == "PROD-1002" && !Cancel(form).Enabled
                                && form.WriteRequests.Length == 0,
                        "key=" + Grid(form).ReadKey() + " " + ButtonStates(form));
            }
        }

        private static async Task TestActionMenu()
        {
            using (ProbeForm form = Open(null, false, true))
            {
                await Task.Delay(500);

                Check("컨텍스트 메뉴 — Loading 에서 네 항목이 버튼과 같이 닫힌다",
                        MenuMatchesButtons(form) && !MenuItem(form, "miSave").Enabled,
                        MenuStates(form));
                Check("컨텍스트 메뉴 — 네 액션이 다 닫혀도 그리드 복사 항목이 붙으므로 메뉴는 뜬다",
                        !MenuOpeningCancelled(form) && InjectedItemCount(form) > 0 && MenuMatchesButtons(form),
                        "injected=" + InjectedItemCount(form) + " / " + MenuStates(form));

                form.ListGate.Set();
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "PROD-1002", 5000);

                Check("컨텍스트 메뉴 — Ready 에서 네 항목이 버튼과 같다", MenuMatchesButtons(form), MenuStates(form));
                MenuItem(form, "miSave").Enabled = !Save(form).Enabled;

                Check("컨텍스트 메뉴 — Opening 이 어긋난 항목 상태를 버튼에 다시 맞춘다",
                        !MenuOpeningCancelled(form) && MenuMatchesButtons(form), MenuStates(form));

                Invoke(form, "OnNewClick");

                Check("컨텍스트 메뉴 — New 상태에서도 네 항목이 버튼과 같다(Delete 닫힘)",
                        MenuMatchesButtons(form) && !MenuItem(form, "miDelete").Enabled, MenuStates(form));

                Invoke(form, "OnCancelClick");

                WriteEditor(Grid(form), "MAKER", "menu click");
                MenuItem(form, "miSave").PerformClick();
                await WaitIdleAsync(form);

                string request = FirstContaining(form.WriteRequests, "MethodCommand=UpdateProduct");

                Check("컨텍스트 메뉴 — Save 항목이 버튼과 같은 요청 한 건을 만든다",
                        form.WriteRequests.Length == 1 && request != null && request.Contains("ProdId=PROD-1002"),
                        string.Join(" | ", form.WriteRequests));
            }
        }

        private static async Task TestEscape()
        {
            using (ProbeForm form = Open(null, true, true))
            {
                await WaitIdleAsync(form);

                ProductsGrid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Grid(form).ReadKey() == "PROD-1002", 5000);

                string original = Grid(form).ReadValue("MAKER");

                Check("Esc — Cancel 이 닫혀 있으면 아무 일도 하지 않고 키를 넘긴다",
                        !PressEscape(form) && !Grid(form).IsDirty, ButtonStates(form));

                WriteEditor(Grid(form), "MAKER", original + " (edited)");

                ProductsGrid(form).ShowFindPanel();
                await Task.Delay(300);

                bool findOpen = ProductsGrid(form).IsFindPanelOpen;
                bool handledWhileFinding = PressEscape(form);

                ProductsGrid(form).HideFindPanel();
                await Task.Delay(200);

                Check("Esc — 찾기 창이 열려 있는 동안에는 되돌리지 않는다",
                        findOpen && !handledWhileFinding && Grid(form).IsDirty,
                        "find=" + findOpen + " handled=" + handledWhileFinding + " dirty=" + Grid(form).IsDirty);

                Check("Esc — Cancel 이 열려 있으면 되돌리고 요청이 없다",
                        PressEscape(form) && Grid(form).ReadValue("MAKER") == original
                                && !Grid(form).IsDirty && form.WriteRequests.Length == 0,
                        "maker=" + Grid(form).ReadValue("MAKER") + " " + ButtonStates(form));
            }
        }

        private static void CheckPreserved(ProbeForm form, string phase)
        {
            string requestText = Grid(form).RequestText;

            Check("값 보존 — " + phase + ": 빈 PRODUCT_TYPE 은 빈 값으로, 모르는 UNIT 코드는 원래 값으로 실린다",
                    Grid(form).ReadValue("PRODUCT_TYPE").Length == 0 && Grid(form).ReadValue("UNIT") == "ZZZ"
                            && requestText.Contains("ProductType= Unit=ZZZ"),
                    requestText);
        }

        private static ProbeForm Open(Func<string, string> reply, bool listOpen, bool comboOpen)
        {
            ProbeForm form = new ProbeForm();
            form.Reply = reply;

            if (!listOpen)
            {
                form.ListGate.Reset();
            }

            if (!comboOpen)
            {
                form.ComboGate.Reset();
            }

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new System.Drawing.Point(-20000, -20000);
            form.ShowInTaskbar = false;
            form.Show();
            return form;
        }

        private static Func<string, string> ListReply(string reply)
        {
            return delegate(string request)
            {
                return request.Contains(ListMethod) ? reply : null;
            };
        }

        private static string QueryReply(string json)
        {
            return "SelectProducts" + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldResultMessage + "=" + json;
        }

        private static string SchemaOnlyReply()
        {
            return "SelectProducts" + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldResultMessage + "=[" + SchemaMarker + "]";
        }

        private static string RejectedReply()
        {
            return "rejectedR"
                    + " " + ServerMessageFormat.FieldReturnCode + "=[1]"
                    + " " + ServerMessageFormat.FieldErrorCode + "=[TEST_FAIL]"
                    + " " + ServerMessageFormat.FieldErrorMessage + "=[Simulated server failure]";
        }

        private static string Json(params string[] rows)
        {
            return "[" + string.Join(",", rows) + "]";
        }

        private static string Row(params string[] pairs)
        {
            StringBuilder builder = new StringBuilder("{");

            for (int i = 0; i + 1 < pairs.Length; i = i + 2)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('"').Append(pairs[i]).Append("\":\"").Append(pairs[i + 1].Replace("\"", "\\\"")).Append('"');
            }

            return builder.Append('}').ToString();
        }

        private static string FirstContaining(string[] requests, string marker)
        {
            foreach (string request in requests)
            {
                if (request.Contains(marker))
                {
                    return request;
                }
            }

            return null;
        }

        private static ModernPropertyGrid Grid(Form form)
        {
            return (ModernPropertyGrid)Field(form, "propertyGrid");
        }

        private static ModernDataGrid ProductsGrid(Form form)
        {
            return (ModernDataGrid)Field(form, "gridProducts");
        }

        private static ModernButton New(Form form)
        {
            return (ModernButton)Field(form, "btnNew");
        }

        private static ModernButton Save(Form form)
        {
            return (ModernButton)Field(form, "btnSave");
        }

        private static ModernButton Delete(Form form)
        {
            return (ModernButton)Field(form, "btnDelete");
        }

        private static ModernButton Cancel(Form form)
        {
            return (ModernButton)Field(form, "btnCancel");
        }

        private static string ButtonStates(ProbeForm form)
        {
            return "new=" + New(form).Enabled + " cancel=" + Cancel(form).Enabled + " save=" + Save(form).Enabled
                    + " delete=" + Delete(form).Enabled + " editor=" + Grid(form).Enabled
                    + " isNew=" + Grid(form).IsNew + " dirty=" + Grid(form).IsDirty;
        }

        private static ToolStripMenuItem MenuItem(Form form, string name)
        {
            return (ToolStripMenuItem)Field(form, name);
        }

        private static bool MenuMatchesButtons(ProbeForm form)
        {
            return MenuItem(form, "miNew").Enabled == New(form).Enabled
                    && MenuItem(form, "miCancel").Enabled == Cancel(form).Enabled
                    && MenuItem(form, "miSave").Enabled == Save(form).Enabled
                    && MenuItem(form, "miDelete").Enabled == Delete(form).Enabled;
        }

        private static string MenuStates(ProbeForm form)
        {
            return "menu new=" + MenuItem(form, "miNew").Enabled
                    + " cancel=" + MenuItem(form, "miCancel").Enabled
                    + " save=" + MenuItem(form, "miSave").Enabled
                    + " delete=" + MenuItem(form, "miDelete").Enabled
                    + " / " + ButtonStates(form);
        }

        private static ContextMenuStrip ActionMenu(Form form)
        {
            return (ContextMenuStrip)Field(form, "menuActions");
        }

        private static void AttachGridMenuItems(ProbeForm form)
        {
            MethodInfo attach = typeof(ModernDataGrid).GetMethod(
                    "AttachGridMenuItems", BindingFlags.NonPublic | BindingFlags.Instance);
            attach.Invoke(ProductsGrid(form), new object[] { ActionMenu(form) });
        }

        private static int InjectedItemCount(ProbeForm form)
        {
            AttachGridMenuItems(form);

            int injected = 0;

            foreach (ToolStripItem item in ActionMenu(form).Items)
            {
                if (item.Tag != null)
                {
                    injected++;
                }
            }

            return injected;
        }

        private static bool MenuOpeningCancelled(ProbeForm form)
        {
            AttachGridMenuItems(form);

            MethodInfo opening = FindMethod(form, "OnActionMenuOpening");
            CancelEventArgs args = new CancelEventArgs();
            opening.Invoke(form, new object[] { null, args });
            return args.Cancel;
        }

        private static bool PressEscape(ProbeForm form)
        {
            MethodInfo process = FindMethod(form, "ProcessCmdKey");
            Message message = new Message();
            return (bool)process.Invoke(form, new object[] { message, Keys.Escape });
        }

        private static string ComboStates(ProbeForm form)
        {
            return "type[" + Combo(form, "PRODUCT_TYPE").SelectedIndex + "]=" + Grid(form).ReadValue("PRODUCT_TYPE")
                    + " unit[" + Combo(form, "UNIT").SelectedIndex + "]=" + Grid(form).ReadValue("UNIT");
        }

        private static ModernComboBox Combo(Form form, string column)
        {
            object editor = EditorOf(Grid(form), column);
            PropertyInfo control = editor.GetType().GetProperty("Control", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ModernComboBox)control.GetValue(editor, null);
        }

        private static void WriteEditor(ModernPropertyGrid grid, string column, string value)
        {
            object editor = EditorOf(grid, column);
            MethodInfo write = editor.GetType().GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance);
            write.Invoke(editor, new object[] { value });
        }

        private static object EditorOf(ModernPropertyGrid grid, string column)
        {
            IEnumerable editors = (IEnumerable)Field(grid, "editors");

            foreach (object editor in editors)
            {
                PropertyInfo columnProperty = editor.GetType().GetProperty("Column", BindingFlags.NonPublic | BindingFlags.Instance);
                string name = (string)columnProperty.GetValue(editor, null);

                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                {
                    return editor;
                }
            }

            throw new InvalidOperationException("편집기를 찾을 수 없음: " + column);
        }

        private static void Invoke(ProbeForm form, string method)
        {
            MethodInfo target = FindMethod(form, method);

            if (target == null)
            {
                throw new InvalidOperationException("메서드를 찾을 수 없음: " + method);
            }

            if (target.GetParameters().Length == 0)
            {
                target.Invoke(form, null);
                return;
            }

            target.Invoke(form, new object[] { null, EventArgs.Empty });
        }

        private static MethodInfo FindMethod(Form form, string name)
        {
            for (Type type = form.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo target = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

                if (target != null)
                {
                    return target;
                }
            }

            throw new InvalidOperationException("메서드를 찾을 수 없음: " + name);
        }

        private static object Field(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (field != null)
                {
                    return field.GetValue(target);
                }
            }

            throw new InvalidOperationException("필드를 찾을 수 없음: " + name);
        }

        private static async Task WaitIdleAsync(ProbeForm form)
        {
            await WaitUntilAsync(() => form.Idle, 15000);
            await Task.Delay(150);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline && !condition())
            {
                await Task.Delay(50);
            }
        }

        private static void Check(string name, bool condition, string detail)
        {
            if (condition)
            {
                passCount++;
                Line("PASS  " + name + " — " + detail);
            }
            else
            {
                failCount++;
                Line("FAIL  " + name + " — " + detail);
            }
        }

        private static void Line(string text)
        {
            report.AppendLine(text);
        }

        private static void Finish()
        {
            report.AppendLine();
            report.AppendLine(string.Format("합계: PASS {0} / FAIL {1}", passCount, failCount));

            string path = Path.Combine(Path.GetTempPath(), "product-uitest.txt");
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
        }

        private sealed class ProbeForm : ProductForm
        {
            private readonly List<string> requests = new List<string>();

            public ProbeForm()
            {
                this.ListGate = new ManualResetEventSlim(true);
                this.ComboGate = new ManualResetEventSlim(true);
                this.NoticeText = string.Empty;
            }

            public Func<string, string> Reply { get; set; }

            public ManualResetEventSlim ListGate { get; private set; }

            public ManualResetEventSlim ComboGate { get; private set; }

            public string NoticeText { get; private set; }

            public bool NoticeShowing { get; private set; }

            public bool Idle
            {
                get { return !this.QueryInProgress && !this.ActionInProgress; }
            }

            public string[] WriteRequests
            {
                get
                {
                    List<string> writes = new List<string>();

                    lock (this.requests)
                    {
                        foreach (string request in this.requests)
                        {
                            if (request.Contains("MethodCommand=InsertProduct")
                                    || request.Contains("MethodCommand=UpdateProduct")
                                    || request.Contains("MethodCommand=DeleteProduct"))
                            {
                                writes.Add(request);
                            }
                        }
                    }

                    return writes.ToArray();
                }
            }

            protected override string SendRequestText(string request)
            {
                lock (this.requests)
                {
                    this.requests.Add(request);
                }

                if (request.Contains(ListMethod))
                {
                    this.ListGate.Wait(10000);
                }
                else if (request.Contains(ComboMethodPrefix))
                {
                    this.ComboGate.Wait(10000);
                }

                Func<string, string> reply = this.Reply;

                if (reply != null)
                {
                    string custom = reply(request);

                    if (custom != null)
                    {
                        return custom;
                    }
                }

                return base.SendRequestText(request);
            }

            protected override DataTable ParseQueryData(string sendMessage)
            {
                if (sendMessage != null && sendMessage.Contains(SchemaMarker))
                {
                    DataTable schema = new DataTable();

                    foreach (string column in SchemaColumns)
                    {
                        schema.Columns.Add(column);
                    }

                    return schema;
                }

                return base.ParseQueryData(sendMessage);
            }

            protected override bool Confirm(string message, string caption)
            {
                return true;
            }

            protected override void ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind kind, string caption, string message)
            {
                lastDialog = (caption ?? string.Empty) + ": " + (message ?? string.Empty);
            }

            protected override void ShowStickyNotice(string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
            {
                this.NoticeText = message ?? string.Empty;
                this.NoticeShowing = true;
                base.ShowStickyNotice(message, kind);
            }

            protected override void HideNotice()
            {
                this.NoticeText = string.Empty;
                this.NoticeShowing = false;
                base.HideNotice();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.ListGate.Set();
                    this.ComboGate.Set();
                }

                base.Dispose(disposing);
            }
        }
    }
}
