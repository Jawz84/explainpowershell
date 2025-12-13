using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;
using ExplainPowershell.SyntaxAnalyzer.Repositories;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
        private const char filterChar = Constants.TableStorage.RangeFilterChar;
        private const char separatorChar = Constants.TableStorage.CommandModuleSeparator;
        private const string PartitionKey = Constants.TableStorage.CommandHelpPartitionKey;
        private readonly List<Explanation> explanations = new();
        private string errorMessage = string.Empty;
        private string extent;
        private int offSet = 0;
        private readonly IHelpRepository helpRepository;
        private readonly ILogger log;
        private readonly Token[]? tokens;
        private readonly Dictionary<string, int> unhandledAstTypeCounts = new(StringComparer.OrdinalIgnoreCase);

        public AnalysisResult GetAnalysisResult()
        {
            var modules = new List<Module>();

            ExplainSemiColons();

            if (unhandledAstTypeCounts.Count > 0)
            {
                var totalUnhandled = unhandledAstTypeCounts.Values.Sum();
                var ordered = unhandledAstTypeCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key)
                    .ToList();

                const int maxTypesToLog = 10;
                var topTypes = string.Join(", ",
                    ordered
                        .Take(maxTypesToLog)
                        .Select(kvp => $"{kvp.Key}({kvp.Value})"));

                var extraTypes = ordered.Count > maxTypesToLog
                    ? $" (+{ordered.Count - maxTypesToLog} more types)"
                    : string.Empty;

                log.LogInformation(
                    "Unhandled AST nodes encountered: {UnhandledCount}. Types: {UnhandledTypes}{ExtraTypes}",
                    totalUnhandled,
                    topTypes,
                    extraTypes);
            }

            foreach (var exp in explanations)
            {
                if (exp.HelpResult == null)
                    continue;

                if (!modules.Any(m => m.ModuleName == exp.HelpResult.ModuleName))
                {
                    modules.Add(
                        new Module()
                        {
                            ModuleName = exp.HelpResult.ModuleName
                        });
                }
            }

            var analysisResult = new AnalysisResult()
            {
                Explanations = explanations,
                DetectedModules = modules,
                ExpandedCode = extent,
                ParseErrorMessage = errorMessage
            };

            return analysisResult;
        }

        private void ExplainSemiColons()
        {
            if (tokens == null)
            {
                return;
            }

            var semiColons = tokens.Where(t => t.Kind == TokenKind.Semi);
            foreach (var semiColon in semiColons)
            {
                var (description, _) = Helpers.TokenExplainer(TokenKind.Semi);
                var help = new HelpEntity
                {
                    DocumentationLink = Constants.Documentation.Chapter08PipelineStatements
                };

                explanations.Add(
                    new Explanation()
                    {
                        CommandName = "Statement terminator",
                        HelpResult = help,
                        Description = description,
                        TextToHighlight = ";"
                    }.AddDefaults(semiColon, explanations));
            }
        }

        public AstVisitorExplainer(string extentText, IHelpRepository helpRepository, ILogger log, Token[]? tokens)
        {
            this.helpRepository = helpRepository ?? throw new ArgumentNullException(nameof(helpRepository));
            this.log = log;
            extent = extentText;
            this.tokens = tokens;
        }

        private static bool HasSpecialVars(string varName)
        {
            if (SpecialVars.InitializedVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private HelpEntity? HelpTableQuery(string resolvedCmd)
        {
            return helpRepository.GetHelpForCommand(resolvedCmd);
        }

        private HelpEntity? HelpTableQuery(string resolvedCmd, string moduleName)
        {
            return helpRepository.GetHelpForCommand(resolvedCmd, moduleName);
        }

        private List<HelpEntity> HelpTableQueryRange(string resolvedCmd)
        {
            return helpRepository.GetHelpForCommandRange(resolvedCmd);
        }

        private void ExpandAliasesInExtent(CommandAst cmd, string resolvedCmd)
        {
            if (string.IsNullOrEmpty(resolvedCmd))
            {
                return;
            }

            int start = offSet + cmd.Extent.StartOffset;
            int length = offSet + cmd.CommandElements[0].Extent.EndOffset - start;
            extent = extent
                .Remove(start, length)
                .Insert(start, resolvedCmd);

            offSet = offSet + resolvedCmd.Length - length;
        }

        public static string SplitCamelCase(string input)
        {
            return Regex.Replace(input, @"([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        private void AstExplainer(Ast ast)
        {
            var astType = ast.GetType().Name.Replace("Ast", "");
            var splitAstType = SplitCamelCase(astType);
            explanations.Add(
                new Explanation()
                {
                    CommandName = splitAstType
                }.AddDefaults(ast, explanations));

            unhandledAstTypeCounts.TryGetValue(splitAstType, out var current);
            unhandledAstTypeCounts[splitAstType] = current + 1;
        }

        public static List<string> GetApprovedVerbs()
        {
            List<string> approvedVerbs = new();
            var verbTypes = new Type[] {
                    typeof(VerbsCommon), typeof(VerbsCommunications), typeof(VerbsData),
                    typeof(VerbsDiagnostic), typeof(VerbsLifecycle), typeof(VerbsOther), typeof(VerbsSecurity) };

            foreach (Type type in verbTypes)
            {
                // FieldInfo referenced explicitly, to prevent a using statement at the top from masking explainpowershell.models.Module by System.Reflection.Module.
                foreach (System.Reflection.FieldInfo field in type.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        approvedVerbs.Add(field.Name);
                    }
                }
            }

            return approvedVerbs;
        }
    }
}
