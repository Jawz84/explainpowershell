using System.Collections.Generic;

namespace explainpowershell.models
{
    public class AnalysisResult
    {
        public string ExpandedCode { get; set; }
        public List<Explanation> Explanations { get; set; } = new List<Explanation>();
        public List<Module> DetectedModules { get; set; } = new List<Module>();
        public string ParseErrorMessage { get; set; }
    }
}
