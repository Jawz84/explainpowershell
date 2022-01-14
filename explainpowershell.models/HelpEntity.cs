using System;
using Azure;
using Azure.Data.Tables;

namespace explainpowershell.models
{
    public class HelpEntity : ITableEntity
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

        // ITableEntity
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
