using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        private const string HelpTableName = "HelpData";

        [FunctionName("SyntaxAnalyzer")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table(HelpTableName)] CloudTable cloudTable,
            ILogger log)
        {
            AnalysisResult analysisResult;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return ResponseHelper(HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");
            }

            var code = JsonConvert
                .DeserializeObject<Code>(requestBody)
                ?.PowershellCode;

            log.LogInformation("PowerShell code sent: " + code); // LogAnalytics does not log the body of requests, so we have to log this ourselves.

            ScriptBlockAst ast = Parser.ParseInput(code, out _, out ParseError[] parseErrors);

            if (string.IsNullOrEmpty(ast.Extent.Text))
                return ResponseHelper(HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");

            try
            {
                var visitor = new AstVisitorExplainer(ast.Extent.Text, cloudTable, log);
                ast.Visit(visitor);
                analysisResult = visitor.GetAnalysisResult();
            }
            catch (Exception e)
            {
                log.LogError(e, "error");
                return ResponseHelper(HttpStatusCode.InternalServerError, "Oops, someting went wrong internally. Please file an issue with the PowerShell code you submitted when this occurred.");
            }

            analysisResult.ParseErrorMessage = parseErrors?.FirstOrDefault()?.Message;

            var json = System.Text.Json.JsonSerializer.Serialize(analysisResult);

            return ResponseHelper(HttpStatusCode.OK, json, "application/json");
        }

        private HttpResponseMessage ResponseHelper(HttpStatusCode status, string message, string mediaType = "text/plain")
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(message, Encoding.UTF8, mediaType)
            };
        }
    }
}
