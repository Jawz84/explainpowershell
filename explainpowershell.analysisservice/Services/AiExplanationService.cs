using System.ClientModel;
using System.Text.Json;
using explainpowershell.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace explainpowershell.analysisservice.Services
{
    public sealed class AiExplanationService : IAiExplanationService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly ILogger<AiExplanationService> logger;
        private readonly AiExplanationOptions options;
        private readonly ChatClient? chatClient;

        public AiExplanationService(
            IOptions<AiExplanationOptions> options,
            ILogger<AiExplanationService> logger,
            ChatClient? chatClient)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.chatClient = chatClient;
        }

        public async Task<(string? explanation, string? modelName)> GenerateAsync(string powershellCode, AnalysisResult analysisResult, CancellationToken cancellationToken = default)
        {
            if (chatClient == null)
            {
                logger.LogDebug("AI explanation requested but ChatClient is not available.");
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(powershellCode) || analysisResult == null)
            {
                logger.LogWarning("AI explanation skipped due to missing PowerShell code or analysis result.");
                return (null, null);
            }

            try
            {
                logger.LogInformation(
                    "Generating AI explanation for code length {CodeLength} with {ExplanationCount} explanation nodes.",
                    powershellCode.Length,
                    analysisResult.Explanations?.Count ?? 0);

                var slimResult = CreateSlimAnalysisResult(analysisResult);
                var payload = JsonSerializer.Serialize(new AiExplanationPayload
                {
                    PowershellCode = powershellCode,
                    ExplanationInfo = slimResult
                }, SerializerOptions);
                
                // If payload is too large, progressively reduce content
                if (payload.Length > options.MaxPayloadCharacters)
                {
                    logger.LogWarning(
                        "AI payload is too large ({PayloadSize} chars exceeds limit of {MaxSize}). Attempting to reduce content.",
                        payload.Length,
                        options.MaxPayloadCharacters);
                    
                    slimResult = ReducePayloadSize(slimResult, options.MaxPayloadCharacters);
                    payload = JsonSerializer.Serialize(new AiExplanationPayload
                    {
                        PowershellCode = powershellCode,
                        ExplanationInfo = slimResult
                    }, SerializerOptions);
                    
                    logger.LogInformation(
                        "Reduced AI payload to {PayloadSize} chars by limiting explanation details.",
                        payload.Length);
                }
                else
                {
                    logger.LogDebug("AI payload prepared with {PayloadLength} characters.", payload.Length);
                }

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(options.SystemPrompt ?? AiExplanationOptions.DefaultSystemPrompt),
                    new UserChatMessage(options.ExamplePrompt ?? AiExplanationOptions.DefaultExamplePrompt),
                    new AssistantChatMessage(options.ExampleResponse ?? AiExplanationOptions.DefaultExampleResponse),
                    new UserChatMessage($"Explain this in just one sentence:\n\n```json\n{payload}\n```")
                };

                // Create cancellation token with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var completionResult = await chatClient!.CompleteChatAsync(messages, cancellationToken: linkedCts.Token)
                    .ConfigureAwait(false);

                var completion = completionResult.Value;
                var bestEffortText = completion?
                    .Content?
                    .Select(part => part.Text)
                    .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
                    ?.Trim();

                var actualModelName = completion?.Model ?? options.DeploymentName;

                if (string.IsNullOrWhiteSpace(bestEffortText))
                {
                    logger.LogWarning("AI explanation call succeeded but returned no content.");
                }
                else
                {
                    logger.LogInformation(
                        "AI explanation generated ({ResponseLength} chars) using model {Model}.",
                        bestEffortText.Length,
                        actualModelName);
                }

                return (bestEffortText, actualModelName);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("AI explanation request was cancelled.");
                return (null, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to generate AI explanation. Exception: {ExceptionType}", ex.GetType().Name);
                return (null, null);
            }
        }

        private sealed class AiExplanationPayload
        {
            public string PowershellCode { get; set; } = string.Empty;
            public AnalysisResult ExplanationInfo { get; set; } = default!;
        }

        private static AnalysisResult CreateSlimAnalysisResult(AnalysisResult source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new AnalysisResult
            {
                ExpandedCode = source.ExpandedCode,
                ParseErrorMessage = source.ParseErrorMessage,
                AiExplanation = source.AiExplanation,
                DetectedModules = source.DetectedModules,
                Explanations = source.Explanations?
                    .Select(CloneExplanationWithoutParameters)
                    .ToList() ?? new List<Explanation>()
            };
        }

        private static AnalysisResult ReducePayloadSize(AnalysisResult result, int targetSize)
        {
            // Progressive reduction strategy:
            // 1. Remove verbose fields from help results
            // 2. Limit number of explanations
            // 3. Remove help results entirely if needed
            
            // Try removing help descriptions and verbose fields
            var reduced = new AnalysisResult
            {
                ExpandedCode = result.ExpandedCode,
                ParseErrorMessage = result.ParseErrorMessage,
                DetectedModules = result.DetectedModules,
                Explanations = result.Explanations?
                    .Select(e => new Explanation
                    {
                        Id = e.Id,
                        ParentId = e.ParentId,
                        OriginalExtent = e.OriginalExtent,
                        CommandName = e.CommandName,
                        Description = e.Description,
                        HelpResult = e.HelpResult == null ? null : new HelpEntity
                        {
                            CommandName = e.HelpResult.CommandName,
                            Synopsis = e.HelpResult.Synopsis,
                            ModuleName = e.HelpResult.ModuleName
                            // Removed: Description, Syntax, RelatedLinks, etc.
                        },
                        TextToHighlight = e.TextToHighlight
                    })
                    .ToList()
            };

            if (GetPayloadSize(reduced) <= targetSize)
                return reduced;

            // Limit explanations to first 20 if still too large
            if (reduced.Explanations?.Count > 20)
            {
                reduced.Explanations = reduced.Explanations.Take(20).ToList();
                if (GetPayloadSize(reduced) <= targetSize)
                    return reduced;
            }

            // Last resort: remove help results entirely, keep only basic explanations
            reduced.Explanations = result.Explanations?
                .Select(e => new Explanation
                {
                    Id = e.Id,
                    ParentId = e.ParentId,
                    OriginalExtent = e.OriginalExtent,
                    CommandName = e.CommandName,
                    Description = e.Description,
                    HelpResult = null,
                    TextToHighlight = e.TextToHighlight
                })
                .Take(30)
                .ToList();

            return reduced;
        }

        private static int GetPayloadSize(AnalysisResult result)
        {
            var payload = JsonSerializer.Serialize(new AiExplanationPayload
            {
                PowershellCode = string.Empty,
                ExplanationInfo = result
            }, SerializerOptions);
            return payload.Length;
        }

        private static Explanation CloneExplanationWithoutParameters(Explanation source)
        {
            if (source == null)
            {
                return new Explanation();
            }

            return new Explanation
            {
                OriginalExtent = source.OriginalExtent,
                CommandName = source.CommandName,
                Description = source.Description,
                HelpResult = CloneHelpEntityWithoutParameters(source.HelpResult) ?? new HelpEntity(),
                Id = source.Id,
                ParentId = source.ParentId,
                TextToHighlight = source.TextToHighlight
            };
        }

        private static HelpEntity? CloneHelpEntityWithoutParameters(HelpEntity? source)
        {
            if (source == null)
            {
                return null;
            }

            return new HelpEntity
            {
                Aliases = source.Aliases,
                CommandName = source.CommandName,
                DefaultParameterSet = source.DefaultParameterSet,
                Description = source.Description,
                DocumentationLink = source.DocumentationLink,
                InputTypes = source.InputTypes,
                ModuleName = source.ModuleName,
                ModuleProjectUri = source.ModuleProjectUri,
                ModuleVersion = source.ModuleVersion,
                ParameterSetNames = source.ParameterSetNames,
                RelatedLinks = source.RelatedLinks,
                ReturnValues = source.ReturnValues,
                Synopsis = source.Synopsis,
                Syntax = source.Syntax
            };
        }
    }
}
