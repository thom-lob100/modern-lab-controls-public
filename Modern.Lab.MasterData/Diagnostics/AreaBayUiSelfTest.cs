using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.MasterData.Controls;
using Modern.Lab.MasterData.Services;
using Modern.Lab.WinForms.Controls.Data;
using Modern.Lab.WinForms.Controls.Input;

namespace Modern.Lab.MasterData.Diagnostics
{
    public static class AreaBayUiSelfTest
    {
        private static readonly StringBuilder Report = new StringBuilder();
        private static int passed;
        private static int failed;

        public static void Run()
        {
            using (Form pump = new Form())
            {
                IntPtr handle = pump.Handle;
                GC.KeepAlive(handle);
                pump.BeginInvoke(new MethodInvoker(async delegate
                {
                    try
                    {
                        TestServer();
                        await TestScreen();
                    }
                    catch (Exception exception)
                    {
                        Check("검사 실행 예외: " + exception, false);
                    }
                    finally
                    {
                        Report.AppendLine(string.Format("합계: PASS {0} / FAIL {1}", passed, failed));
                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "areabay-uitest.txt"), Report.ToString(), new UTF8Encoding(false));
                        Environment.ExitCode = failed == 0 ? 0 : 1;
                        Application.ExitThread();
                    }
                }));
                Application.Run();
            }
        }

        private static void TestServer()
        {
            Check("Area 등록", Accepted(Save("InsertLocation", "TEST-AREA", "AREA", "FAB-T", "", "Y")));
            RejectUnchanged("대소문자 중복 코드", () => Save("InsertLocation", "test-area", "AREA", "FAB-T", "", "Y"));
            RejectUnchanged("빈 코드", () => Save("InsertLocation", "", "AREA", "FAB-T", "", "Y"));
            RejectUnchanged("알 수 없는 구분", () => Save("InsertLocation", "TEST-X", "ZONE", "FAB-T", "", "Y"));
            RejectUnchanged("빈 이름", () => Send("UpdateLocation", "LocationId", "TEST-AREA", "LocationName", ""));
            RejectUnchanged("빈 Fab", () => Save("InsertLocation", "TEST-X", "AREA", "", "", "Y"));
            RejectUnchanged("잘못된 활성 값", () => Save("InsertLocation", "TEST-X", "AREA", "FAB-T", "", "X"));
            RejectUnchanged("Area의 부모", () => Save("InsertLocation", "TEST-X", "AREA", "FAB-T", "TEST-AREA", "Y"));
            RejectUnchanged("Bay 부모 필수", () => Save("InsertLocation", "TEST-X", "BAY", "FAB-T", "", "Y"));
            RejectUnchanged("다른 Fab 부모", () => Save("InsertLocation", "TEST-X", "BAY", "FAB-X", "TEST-AREA", "Y"));
            Check("Bay 등록", Accepted(Save("InsertLocation", "TEST-BAY", "BAY", "FAB-T", "test-area", "Y")));
            RejectUnchanged("Bay를 부모로 지정", () => Save("InsertLocation", "TEST-X", "BAY", "FAB-T", "TEST-BAY", "Y"));
            RejectUnchanged("부모 삭제", () => Send("DeleteLocation", "LocationId", "TEST-AREA"));
            RejectUnchanged("자식 있는 Area의 Fab 변경", () => Save("UpdateLocation", "TEST-AREA", "AREA", "FAB-X", "", "Y"));
            RejectUnchanged("활성 Bay의 부모 비활성화", () => Save("UpdateLocation", "TEST-AREA", "AREA", "FAB-T", "", "N"));
            Check("비활성 Area 등록", Accepted(Save("InsertLocation", "TEST-OFF", "AREA", "FAB-T", "", "N")));
            RejectUnchanged("자식 있는 Area의 타입 변경", () => Save("UpdateLocation", "TEST-AREA", "BAY", "FAB-T", "TEST-OFF", "N"));
            RejectUnchanged("비활성 부모에 활성 Bay 등록", () => Save("InsertLocation", "TEST-X", "BAY", "FAB-T", "TEST-OFF", "Y"));
            Check("비활성 Bay 이동", Accepted(Save("UpdateLocation", "TEST-BAY", "BAY", "FAB-T", "TEST-OFF", "N")));
            RejectUnchanged("비활성 자식도 부모 삭제 금지", () => Send("DeleteLocation", "LocationId", "TEST-OFF"));
            RejectUnchanged("비활성 부모의 Bay 활성화", () => Save("UpdateLocation", "TEST-BAY", "BAY", "FAB-T", "TEST-OFF", "Y"));
            Check("Bay 삭제", Accepted(Send("DeleteLocation", "LocationId", "TEST-BAY")));
            Check("자식 없는 Area 삭제", Accepted(Send("DeleteLocation", "LocationId", "TEST-AREA")));
            Check("나머지 Area 삭제", Accepted(Send("DeleteLocation", "LocationId", "TEST-OFF")));
            Dictionary<string, object> reply = ReplyMessageParser.Parse(Send("SelectLocations", "Keyword", "no-such-location"));
            DataTable empty = AreaBayLocalServer.ParseTable(MasterDataLocalReply.Text(reply, "ResultMessage"));
            Check("빈 조회도 명시 스키마 유지", empty.Rows.Count == 0 && empty.Columns.Contains("LOCATION_ID"));
            bool rejected = false;
            try
            {
                AreaBayLocalServer.ParseTable("{\"Rows\":[]}");
            }
            catch (FormatException)
            {
                rejected = true;
            }
            Check("잘못된 응답에 스키마를 만들어내지 않음", rejected);
        }

        private static async Task TestScreen()
        {
            using (Form menuScreen = MasterDataMenuForm.Create("areabay"))
            {
                Check("메뉴 진입점", menuScreen is AreaBayForm);
            }
            using (ProbeForm form = new ProbeForm())
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new System.Drawing.Point(-20000, -20000);
                form.ShowInTaskbar = false;
                form.Show();
                await Idle(form);
                ModernPropertyGrid editor = (ModernPropertyGrid)Field(form, "propertyGrid");
                ModernDataGrid grid = (ModernDataGrid)Field(form, "gridLocations");
                Check("최초 조회와 편집기", ((DataTable)grid.DataSource).Rows.Count > 0 && editor.Columns.Length == 9);
                grid.SelectedIndex = 0;
                await Until(() => editor.ReadKey() == "AREA-ETCH");
                Check("기존 코드와 감사 필드 읽기 전용", ReadOnly(editor, "LOCATION_ID")
                        && ReadOnly(editor, "UPDATED_BY") && ReadOnly(editor, "UPDATED_AT"));
                string original = editor.ReadValue("DESCRIPTION");
                Write(editor, "DESCRIPTION", "changed");
                Invoke(form, "OnCancelClick");
                Check("Cancel은 기존 값으로 복구", editor.ReadValue("DESCRIPTION") == original && !editor.IsDirty);

                ((ModernTextBox)Field(form, "txtKeyword")).Text = "no-such-location";
                Invoke(form, "OnSearchClick");
                await Idle(form);
                Check("빈 검색에서도 신규 등록 가능", ((DataTable)grid.DataSource).Rows.Count == 0 && Button(form, "btnNew").Enabled && editor.IsNew);
                ((ModernTextBox)Field(form, "txtKeyword")).Text = string.Empty;
                Invoke(form, "OnSearchClick");
                await Idle(form);
                Invoke(form, "OnNewClick");
                Write(editor, "LOCATION_ID", "UI-AREA");
                Write(editor, "LOCATION_TYPE", "AREA");
                Write(editor, "LOCATION_NAME", "UI area");
                Write(editor, "FAB_ID", "FAB-UI");
                Write(editor, "USE_YN", "Y");
                Invoke(form, "OnSaveClick");
                Invoke(form, "OnSaveClick");
                await Idle(form);
                Check("신규 저장 한 번 후 반환 키 재선택", form.Writes == 1 && editor.ReadKey() == "UI-AREA" && !editor.IsNew);
                Dictionary<string, DataTable> sources = (Dictionary<string, DataTable>)Field(editor, "comboSources");
                Check("Area 등록 후 부모 목록 갱신", sources["PARENT_AREA_ID"].Select("CODE = 'UI-AREA'").Length == 1);
                Write(editor, "DESCRIPTION", "updated through UI");
                Invoke(form, "OnSaveClick");
                await Idle(form);
                Check("UI 수정과 재조회", editor.ReadValue("DESCRIPTION") == "updated through UI" && !editor.IsDirty);
                Invoke(form, "OnNewClick");
                Check("신규 코드는 편집 가능", !ReadOnly(editor, "LOCATION_ID"));
                Write(editor, "LOCATION_ID", "UI-BAY");
                Write(editor, "LOCATION_TYPE", "BAY");
                Write(editor, "LOCATION_NAME", "UI bay");
                Write(editor, "FAB_ID", "FAB-UI");
                Write(editor, "PARENT_AREA_ID", "UI-AREA");
                Write(editor, "USE_YN", "Y");
                Invoke(form, "OnSaveClick");
                await Idle(form);
                Check("Bay UI 등록과 부모 선택 보존", editor.ReadKey() == "UI-BAY" && editor.ReadValue("PARENT_AREA_ID") == "UI-AREA");
                Invoke(form, "OnDeleteClick");
                await Idle(form);
                DataTable locations = (DataTable)grid.DataSource;
                grid.SelectedIndex = locations.Rows.IndexOf(locations.Select("LOCATION_ID = 'UI-AREA'")[0]);
                await Until(() => editor.ReadKey() == "UI-AREA");
                Invoke(form, "OnDeleteClick");
                await Idle(form);
                Check("UI 삭제와 부모 목록 갱신", AreaBayLocalServer.Snapshot().Select("LOCATION_ID = 'UI-AREA'").Length == 0
                        && sources["PARENT_AREA_ID"].Select("CODE = 'UI-AREA'").Length == 0);

                form.ListGate.Reset();
                Invoke(form, "OnSearchClick");
                await Task.Delay(150);
                int writes = form.Writes;
                Check("재조회 시작은 과거 목록과 편집기를 지우고 쓰기 잠금", grid.DataSource == null && editor.Columns.Length == 0
                        && !editor.Enabled && !Button(form, "btnSave").Enabled);
                Invoke(form, "OnSaveClick");
                Invoke(form, "OnDeleteClick");
                Check("Loading 핸들러도 쓰기 금지", form.Writes == writes);
                form.ListGate.Set();
                await Idle(form);
                form.FailList = true;
                Invoke(form, "OnSearchClick");
                await Idle(form);
                Check("실패 시 과거 데이터 없이 쓰기 닫힘", grid.DataSource == null && editor.Columns.Length == 0 && !Button(form, "btnSave").Enabled);
                form.FailList = false;
                form.BadSchema = true;
                Invoke(form, "OnSearchClick");
                await Idle(form);
                Check("무스키마 응답 쓰기 차단", editor.Columns.Length == 0 && !Button(form, "btnNew").Enabled);
                form.BadSchema = false;
                Invoke(form, "OnSearchClick");
                await Idle(form);
                Check("실패 후 정상 복구", Button(form, "btnNew").Enabled && editor.Columns.Length == 9);
            }
        }

        private static string Save(string method, string key, string type, string fab, string parent, string enabled)
        {
            return Send(method, "LocationId", key, "LocationType", type, "LocationName", "Test location",
                    "FabId", fab, "ParentAreaId", parent, "UseYn", enabled, "Description", "test");
        }

        private static string Send(string method, params object[] pairs)
        {
            List<object> fields = new List<object> { "MethodCommand", method };
            fields.AddRange(pairs);
            return AreaBayLocalServer.Send(ServerMessageFormat.BuildRequestText("AreaBayAction", fields.ToArray()));
        }

        private static bool Accepted(string reply)
        {
            return MasterDataLocalReply.Text(ReplyMessageParser.Parse(reply), "ReturnCode") == "0";
        }

        private static void RejectUnchanged(string name, Func<string> action)
        {
            string before = SnapshotXml();
            bool accepted = Accepted(action());
            Check(name + " 거절 · 감사 필드 포함 원자성", !accepted && before == SnapshotXml());
        }

        private static string SnapshotXml()
        {
            using (StringWriter writer = new StringWriter())
            {
                AreaBayLocalServer.Snapshot().WriteXml(writer);
                return writer.ToString();
            }
        }

        private static object Field(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(target);
                }
            }
            throw new InvalidOperationException(name);
        }

        private static ModernButton Button(ProbeForm form, string name)
        {
            return (ModernButton)Field(form, name);
        }

        private static void Invoke(ProbeForm form, string method)
        {
            typeof(MasterDataCrudFormBase).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, new object[] { null, EventArgs.Empty });
        }

        private static void Write(ModernPropertyGrid grid, string column, string value)
        {
            foreach (object editor in (IEnumerable)Field(grid, "editors"))
            {
                Type type = editor.GetType();
                if ((string)type.GetProperty("Column", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor, null) == column)
                {
                    type.GetMethod("Write", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { value });
                    return;
                }
            }
            throw new InvalidOperationException(column);
        }

        private static bool ReadOnly(ModernPropertyGrid grid, string column)
        {
            foreach (object editor in (IEnumerable)Field(grid, "editors"))
            {
                Type type = editor.GetType();
                if ((string)type.GetProperty("Column", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor, null) == column)
                {
                    return (bool)type.GetProperty("ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor, null);
                }
            }
            throw new InvalidOperationException(column);
        }

        private static async Task Idle(ProbeForm form)
        {
            await Until(() => form.Idle);
            await Task.Delay(150);
        }

        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("UI condition was not reached.");
                }
                await Task.Delay(50);
            }
        }

        private static void Check(string name, bool condition)
        {
            Report.AppendLine((condition ? "PASS  " : "FAIL  ") + name);
            if (condition)
            {
                passed++;
            }
            else
            {
                failed++;
            }
        }

        private sealed class ProbeForm : AreaBayForm
        {
            internal readonly ManualResetEventSlim ListGate = new ManualResetEventSlim(true);
            internal bool FailList { get; set; }
            internal bool BadSchema { get; set; }
            internal int Writes;
            internal bool Idle { get { return !this.QueryInProgress && !this.ActionInProgress; } }

            protected override string SendRequestText(string request)
            {
                if (request.Contains("MethodCommand=SelectLocations"))
                {
                    if (!this.ListGate.Wait(10000))
                    {
                        throw new TimeoutException("List gate timed out.");
                    }
                    if (this.FailList)
                    {
                        throw new InvalidOperationException("Simulated list failure.");
                    }
                    if (this.BadSchema)
                    {
                        return "SelectLocationsR ReturnCode=[0] ResultMessage=[]";
                    }
                }
                if (request.Contains("MethodCommand=Insert") || request.Contains("MethodCommand=Update") || request.Contains("MethodCommand=Delete"))
                {
                    Interlocked.Increment(ref this.Writes);
                }
                return base.SendRequestText(request);
            }

            protected override bool Confirm(string message, string caption)
            {
                return true;
            }

            protected override void ShowMessage(Modern.Lab.Controls.Wpf.Display.ModernMessageKind kind, string caption, string message)
            {
                Report.AppendLine("대화상자: " + caption + " / " + message);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.ListGate.Set();
                }
                base.Dispose(disposing);
            }
        }
    }
}
