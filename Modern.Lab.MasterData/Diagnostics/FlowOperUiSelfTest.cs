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

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.MasterData.Controls;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Input;

namespace Modern.Lab.MasterData.Diagnostics
{
    public static class FlowOperUiSelfTest
    {
        private static readonly StringBuilder report = new StringBuilder();
        private static int passCount;
        private static int failCount;

        private const string SchemaMarker = "__schema_only__";
        private const string AlphaColumn = "ALPHA_CODE";
        private const string BetaColumn = "BETA_NOTE";

        private static readonly ScreenProfile FlowProfile = new ScreenProfile(
                "Flow", "FLOW_ID", "FlowId", "FlowAction", "Flows", "gridFlows", "LoadFlows",
                "FLW-CMOS-14", "FLW-UITEST-1", () => new FlowProbeForm());

        private static readonly ScreenProfile OperProfile = new ScreenProfile(
                "Oper", "OPER_ID", "OperId", "OperAction", "Opers", "gridOpers", "LoadOpers",
                "OP-1020", "OP-UITEST-1", () => new OperProbeForm());

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
            await RunScreenAsync(FlowProfile);
            await RunScreenAsync(OperProfile);
        }

        private static async Task RunScreenAsync(ScreenProfile profile)
        {
            await TestLoadingGate(profile);
            await TestBlocked(profile, "0컬럼 응답", "[]", "no columns");
            await TestBlocked(profile, profile.Key + " 누락 응답",
                    Json(Row(AlphaColumn, "A-1", BetaColumn, "note")), profile.Key);
            await TestReservedMethodCommand(profile);
            await TestUnknownColumnsAreReady(profile);
            await TestReadyEmptySchema(profile);
            await TestEmptyKeyBlocksSave(profile);
            await TestFailedKeepsList(profile);
            await TestRecoveryFromBlocked(profile);
            await TestSaveAndReselect(profile);
            await TestDisqualifiedRow(profile);
            await TestCancel(profile);
            await TestDirtySurvivesSchemaChange(profile);
            await TestReturningColumnStartsFresh(profile);
            await TestReadOnlyFocusBorder(profile);
            await TestInsertAndDeleteRoundTrip(profile);
            await TestActionMenu(profile);
            await TestEscape(profile);
        }

