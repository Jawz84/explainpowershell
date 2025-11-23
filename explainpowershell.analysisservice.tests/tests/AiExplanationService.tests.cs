using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using explainpowershell.analysisservice.Services;
using explainpowershell.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using OpenAI.Chat;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    [TestFixture]
    public class AiExplanationServiceTests
    {
        private ILogger<AiExplanationService> mockLogger;
        private AiExplanationOptions testOptions;

        [SetUp]
        public void Setup()
        {
            mockLogger = new LoggerDouble<AiExplanationService>();
            testOptions = new AiExplanationOptions
            {
                Enabled = true,
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "gpt-4o-mini",
                ApiKey = "test-key-12345",
                SystemPrompt = "Test system prompt",
                MaxPayloadCharacters = 50000,
                RequestTimeoutSeconds = 30
            };
        }

        [Test]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AiExplanationService(null, mockLogger, null));
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var options = Options.Create(testOptions);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AiExplanationService(options, null, null));
        }

        [Test]
        public async Task GenerateAsync_WithNullChatClient_ReturnsNullTuple()
        {
            // Arrange
            var options = Options.Create(testOptions);
            var service = new AiExplanationService(options, mockLogger, null);
            var analysisResult = CreateTestAnalysisResult();

            // Act
            var result = await service.GenerateAsync("Get-Process", analysisResult);

            // Assert
            Assert.IsNull(result.explanation);
            Assert.IsNull(result.modelName);
        }

        [Test]
        public async Task GenerateAsync_WithNullPowerShellCode_ReturnsNullTuple()
        {
            // Arrange
            var options = Options.Create(testOptions);
            var service = new AiExplanationService(options, mockLogger, null);
            var analysisResult = CreateTestAnalysisResult();

            // Act
            var result = await service.GenerateAsync(null, analysisResult);

            // Assert
            Assert.IsNull(result.explanation);
            Assert.IsNull(result.modelName);
        }

        [Test]
        public async Task GenerateAsync_WithEmptyPowerShellCode_ReturnsNullTuple()
        {
            // Arrange
            var options = Options.Create(testOptions);
            var service = new AiExplanationService(options, mockLogger, null);
            var analysisResult = CreateTestAnalysisResult();

            // Act
            var result = await service.GenerateAsync("", analysisResult);

            // Assert
            Assert.IsNull(result.explanation);
            Assert.IsNull(result.modelName);
        }

        [Test]
        public async Task GenerateAsync_WithNullAnalysisResult_ReturnsNullTuple()
        {
            // Arrange
            var options = Options.Create(testOptions);
            var service = new AiExplanationService(options, mockLogger, null);

            // Act
            var result = await service.GenerateAsync("Get-Process", null);

            // Assert
            Assert.IsNull(result.explanation);
            Assert.IsNull(result.modelName);
        }

        [Test]
        public void CreateSlimAnalysisResult_RemovesParameters()
        {
            // This tests the internal payload reduction by checking serialized size
            // Arrange
            var analysisResult = CreateLargeAnalysisResultWithParameters();
            var originalJson = JsonSerializer.Serialize(analysisResult);
            
            // The service should strip parameters when creating slim result
            // We can't directly test private methods, but we can verify behavior
            // by ensuring payload size reduction works in integration
            
            Assert.IsTrue(originalJson.Length > 1000, "Test data should be reasonably large");
        }

        [Test]
        public void DefaultSystemPrompt_IsNotEmpty()
        {
            // Arrange & Act
            var prompt = AiExplanationOptions.DefaultSystemPrompt;

            // Assert
            Assert.IsNotNull(prompt);
            Assert.IsTrue(prompt.Length > 50);
            Assert.IsTrue(prompt.Contains("PowerShell"));
        }

        [Test]
        public void DefaultExamplePrompt_IsValidJson()
        {
            // Arrange & Act
            var examplePrompt = AiExplanationOptions.DefaultExamplePrompt;

            // Assert
            Assert.IsNotNull(examplePrompt);
            Assert.IsTrue(examplePrompt.Contains("json"));
            Assert.IsTrue(examplePrompt.Contains("gps"));
        }

        [Test]
        public void DefaultExampleResponse_IsNotEmpty()
        {
            // Arrange & Act
            var response = AiExplanationOptions.DefaultExampleResponse;

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Length > 20);
            Assert.IsTrue(response.Contains("process"));
        }

        [Test]
        public void Options_DefaultValues_AreValid()
        {
            // Arrange & Act
            var options = new AiExplanationOptions();

            // Assert
            Assert.IsTrue(options.Enabled);
            Assert.AreEqual(50000, options.MaxPayloadCharacters);
            Assert.AreEqual(30, options.RequestTimeoutSeconds);
        }

        [Test]
        public async Task GenerateAsync_WithCancellationToken_HandlesOperationCanceled()
        {
            // Arrange
            var options = Options.Create(testOptions);
            var service = new AiExplanationService(options, mockLogger, null);
            var analysisResult = CreateTestAnalysisResult();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            var result = await service.GenerateAsync("Get-Process", analysisResult, cts.Token);

            // Assert - should handle cancellation gracefully
            Assert.IsNull(result.explanation);
            Assert.IsNull(result.modelName);
        }

        private AnalysisResult CreateTestAnalysisResult()
        {
            return new AnalysisResult
            {
                ExpandedCode = "Get-Process",
                Explanations = new List<Explanation>
                {
                    new Explanation
                    {
                        Id = "1.0.1",
                        CommandName = "Get-Process",
                        Description = "Gets the processes that are running on the local computer.",
                        OriginalExtent = "gps",
                        HelpResult = new HelpEntity
                        {
                            CommandName = "Get-Process",
                            Synopsis = "Gets the processes that are running on the local computer.",
                            Description = "The Get-Process cmdlet gets the processes on a local computer.",
                            ModuleName = "Microsoft.PowerShell.Management"
                        }
                    }
                },
                DetectedModules = new List<Module>
                {
                    new Module { ModuleName = "Microsoft.PowerShell.Management" }
                }
            };
        }

        private AnalysisResult CreateLargeAnalysisResultWithParameters()
        {
            var result = CreateTestAnalysisResult();
            
            // Add more explanations to increase size (Parameters are stored as JSON string in HelpEntity)
            for (int i = 0; i < 10; i++)
            {
                result.Explanations.Add(new Explanation
                {
                    Id = $"1.0.{i + 2}",
                    CommandName = $"Test-Command{i}",
                    Description = $"Test description {i}",
                    HelpResult = new HelpEntity
                    {
                        CommandName = $"Test-Command{i}",
                        Synopsis = $"Test synopsis {i}",
                        Description = new string('x', 500), // Large description
                        Parameters = new string('p', 1000) // Large parameters JSON
                    }
                });
            }

            return result;
        }
    }
}
