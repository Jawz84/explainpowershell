using System.Text.Json.Serialization;

namespace explainpowershell.models
{
    public class ParameterTypeData
    {
        [JsonPropertyName("value")]
        public string Value {get; set;}
        [JsonPropertyName("required")]
        public string Required {get; set;}
        [JsonPropertyName("variableLength")]
        public string VariableLength {get; set;}
    }
}
