using System.Data;

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class UserForm
    {
        private const string ActionName = "UserAction";
        private const string UserIdColumn = "USER_ID";

        // 홈 예제 전문이다. 회사의 전문 이름·키는 이 파일에서 변경한다.
        // 응답 필수: USER_ID. 나머지 컬럼은 응답 스키마대로 표시한다.
        private DataTable RequestUsers(string keyword)
        {
            return this.RequestFields(ActionName, "MethodCommand", "SelectUsers", "Keyword", keyword).Table;
        }

        private DataActionResult InsertUser(object[] fields)
        {
            return this.RequestFields(ActionName, WithMethodCommand("InsertUser", fields));
        }

        private DataActionResult UpdateUser(object[] fields)
        {
            return this.RequestFields(ActionName, WithMethodCommand("UpdateUser", fields));
        }

        private DataActionResult DeleteUser(string userId)
        {
            return this.RequestFields(
                    ActionName, "MethodCommand", "DeleteUser", this.CrudEditor.ParameterNameOf(UserIdColumn), userId);
        }
    }
}
