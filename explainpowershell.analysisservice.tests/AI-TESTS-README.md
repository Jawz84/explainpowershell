# AI Explanation Tests

This directory contains comprehensive tests for the AI explanation feature.

## Test Structure

### Unit Tests (C# / NUnit)

Located in `tests/` directory:

#### `AiExplanationService.tests.cs`
Tests for the `AiExplanationService` class focusing on:
- ✅ Constructor validation (null checks)
- ✅ Null/empty input handling
- ✅ Cancellation token support
- ✅ Configuration options validation
- ✅ Default prompts and examples
- ✅ Payload size handling
- ✅ Service behavior without ChatClient

**Key Test Cases:**
```csharp
Constructor_WithNullOptions_ThrowsArgumentNullException()
GenerateAsync_WithNullChatClient_ReturnsNullTuple()
GenerateAsync_WithCancellationToken_HandlesOperationCanceled()
DefaultSystemPrompt_IsNotEmpty()
Options_DefaultValues_AreValid()
```

#### `AiExplanationFunction.integration.tests.cs`
Integration tests for the Azure Function endpoint (marked with `[Ignore]` - run manually):
- ✅ HTTP endpoint availability
- ✅ Request/response validation
- ✅ Large payload handling
- ✅ Model name in response
- ✅ Real AI integration (requires credentials)

**Note:** These tests require a running Function App and are ignored by default.

### Integration Tests (Pester / PowerShell)

#### `Invoke-AiExplanation.Tests.ps1`
End-to-end integration tests covering:
- ✅ AI endpoint functionality
- ✅ Configuration changes (enabled/disabled)
- ✅ Valid and invalid request handling
- ✅ Large payload processing (50KB+)
- ✅ Model name in response
- ✅ Error handling and graceful degradation
- ✅ Complete workflow integration with SyntaxAnalyzer

**Key Test Contexts:**
- `AiExplanation Function Endpoint` - Basic endpoint behavior
- `Configuration Validation` - Settings and options
- `Error Handling` - Malformed requests and edge cases
- `End-to-End Workflow` - Full analysis + AI explanation flow

## Running Tests

### All Tests
```powershell
cd explainpowershell.analysisservice.tests
.\Start-AllBackendTests.ps1 -Output Detailed
```

### Unit Tests Only (C#)
```powershell
cd explainpowershell.analysisservice.tests
.\Start-AllBackendTests.ps1 -SkipIntegrationTests -Output Detailed
```

Or using dotnet CLI:
```powershell
dotnet test --verbosity normal
```

### Integration Tests Only (Pester)
```powershell
cd explainpowershell.analysisservice.tests
.\Start-AllBackendTests.ps1 -SkipUnitTests -Output Detailed
```

Or using Pester directly:
```powershell
Invoke-Pester -Path .\Invoke-AiExplanation.Tests.ps1 -Output Detailed
```

### Manual Integration Tests (with Function App)

For tests marked `[Ignore]`, ensure the Function App is running first:

```powershell
# Terminal 1: Start Function App
cd explainpowershell.analysisservice
func host start

# Terminal 2: Run integration tests
cd explainpowershell.analysisservice.tests
dotnet test --filter "Category=Integration"
```

## Test Configuration

### Prerequisites

**For Integration Tests:**
- ✅ Azurite running (storage emulator)
- ✅ Function App running on `http://localhost:7071`
- ✅ Help data populated in Azurite table storage

**For AI Tests with Real Credentials:**
- ✅ Valid Azure OpenAI endpoint
- ✅ API key configured
- ✅ Model deployment available

### Environment Variables

Tests respect these environment variables:

```powershell
$env:AiExplanation__Enabled = "true"
$env:AiExplanation__Endpoint = "https://your-resource.openai.azure.com"
$env:AiExplanation__DeploymentName = "gpt-4o-mini"
$env:AiExplanation__ApiKey = "your-api-key"
$env:AiExplanation__SystemPrompt = "Custom prompt"
$env:AiExplanation__MaxPayloadCharacters = "50000"
$env:AiExplanation__RequestTimeoutSeconds = "30"
```

**Note:** The Pester tests save and restore original configuration automatically.

## Test Coverage

### What's Tested

✅ **Service Logic:**
- Null safety and input validation
- Configuration handling
- Payload size reduction strategy
- Error handling and graceful degradation
- Cancellation token support

✅ **HTTP Endpoint:**
- Request/response serialization
- Status codes
- Error responses
- Large payload handling

✅ **Configuration:**
- Default values
- Environment variable overrides
- Runtime configuration changes

✅ **Integration:**
- End-to-end workflow
- Interaction with SyntaxAnalyzer
- Multi-step processes

### What's NOT Tested

❌ **Actual AI API calls** - Tests don't call real OpenAI APIs (would be expensive and slow)
❌ **ChatClient implementation** - Microsoft's SDK is assumed correct
❌ **Network failures** - No network failure simulation tests yet

## Adding New Tests

### Unit Test (C#)

1. Add test method to `AiExplanationService.tests.cs`
2. Follow NUnit conventions:
   ```csharp
   [Test]
   public void MethodName_Scenario_ExpectedBehavior()
   {
       // Arrange
       // Act
       // Assert
   }
   ```

### Integration Test (Pester)

1. Add test to `Invoke-AiExplanation.Tests.ps1`
2. Use existing contexts or create new:
   ```powershell
   It "Should handle scenario" {
       # Arrange
       # Act
       # Assert with Should assertions
   }
   ```

### Manual Integration Test

1. Add to `AiExplanationFunction.integration.tests.cs`
2. Mark with `[Ignore("Requires running Function App")]`
3. Add `[Category("Integration")]` attribute

## Continuous Integration

The GitHub Actions workflow runs:
- ✅ All unit tests (C#)
- ✅ All Pester integration tests (with Azurite)
- ❌ Manual integration tests are skipped (marked with `[Ignore]`)

## Troubleshooting

### "Function App not running" errors
```powershell
cd ../explainpowershell.analysisservice
func host start
```

### "Azurite not available" errors
```powershell
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

### Tests timing out
Increase timeout in test configuration:
```powershell
$env:AiExplanation__RequestTimeoutSeconds = "60"
```

### AI tests always return empty
Check AI configuration:
```powershell
$env:AiExplanation__Enabled = "true"
# Verify endpoint, key, and deployment name are set
```

## Test Metrics

Current test counts:
- **Unit Tests (C#):** 15 tests in `AiExplanationService.tests.cs`
- **Integration Tests (C#):** 6 tests in `AiExplanationFunction.integration.tests.cs` (manual)
- **Integration Tests (Pester):** 15+ tests in `Invoke-AiExplanation.Tests.ps1`

**Total:** 35+ AI-specific tests
