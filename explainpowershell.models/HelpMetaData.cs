using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace explainpowershell.models
{
    public class HelpMetaData : TableEntity
    {
        public int NumberOfCommands { get; set; }
        public int NumberOfAboutArticles { get; set; }
        public int NumberOfModules { get; set; }
        public IEnumerable<string> ModuleNames { get; set; }
        public string LastPublished {get; set;}
    }
}
