using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using explainpowershell.models;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    [TestFixture]
    public class AiExplanationFunctionIntegrationTests
    {
        private const string FunctionBaseUrl = "http://localhost:7071";
        private HttpClient httpClient;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(FunctionBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient?.Dispose();
        }

        [Test]
        [Category("Integration")]
        [Ignore("Requires running Function App - run manually when testing")]
        public async Task AiExplanationEndpoint_WithValidRequest_ReturnsSuccessStatusCode()
        {
            // Arrange
            var request = new
            {
                PowershellCode = "Get-Process",
                AnalysisResult = new AnalysisResult
                {
                    ExpandedCode = "Get-Process",
                    Explanations = new List<Explanation>
                    {
                        new Explanation
                        {
                            Id = "1.0.1",
                            CommandName = "Get-Process",
                            Description = "Gets the processes that are running on the local computer."
                        }
                    }
                }
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/AiExplanation", request);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, 
                $"Expected success status code but got {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Length > 0);
        }

        [Test]
        [Category("Integration")]
        [Ignore("Requires running Function App - run manually when testing")]
        public async Task AiExplanationEndpoint_WithEmptyCode_ReturnsEmptyExplanation()
        {
            // Arrange
            var request = new
            {
                PowershellCode = "",
                AnalysisResult = new AnalysisResult
                {
                    ExpandedCode = "",
                    Explanations = new List<Explanation>()
                }
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/AiExplanation", request);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<AiExplanationResponse>();
            Assert.IsNotNull(result);
            Assert.IsTrue(string.IsNullOrEmpty(result.AiExplanation));
        }

        [Test]
        [Category("Integration")]
        [Ignore("Requires running Function App - run manually when testing")]
        public async Task AiExplanationEndpoint_WithComplexCode_HandlesLargePayload()
        {
            // Arrange
            var explanations = new Explanation[30];
            for (int i = 0; i < 30; i++)
            {
                explanations[i] = new Explanation
                {
                    Id = $"1.0.{i}",
                    CommandName = $"Test-Command{i}",
                    Description = new string('x', 500),
                    HelpResult = new HelpEntity
                    {
                        CommandName = $"Test-Command{i}",
                        Synopsis = new string('y', 200),
                        Description = new string('z', 1000)
                    }
                };
            }

            var request = new
            {
                PowershellCode = "Get-Process | Where-Object CPU -gt 100 | Sort-Object WorkingSet -Descending",
                AnalysisResult = new AnalysisResult
                {
                    ExpandedCode = "Get-Process | Where-Object CPU -gt 100 | Sort-Object WorkingSet -Descending",
                    Explanations = new List<Explanation>(explanations)
                }
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/AiExplanation", request);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode, 
                "Should handle large payloads with payload reduction");
            
            var result = await response.Content.ReadFromJsonAsync<AiExplanationResponse>();
            Assert.IsNotNull(result);
        }

        [Test]
        [Category("Integration")]
        [Ignore("Requires running Function App - run manually when testing")]
        public async Task AiExplanationEndpoint_ResponseContainsModelName()
        {
            // Arrange
            var request = new
            {
                PowershellCode = "gps",
                AnalysisResult = new AnalysisResult
                {
                    ExpandedCode = "Get-Process",
                    Explanations = new List<Explanation>
                    {
                        new Explanation
                        {
                            CommandName = "Get-Process",
                            Description = "Gets processes"
                        }
                    }
                }
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/AiExplanation", request);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<AiExplanationResponse>();
            Assert.IsNotNull(result);
            
            // ModelName property should exist (may be empty if AI disabled)
            Assert.IsNotNull(result.ModelName);
        }

        [Test]
        [Category("Integration")]
        [Ignore("Requires running Function App with AI configured - run manually when testing")]
        public async Task AiExplanationEndpoint_WithRealAI_ReturnsValidExplanation()
        {
            // This test requires actual AI configuration
            // Arrange
            var request = new
            {
                PowershellCode = "Get-Process | Select-Object Name, CPU",
                AnalysisResult = new AnalysisResult
                {
                    ExpandedCode = "Get-Process | Select-Object -Property Name, CPU",
                    Explanations = new List<Explanation>
                    {
                        new Explanation
                        {
                            Id = "1.0.1",
                            CommandName = "Get-Process",
                            Description = "Gets the processes that are running on the local computer.",
                            HelpResult = new HelpEntity
                            {
                                CommandName = "Get-Process",
                                Synopsis = "Gets the processes that are running on the local computer.",
                                ModuleName = "Microsoft.PowerShell.Management"
                            }
                        },
                        new Explanation
                        {
                            Id = "1.0.2",
                            CommandName = "Select-Object",
                            Description = "Selects objects or object properties.",
                            HelpResult = new HelpEntity
                            {
                                CommandName = "Select-Object",
                                Synopsis = "Selects objects or object properties.",
                                ModuleName = "Microsoft.PowerShell.Utility"
                            }
                        }
                    }
                }
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/AiExplanation", request);

            // Assert
            Assert.IsTrue(response.IsSuccessStatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<AiExplanationResponse>();
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AiExplanation), 
                "AI explanation should not be empty when AI is configured");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ModelName),
                "Model name should be returned when AI is configured");
            
            // Verify explanation looks reasonable
            Assert.IsTrue(result.AiExplanation.Length > 20,
                "AI explanation should be a reasonable length");
        }

        private class AiExplanationResponse
        {
            public string AiExplanation { get; set; }
            public string ModelName { get; set; }
        }
    }
}
