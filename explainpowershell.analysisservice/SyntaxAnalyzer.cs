using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Data.Tables;
using explainpowershell.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Management.Automation.Language;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        private const string HelpTableName = "HelpData";

        [Function("SyntaxAnalyzer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [TableInput(HelpTableName)] TableClient tableClient,
            FunctionContext context)
        {
            var logger = context.GetLogger<SyntaxAnalyzer>();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return await ResponseHelper(req, HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");
            }

            var code = JsonSerializer.Deserialize<Code>(requestBody ?? string.Empty)?.PowershellCode ?? string.Empty;
            logger.LogInformation("PowerShell code sent: {Code}", code);

            ScriptBlockAst? ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] parseErrors);
            if (ast == null)
            {
                return await ResponseHelper(req, HttpStatusCode.BadRequest, "Unable to parse PowerShell code.");
            }
            
            var astText = ast.Extent?.Text ?? string.Empty;
            if (string.IsNullOrEmpty(astText))
            {
                return await ResponseHelper(req, HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");
            }

            AnalysisResult analysisResult;
            try
            {
                var visitor = new AstVisitorExplainer(astText, tableClient, logger, tokens);
                ast.Visit(visitor);
                analysisResult = visitor.GetAnalysisResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during AST analysis");
                return await ResponseHelper(req, HttpStatusCode.InternalServerError, "Oops, something went wrong internally. Please file an issue with the PowerShell code you submitted when this occurred.");
            }

            var parseErrorMessage = parseErrors?.FirstOrDefault()?.Message;
            analysisResult.ParseErrorMessage = string.IsNullOrEmpty(analysisResult.ParseErrorMessage)
                ? parseErrorMessage ?? string.Empty
                : $"{analysisResult.ParseErrorMessage}\n{parseErrorMessage ?? string.Empty}";

            var json = JsonSerializer.Serialize(analysisResult);
            return await ResponseHelper(req, HttpStatusCode.OK, json, "application/json");
        }

        private static async Task<HttpResponseData> ResponseHelper(HttpRequestData req, HttpStatusCode status, string message, string mediaType = "text/plain")
        {
            var response = req.CreateResponse(status);
            await response.WriteStringAsync(message, Encoding.UTF8);
            response.Headers.Add("Content-Type", mediaType);
            return response;
        }
    }
}
