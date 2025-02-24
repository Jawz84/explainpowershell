using System.Collections.Generic;
using System.Text.Json;
using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public static partial class Helpers
    {
        public static List<ParameterSetData> GetParameterSetData(ParameterData paramData, string[] paramSetNames)
        {
            List<ParameterSetData> paramSetData = new();

            foreach (var paramSet in paramSetNames)
            {
                paramData.ParameterSets.TryGetProperty(paramSet, out JsonElement foundParamSet);

                if (foundParamSet.ValueKind == JsonValueKind.Undefined)
                    continue;

                var helpMessage = foundParamSet.GetProperty("HelpMessage").ValueKind != JsonValueKind.Null 
                    ? foundParamSet.GetProperty("HelpMessage").GetString() 
                    : null;
                var helpMessageBaseName = foundParamSet.GetProperty("HelpMessageBaseName").ValueKind != JsonValueKind.Null 
                    ? foundParamSet.GetProperty("HelpMessageBaseName").GetString() 
                    : null;
                var helpMessageResourceId = foundParamSet.GetProperty("HelpMessageResourceId").ValueKind != JsonValueKind.Null 
                    ? foundParamSet.GetProperty("HelpMessageResourceId").GetString() 
                    : null;

                paramSetData.Add(
                    new ParameterSetData()
                    {
                        ParameterSetName = paramSet,
                        HelpMessage = helpMessage,
                        HelpMessageBaseName = helpMessageBaseName,
                        HelpMessageResourceId = helpMessageResourceId,
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