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
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.Azure.Cosmos.Table;

using explainpowershell.models;

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
            ScriptBlock sb;
            List<Explanation> explanations;
            var AnalysisResult = new AnalysisResult();
            var modules = new List<Module>();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var code = JsonConvert
                .DeserializeObject<Code>(requestBody)
                ?.PowershellCode;

            try
            {
                sb = ScriptBlock.Create(code);
            }
            catch (ParseException e)
            {
                return ResponseHelper(HttpStatusCode.UnprocessableEntity, e.Errors[0].Message);
            }
            catch (Exception e)
            {
                return ResponseHelper(HttpStatusCode.InternalServerError, e.Message);
            }

            var ast = sb.Ast;
            extent = ast.Extent.ToString();

            if (string.IsNullOrEmpty(extent))
                return ResponseHelper(HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");

            var foundPipelineAsts = ast.FindAll(ast => ast is PipelineAst, true);

            try
            {
                explanations = GetExplanations(cloudTable, foundPipelineAsts);
            }
            catch (Exception e)
            {
                return ResponseHelper(HttpStatusCode.InternalServerError, e.Message);
            }

            foreach (var exp in explanations)
            {
                if (exp.HelpResult == null)
                    continue;

                if (!modules.Any(m => m.ModuleName == exp.HelpResult.ModuleName))
                {
                    modules.Add(
                        new Module()
                        {
                            ModuleName = exp.HelpResult.ModuleName
                        });
                }
            }

            AnalysisResult.ExpandedCode = extent;
            AnalysisResult.Explanations = explanations;
            AnalysisResult.DetectedModules = modules;

            var json = System.Text.Json.JsonSerializer.Serialize(AnalysisResult);

            return ResponseHelper(HttpStatusCode.OK, json, "application/json");
        }

        private List<Explanation> GetExplanations(CloudTable cloudTable, IEnumerable<Ast> foundPipelineAsts)
        {
            var explanations = new List<Explanation>();

            foreach (PipelineAst pipeline in foundPipelineAsts)
            {
                foreach (CommandBaseAst element in pipeline.PipelineElements)
                {
                    if (element is CommandAst)
                    {
                        var cmd = element as CommandAst;
                        string cmdName = cmd.GetCommandName();
                        string resolvedCmd = ResolveCmd(cmdName);
                        if (string.IsNullOrEmpty(resolvedCmd))
                        {
                            resolvedCmd = cmdName;
                        }

                        ExpandAliasesInExtent(cmd, resolvedCmd);

                        TableQuery<HelpEntity> query = new TableQuery<HelpEntity>().Where(
                            TableQuery.CombineFilters(
                                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                                TableOperators.And,
                                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, resolvedCmd.ToLower()))); // Azure Table query does not support StringComparer.IgnoreOrdinalCase. RowKey command names are all stored lowercase.

                        var helpResult = cloudTable.ExecuteQuery(query).FirstOrDefault();

                        var synopsis = helpResult?.Synopsis?.ToString() ?? "";

                        explanations.Add(
                            new Explanation()
                            {
                                OriginalExtent = cmd.Extent.Text,
                                CommandName = helpResult.CommandName ?? resolvedCmd,
                                Synopsis = synopsis,
                                HelpResult = helpResult
                            });
                    }
                    else
                    {
                        var cmdExp = element as Ast;

                        explanations.Add(
                            new Explanation()
                            {
                                OriginalExtent = cmdExp.Extent.Text,
                                CommandName = element.GetType().Name.Replace("Ast","")
                            });
                    }
                }
            }

            return explanations;
        }

        private HttpResponseMessage ResponseHelper(HttpStatusCode status, string message, string mediaType = "text/plain")
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(message, Encoding.UTF8, mediaType)
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
