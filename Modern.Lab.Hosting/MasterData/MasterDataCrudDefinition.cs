namespace Modern.Lab.Hosting.MasterData
{
    public sealed class MasterDataCrudDefinition
    {
        public string EntityName { get; set; }

        /// <summary>단일 키 컬럼. 복합 키 화면은 비워 두고 <see cref="KeyColumns"/>를 쓴다.</summary>
        public string KeyColumn { get; set; }

        /// <summary>복합 키 컬럼(순서 = 삭제 전문·재선택 비교 순서). <see cref="KeyColumn"/>과 둘 중 하나만 채운다 —
        /// 둘을 함께 채우면 CRUD 초기화가 예외를 낸다.</summary>
        public string[] KeyColumns { get; set; }

        public string ListTableId { get; set; }

        public string[] RequiredEditorColumns { get; set; }
    }
}
