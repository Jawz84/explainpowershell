using System;
using Azure.Data.Tables;

namespace explainpowershell.analysisservice
{
    internal static class TableClientFactory
    {
        private const string StorageConnectionSetting = "AzureWebJobsStorage";

        public static TableClient Create(string tableName)
        {
            var connectionString = Environment.GetEnvironmentVariable(StorageConnectionSetting);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Configuration value '{StorageConnectionSetting}' is not set.");
            }

            var serviceClient = new TableServiceClient(connectionString);
            return serviceClient.GetTableClient(tableName);
        }
    }
}
