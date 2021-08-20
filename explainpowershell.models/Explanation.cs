namespace explainpowershell.models
{
    public class Explanation
    {
        public string OriginalExtent { get; set; }
        public string CommandName { get; set; }
        public string Description { get; set; }
        public HelpEntity HelpResult { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string TextToHighlight { get; set; }
    }
}
