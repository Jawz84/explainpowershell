using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using explainpowershell.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace explainpowershell.analysisservice
{
    public class MetaData
    {
        private const string HelpTableName = "HelpData";
        private const string MetaDataPartitionKey = "HelpMetaData";
        private const string MetaDataRowKey = "HelpMetaData";
        private const string CommandHelpPartitionKey = "CommandHelp";

        [Function("MetaData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [TableInput(HelpTableName)] TableClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger<MetaData>();
            HelpMetaData helpMetaData;

            var refresh = req.Query["refresh"]?.ToString() ?? "false";
            if (refresh == "true")
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(helpMetaData);
            return response;
        }

        private static HelpMetaData CalculateMetaData(TableClient client, ILogger logger)
        {
            logger.LogInformation("Calculating meta data on HelpTable");

            string filter = TableServiceClient.CreateQueryFilter($"PartitionKey eq {CommandHelpPartitionKey}");
            var select = new string[] { "CommandName", "ModuleName" };
            var entities = client.Query<HelpEntity>(filter: filter, select: select);

            var numAbout = entities
                .Where(r => r
                    .CommandName
                    .StartsWith("about_", StringComparison.OrdinalIgnoreCase))
                .Count();

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
    }
}
