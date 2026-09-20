using System.Data;
using System.Text;
using Modern.Lab.Hosting.Contracts;
using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.Samples
{

    public partial class RequestManagementForm
    {
        private DataTable RequestRequests(SearchConditions conditions)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetReqList");
            AppendCondition(request, ServerFields.Request.ReqSerialNo, conditions.ReqSerialNo);
            AppendCondition(request, ServerFields.Request.Requester, conditions.Requester);
            AppendCondition(request, ServerFields.Request.StateCode.Column, conditions.StateCodes);
            AppendCondition(request, ServerFields.Request.ReqFromDt, conditions.ReqFromDt);
            AppendCondition(request, ServerFields.Request.ReqToDt, conditions.ReqToDt);
            AppendCondition(request, ServerFields.Request.AcceptFromDt, conditions.AcceptFromDt);
            AppendCondition(request, ServerFields.Request.AcceptToDt, conditions.AcceptToDt);

            return this.Request(request.ToString()).Table;
        }

        private static void AppendCondition(StringBuilder request, string key, string value)
        {
            if (value.Length > 0)
            {
                request.Append(" " + key + "=").Append(value);
            }
        }

        private DataActionResult SetRequestState(string reqSerialNo, string stateCode, string status)
        {
            StringBuilder request = new StringBuilder();
            request.Append("SetRequestState");
            request.Append(" " + ServerFields.Request.ReqSerialNo + "=").Append(reqSerialNo);
            request.Append(" " + ServerFields.Request.StateCode.Column + "=").Append(stateCode);
            request.Append(" " + ServerFields.Request.Status + "=").Append(status);
            request.Append(" " + ServerFields.UserId + "=").Append(this.CurrentUserId);

            return this.Request(request.ToString());
        }

        private DataTable RequestDetails(string reqSerialNo)
        {
            StringBuilder request = new StringBuilder();
            request.Append("GetRequestInfo");
            request.Append(" " + ServerFields.Request.ReqSerialNo + "=").Append(reqSerialNo);

            return this.Request(request.ToString()).Table;
        }
    }
}
