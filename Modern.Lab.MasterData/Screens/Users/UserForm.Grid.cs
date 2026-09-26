namespace Modern.Lab.MasterData
{
    public partial class UserForm
    {
        private void DefineEditors()
        {
            // 추가 컬럼은 홈 예제용이다. 회사 응답에 존재하는 컬럼만 편집기에 나타난다.
            this.CrudEditor.DefineToggle("USE_YN");
        }
    }
}
