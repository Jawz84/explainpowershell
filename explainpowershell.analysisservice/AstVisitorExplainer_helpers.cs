using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
        private const char filterChar = '!';
        private const char separatorChar = ' ';
        private const string PartitionKey = "CommandHelp";
        private readonly List<Explanation> explanations = new();
        private string errorMessage;
        private string extent;
        private int offSet = 0;
        private readonly TableClient tableClient;
        private readonly ILogger log;

        public AnalysisResult GetAnalysisResult()
        {
            var modules = new List<Module>();

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

        public AstVisitorExplainer(string extentText, TableClient client, ILogger log)
        {
            tableClient = client;
            this.log = log;
            extent = extentText;
        }

        private static bool HasSpecialVars(string varName)
        {
            if (SpecialVars.InitializedVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private HelpEntity HelpTableQuery(string resolvedCmd)
        {
            string filter = TableServiceClient.CreateQueryFilter($"PartitionKey eq {PartitionKey} and RowKey eq {resolvedCmd.ToLower()}");
            var entities = tableClient.Query<HelpEntity>(filter: filter);
            var helpResult = entities.FirstOrDefault();
            return helpResult;
        }

        private HelpEntity HelpTableQuery(string resolvedCmd, string moduleName)
        {
            var rowKey = $"{resolvedCmd.ToLower()}{separatorChar}{moduleName.ToLower()}";
            return HelpTableQuery(rowKey);
        }

        private List<HelpEntity> HelpTableQueryRange(string resolvedCmd)
        {
            if (string.IsNullOrEmpty(resolvedCmd))
            {
                return new List<HelpEntity> { new HelpEntity() };
            }

            // Getting a range from Azure Table storage works based on ascii char filtering. You can match prefixes. I use a space ' ' (char)32 as a divider 
            // between the name of a command and the name of its module for commands that appear in more than one module. Filtering this way makes sure I 
            // only match entries with '<myCommandName> <myModuleName>'.
            // filterChar = (char)33 = '!'.
            string rowKeyFilter = $"{resolvedCmd.ToLower()}{filterChar}";
            string filter = TableServiceClient.CreateQueryFilter(
                $"PartitionKey eq {PartitionKey} and RowKey ge {resolvedCmd.ToLower()} and RowKey lt {rowKeyFilter}");
            var entities = tableClient.Query<HelpEntity>(filter: filter);
            return entities.ToList();
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

            log.LogWarning($"Unhandled ast: {splitAstType}");
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
