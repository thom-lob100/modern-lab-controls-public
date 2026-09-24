namespace Modern.Lab.MasterData
{
    public partial class CommonCodeForm
    {
        private void DefineEditors()
        {
            // 키는 CRUD 정의가 편집기와 목록에 넘긴다. 코드 화면의 COMMON_TYP은 선택한 대분류라 신규에서도 잠근다.
            this.CrudEditor.DefineReadOnly("UPDATED_BY", "UPDATED_AT");
            this.CrudEditor.DefineToggle("USE_YN");
            this.CrudEditor.DefineNumber("SORT_NO", 0);
            this.CrudEditor.DefineMultiline("DESCRIPTION");
            // 신규 입력 기본값이다. 기존 조회 값은 덮지 않는다. 코드 화면의 COMMON_TYP은 선택한 대분류를 폼이 채운다.
            this.CrudEditor.DefineDefaultValue("SORT_NO", "0");
            this.CrudEditor.DefineDefaultValue("USE_YN", "Y");
            if (!this.manageTypes)
            {
                this.CrudEditor.DefineReadOnly("COMMON_TYP");
            }
        }
    }
}
