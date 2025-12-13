using System.Threading;
using System.Threading.Tasks;
using explainpowershell.models;

namespace explainpowershell.frontend.Clients;

public interface ISyntaxAnalyzerClient
{
    Task<ApiCallResult<AnalysisResult>> AnalyzeAsync(Code code, CancellationToken cancellationToken = default);

    Task<ApiCallResult<AiExplanationResponse>> GetAiExplanationAsync(
        Code code,
        AnalysisResult analysisResult,
        CancellationToken cancellationToken = default);
}
