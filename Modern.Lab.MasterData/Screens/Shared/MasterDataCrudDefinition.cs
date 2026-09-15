namespace Modern.Lab.MasterData
{
    public sealed class MasterDataCrudDefinition
    {
        public string EntityName { get; set; }

        public string KeyColumn { get; set; }

        public string ActionName { get; set; }

        public string ListTableId { get; set; }

        public string SelectCommand { get; set; }

        public string InsertCommand { get; set; }

        public string UpdateCommand { get; set; }

        public string DeleteCommand { get; set; }

        public string[] RequiredEditorColumns { get; set; }
    }
}
