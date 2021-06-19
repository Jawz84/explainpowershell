using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using explainpowershell.models;

namespace explainpowershell.SyntaxAnalyzer.ExtensionMethods
{
    public static class SyntaxAnalyzerExtensions 
    {
        public static Explanation AddDefaults(this Explanation explanation, Ast ast, List<Explanation> explanations)
        {
            explanation.OriginalExtent = ast.Extent.Text;
            explanation.Id = ast.GenerateId();
            explanation.ParentId = TryFindParentExplanation(ast, explanations);
            return explanation;
        }

        private static string GenerateId(this Ast ast)
        {
            return ast.Extent.StartLineNumber + ast.Extent.StartColumnNumber + ast.Extent.EndColumnNumber + ast.GetType().Name;
        }

        public static string TryFindParentExplanation(Ast ast, List<Explanation> explanations, int level = 0)
        {
            if (explanations.Count == 0 | ast.Parent == null)
                return null;

            var parentId = ast.Parent.GenerateId();

            if ((!explanations.Any(e => e.Id == parentId)) & level < 100)
            {
                return TryFindParentExplanation(ast.Parent, explanations, ++level);
            }

            if (level >= 99)
                return null;

            return parentId;
        }
    }
}