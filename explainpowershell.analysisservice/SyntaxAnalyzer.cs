using System.Management.Automation.Language;
using System.Net;
using System.Text;
using explainpowershell.analysisservice;
using explainpowershell.models;
using ExplainPowershell.SyntaxAnalyzer.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public sealed class SyntaxAnalyzerFunction
    {
        private readonly ILogger<SyntaxAnalyzerFunction> logger;
        private readonly IHelpRepository helpRepository;

        public SyntaxAnalyzerFunction(ILogger<SyntaxAnalyzerFunction> logger, IHelpRepository helpRepository)
        {
            this.logger = logger;
            this.helpRepository = helpRepository;
        }

        [Function("SyntaxAnalyzer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                return CreateResponse(req, HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");
            }

            Code? request;
            try
            {
                request = JsonSerializer.Deserialize<Code>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to deserialize SyntaxAnalyzer request");
                return CreateResponse(req, HttpStatusCode.BadRequest, "Invalid request format. Pass powershell code in the request body for an AST analysis.");
            }

            var code = request?.PowershellCode ?? string.Empty;

            logger.LogInformation("PowerShell code sent: {Code}", code);

            ScriptBlockAst ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] parseErrors);

            if (string.IsNullOrEmpty(ast.Extent.Text))
            {
                return CreateResponse(req, HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");
            }

            AnalysisResult analysisResult;
            try
            {
                var visitor = new AstVisitorExplainer(ast.Extent.Text, helpRepository: helpRepository, logger, tokens);
                ast.Visit(visitor);
                analysisResult = visitor.GetAnalysisResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while analyzing the AST");
                return CreateResponse(req, HttpStatusCode.InternalServerError, "Oops, someting went wrong internally. Please file an issue with the PowerShell code you submitted when this occurred.");
            }

            analysisResult.ParseErrorMessage = string.IsNullOrEmpty(analysisResult.ParseErrorMessage)
                ? parseErrors?.FirstOrDefault()?.Message ?? string.Empty
                : (analysisResult.ParseErrorMessage + "\n" + parseErrors?.FirstOrDefault()?.Message) ?? string.Empty;

            analysisResult.AiExplanation = string.Empty;

            var json = System.Text.Json.JsonSerializer.Serialize(analysisResult);
            return CreateResponse(req, HttpStatusCode.OK, json, "application/json");
        }

        private static HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode status, string message, string mediaType = "text/plain")
        {
            var response = req.CreateResponse(status);
            response.Headers.Add("Content-Type", mediaType);
            response.WriteString(message, Encoding.UTF8);
            return response;
        }
    }
}
