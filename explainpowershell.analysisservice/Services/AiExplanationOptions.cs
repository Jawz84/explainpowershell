namespace explainpowershell.analysisservice.Services
{
    public sealed class AiExplanationOptions
    {
        public const string SectionName = "AiExplanation";
        public const string DefaultSystemPrompt = "You are a PowerShell expert that explains PowerShell oneliners to end users in the shortest form possible. You will be given a oneliner, and metadata that contains command documentation. Keep the answer short and stick to what is present in the oneliner. Answer in one sentence only.";
        public const string DefaultExamplePrompt = """
            Explain this in just one sentence:
                ```json
                {"powershellCode":"gps","explanationInfo":{"expandedCode":"Get-Process","explanations":[{"originalExtent":"gps","commandName":"Get-Process","description":"Gets the processes that are running on the local computer."}]}}
                ```
            """;
        public const string DefaultExampleResponse = "Returns information about all running processes on the local computer, like name, ID, and resource usage.";

        public bool Enabled { get; set; } = true;
        public string? Endpoint { get; set; }
        public string? DeploymentName { get; set; }
        public string? ApiKey { get; set; }
        public string? SystemPrompt { get; set; }
        public string? ExamplePrompt { get; set; }
        public string? ExampleResponse { get; set; }
        public int MaxPayloadCharacters { get; set; } = 50000;
        public int RequestTimeoutSeconds { get; set; } = 30;
    }
}
