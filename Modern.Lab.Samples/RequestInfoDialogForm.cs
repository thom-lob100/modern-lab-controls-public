using System;
using System.Data;
using System.Windows.Forms;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Data;
using Modern.Lab.Samples.Hosting;
using Modern.Lab.Samples.Management.Contracts;
using Modern.Lab.Samples.Services;
using Modern.Lab.WinForms.Controls.Display;

namespace Modern.Lab.Samples
{

    public partial class RequestInfoDialogForm : ModernFormBase
    {
        public RequestInfoDialogForm()
        {
            this.InitializeComponent();

            this.InitializeModernForm();
            this.ApplyHeaderColors();

            this.gridSpecimens.AllowColumnFilters = false;
            this.lblRequestNoCaption.Text = FieldCaption(ReqSerialNoColumn);
            this.lblRequestRemarkCaption.ForeColor = Modern.Lab.Theming.ModernTheme.TextSecondary;
        }


        public void SetRequest(RequestInfoData data)
        {
            if (data == null)
            {
                this.lblRequestNoCaption.Text = FieldCaption(ReqNoColumn);
                this.ApplyRequest(string.Empty, null, null, string.Empty);
                return;
            }

            DataTable overview = OverviewOf(data);
            overview.Columns.Remove(ServerFields.Request.Details);
            this.lblRequestNoCaption.Text = FieldCaption(ReqNoColumn);
            this.ApplyRequest(data.RequestNo, overview, data.Specimens, data.Details);
        }

        public void SetRequest(string requestSerialNo, DataTable master, object specimens, string remarks)
        {
            this.lblRequestNoCaption.Text = FieldCaption(ReqSerialNoColumn);
            this.ApplyRequest(requestSerialNo, master, specimens, remarks);
        }




        public void SetRequest(DataRow header, object specimens)
        {
            this.SetRequest(header, specimens, null);
        }




        public void SetRequest(DataRow header, object specimens, string detailsMember)
        {
            DataTable overview = header == null ? null : OverviewOf(header);
            string remarks = ExtractMember(overview, detailsMember);
            this.lblRequestNoCaption.Text = FieldCaption(ReqNoColumn);
            this.ApplyRequest(TableHelper.CellText(header, ReqNoColumn), overview, specimens, remarks);
        }







        public void SetRequest(DataTable joined, string firstSpecimenMember, string detailsMember)
        {
            DataTable overview = RequestInfoTables.Overview(joined, firstSpecimenMember);
            DataRow header = overview == null || overview.Rows.Count == 0 ? null : overview.Rows[0];

            this.SetRequest(header, RequestInfoTables.Specimens(joined, firstSpecimenMember), detailsMember);
        }

        private void ApplyRequest(string requestNumber, DataTable overview, object specimens, string remarks)
        {
            this.lblRequestNo.Text = ValueOrDash(requestNumber);
            this.BindRequestFields(overview, remarks);
            this.gridSpecimens.DataSource = specimens;
        }

        private void BindRequestFields(DataTable overview, string remarks)
        {
            string[] members = AutoColumns.MembersOf(overview);
            System.Collections.Generic.List<ModernFieldDefinition> fields =
                    new System.Collections.Generic.List<ModernFieldDefinition>();

            for (int index = 0; index < members.Length; index++)
            {
                fields.Add(new ModernFieldDefinition(members[index]));
            }

            this.fieldRequest.DefineFields(fields.ToArray());
            DataRow row = overview == null || overview.Rows.Count == 0 ? null : overview.Rows[0];
            this.fieldRequest.SetRow(row);
            int fieldRows = (fields.Count + this.fieldRequest.Columns - 1) / this.fieldRequest.Columns;
            int fieldHeight = Math.Max(1, fieldRows) * 40 * this.DeviceDpi / 96;
            int remarkHeight = row == null ? 0 : 72 * this.DeviceDpi / 96;
            this.tableRequestMaster.RowStyles[0].Height = fieldHeight;
            this.tableRequestMaster.RowStyles[1].Height = remarkHeight;
            this.tableRequestMaster.Height = fieldHeight + remarkHeight;
            this.lblRequestRemarkCaption.Text = "Remarks";
            this.lblRequestRemark.Text = row == null ? string.Empty : ValueOrDash(remarks);
            this.panelRequestRemark.Visible = row != null;
            this.tableRequestMaster.Visible = row != null;
            this.lblRequestEmpty.Visible = row == null;
        }

        private static string ExtractMember(DataTable overview, string member)
        {
            string resolvedMember = ResolveRemarkMember(overview, member);

            if (resolvedMember == null)
            {
                return string.Empty;
            }

            string value = overview.Rows.Count == 0 ? string.Empty : TableHelper.CellText(overview.Rows[0], resolvedMember);
            overview.Columns.Remove(resolvedMember);
            return value;
        }

        private static string ResolveRemarkMember(DataTable overview, string preferred)
        {
            if (overview == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(preferred) && overview.Columns.Contains(preferred))
            {
                return preferred;
            }

            if (overview.Columns.Contains("REMARK"))
            {
                return "REMARK";
            }

            return overview.Columns.Contains(ServerFields.Request.Details) ? ServerFields.Request.Details : null;
        }

        private static DataTable OverviewOf(RequestInfoData data)
        {
            DataTable overview = new DataTable("REQUEST");
            overview.Columns.Add(ServerFields.Request.ReqNo, typeof(string));
            overview.Columns.Add(ServerFields.Request.LayerNm, typeof(string));
            overview.Columns.Add(ServerFields.Request.Requester, typeof(string));
            overview.Columns.Add(ServerFields.Request.Department, typeof(string));
            overview.Columns.Add(ServerFields.Request.DeviceNm, typeof(string));
            overview.Columns.Add(ServerFields.Request.ProjectNm, typeof(string));
            overview.Columns.Add(ServerFields.Request.Details, typeof(string));

            DataRow row = overview.NewRow();
            row[ServerFields.Request.ReqNo] = data.RequestNo ?? string.Empty;
            row[ServerFields.Request.LayerNm] = data.Layer ?? string.Empty;
            row[ServerFields.Request.Requester] = data.Requester ?? string.Empty;
            row[ServerFields.Request.Department] = data.Department ?? string.Empty;
            row[ServerFields.Request.DeviceNm] = data.Device ?? string.Empty;
            row[ServerFields.Request.ProjectNm] = data.Project ?? string.Empty;
            row[ServerFields.Request.Details] = data.Details ?? string.Empty;
            overview.Rows.Add(row);
            overview.AcceptChanges();
            return overview;
        }

        private static DataTable OverviewOf(DataRow header)
        {
            DataTable overview = header.Table.Clone();
            overview.ImportRow(header);
            overview.AcceptChanges();
            return overview;
        }

        private static string FieldCaption(string member)
        {
            return new ModernDataGridColumn(member).HeaderText;
        }

        private void ApplyHeaderColors()
        {
            this.panelAccentBar.BackColor = Modern.Lab.Theming.ModernTheme.Accent;
            this.lblRequestNo.ForeColor = Modern.Lab.Theming.ModernTheme.Accent;
            this.lblRequestNoCaption.ForeColor = Modern.Lab.Theming.ModernTheme.TextSecondary;
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private void OnCloseClick(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
