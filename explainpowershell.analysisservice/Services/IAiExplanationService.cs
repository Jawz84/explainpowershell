using System.Threading;
using System.Threading.Tasks;
using explainpowershell.models;

namespace explainpowershell.analysisservice.Services
{
    public interface IAiExplanationService
    {
        Task<(string? explanation, string? modelName)> GenerateAsync(string powershellCode, AnalysisResult analysisResult, CancellationToken cancellationToken = default);
    }
}
