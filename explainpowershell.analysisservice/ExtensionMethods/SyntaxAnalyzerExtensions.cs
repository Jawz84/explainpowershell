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

        public static Explanation AddDefaults(this Explanation explanation, Token token, List<Explanation> explanations)
        {
            explanation.OriginalExtent = token.Extent.Text;
            explanation.Id = token.GenerateId();
            explanation.ParentId = TryFindParentExplanation(token, explanations);
            return explanation;
        }
        private static string GenerateId(this Ast ast)
        {
            return $"{ast.Extent.StartLineNumber}.{ast.Extent.StartOffset}.{ast.Extent.EndOffset}.{ast.GetType().Name}";
        }

        private static string GenerateId(this Token token)
        {
            return $"{token.Extent.StartLineNumber}.{token.Extent.StartOffset}.{token.Extent.EndOffset}.{token.Kind}";
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
                return string.Empty;

            return parentId;
        }

        public static string TryFindParentExplanation(Token token, List<Explanation> explanations)
        {
            var start = token.Extent.StartOffset;
            var explanationsBeforeToken = explanations.Where(e => GetEndOffSet(e) <= start);

            if (!explanationsBeforeToken.Any())
            {
                return string.Empty;
            }

            var closestNeigbour = explanationsBeforeToken.Max(e => GetEndOffSet(e));
            return explanationsBeforeToken.FirstOrDefault(t => GetEndOffSet(t) == closestNeigbour).Id;
        }

        private static int GetEndOffSet(Explanation e)
        {
            return int.Parse(e.Id.Split('.')[2]);
        }
    }
}