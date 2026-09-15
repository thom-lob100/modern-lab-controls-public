using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.MasterData.Services;
using Modern.Lab.WinForms.Controls.Input;
using Modern.Lab.WinForms.Controls.Selection;

namespace Modern.Lab.MasterData.Diagnostics
{
    public static class PfoNodeUiSelfTest
    {
        private static readonly StringBuilder report = new StringBuilder();
        private static int passCount;
        private static int failCount;

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
            Check("메뉴 — PFO 화면 생성", MasterDataMenuForm.Create("pfo") is PfoNodeForm, string.Empty);

            using (PfoNodeForm form = Open())
            {
                await WaitReadyAsync(form);

                ModernComboBox products = Field<ModernComboBox>(form, "cboProduct");
                ListBox flows = Field<ListBox>(form, "lstFlows");
                ListBox opers = Field<ListBox>(form, "lstOpers");
                TreeView tree = Field<TreeView>(form, "treeNodes");

                DataTable productRows = products.DataSource as DataTable;
                int productCount = productRows == null ? 0 : productRows.Rows.Count;
                Check("조회 — Product 후보", productCount > 0, "count=" + productCount);
                Check("조회 — Flow 후보", flows.DataSource != null && flows.Items.Count > 0,
                        "count=" + flows.Items.Count);
                Check("조회 — Oper 후보", opers.DataSource != null && opers.Items.Count > 0,
                        "count=" + opers.Items.Count);
                Check("조회 — Product 루트", tree.Nodes.Count == 1,
                        "roots=" + tree.Nodes.Count);
                Check("키 — 모든 NODE_ID 20자리 숫자", AllNodeIdsValid(tree.Nodes), NodeIds(tree.Nodes));

                string productId = Convert.ToString(products.SelectedValue, CultureInfo.InvariantCulture);
                string flowId = ValueOf((DataRowView)flows.Items[0], "FLOW_ID");
                string operId = ValueOf((DataRowView)opers.Items[0], "OPER_ID");
                ModernButton save = Field<ModernButton>(form, "btnSave");
                ModernButton cancel = Field<ModernButton>(form, "btnCancel");
                ModernButton delete = Field<ModernButton>(form, "btnDelete");
                ModernButton createRoot = Field<ModernButton>(form, "btnCreateProductNode");
                Check("상태 — 정상 Product에서는 복구 버튼 숨김", !createRoot.Visible,
                        "visible=" + createRoot.Visible);

                int originalNodeCount = CountNodes(tree.Nodes);
                int originalFlowCount = tree.Nodes[0].Nodes.Count;
                int writesBeforeDraft = PfoNodeLocalServer.SaveRequestCount;
                Invoke(form, "AddDraftFlow", flowId);
                TreeNode draftFlow = tree.SelectedNode;
                Check("초안 — Flow 드롭은 서버를 호출하지 않음",
                        PfoNodeLocalServer.SaveRequestCount == writesBeforeDraft,
                        "writes=" + PfoNodeLocalServer.SaveRequestCount);
                Check("유효성 — Oper 없는 Flow는 Save 차단", !save.Enabled,
                        "enabled=" + save.Enabled);
                Invoke(form, "AddDraftOper", draftFlow, operId);
                Check("유효성 — Oper 추가 뒤 Save 활성", save.Enabled,
                        "enabled=" + save.Enabled);
                cancel.PerformClick();
                Check("Cancel — 서버 호출 없이 마지막 조회 복원",
                        CountNodes(tree.Nodes) == originalNodeCount
                        && PfoNodeLocalServer.SaveRequestCount == writesBeforeDraft,
                        "nodes=" + CountNodes(tree.Nodes));

                Invoke(form, "AddDraftFlow", flowId);
                draftFlow = tree.SelectedNode;
                Invoke(form, "AddDraftOper", draftFlow, operId);
                int writesBeforeSave = PfoNodeLocalServer.SaveRequestCount;
                save.PerformClick();
                await WaitReadyAsync(form);
                Check("Save — 전체 초안을 서버 요청 한 건으로 저장",
                        PfoNodeLocalServer.SaveRequestCount == writesBeforeSave + 1,
                        "writes=" + PfoNodeLocalServer.SaveRequestCount);
                Check("Save — 재조회 뒤 생성 Node는 20자리 서버 키",
                        AllNodeIdsValid(tree.Nodes), NodeIds(tree.Nodes));
                Check("Save — 추가 Flow와 Oper가 함께 반영",
                        tree.Nodes[0].Nodes.Count == originalFlowCount + 1,
                        "flows=" + tree.Nodes[0].Nodes.Count);

                TreeNode savedOper = tree.SelectedNode;
                TreeNode savedFlow = savedOper == null ? null : savedOper.Parent;
                tree.SelectedNode = savedOper;
                int operCount = savedFlow == null ? 0 : savedFlow.Nodes.Count;
                int writesBeforeDelete = PfoNodeLocalServer.SaveRequestCount;
                delete.PerformClick();
                Check("삭제 — Oper는 선택 Node만 초안에서 제거",
                        savedFlow != null && savedFlow.Nodes.Count == operCount - 1,
                        "opers=" + (savedFlow == null ? -1 : savedFlow.Nodes.Count));
                Check("삭제 — 마지막 Oper 제거 시 Save 차단", !save.Enabled,
                        "enabled=" + save.Enabled);
                Check("삭제 — 초안 삭제는 서버를 호출하지 않음",
                        PfoNodeLocalServer.SaveRequestCount == writesBeforeDelete,
                        "writes=" + PfoNodeLocalServer.SaveRequestCount);
                cancel.PerformClick();

                TreeNode flowToDelete = tree.Nodes[0].Nodes[tree.Nodes[0].Nodes.Count - 1];
                int removedSubtreeSize = 1 + CountNodes(flowToDelete.Nodes);
                tree.SelectedNode = flowToDelete;
                delete.PerformClick();
                Check("삭제 — Flow는 하위 Oper까지 초안에서 제거",
                        CountNodes(tree.Nodes) == originalNodeCount + 2 - removedSubtreeSize,
                        "nodes=" + CountNodes(tree.Nodes));
                Check("삭제 — 완성된 나머지 구조는 Save 가능", save.Enabled,
                        "enabled=" + save.Enabled);
                save.PerformClick();
                await WaitReadyAsync(form);
                Check("삭제 Save — 재조회 뒤 Flow 하위 구조 제거",
                        tree.Nodes[0].Nodes.Count == originalFlowCount,
                        "flows=" + tree.Nodes[0].Nodes.Count);

                tree.SelectedNode = tree.Nodes[0];
                Check("삭제 — Product Node는 삭제 불가", !delete.Enabled,
                        "enabled=" + delete.Enabled);

                List<Dictionary<string, object>> invalidPlan = new List<Dictionary<string, object>>();
                Dictionary<string, object> invalidFlow = new Dictionary<string, object>();
                invalidFlow["ClientId"] = "draft-invalid";
                invalidFlow["ParentClientId"] = string.Empty;
                invalidFlow["NodeId"] = string.Empty;
                invalidFlow["Kind"] = "Flow";
                invalidFlow["ReferenceId"] = flowId;
                invalidFlow["NodeSeq"] = 1;
                invalidPlan.Add(invalidFlow);
                DataActionResult rejected = RequestAction(form, "SavePfoNodes",
                        PfoNodeLocalServer.ProductProcessColumn, productId,
                        "Plan", invalidPlan,
                        "SelectedClientId", "draft-invalid");
                Check("서버 유효성 — Oper 없는 Flow Plan 거절", !rejected.Success,
                        rejected.Code);
            }
        }

