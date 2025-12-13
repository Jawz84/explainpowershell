using System;
using Azure;
using Azure.Data.Tables;

namespace explainpowershell.models
{
    public class HelpMetaData : ITableEntity
    {
        public int NumberOfCommands { get; set; }
        public int NumberOfAboutArticles { get; set; }
        public int NumberOfModules { get; set; }
        public string? ModuleNames { get; set; }
        public string? LastPublished {get; set;}

        // ITableEntity
        public required string PartitionKey { get; set; }
        public required string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
