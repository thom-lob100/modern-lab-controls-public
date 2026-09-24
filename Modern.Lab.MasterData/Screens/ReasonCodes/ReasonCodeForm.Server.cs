using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Hosting.Messaging;

namespace Modern.Lab.MasterData
{
    public partial class ReasonCodeForm
    {
        // 홈 예제 전문이다. 회사 전문명·필드 매핑은 이 파일에서 변경한다.
        private const string ActionName = "ReasonCodeAction";

        // 복합 키이자 응답 필수 컬럼이다. 순서는 아래 전문의 키 순서(FacId, ReasonCatgCd, ReasonCd)와 같다.
        private static readonly string[] KeyColumns = { "FAC_ID", "REASON_CATG_CD", "REASON_CD" };

        // 응답 필수: FAC_ID, REASON_CATG_CD, REASON_CD. 빈 결과도 같은 스키마를 반환한다.
        // 분류 값은 등록 데이터다. HOLD·RELEASE의 회사 저장값을 화면에서 제한하지 않는다.
        private DataTable RequestReasonCodes(string keyword)
        {
            return this.RequestFields(ActionName, "MethodCommand", "SelectReasonCodes", "Keyword", keyword).Table;
        }

        // 등록·수정 전문의 키는 편집기 값에서 꺼낸다. 신규 키만 앞뒤 공백을 지우고, 수정은 조회한 행의 원값 그대로 보낸다.
        private static string[] KeyOf(object[] fields, bool isNew)
        {
            string[] key = { string.Empty, string.Empty, string.Empty };
            for (int i = 0; i + 1 < fields.Length; i += 2)
            {
                string name = Convert.ToString(fields[i]);
                string value = Convert.ToString(fields[i + 1]);
                if (isNew) { value = value.Trim(); }
                if (name == "FacId") { key[0] = value; }
                else if (name == "ReasonCatgCd") { key[1] = value; }
                else if (name == "ReasonCd") { key[2] = value; }
            }
            return key;
        }

        private DataActionResult WriteReasonCode(string method, string[] key, object[] fields)
        {
            List<object> request = new List<object> { "MethodCommand", method,
                    "FacId", key[0], "ReasonCatgCd", key[1], "ReasonCd", key[2] };
            for (int i = 0; i < fields.Length; i += 2)
            {
                string name = Convert.ToString(fields[i]);
                if (name == "FacId" || name == "ReasonCatgCd" || name == "ReasonCd") { continue; }
                request.Add(name);
                request.Add(fields[i + 1]);
            }
            return this.RequestFields(ActionName, request.ToArray());
        }
    }
}
