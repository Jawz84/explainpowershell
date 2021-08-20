using System.Text.Json;

namespace explainpowershell.models
{
    public class ParameterData
    {
        public string Aliases { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
        public string Globbing { get; set; }
        public bool IsDynamic { get; set; }
        public string Name { get; set; }
        public string PipelineInput { get; set; }
        public string Position { get; set; }
        public string Required { get; set; }
        public bool SwitchParameter { get; set; }
        public ParameterTypeData TypeName { get; set; }
        public JsonElement ParameterSets { get; set; }
    }
}
