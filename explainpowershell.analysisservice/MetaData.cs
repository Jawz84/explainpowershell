using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using explainpowershell.models;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Azure.Data.Tables;

namespace explainpowershell.analysisservice
{
    public static class MetaData
    {
        private const string HelpTableName = "HelpData";

        [FunctionName("MetaData")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table(HelpTableName)] TableClient client,
            ILogger log)
        {
            HelpMetaData helpMetaData;
            var refresh = req.Query["refresh"].ToString();

            if (refresh == "true")
            {
                helpMetaData = CalculateMetaData(client, log);
            }
            else
            {
                log.LogInformation("Trying to get HelpMetaData from cache");
                string filter = TableServiceClient.CreateQueryFilter($"PartitionKey eq 'HelpMetaData'");
                var entities = client.Query<HelpMetaData>(filter);

                helpMetaData = entities.FirstOrDefault();

                if ( helpMetaData == null )
                {
                    helpMetaData = CalculateMetaData(client, log);
                }
            }

            var json = JsonSerializer.Serialize(helpMetaData);

            return new OkObjectResult(json);
        }

        public static HelpMetaData CalculateMetaData(TableClient client, ILogger log)
        {
            log.LogInformation("Calculating meta data on HelpTable");

            string filter = TableServiceClient.CreateQueryFilter($"PartitionKey eq 'CommandHelp'");
            var select = new string[] { "CommandName", "ModuleName" };
            var entities = client.Query<HelpEntity>(filter: filter, select: select);

            var numAbout = entities
                .Where(r => r
                    .CommandName
                    .StartsWith("about_", StringComparison.OrdinalIgnoreCase))
                .Count();

            var moduleNames = entities
                .Select(r => r.ModuleName)
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct();

            var helpMetaData = new HelpMetaData()
            {
                PartitionKey = "HelpMetaData",
                RowKey = "HelpMetaData",
                NumberOfAboutArticles = numAbout,
                NumberOfCommands = entities.Count() - numAbout,
                NumberOfModules = moduleNames.Count(),
                ModuleNames = string.Join(',', moduleNames),
                LastPublished = Helpers.GetBuildDate(Assembly.GetExecutingAssembly()).ToLongDateString()
            };

            _ = client.UpsertEntity(helpMetaData);

            return helpMetaData;
        }
    }
}
