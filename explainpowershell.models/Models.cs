using System;
using Microsoft.Azure.Cosmos.Table;

namespace explainpowershell.models
{
    public class Command
    {
        public string CommandName { get; set; }
    }

    public class Code
    {
        public string PowershellCode { get; set; }
    }

    public class Explanation
    {
        public string OriginalExtent { get;set; }
        public string CommandName { get;set; }
        public string Synopsis { get;set; }
    }

    public class HelpEntity : TableEntity {
        public string TimeStamp {get; set;}
        public string DocumentationLink {get; set;}
        public string Synopsis {get; set;}
        public string Syntax {get; set;}
        public string ModuleName {get; set;}
        public string CommandName {
            get {
                return this.RowKey;
            }
        }
    }
}
