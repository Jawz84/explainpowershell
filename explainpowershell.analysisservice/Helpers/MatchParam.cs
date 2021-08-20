
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public static partial class Helpers
    {
        public static ParameterData MatchParam(string foundParameter, string json)
        {
            var doc = JsonSerializer.Deserialize<List<ParameterData>>(json, new JsonSerializerOptions() {IgnoreNullValues = true});
            List<ParameterData> matchedParam = new List<ParameterData>();

            // First check for aliases, because they take precendence
            if (!string.Equals(foundParameter, "none", StringComparison.OrdinalIgnoreCase))
            {
                matchedParam = doc.Where(
                    p => p.Aliases.Split(", ").ToList().Contains(foundParameter, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            if (matchedParam.Count == 0)
            {
                // If no aliases match, then try partial parameter names for static params (aliases and static params take precedence)
                matchedParam = doc.Where(
                    p => ! (p.IsDynamic ?? false) && 
                        p.Name.StartsWith(foundParameter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (matchedParam.Count == 0)
            {
                // If no aliases or static params match, then try partial parameter names for dynamic params too.
                matchedParam = doc.Where(
                    p => p.Name.StartsWith(foundParameter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (matchedParam.Count == 0)
            {
                return null;
            }

            if (matchedParam.Count > 1)
            {
                throw new ArgumentException($"Abiguous parameter: {foundParameter}");
            }

            return matchedParam.FirstOrDefault();
        }
    }
}