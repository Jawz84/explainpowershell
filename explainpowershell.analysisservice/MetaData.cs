using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Azure.Cosmos.Table;
using explainpowershell.models;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace explainpowershell.analysisservice
{
    public static class MetaData
    {
        private const string HelpTableName = "HelpData";

        [FunctionName("MetaData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table(HelpTableName)] CloudTable cloudTable,
            ILogger log)
        {
            TableQuery<HelpEntity> query = new TableQuery<HelpEntity>()
                .Select(new string[] { "CommandName", "ModuleName" });

            var tableQueryResult = await cloudTable.ExecuteQuerySegmentedAsync(query, null);

            var numAbout = tableQueryResult
                .Where(r => r
                    .CommandName
                    .StartsWith("about_", StringComparison.OrdinalIgnoreCase))
                .Count();

            var moduleNames = tableQueryResult
                .Select(r => r.ModuleName)
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct();

            var helpMetaData = new HelpMetaData()
            {
                NumberOfAboutArticles = numAbout,
                NumberOfCommands = tableQueryResult.Count() - numAbout,
                NumberOfModules = moduleNames.Count(),
                ModuleNames = moduleNames,
                LastPublished = Helpers.GetBuildDate(Assembly.GetExecutingAssembly()).ToLongDateString()
            };

            var json = JsonSerializer.Serialize(helpMetaData);

            return new OkObjectResult(json);
        }
    }
}
