using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using explainpowershell.models;

namespace explainpowershell.frontend.Clients;

public sealed class SyntaxAnalyzerClient : ISyntaxAnalyzerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient http;

    public SyntaxAnalyzerClient(HttpClient http)
    {
        this.http = http;
    }

    public async Task<ApiCallResult<AnalysisResult>> AnalyzeAsync(Code code, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("SyntaxAnalyzer", code, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ApiCallResult<AnalysisResult>.Failure(reason, response.StatusCode);
            }

            var analysisResult = await response.Content.ReadFromJsonAsync<AnalysisResult>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (analysisResult is null)
            {
                return ApiCallResult<AnalysisResult>.Failure("Empty response from SyntaxAnalyzer.", response.StatusCode);
            }

            return ApiCallResult<AnalysisResult>.Success(analysisResult, response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ApiCallResult<AnalysisResult>.Failure($"Request failed: {ex.Message}", HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<ApiCallResult<AiExplanationResponse>> GetAiExplanationAsync(
        Code code,
        AnalysisResult analysisResult,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var aiRequest = new
            {
                PowershellCode = code.PowershellCode,
                AnalysisResult = analysisResult
            };

            using var response = await http.PostAsJsonAsync("AiExplanation", aiRequest, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return ApiCallResult<AiExplanationResponse>.Failure(null, response.StatusCode);
            }

            var aiResponse = await response.Content.ReadFromJsonAsync<AiExplanationResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (aiResponse is null)
            {
                return ApiCallResult<AiExplanationResponse>.Success(new AiExplanationResponse(), response.StatusCode);
            }

            return ApiCallResult<AiExplanationResponse>.Success(aiResponse, response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // AI is optional; treat failures as empty response.
            return ApiCallResult<AiExplanationResponse>.Success(new AiExplanationResponse(), HttpStatusCode.OK);
        }
    }
}
