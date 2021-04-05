using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.Azure.Cosmos.Table;

using explainpowershell.models;
using System.Linq;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        private const string HelpTableName = "HelpData";
        private const string PartitionKey = "CommandHelp";
        private string extent;
        private int offSet = 0;

        private static readonly PowerShell powerShell = PowerShell.Create();
        private Dictionary<String, String> AliasToCmdletDictionary;

        public CommandInvocationIntrinsics InvokeCommand { get => powerShell.Runspace.SessionStateProxy.InvokeCommand; }

        [FunctionName("SyntaxAnalyzer")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table(HelpTableName)] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Code data = JsonConvert.DeserializeObject<Code>(requestBody);
            string code = data?.PowershellCode;

            ScriptBlock sb = ScriptBlock.Create(code);
            var ast = sb.Ast;

            extent = ast.Extent.ToString();

            if (string.IsNullOrEmpty(extent))
            {
                log.LogError("That didn't go as planned");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("This HTTP triggered function executed successfully, but there was no powershell code passed to it. Pass code in the request body for an AST analysis.",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            IEnumerable<Ast> foundCommandAsts = ast.FindAll(ast => ast is CommandAst, true);
            var explanations = new List<Explanation>();

            foreach (CommandAst cmd in foundCommandAsts)
            {
                try 
                {
                    string resolvedCmd = ResolveCmd(cmd.GetCommandName());
                    if (string.IsNullOrEmpty(resolvedCmd))
                    {
                        continue;
                    }
                    log.LogInformation(resolvedCmd);

                    ExpandAliasesInExtent(cmd, resolvedCmd);

                    TableQuery<HelpEntity> query = new TableQuery<HelpEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, resolvedCmd)));

                    var helpResult = cloudTable.ExecuteQuery(query).FirstOrDefault();

                    var synopsis = helpResult?.Synopsis?.ToString() ?? "";

                    log.LogInformation(synopsis);

                    explanations.Add(
                        new Explanation()
                        {
                            OriginalExtent = cmd.Extent.Text,
                            CommandName = resolvedCmd,
                            Synopsis = synopsis
                        });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }

            log.LogInformation("expanded: '{extent}'", extent);

            var json = JsonConvert.SerializeObject(explanations, Formatting.Indented);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        }

        private void ExpandAliasesInExtent(CommandAst cmd, string resolvedCmd)
        {
            int start = offSet + cmd.Extent.StartOffset;
            int length = offSet + cmd.CommandElements[0].Extent.EndOffset - start;
            extent = extent
                .Remove(start, length)
                .Insert(start, resolvedCmd);

            offSet = offSet + resolvedCmd.Length - length;
        }

        private string ResolveCmd(string cmdName)
        {
            if (AliasToCmdletDictionary == null)
            {
                InitializeAliasDictionary();
            }

            if (AliasToCmdletDictionary.ContainsKey(cmdName))
            {
                return AliasToCmdletDictionary[cmdName];
            }

            return InvokeCommand.GetCommandName(cmdName, false, false).FirstOrDefault();
        }

        public void InitializeAliasDictionary()
        {
            AliasToCmdletDictionary = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<CommandInfo> aliases = InvokeCommand.GetCommands("*", CommandTypes.Alias, true);

            foreach (AliasInfo aliasInfo in aliases)
            {
                AliasToCmdletDictionary.Add(aliasInfo.Name, aliasInfo.Definition);
            }
        }
    }
}
