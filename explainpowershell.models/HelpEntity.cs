using Microsoft.Azure.Cosmos.Table;

namespace explainpowershell.models
{
    public class HelpEntity : TableEntity
    {
        public string Aliases { get; set; }
        public string CommandName { get; set; }
        public string DefaultParameterSet { get; set; }
        public string Description { get; set; }
        public string DocumentationLink { get; set; }
        public string InputTypes { get; set; }
        public string ModuleName { get; set; }
        public string ModuleProjectUri { get; set; }
        public string ModuleVersion { get; set; }
        public string Parameters { get; set; }
        public string ParameterSetNames { get; set; }
        public string RelatedLinks { get; set; }
        public string ReturnValues { get; set; }
        public string Synopsis { get; set; }
        public string Syntax { get; set; }
    }
}
