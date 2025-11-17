using Azure;
using Azure.Data.Tables;
using explainpowershell.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace explainpowershell.analysisservice
{
    public sealed class MetaDataFunction
    {
        private const string HelpTableName = "HelpData";
        private const string MetaDataPartitionKey = "HelpMetaData";
        private const string MetaDataRowKey = "HelpMetaData";
        private const string CommandHelpPartitionKey = "CommandHelp";
        private readonly ILogger<MetaDataFunction> logger;

        public MetaDataFunction(ILogger<MetaDataFunction> logger)
        {
            this.logger = logger;
        }

        [Function("MetaData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var client = TableClientFactory.Create(HelpTableName);
            HelpMetaData helpMetaData;
            if (ShouldRefresh(req))
            {
                helpMetaData = CalculateMetaData(client, logger);
            }
            else
            {
                logger.LogInformation("Trying to get HelpMetaData from cache");
                try
                {
                    helpMetaData = client.GetEntity<HelpMetaData>(MetaDataPartitionKey, MetaDataRowKey);
                }
                catch (RequestFailedException)
                {
                    helpMetaData = CalculateMetaData(client, logger);
                }
            }

            var json = JsonSerializer.Serialize(helpMetaData);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(json).ConfigureAwait(false);
            return response;
        }

        public static HelpMetaData CalculateMetaData(TableClient client, ILogger log)
        {
            log.LogInformation("Calculating meta data on HelpTable");

            string filter = TableServiceClient.CreateQueryFilter($"PartitionKey eq {CommandHelpPartitionKey}");
            var select = new[] { "CommandName", "ModuleName" };
            var entities = client.Query<HelpEntity>(filter: filter, select: select).ToList();

            var numAbout = entities
                .Count(r => r.CommandName.StartsWith("about_", StringComparison.OrdinalIgnoreCase));

            var moduleNames = entities
                .Select(r => r.ModuleName)
                .Where(moduleName => !string.IsNullOrEmpty(moduleName))
                .Distinct();

            var helpMetaData = new HelpMetaData()
            {
                PartitionKey = MetaDataPartitionKey,
                RowKey = MetaDataRowKey,
                NumberOfAboutArticles = numAbout,
                NumberOfCommands = entities.Count() - numAbout,
                NumberOfModules = moduleNames.Count(),
                ModuleNames = string.Join(',', moduleNames),
                LastPublished = Helpers.GetBuildDate(Assembly.GetExecutingAssembly()).ToLongDateString()
            };

            try
            {
                _ = client.GetEntity<HelpMetaData>(MetaDataPartitionKey, MetaDataRowKey);
                _ = client.UpsertEntity(helpMetaData);
            }
            catch (RequestFailedException)
            {
                _ = client.AddEntity(helpMetaData);
            }

            return helpMetaData;
        }

        private static bool ShouldRefresh(HttpRequestData req)
        {
            var query = req.Url.Query;
            if (string.IsNullOrEmpty(query))
            {
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kvp = pair.Split('=', 2);
                if (kvp.Length == 0)
                {
                    continue;
                }

                if (!kvp[0].Equals("refresh", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = kvp.Length > 1 ? Uri.UnescapeDataString(kvp[1]) : string.Empty;
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
