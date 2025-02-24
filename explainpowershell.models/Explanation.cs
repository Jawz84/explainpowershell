namespace explainpowershell.models
{
    public class Explanation
    {
        public string OriginalExtent { get; set; } = string.Empty;
        public string CommandName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HelpEntity? HelpResult { get; set; }
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string TextToHighlight { get; set; } = string.Empty;
    }
}
