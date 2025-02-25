using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using explainpowershell.models;
using explainpowershell.helpcollector.tools;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public static partial class Helpers
    {
        public static ParameterData? MatchParam(string foundParameter, string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            List<ParameterData>? doc;
            List<ParameterData> matchedParam = new();
            
            try {
                doc = JsonSerializer.Deserialize<List<ParameterData>>(json, new JsonSerializerOptions() {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});
            }
            catch {
                try {
                    json = DeCompress.Decompress(json);
                    doc = JsonSerializer.Deserialize<List<ParameterData>>(json, new JsonSerializerOptions() {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});
                }
                catch {
                    return null;
                }
            }

            if (doc == null || string.IsNullOrEmpty(foundParameter)) {
                return null;
            }

            // First check for aliases, because they take precedence
            if (!string.Equals(foundParameter, "none", StringComparison.OrdinalIgnoreCase))
            {
                matchedParam = doc.Where(p => 
                    p?.Aliases != null && 
                    p.Aliases.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                        .Any(q => q.StartsWith(foundParameter, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList();
            }

            if (matchedParam.Count == 0)
            {
                // If no aliases match, then try partial parameter names for static params (aliases and static params take precedence)
                matchedParam = doc.Where(p => 
                    p?.Name != null && 
                    !(p.IsDynamic ?? false) && 
                    p.Name.StartsWith(foundParameter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (matchedParam.Count == 0)
            {
                // If no aliases or static params match, then try partial parameter names for dynamic params too.
                matchedParam = doc.Where(p => 
                    p?.Name != null && 
                    p.Name.StartsWith(foundParameter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (matchedParam.Count == 0)
            {
                return null;
            }

            if (matchedParam.Count > 1)
            {
                throw new ArgumentException($"Ambiguous parameter: {foundParameter}");
            }

            return matchedParam.FirstOrDefault();
        }
    }
}