using System.Net;
using System.Text;
using System.Text.Json;
using explainpowershell.analysisservice.Services;
using explainpowershell.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public sealed class AiExplanationFunction
    {
        private readonly ILogger<AiExplanationFunction> logger;
        private readonly IAiExplanationService aiExplanationService;

        public AiExplanationFunction(ILogger<AiExplanationFunction> logger, IAiExplanationService aiExplanationService)
        {
            this.logger = logger;
            this.aiExplanationService = aiExplanationService;
        }

        [Function("AiExplanation")]
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
                return CreateResponse(req, HttpStatusCode.BadRequest, "Empty request. Pass an AnalysisResult with PowerShell code in the request body.");
            }

            AiExplanationRequest? request;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                request = JsonSerializer.Deserialize<AiExplanationRequest>(requestBody, options);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to deserialize AI explanation request");
                return CreateResponse(req, HttpStatusCode.BadRequest, "Invalid request format.");
            }

            if (request == null || string.IsNullOrEmpty(request.PowershellCode) || request.AnalysisResult == null)
            {
                return CreateResponse(req, HttpStatusCode.BadRequest, "Request must contain PowershellCode and AnalysisResult.");
            }

            logger.LogInformation("AI explanation requested for code: {Code}", request.PowershellCode);

            (string? aiExplanation, string? modelName) result;
            try
            {
                result = await aiExplanationService.GenerateAsync(request.PowershellCode, request.AnalysisResult).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while generating AI explanation");
                return CreateResponse(req, HttpStatusCode.InternalServerError, "Failed to generate AI explanation.");
            }

            var response = new AiExplanationResponse
            {
                AiExplanation = result.aiExplanation ?? string.Empty,
                ModelName = result.modelName
            };

            var json = JsonSerializer.Serialize(response);
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

    public class AiExplanationRequest
    {
        public string PowershellCode { get; set; } = string.Empty;
        public AnalysisResult AnalysisResult { get; set; } = new();
    }

    public class AiExplanationResponse
    {
        public string AiExplanation { get; set; } = string.Empty;
        public string? ModelName { get; set; }
    }
}
