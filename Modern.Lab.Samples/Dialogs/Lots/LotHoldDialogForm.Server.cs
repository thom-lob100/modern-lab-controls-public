using System.Data;

namespace Modern.Lab.Samples
{
    public partial class LotHoldDialogForm
    {
        // 홈 예제 분류다. 회사 조회 전문과 분류 매핑은 이 파일에서 교체한다.
        // 시설은 로그인 CurrentFacId를 사용한다. 응답 필수: REASON_CD, CTN_DESC.
        private DataTable RequestReasonCodes(bool releaseMode)
        {
            return this.RequestFields("GetLotHoldReasonList", "FacId", this.CurrentFacId,
                    "ReasonCatgCd", releaseMode ? "RELEASE" : "HOLD").Table;
        }

        // 창을 열 때 전체 사용자를 한 번 조회한다. 회사 전문이 검색어 없이 전체를 주지 않으면 이 파일에서 교체한다.
        // 응답 필수: USER_ID, USER_NM.
        private DataTable RequestEngineers()
        {
            return this.RequestFields("UserAction", "MethodCommand", "SelectUsers").Table;
        }
    }
}