        private static PfoNodeForm Open()
        {
            PfoNodeForm form = new PfoNodeForm();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new System.Drawing.Point(-20000, -20000);
            form.ShowInTaskbar = false;
            form.Show();
            return form;
        }

        private static async Task WaitReadyAsync(PfoNodeForm form)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);

            while (DateTime.UtcNow < deadline)
            {
                string state = Convert.ToString(FieldObject(form, "screenState"), CultureInfo.InvariantCulture);

                if (!string.Equals(state, "Loading", StringComparison.Ordinal)
                        && !string.Equals(state, "Saving", StringComparison.Ordinal))
                {
                    return;
                }

                await Task.Delay(50);
            }

            throw new TimeoutException("PFO 화면 정착 시간 초과");
        }

        private static DataActionResult RequestAction(PfoNodeForm form, string method, params object[] fields)
        {
            MethodInfo request = typeof(PfoNodeForm).GetMethod("RequestAction",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            return (DataActionResult)request.Invoke(form, new object[] { method, fields });
        }

        private static string ReturnedNodeId(DataActionResult reply)
        {
            string[] values = reply.Values;
            return values.Length == 0 ? string.Empty : values[0].Trim();
        }

        private static string ValueOf(DataRowView row, string column)
        {
            return Convert.ToString(row[column], CultureInfo.InvariantCulture);
        }

        private static bool AllNodeIdsValid(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (!IsNodeId(node.Name) || !AllNodeIdsValid(node.Nodes))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsNodeId(string value)
        {
            if (value == null || value.Length != 20)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static TreeNode FindNode(TreeNodeCollection nodes, string nodeId)
        {
            foreach (TreeNode node in nodes)
            {
                if (string.Equals(node.Name, nodeId, StringComparison.Ordinal))
                {
                    return node;
                }

                TreeNode found = FindNode(node.Nodes, nodeId);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static int CountNodes(TreeNodeCollection nodes)
        {
            int count = 0;

            foreach (TreeNode node in nodes)
            {
                count = count + 1 + CountNodes(node.Nodes);
            }

            return count;
        }

        private static string NodeIds(TreeNodeCollection nodes)
        {
            List<string> values = new List<string>();
            AddNodeIds(nodes, values);
            return string.Join(",", values.ToArray());
        }

        private static void AddNodeIds(TreeNodeCollection nodes, List<string> values)
        {
            foreach (TreeNode node in nodes)
            {
                values.Add(node.Name);
                AddNodeIds(node.Nodes, values);
            }
        }

        private static T Field<T>(object target, string name) where T : class
        {
            return (T)FieldObject(target, name);
        }

        private static object FieldObject(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                throw new InvalidOperationException("필드를 찾을 수 없음: " + name);
            }

            return field.GetValue(target);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
            {
                throw new InvalidOperationException("메서드를 찾을 수 없음: " + name);
            }

            method.Invoke(target, arguments);
        }

        private static void Check(string name, bool passed, string details)
        {
            if (passed)
            {
                passCount++;
                Line("PASS " + name);
                return;
            }

            failCount++;
            Line("FAIL " + name + (details.Length == 0 ? string.Empty : " — " + details));
        }

        private static void Line(string text)
        {
            report.AppendLine(text);
        }

        private static void Finish()
        {
            Line("합계: PASS " + passCount + " / FAIL " + failCount);
            string path = Path.Combine(Path.GetTempPath(), "pfo-uitest.txt");
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            Console.Write(report.ToString());
            Environment.ExitCode = failCount == 0 ? 0 : 1;
        }
    }
}
