using System.Data;

namespace Modern.Lab.Samples
{
    public partial class LotHoldDialogForm
    {
        // 홈 예제 분류다. 회사 조회 전문과 분류 매핑은 이 파일에서 교체한다.
        // 시설은 로그인 CurrentFacId를 사용한다. 응답 필수: REASON_CD, REASON_NM.
        private DataTable RequestReasonCodes(bool releaseMode)
        {
            return this.RequestFields("GetLotHoldReasonList", "FacId", this.CurrentFacId,
                    "ReasonCatgCd", releaseMode ? "RELEASE" : "HOLD").Table;
        }

        // Keyword는 입력한 사번 또는 성명이다. 응답 필수: USER_ID, USER_NM.
        private DataTable RequestEngineers(string keyword)
        {
            return this.RequestFields("UserAction", "MethodCommand", "SelectUsers", "Keyword", keyword).Table;
        }
    }
}
