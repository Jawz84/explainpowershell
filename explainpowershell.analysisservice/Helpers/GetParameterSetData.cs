using System.Collections.Generic;
using System.Text.Json;
using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public static partial class Helpers
    {
        public static List<ParameterSetData> GetParameterSetData(ParameterData paramData, string[] paramSetNames)
        {
            List<ParameterSetData> paramSetData = new List<ParameterSetData>();

            foreach (var paramSet in paramSetNames)
            {
                paramData.ParameterSets.TryGetProperty(paramSet, out JsonElement foundParamSet);

                if (foundParamSet.ValueKind == JsonValueKind.Undefined)
                    continue;

                paramSetData.Add(
                    new ParameterSetData()
                    {
                        ParameterSetName = paramSet,
                        HelpMessage = foundParamSet.GetProperty("HelpMessage").GetString(),
                        HelpMessageBaseName = foundParamSet.GetProperty("HelpMessageBaseName").GetString(),
                        HelpMessageResourceId = foundParamSet.GetProperty("HelpMessageResourceId").GetString(),
                        IsMandatory = foundParamSet.GetProperty("IsMandatory").GetBoolean(),
                        Position = foundParamSet.GetProperty("Position").GetInt32(),
                        ValueFromPipeline = foundParamSet.GetProperty("ValueFromPipeline").GetBoolean(),
                        ValueFromPipelineByPropertyName = foundParamSet.GetProperty("ValueFromPipelineByPropertyName").GetBoolean(),
                        ValueFromRemainingArguments = foundParamSet.GetProperty("ValueFromRemainingArguments").GetBoolean()
                    });
            }

            return paramSetData;
        }
    }
}