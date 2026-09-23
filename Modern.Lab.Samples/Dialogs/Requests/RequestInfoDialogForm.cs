using System;
using System.Data;
using System.Windows.Forms;
using Modern.Lab.Controls.Wpf.Data;
using Modern.Lab.Data;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting;
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
            this.lblRequestSerialNoCaption.Text = FieldCaption(ReqSerialNoColumn);
            this.lblRequestRemarkCaption.ForeColor = Modern.Lab.Theming.ModernTheme.TextSecondary;
        }


        public void SetRequest(string requestSerialNo, DataTable master, object specimens, string purpose)
        {
            this.lblRequestSerialNoCaption.Text = FieldCaption(ReqSerialNoColumn);
            this.ApplyRequest(requestSerialNo, master, specimens, purpose);
        }




        public void SetRequest(DataRow header, object specimens)
        {
            this.SetRequest(header, specimens, null);
        }




        public void SetRequest(DataRow header, object specimens, string purposeMember)
        {
            DataTable overview = header == null ? null : OverviewOf(header);
            string purpose = ExtractMember(overview, purposeMember);
            this.lblRequestSerialNoCaption.Text = FieldCaption(ReqSerialNoColumn);
            this.ApplyRequest(TableHelper.CellText(header, ReqSerialNoColumn), overview, specimens, purpose);
        }







        public void SetRequest(DataTable joined, string firstSpecimenMember, string purposeMember)
        {
            DataTable overview = RequestInfoTables.Overview(joined, firstSpecimenMember);
            DataRow header = overview == null || overview.Rows.Count == 0 ? null : overview.Rows[0];

            this.SetRequest(header, RequestInfoTables.Specimens(joined, firstSpecimenMember), purposeMember);
        }

        private void ApplyRequest(string requestSerialNo, DataTable overview, object specimens, string purpose)
        {
            this.lblRequestSerialNo.Text = ValueOrDash(requestSerialNo);
            this.BindRequestFields(overview, purpose);
            this.gridSpecimens.DataSource = specimens;
        }

        private void BindRequestFields(DataTable overview, string purpose)
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
            this.lblRequestRemark.Text = row == null ? string.Empty : ValueOrDash(purpose);
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

            return overview.Columns.Contains(ServerFields.Request.Purpose) ? ServerFields.Request.Purpose : null;
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
            this.lblRequestSerialNo.ForeColor = Modern.Lab.Theming.ModernTheme.Accent;
            this.lblRequestSerialNoCaption.ForeColor = Modern.Lab.Theming.ModernTheme.TextSecondary;
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
