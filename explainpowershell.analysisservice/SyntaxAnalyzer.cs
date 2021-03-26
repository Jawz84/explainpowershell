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

using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        [FunctionName("SyntaxAnalyzer")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Code data = JsonConvert.DeserializeObject<Code>(requestBody);
            string code = data?.PowershellCode;

            ScriptBlock sb = ScriptBlock.Create(code);
            var ast = sb.Ast;

            IEnumerable<Ast> foundCommandAsts = ast.FindAll(ast => ast is CommandAst, true);
            List<Command> resolvedCmds = new List<Command>();
            foreach (CommandAst cmd in foundCommandAsts)
            {

                string cmdName = cmd.GetCommandName();
                if (String.IsNullOrEmpty(cmdName))
                {
                    continue;
                }

                log.LogInformation(cmdName);

                string resolvedCmd = ResolveCmd(cmdName);

                resolvedCmds.Add(new Command() { CommandName = resolvedCmd });

                log.LogInformation(resolvedCmd);
            }

            string ext = ast.Extent.ToString();

            if (string.IsNullOrEmpty(ext))
            {
                log.LogError("That didn't go as planned");
                return new HttpResponseMessage(HttpStatusCode.BadRequest) 
                {
                    Content = new StringContent("This HTTP triggered function executed successfully, but there was no powershell code passed to it. Pass code in the request body for an AST analysis.",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            var json = JsonConvert.SerializeObject(resolvedCmds, Formatting.Indented);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        }

        private string ResolveCmd(string cmdName)
        {
            if (AliasToCmdletDictionary == null)
            {
                Initialize();
            }

            if (AliasToCmdletDictionary.ContainsKey(cmdName))
            {
                return AliasToCmdletDictionary[cmdName];
            }

            return String.Empty;
        }

        private CommandInvocationIntrinsics invokeCommand;
        private Dictionary<String, String> AliasToCmdletDictionary;

        public void Initialize()
        {
            var ps = PowerShell.Create();

            this.invokeCommand = ps.Runspace.SessionStateProxy.InvokeCommand;

            AliasToCmdletDictionary = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<CommandInfo> aliases = this.invokeCommand.GetCommands("*", CommandTypes.Alias, true);

            foreach (AliasInfo aliasInfo in aliases)
            {
                AliasToCmdletDictionary.Add(aliasInfo.Name, aliasInfo.Definition);
            }
        }
    }
}
