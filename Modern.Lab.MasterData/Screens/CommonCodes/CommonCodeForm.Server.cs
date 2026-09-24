using System;
using System.Collections.Generic;
using System.Data;

using Modern.Lab.Hosting.Messaging;
using Modern.Lab.Hosting.ResponseContracts;

namespace Modern.Lab.MasterData
{
    public partial class CommonCodeForm
    {
        // 아래 전문명은 홈 데모다. 회사 전문명과 필드 매핑은 이 파일에서 교체한다.
        private const string ActionName = "CommonCodeAction";

        // 키: 대분류는 COMMON_TYP 하나, 코드는 COMMON_TYP + TYP_VAL(부모 + 코드) 복합 키다.
        private const string TypeKeyColumn = "COMMON_TYP";
        private static readonly string[] CodeKeyColumns = { "COMMON_TYP", "TYP_VAL" };

        // 공통 필수: COMMON_TYP, DESCRIPTION, SORT_NO, USE_YN, UPDATED_BY, UPDATED_AT
        // 추가 필수: 대분류는 COMMON_NM, 코드는 TYP_VAL과 TYP_NM. 빈 결과도 같은 컬럼을 반환한다.
        private DataTable RequestItems(bool isDetail, string commonTyp, string keyword)
        {
            return this.RequestFields(ActionName, "MethodCommand",
                    isDetail ? "SelectCommonCodes" : "SelectCommonTypes", "CommonTyp", commonTyp, "Keyword", keyword).Table;
        }

        private DataActionResult WriteItem(string method, string commonTyp, string typVal, object[] fields)
        {
            List<object> request = new List<object> { "MethodCommand", method, "CommonTyp", commonTyp };
            if (typVal != null)
            {
                request.Add("TypVal");
                request.Add(typVal);
            }
            for (int i = 0; i < fields.Length; i += 2)
            {
                string name = Convert.ToString(fields[i]);
                if (name == "CommonTyp" || name == "TypVal" || name == "UpdatedBy" || name == "UpdatedAt")
                {
                    continue;
                }
                request.Add(name);
                request.Add(fields[i + 1]);
            }
            return this.RequestFields(ActionName, request.ToArray());
        }

        // 등록·수정 전문의 키(CommonTyp·TypVal)는 편집기 값에서 꺼낸다. 신규 키만 앞뒤 공백을 지우고, 수정은 조회한 행의 원값 그대로 보낸다.
        private static string KeyText(object[] fields, string name, bool isNew)
        {
            for (int i = 0; i + 1 < fields.Length; i += 2)
            {
                if (Convert.ToString(fields[i]) == name)
                {
                    string value = Convert.ToString(fields[i + 1]);
                    return isNew ? value.Trim() : value;
                }
            }
            return string.Empty;
        }
    }
}
