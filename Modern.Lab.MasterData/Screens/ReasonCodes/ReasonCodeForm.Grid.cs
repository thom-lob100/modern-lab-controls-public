namespace Modern.Lab.MasterData
{
    public partial class ReasonCodeForm
    {
        private void DefineEditors()
        {
            // 세 키는 CRUD 정의(KeyColumns)가 편집기에 넘긴다. 신규는 입력하고 기존 행은 읽기 전용으로 표시한다.
            // 홈 예제용 선택 컬럼이며 회사 응답에 없어도 조회·등록을 막지 않는다.
            this.propertyGrid.DefineToggle("USE_YN");
        }
    }
}
