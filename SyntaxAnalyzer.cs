using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Collections.Generic;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        [FunctionName("SyntaxAnalyzer")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string code = data?.code;

            ScriptBlock sb = ScriptBlock.Create(code);
            var ast = sb.Ast;

            IEnumerable<Ast> foundCommandAsts = ast.FindAll(ast => ast is CommandAst, true);
            List<string> resolvedCmds = new List<string>();
            foreach (CommandAst cmd in foundCommandAsts)
            {

                string cmdName = cmd.GetCommandName();
                if (cmdName == null)
                {
                    continue;
                }

                log.LogInformation(cmdName);

                string resolvedCmd = ResolveCmd(cmdName);

                resolvedCmds.Add(resolvedCmd);

                log.LogInformation(resolvedCmd);
            }

            string ext = ast.Extent.ToString();
            string responseMessage;
            if (string.IsNullOrEmpty(ext))
            {
                responseMessage = "This HTTP triggered function executed successfully. Pass code in the request body for an AST analysis.";
            }
            else
            {
                responseMessage = $"Found '{ext}'. This HTTP triggered function executed successfully. Resolved Command Names: ";
                foreach (var cmd in resolvedCmds)
                {
                    responseMessage += cmd + ", ";
                }
            }
            return new OkObjectResult(responseMessage);
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