        private static async Task TestActionMenu(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, false))
            {
                await Task.Delay(500);

                Check(profile, "컨텍스트 메뉴 — Loading 에서 네 항목이 버튼과 같이 닫힌다",
                        MenuMatchesButtons(form) && !MenuItem(form, "miSave").Enabled, MenuStates(form));
                Check(profile, "컨텍스트 메뉴 — 네 액션이 다 닫혀도 그리드 복사 항목이 붙으므로 메뉴는 뜬다",
                        !MenuOpeningCancelled(form) && InjectedItemCount(form) > 0 && MenuMatchesButtons(form),
                        "injected=" + InjectedItemCount(form) + " / " + MenuStates(form));

                form.State.ListGate.Set();
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                Check(profile, "컨텍스트 메뉴 — Ready 에서 네 항목이 버튼과 같다",
                        MenuMatchesButtons(form), MenuStates(form));
                MenuItem(form, "miSave").Enabled = !Save(form).Enabled;

                Check(profile, "컨텍스트 메뉴 — Opening 이 어긋난 항목 상태를 버튼에 다시 맞춘다",
                        !MenuOpeningCancelled(form) && MenuMatchesButtons(form), MenuStates(form));

                Invoke(form, "OnNewClick");

                Check(profile, "컨텍스트 메뉴 — New 상태에서도 네 항목이 버튼과 같다(Delete 닫힘)",
                        MenuMatchesButtons(form) && !MenuItem(form, "miDelete").Enabled, MenuStates(form));

                Invoke(form, "OnCancelClick");

                WriteEditor(Editor(form), "REMARK", "menu click");
                MenuItem(form, "miSave").PerformClick();
                await WaitIdleAsync(form);

                string request = FirstContaining(form.State.WriteRequests, profile.UpdateMarker);

                Check(profile, "컨텍스트 메뉴 — Save 항목이 버튼과 같은 요청 한 건을 만든다",
                        form.State.WriteRequests.Length == 1 && request != null
                                && request.Contains(profile.KeyParam + "=" + profile.SeedKey),
                        string.Join(" | ", form.State.WriteRequests));
            }
        }

        private static async Task TestEscape(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                string original = Editor(form).ReadValue("REMARK");

                Check(profile, "Esc — Cancel 이 닫혀 있으면 아무 일도 하지 않고 키를 넘긴다",
                        !PressEscape(form) && !Editor(form).IsDirty, ButtonStates(form));

                WriteEditor(Editor(form), "REMARK", original + " (edited by self test)");

                Grid(form).ShowFindPanel();
                await Task.Delay(300);

                bool findOpen = Grid(form).IsFindPanelOpen;
                bool handledWhileFinding = PressEscape(form);

                Grid(form).HideFindPanel();
                await Task.Delay(200);

                Check(profile, "Esc — 찾기 창이 열려 있는 동안에는 되돌리지 않는다",
                        findOpen && !handledWhileFinding && Editor(form).IsDirty,
                        "find=" + findOpen + " handled=" + handledWhileFinding + " dirty=" + Editor(form).IsDirty);

                Check(profile, "Esc — Cancel 이 열려 있으면 되돌리고 요청이 없다",
                        PressEscape(form) && Editor(form).ReadValue("REMARK") == original
                                && !Editor(form).IsDirty && form.State.WriteRequests.Length == 0,
                        "remark=" + Editor(form).ReadValue("REMARK") + " " + ButtonStates(form));
            }
        }

        private static ToolStripMenuItem MenuItem(ProbeHandle form, string name)
        {
            return (ToolStripMenuItem)Field(form.Form, name);
        }

        private static bool MenuMatchesButtons(ProbeHandle form)
        {
            return MenuItem(form, "miNew").Enabled == New(form).Enabled
                    && MenuItem(form, "miCancel").Enabled == Cancel(form).Enabled
                    && MenuItem(form, "miSave").Enabled == Save(form).Enabled
                    && MenuItem(form, "miDelete").Enabled == Delete(form).Enabled;
        }

        private static string MenuStates(ProbeHandle form)
        {
            return "menu new=" + MenuItem(form, "miNew").Enabled
                    + " cancel=" + MenuItem(form, "miCancel").Enabled
                    + " save=" + MenuItem(form, "miSave").Enabled
                    + " delete=" + MenuItem(form, "miDelete").Enabled
                    + " / " + ButtonStates(form);
        }

        private static ContextMenuStrip ActionMenu(ProbeHandle form)
        {
            return (ContextMenuStrip)Field(form.Form, "menuActions");
        }

        private static void AttachGridMenuItems(ProbeHandle form)
        {
            MethodInfo attach = typeof(ModernDataGrid).GetMethod(
                    "AttachGridMenuItems", BindingFlags.NonPublic | BindingFlags.Instance);
            attach.Invoke(Grid(form), new object[] { ActionMenu(form) });
        }

        private static int InjectedItemCount(ProbeHandle form)
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

        private static bool MenuOpeningCancelled(ProbeHandle form)
        {
            AttachGridMenuItems(form);

            CancelEventArgs args = new CancelEventArgs();
            FindMethod(form, "OnActionMenuOpening").Invoke(form.Form, new object[] { null, args });
            return args.Cancel;
        }

        private static bool PressEscape(ProbeHandle form)
        {
            Message message = new Message();
            return (bool)FindMethod(form, "ProcessCmdKey").Invoke(
                    form.Form, new object[] { message, Keys.Escape });
        }

        private static MethodInfo FindMethod(ProbeHandle form, string name)
        {
            for (Type type = form.Form.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo target = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

                if (target != null)
                {
                    return target;
                }
            }

            throw new InvalidOperationException("메서드를 찾을 수 없음: " + name);
        }

        private static async Task TestLoadingGate(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, false))
            {
                await Task.Delay(500);

                Check(profile, "Loading — 목록 응답 전에는 네 버튼이 닫히고 편집기가 잠긴다",
                        !New(form).Enabled && !Cancel(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled
                                && !Editor(form).Enabled,
                        ButtonStates(form));

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check(profile, "Loading — Save·Delete 핸들러가 요청을 조립하지 않는다",
                        form.State.WriteRequests.Length == 0, string.Join(" | ", form.State.WriteRequests));

                form.State.ListGate.Set();
                await WaitIdleAsync(form);

                Check(profile, "Ready — 정상 응답 뒤 New·Save 가 열린다",
                        New(form).Enabled && Save(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestBlocked(ScreenProfile profile, string title, string json, string expectedDetail)
        {
            using (ProbeHandle form = Open(profile, ListReply(profile, QueryReply(profile, json)), true))
            {
                await WaitIdleAsync(form);

                Check(profile, "Blocked — " + title + ": 스티키 알림에 사유가 적힌다",
                        form.State.NoticeShowing && form.State.NoticeText.Contains("did not match")
                                && form.State.NoticeText.Contains(expectedDetail),
                        form.State.NoticeText);
                Check(profile, "Blocked — " + title + ": 네 버튼 닫힘 · 편집기 비움",
                        !New(form).Enabled && !Cancel(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled
                                && Editor(form).Columns.Length == 0,
                        ButtonStates(form) + " columns=" + Editor(form).Columns.Length);

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check(profile, "Blocked — " + title + ": 핸들러가 요청을 조립하지 않는다",
                        form.State.WriteRequests.Length == 0, string.Join(" | ", form.State.WriteRequests));
            }
        }

        private static async Task TestReservedMethodCommand(ScreenProfile profile)
        {
            string json = Json(Row(profile.Key, "K-METHOD", AlphaColumn, "A-1", "METHOD_COMMAND", "InjectedMethod"));

            using (ProbeHandle form = Open(profile, ListReply(profile, QueryReply(profile, json)), true))
            {
                await WaitIdleAsync(form);
                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");

                Check(profile, "Blocked — 예약 필드 METHOD_COMMAND은 알림에 컬럼 이름을 적고 세 버튼을 닫는다",
                        form.State.NoticeShowing && form.State.NoticeText.Contains("METHOD_COMMAND")
                                && !New(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled,
                        ButtonStates(form) + " notice=" + form.State.NoticeText);
                Check(profile, "Blocked — 예약 필드 METHOD_COMMAND은 쓰기 요청을 조립하지 않는다",
                        form.State.WriteRequests.Length == 0, string.Join(" | ", form.State.WriteRequests));
            }
        }

        private static async Task TestUnknownColumnsAreReady(ScreenProfile profile)
        {
            string json = Json(
                    Row(profile.Key, "K-1", AlphaColumn, "A-1", BetaColumn, "first"),
                    Row(profile.Key, "K-2", AlphaColumn, "A-2", BetaColumn, "second"));

            using (ProbeHandle form = Open(profile, ListReply(profile, QueryReply(profile, json)), true))
            {
                await WaitIdleAsync(form);

                Check(profile, "Ready — 키 밖의 컬럼 이름을 모르는 응답도 Ready 다(입력 필수 컬럼 계약이 없다)",
                        New(form).Enabled && Save(form).Enabled && !form.State.NoticeShowing,
                        ButtonStates(form) + " notice=" + form.State.NoticeText);

                Grid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => Editor(form).ReadKey() == "K-1", 5000);

                string[] columns = Editor(form).Columns;

                Check(profile, "Ready — 편집기와 전문이 수신 컬럼·그 순서를 그대로 따른다",
                        columns.Length == 3 && columns[0] == profile.Key && columns[1] == AlphaColumn && columns[2] == BetaColumn
                                && Editor(form).RequestText == profile.KeyParam + "=K-1 AlphaCode=A-1 BetaNote=first",
                        string.Join(",", columns) + " | " + Editor(form).RequestText);
            }
        }

        private static async Task TestReadyEmptySchema(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, SchemaOnlyAllReply(profile), true))
            {
                await WaitIdleAsync(form);

                Check(profile, "ReadyEmpty — 스키마 있는 0행은 New·Save 열림 · Delete 닫힘 · 편집기 New",
                        New(form).Enabled && Save(form).Enabled && !Delete(form).Enabled
                                && Editor(form).IsNew && !form.State.NoticeShowing,
                        ButtonStates(form));
                Check(profile, "ReadyEmpty — 신규 행에서 키 편집기가 열려 있다",
                        !EditorReadOnly(Editor(form), profile.Key), "readOnly=" + EditorReadOnly(Editor(form), profile.Key));

                WriteEditor(Editor(form), profile.Key, profile.NewKey);
                WriteEditor(Editor(form), AlphaColumn, "A-9");
                Invoke(form, "OnSaveClick");
                await WaitIdleAsync(form);

                string insert = FirstContaining(form.State.WriteRequests, profile.InsertMarker);

                Check(profile, "ReadyEmpty — Save 는 Insert 한 건이고 키가 사용자가 친 값 그대로 나간다",
                        insert != null && form.State.WriteRequests.Length == 1
                                && insert.StartsWith(profile.Action + " " + profile.InsertMarker + " ", StringComparison.Ordinal)
                                && insert.Contains(" " + profile.KeyParam + "=" + profile.NewKey + " ")
                                && insert.Contains("AlphaCode=A-9"),
                        insert ?? "(없음)");
            }
        }

        private static async Task TestEmptyKeyBlocksSave(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, SchemaOnlyAllReply(profile), true))
            {
                await WaitIdleAsync(form);

                WriteEditor(Editor(form), AlphaColumn, "A-9");
                Invoke(form, "OnSaveClick");
                await Task.Delay(150);

                Check(profile, "ReadyEmpty — 키를 비운 채 Save 하면 요청이 없고 필수 누락에 키가 든다",
                        form.State.WriteRequests.Length == 0
                                && Array.IndexOf(Editor(form).MissingRequired(), profile.Key) >= 0,
                        "missing=" + string.Join(",", Editor(form).MissingRequired())
                                + " | " + string.Join(" | ", form.State.WriteRequests));
            }
        }

        private static async Task TestFailedKeepsList(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);
                object before = Grid(form).DataSource;
                DataTable beforeTable = before as DataTable;
                int rowsBefore = beforeTable == null ? 0 : beforeTable.Rows.Count;

                form.State.Reply = ListReply(profile, RejectedReply());
                Invoke(form, profile.LoadMethod);
                await WaitIdleAsync(form);

                Check(profile, "Failed — 오류 응답 뒤 네 버튼 닫힘 · 직전 목록 유지",
                        !New(form).Enabled && !Cancel(form).Enabled && !Save(form).Enabled && !Delete(form).Enabled
                                && object.ReferenceEquals(Grid(form).DataSource, before) && rowsBefore > 0,
                        ButtonStates(form) + " rows=" + rowsBefore + " dialog=" + form.State.LastDialog);

                Invoke(form, "OnSaveClick");
                Check(profile, "Failed — Save 핸들러가 요청을 조립하지 않는다",
                        form.State.WriteRequests.Length == 0, string.Join(" | ", form.State.WriteRequests));

                form.State.Reply = null;
                Invoke(form, profile.LoadMethod);
                await WaitIdleAsync(form);

                Check(profile, "Failed → Ready — 다음 정상 응답이 쓰기를 다시 연다",
                        New(form).Enabled && Save(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestRecoveryFromBlocked(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, ListReply(profile, QueryReply(profile, "[]")), true))
            {
                await WaitIdleAsync(form);
                bool blocked = form.State.NoticeShowing && !New(form).Enabled;

                form.State.Reply = null;
                Invoke(form, profile.LoadMethod);
                await WaitIdleAsync(form);

                Check(profile, "Blocked → Ready — 정상 응답이 오면 버튼이 열리고 스티키 알림이 내려간다",
                        blocked && New(form).Enabled && Save(form).Enabled && !form.State.NoticeShowing,
                        ButtonStates(form) + " notice=" + form.State.NoticeShowing);
            }
        }

        private static async Task TestSaveAndReselect(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                Check(profile, "Ready — 기존 행 선택 뒤 Save·Delete 열림 · 키 편집기는 읽기 전용",
                        Save(form).Enabled && Delete(form).Enabled && !Editor(form).IsNew
                                && EditorReadOnly(Editor(form), profile.Key),
                        ButtonStates(form) + " key=" + Editor(form).ReadKey());

                WriteEditor(Editor(form), "REMARK", "edited by self test");
                Invoke(form, "OnSaveClick");
                await WaitIdleAsync(form);

                string update = FirstContaining(form.State.WriteRequests, profile.UpdateMarker);

                Check(profile, "Ready — Save 는 Update 한 건이고 키가 그 행 값 · 재조회 뒤 그 키가 선택된다",
                        update != null && form.State.WriteRequests.Length == 1
                                && update.StartsWith(profile.Action + " " + profile.UpdateMarker + " ", StringComparison.Ordinal)
                                && update.Contains(" " + profile.KeyParam + "=" + profile.SeedKey + " ")
                                && update.Contains("Remark=[edited by self test]")
                                && Editor(form).ReadKey() == profile.SeedKey && !Editor(form).IsNew,
                        (update ?? "(없음)") + " key=" + Editor(form).ReadKey());

                Invoke(form, "OnNewClick");
                Invoke(form, "OnDeleteClick");

                Check(profile, "Ready — New 상태에서 키가 다시 열리고 Delete 는 닫혀 요청이 없다",
                        Editor(form).IsNew && !EditorReadOnly(Editor(form), profile.Key) && !Delete(form).Enabled
                                && FirstContaining(form.State.WriteRequests, profile.DeleteMarker) == null,
                        ButtonStates(form));
            }
        }

        private static async Task TestDisqualifiedRow(ScreenProfile profile)
        {
            string json = Json(
                    Row(profile.Key, string.Empty, AlphaColumn, "A-1", BetaColumn, "no key"),
                    Row(profile.Key, "K-OK", AlphaColumn, "A-2", BetaColumn, "ok"));

            using (ProbeHandle form = Open(profile, ListReply(profile, QueryReply(profile, json)), true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 0;
                await WaitUntilAsync(() => !Editor(form).IsNew && Editor(form).ReadValue(BetaColumn) == "no key", 5000);

                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");

                Check(profile, "결격 행 — 키 없는 행을 선택하면 Save·Delete 닫힘 · 요청 없음",
                        !Save(form).Enabled && !Delete(form).Enabled && form.State.WriteRequests.Length == 0 && New(form).Enabled,
                        ButtonStates(form) + " " + string.Join(" | ", form.State.WriteRequests));

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == "K-OK", 5000);

                Check(profile, "결격 행 — 유효한 행으로 옮기면 Save·Delete 열림",
                        Save(form).Enabled && Delete(form).Enabled, ButtonStates(form));
            }
        }

        private static async Task TestCancel(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                string original = Editor(form).ReadValue("REMARK");

                Check(profile, "Cancel — 불러온 그대로면 닫혀 있다",
                        !Cancel(form).Enabled && !Editor(form).IsDirty, ButtonStates(form));

                WriteEditor(Editor(form), "REMARK", original + " (edited by self test)");

                Check(profile, "Cancel — 값을 고치면 열린다",
                        Cancel(form).Enabled && Editor(form).IsDirty, ButtonStates(form));

                Invoke(form, "OnCancelClick");

                Check(profile, "Cancel — 기존 행은 원래 값으로 돌아가고 버튼이 다시 닫힌다 · 요청 없음",
                        Editor(form).ReadValue("REMARK") == original && !Cancel(form).Enabled
                                && !Editor(form).IsDirty && form.State.WriteRequests.Length == 0
                                && Editor(form).ReadKey() == profile.SeedKey && !Editor(form).IsNew,
                        "remark=" + Editor(form).ReadValue("REMARK") + " " + ButtonStates(form));

                Invoke(form, "OnNewClick");

                Check(profile, "Cancel — New 상태에서는 고친 것이 없어도 열린다(선택 행으로 돌아갈 수 있다)",
                        Editor(form).IsNew && Cancel(form).Enabled && !Editor(form).IsDirty, ButtonStates(form));

                Invoke(form, "OnCancelClick");

                Check(profile, "Cancel — New 를 취소하면 선택 행으로 돌아가고 키가 다시 잠긴다",
                        !Editor(form).IsNew && Editor(form).ReadKey() == profile.SeedKey
                                && EditorReadOnly(Editor(form), profile.Key) && !Cancel(form).Enabled
                                && form.State.WriteRequests.Length == 0,
                        "key=" + Editor(form).ReadKey() + " " + ButtonStates(form));
            }
        }

        private static async Task TestDirtySurvivesSchemaChange(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                string original = Editor(form).ReadValue("REMARK");
                string edited = original + " (edited by self test)";
                WriteEditor(Editor(form), "REMARK", edited);

                DataTable wider = new DataTable();

                foreach (string column in Editor(form).Columns)
                {
                    wider.Columns.Add(column);
                }

                wider.Columns.Add("EXTRA_NOTE");
                Editor(form).SetSchema(wider);

                Check(profile, "스키마가 바뀌어도 고치던 값과 되돌릴 기준이 살아남는다",
                        Editor(form).IsDirty && Editor(form).ReadValue("REMARK") == edited
                                && Array.IndexOf(Editor(form).Columns, "EXTRA_NOTE") >= 0,
                        "dirty=" + Editor(form).IsDirty + " remark=" + Editor(form).ReadValue("REMARK"));

                Editor(form).RevertEdits();

                Check(profile, "스키마 변경 뒤 되돌리면 원래 값으로 돌아간다",
                        !Editor(form).IsDirty && Editor(form).ReadValue("REMARK") == original,
                        "dirty=" + Editor(form).IsDirty + " remark=" + Editor(form).ReadValue("REMARK"));
            }
        }

        private static async Task TestReturningColumnStartsFresh(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                string[] full = Editor(form).Columns;

                DataTable narrow = new DataTable();

                foreach (string column in full)
                {
                    if (!string.Equals(column, "REMARK", StringComparison.OrdinalIgnoreCase))
                    {
                        narrow.Columns.Add(column);
                    }
                }

                Editor(form).SetSchema(narrow);

                DataTable back = new DataTable();

                foreach (string column in full)
                {
                    back.Columns.Add(column);
                }

                Editor(form).SetSchema(back);
                WriteEditor(Editor(form), "REMARK", "typed after the column came back");

                Check(profile, "없어졌다 돌아온 컬럼은 새 컬럼으로 시작한다(옛 기준이 되살아나지 않는다)",
                        Editor(form).IsDirty, "dirty=" + Editor(form).IsDirty);

                Editor(form).RevertEdits();

                Check(profile, "돌아온 컬럼의 기준은 돌아온 시점의 값이다(빈 값으로 되돌아간다)",
                        !Editor(form).IsDirty && Editor(form).ReadValue("REMARK").Length == 0,
                        "dirty=" + Editor(form).IsDirty + " remark=[" + Editor(form).ReadValue("REMARK") + "]");
            }
        }

        private static async Task TestReadOnlyFocusBorder(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);

                Grid(form).SelectedIndex = 1;
                await WaitUntilAsync(() => Editor(form).ReadKey() == profile.SeedKey, 5000);

                object keyEditor = EditorOf(Editor(form), profile.Key);
                PropertyInfo controlProperty = keyEditor.GetType().GetProperty(
                        "Control", BindingFlags.NonPublic | BindingFlags.Instance);
                System.Windows.Forms.Integration.ElementHost host =
                        (System.Windows.Forms.Integration.ElementHost)controlProperty.GetValue(keyEditor, null);
                System.Windows.FrameworkElement wpf = (System.Windows.FrameworkElement)host.Child;
                System.Windows.Controls.Border outer =
                        (System.Windows.Controls.Border)wpf.FindName("OuterBorder");
                System.Windows.Media.Brush subtle = (System.Windows.Media.Brush)wpf.TryFindResource("Brush.BorderSubtle");
                System.Windows.Media.Brush hover = (System.Windows.Media.Brush)wpf.TryFindResource("Brush.BorderHover");

                Check(profile, "읽기 전용 키 칸은 포커스가 없으면 흐린 테두리다",
                        EditorReadOnly(Editor(form), profile.Key)
                                && object.ReferenceEquals(outer.BorderBrush, subtle),
                        "brush=" + Convert.ToString(outer.BorderBrush));

                int readOnlyAt = -1;
                int focusedReadOnlyAt = -1;
                System.Windows.Media.Brush focusedBrush = null;

                for (int i = 0; i < outer.Style.Triggers.Count; i = i + 1)
                {
                    System.Windows.DataTrigger single = outer.Style.Triggers[i] as System.Windows.DataTrigger;

                    if (single != null && BindsTo(single.Binding, "IsReadOnly"))
                    {
                        readOnlyAt = i;
                    }

                    System.Windows.MultiDataTrigger multi = outer.Style.Triggers[i] as System.Windows.MultiDataTrigger;

                    if (multi == null || multi.Conditions.Count != 2)
                    {
                        continue;
                    }

                    if (BindsTo(multi.Conditions[0].Binding, "IsReadOnly")
                            && BindsTo(multi.Conditions[1].Binding, "IsKeyboardFocusWithin"))
                    {
                        focusedReadOnlyAt = i;
                        focusedBrush = BorderBrushOf(multi.Setters);
                    }
                }

                Check(profile, "읽기 전용 + 키보드 포커스 규칙이 읽기 전용 규칙보다 뒤에 있어 테두리를 되살린다",
                        focusedReadOnlyAt >= 0 && readOnlyAt >= 0 && focusedReadOnlyAt > readOnlyAt
                                && object.ReferenceEquals(focusedBrush, hover),
                        "readOnlyAt=" + readOnlyAt + " focusedAt=" + focusedReadOnlyAt
                                + " brush=" + Convert.ToString(focusedBrush));
            }
        }

        private static bool BindsTo(System.Windows.Data.BindingBase binding, string path)
        {
            System.Windows.Data.Binding typed = binding as System.Windows.Data.Binding;
            return typed != null && typed.Path != null && typed.Path.Path == path;
        }

        private static System.Windows.Media.Brush BorderBrushOf(System.Windows.SetterBaseCollection setters)
        {
            foreach (System.Windows.SetterBase setterBase in setters)
            {
                System.Windows.Setter setter = setterBase as System.Windows.Setter;

                if (setter != null && setter.Property == System.Windows.Controls.Border.BorderBrushProperty)
                {
                    return setter.Value as System.Windows.Media.Brush;
                }
            }

            return null;
        }

        private static async Task TestInsertAndDeleteRoundTrip(ScreenProfile profile)
        {
            using (ProbeHandle form = Open(profile, null, true))
            {
                await WaitIdleAsync(form);
                int seedRows = RowCount(form);

                Invoke(form, "OnNewClick");
                WriteEditor(Editor(form), profile.Key, profile.NewKey);
                WriteEditor(Editor(form), "REMARK", "round trip");
                Invoke(form, "OnSaveClick");
                await WaitIdleAsync(form);

                string insert = FirstContaining(form.State.WriteRequests, profile.InsertMarker);

                Check(profile, "왕복 — 사용자가 친 키로 Insert 하고 재조회 뒤 그 행이 선택된다",
                        insert != null && insert.Contains(" " + profile.KeyParam + "=" + profile.NewKey + " ")
                                && RowCount(form) == seedRows + 1
                                && Editor(form).ReadKey() == profile.NewKey && !Editor(form).IsNew,
                        (insert ?? "(없음)") + " rows=" + RowCount(form) + " key=" + Editor(form).ReadKey());

                Invoke(form, "OnDeleteClick");
                await WaitIdleAsync(form);

                string delete = FirstContaining(form.State.WriteRequests, profile.DeleteMarker);

                Check(profile, "왕복 — Delete 는 사용자가 고른 키 하나를 그대로 보내고 그 행이 사라진다",
                        delete != null
                                && delete == profile.Action + " " + profile.DeleteMarker + " " + profile.KeyParam + "=" + profile.NewKey
                                && RowCount(form) == seedRows,
                        (delete ?? "(없음)") + " rows=" + RowCount(form));
            }
        }

        private static int RowCount(ProbeHandle form)
        {
            DataTable table = Grid(form).DataSource as DataTable;
            return table == null ? -1 : table.Rows.Count;
        }

        private static ProbeHandle Open(ScreenProfile profile, Func<string, string> reply, bool listOpen)
        {
            ProbeHandle handle = new ProbeHandle(profile.Create());
            handle.State.Reply = reply;

            if (!listOpen)
            {
                handle.State.ListGate.Reset();
            }

            handle.Form.StartPosition = FormStartPosition.Manual;
            handle.Form.Location = new System.Drawing.Point(-20000, -20000);
            handle.Form.ShowInTaskbar = false;
            handle.Form.Show();
            return handle;
        }

        private static Func<string, string> ListReply(ScreenProfile profile, string reply)
        {
            return delegate(string request)
            {
                return request.Contains(profile.ListMarker) ? reply : null;
            };
        }

        private static string QueryReply(ScreenProfile profile, string json)
        {
            return profile.ListMethod + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldResultMessage + "=" + json;
        }

        private static Func<string, string> SchemaOnlyAllReply(ScreenProfile profile)
        {
            return delegate(string request)
            {
                return request.Contains(profile.ListMarker) ? SchemaOnlyReply(profile) : AcceptedReply(profile);
            };
        }

        private static string AcceptedReply(ScreenProfile profile)
        {
            return profile.Action + ServerMessageFormat.ReplyNameSuffix
                    + " " + ServerMessageFormat.FieldReturnCode + "=[" + ServerMessageFormat.SuccessReturnCode + "]"
                    + " " + ServerMessageFormat.FieldSendMessage + "=[Accepted by self test]";
        }

        private static string SchemaOnlyReply(ScreenProfile profile)
        {
            return profile.ListMethod + ServerMessageFormat.ReplyNameSuffix
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

        private static ModernPropertyGrid Editor(ProbeHandle form)
        {
            return (ModernPropertyGrid)Field(form.Form, "propertyGrid");
        }

        private static ModernDataGrid Grid(ProbeHandle form)
        {
            return (ModernDataGrid)Field(form.Form, form.State.Profile.GridField);
        }

        private static ModernButton New(ProbeHandle form)
        {
            return (ModernButton)Field(form.Form, "btnNew");
        }

        private static ModernButton Save(ProbeHandle form)
        {
            return (ModernButton)Field(form.Form, "btnSave");
        }

        private static ModernButton Delete(ProbeHandle form)
        {
            return (ModernButton)Field(form.Form, "btnDelete");
        }

        private static ModernButton Cancel(ProbeHandle form)
        {
            return (ModernButton)Field(form.Form, "btnCancel");
        }

        private static string ButtonStates(ProbeHandle form)
        {
            return "new=" + New(form).Enabled + " cancel=" + Cancel(form).Enabled + " save=" + Save(form).Enabled
                    + " delete=" + Delete(form).Enabled + " editor=" + Editor(form).Enabled
                    + " isNew=" + Editor(form).IsNew + " dirty=" + Editor(form).IsDirty;
        }

        private static void WriteEditor(ModernPropertyGrid grid, string column, string value)
        {
            object editor = EditorOf(grid, column);
            MethodInfo write = editor.GetType().GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance);
            write.Invoke(editor, new object[] { value });
        }

        private static bool EditorReadOnly(ModernPropertyGrid grid, string column)
        {
            object editor = EditorOf(grid, column);
            PropertyInfo readOnly = editor.GetType().GetProperty("ReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)readOnly.GetValue(editor, null);
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

        private static void Invoke(ProbeHandle form, string method)
        {
            for (Type type = form.Form.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo target = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);

                if (target == null)
                {
                    continue;
                }

                if (target.GetParameters().Length == 0)
                {
                    target.Invoke(form.Form, null);
                    return;
                }

                target.Invoke(form.Form, new object[] { null, EventArgs.Empty });
                return;
            }

            throw new InvalidOperationException("메서드를 찾을 수 없음: " + method);
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

        private static async Task WaitIdleAsync(ProbeHandle form)
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

        private static void Check(ScreenProfile profile, string name, bool condition, string detail)
        {
            string title = profile.Title + " — " + name;

            if (condition)
            {
                passCount++;
                Line("PASS  " + title + " — " + detail);
            }
            else
            {
                failCount++;
                Line("FAIL  " + title + " — " + detail);
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

            string path = Path.Combine(Path.GetTempPath(), "flowoper-uitest.txt");
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
        }

        private sealed class ScreenProfile
        {
            private readonly Func<Form> create;

            internal ScreenProfile(
                    string title,
                    string key,
                    string keyParam,
                    string action,
                    string plural,
                    string gridField,
                    string loadMethod,
                    string seedKey,
                    string newKey,
                    Func<Form> create)
            {
                this.Title = title;
                this.Key = key;
                this.KeyParam = keyParam;
                this.Action = action;
                this.ListMethod = "Select" + plural;
                this.GridField = gridField;
                this.LoadMethod = loadMethod;
                this.SeedKey = seedKey;
                this.NewKey = newKey;
                this.create = create;
            }

            internal string Title { get; private set; }

            internal string Key { get; private set; }

            internal string KeyParam { get; private set; }

            internal string Action { get; private set; }

            internal string ListMethod { get; private set; }

            internal string GridField { get; private set; }

            internal string LoadMethod { get; private set; }

            internal string SeedKey { get; private set; }

            internal string NewKey { get; private set; }

            internal string ListMarker
            {
                get { return "MethodCommand=" + this.ListMethod; }
            }

            internal string InsertMarker
            {
                get { return "MethodCommand=Insert" + this.Title; }
            }

            internal string UpdateMarker
            {
                get { return "MethodCommand=Update" + this.Title; }
            }

            internal string DeleteMarker
            {
                get { return "MethodCommand=Delete" + this.Title; }
            }

            internal string[] SchemaColumns
            {
                get { return new string[] { this.Key, AlphaColumn, BetaColumn }; }
            }

            internal Form Create()
            {
                return this.create();
            }
        }

        private sealed class ProbeState
        {
            private readonly List<string> requests = new List<string>();

            internal ProbeState(ScreenProfile profile)
            {
                this.Profile = profile;
                this.ListGate = new ManualResetEventSlim(true);
                this.NoticeText = string.Empty;
                this.LastDialog = string.Empty;
            }

            internal ScreenProfile Profile { get; private set; }

            internal ManualResetEventSlim ListGate { get; private set; }

            internal Func<string, string> Reply { get; set; }

            internal string NoticeText { get; private set; }

            internal bool NoticeShowing { get; private set; }

            internal string LastDialog { get; set; }

            internal string[] WriteRequests
            {
                get
                {
                    List<string> writes = new List<string>();

                    lock (this.requests)
                    {
                        foreach (string request in this.requests)
                        {
                            if (request.Contains(this.Profile.InsertMarker)
                                    || request.Contains(this.Profile.UpdateMarker)
                                    || request.Contains(this.Profile.DeleteMarker))
                            {
                                writes.Add(request);
                            }
                        }
                    }

                    return writes.ToArray();
                }
            }

            internal string Send(string request, Func<string, string> fallback)
            {
                lock (this.requests)
                {
                    this.requests.Add(request);
                }

                if (request.Contains(this.Profile.ListMarker))
                {
                    this.ListGate.Wait(10000);
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

                return fallback(request);
            }

            internal DataTable Parse(string sendMessage, Func<string, DataTable> fallback)
            {
                if (sendMessage != null && sendMessage.Contains(SchemaMarker))
                {
                    DataTable schema = new DataTable();

                    foreach (string column in this.Profile.SchemaColumns)
                    {
                        schema.Columns.Add(column);
                    }

                    return schema;
                }

                return fallback(sendMessage);
            }

            internal void Notice(string message)
            {
                this.NoticeText = message ?? string.Empty;
                this.NoticeShowing = true;
            }

            internal void ClearNotice()
            {
                this.NoticeText = string.Empty;
                this.NoticeShowing = false;
            }

            internal void Release()
            {
                this.ListGate.Set();
            }
        }

        private interface IProbe
        {
            ProbeState State { get; }

            bool Idle { get; }
        }

        private sealed class ProbeHandle : IDisposable
        {
            private readonly IProbe probe;

            internal ProbeHandle(Form form)
            {
                this.probe = (IProbe)form;
                this.Form = form;
                this.State = this.probe.State;
            }

            internal Form Form { get; private set; }

            internal ProbeState State { get; private set; }

            internal bool Idle
            {
                get { return this.probe.Idle; }
            }

            public void Dispose()
            {
                this.Form.Dispose();
            }
        }

        private sealed class FlowProbeForm : FlowForm, IProbe
        {
            private readonly ProbeState state = new ProbeState(FlowProfile);

            public ProbeState State
            {
                get { return this.state; }
            }

            public bool Idle
            {
                get { return !this.QueryInProgress && !this.ActionInProgress; }
            }

            protected override string SendRequestText(string request)
            {
                return this.state.Send(request, base.SendRequestText);
            }

            protected override DataTable ParseQueryData(string sendMessage)
            {
                return this.state.Parse(sendMessage, base.ParseQueryData);
            }

            protected override bool Confirm(string message, string caption)
            {
                return true;
            }

            protected override void ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind kind, string caption, string message)
            {
                this.state.LastDialog = (caption ?? string.Empty) + ": " + (message ?? string.Empty);
            }

            protected override void ShowStickyNotice(string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
            {
                this.state.Notice(message);
                base.ShowStickyNotice(message, kind);
            }

            protected override void HideNotice()
            {
                this.state.ClearNotice();
                base.HideNotice();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.state.Release();
                }

                base.Dispose(disposing);
            }
        }

        private sealed class OperProbeForm : OperForm, IProbe
        {
            private readonly ProbeState state = new ProbeState(OperProfile);

            public ProbeState State
            {
                get { return this.state; }
            }

            public bool Idle
            {
                get { return !this.QueryInProgress && !this.ActionInProgress; }
            }

            protected override string SendRequestText(string request)
            {
                return this.state.Send(request, base.SendRequestText);
            }

            protected override DataTable ParseQueryData(string sendMessage)
            {
                return this.state.Parse(sendMessage, base.ParseQueryData);
            }

            protected override bool Confirm(string message, string caption)
            {
                return true;
            }

            protected override void ShowMessage(
                    Modern.Lab.Controls.Wpf.Display.ModernMessageKind kind, string caption, string message)
            {
                this.state.LastDialog = (caption ?? string.Empty) + ": " + (message ?? string.Empty);
            }

            protected override void ShowStickyNotice(string message, Modern.Lab.Controls.Wpf.Display.ToastKind kind)
            {
                this.state.Notice(message);
                base.ShowStickyNotice(message, kind);
            }

            protected override void HideNotice()
            {
                this.state.ClearNotice();
                base.HideNotice();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.state.Release();
                }

                base.Dispose(disposing);
            }
        }
    }
}
