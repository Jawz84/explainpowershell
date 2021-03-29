using System;

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
}
