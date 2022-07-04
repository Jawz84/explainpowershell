using System;
using System.Linq;
using System.Management.Automation.Language;
using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            string moduleName = string.Empty;
            string cmdName = commandAst.GetCommandName() ?? string.Empty;

            if (cmdName.IndexOf('\\') != -1)
            {
                var s = cmdName.Split('\\');
                moduleName = s[0];
                cmdName = s[1];
            }

            string resolvedCmd = Helpers.ResolveAlias(cmdName) ?? cmdName;

            HelpEntity helpResult;
            if (string.IsNullOrEmpty(moduleName))
            {
                var helpResults = HelpTableQueryRange(resolvedCmd);
                helpResult = helpResults?.FirstOrDefault();
                if (helpResults.Count > 1)
                {
                    this.errorMessage = $"The command '{helpResult?.CommandName}' is present in more than one module: '{string.Join("', '", helpResults.Select(r => r.ModuleName))}'. Explicitly prepend the module name to the command to select one: '{helpResults.First().ModuleName}\\{helpResult?.CommandName}'";
                }
            }
            else
            {
                helpResult = HelpTableQuery(resolvedCmd, moduleName);
                if (string.IsNullOrEmpty(helpResult?.ModuleName))
                {
                    helpResult = new()
                    {
                        ModuleName = moduleName
                    };
                }
            }

            var description = helpResult?.Synopsis?.ToString();

            if (string.IsNullOrEmpty(description))
            {
                // Try to find out if this may be a cmdlet
                if (resolvedCmd.IndexOf('-') > 0)
                {
                    var approvedVerbs = GetApprovedVerbs();
                    var possibleVerb = resolvedCmd[..resolvedCmd.IndexOf('-')];

                    if (approvedVerbs.Any(v => v.IndexOf(possibleVerb, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        description = "Unrecognized cmdlet. Try finding the module that contains this cmdlet and add it to my database. See issue #43 on GitHub.";
                    }
                    else
                    {
                        description = "Unrecognized command.";
                    }
                }
                else
                {
                    description = "Unrecognized command.";
                }
            }

            resolvedCmd = helpResult?.CommandName ?? resolvedCmd;
            if (! string.IsNullOrEmpty(helpResult?.CommandName)) {
                ExpandAliasesInExtent(commandAst, resolvedCmd);
            }

            if (commandAst.InvocationOperator != TokenKind.Unknown)
            {
                var (tokenDescription, _) = Helpers.TokenExplainer(commandAst.InvocationOperator);
                string invocationOperatorExplanation = commandAst.InvocationOperator == TokenKind.Dot ?
                    "The dot source invocation operator '.'" :
                    tokenDescription;

                description = invocationOperatorExplanation + " " + description;
            }

            explanations.Add(
                new Explanation()
                {
                    CommandName = resolvedCmd,
                    Description = description,
                    HelpResult = helpResult,
                    TextToHighlight = cmdName
                }.AddDefaults(commandAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst)
        {
            var exp = new Explanation()
            {
                CommandName = "Parameter",
                TextToHighlight = commandParameterAst.ParameterName,
            }.AddDefaults(commandParameterAst, explanations);

            var parentCommandExplanation = explanations.FirstOrDefault(e => e.Id == exp.ParentId);

            ParameterData matchedParameter;
            if (parentCommandExplanation.HelpResult?.Parameters != null)
            {
                try
                {
                    matchedParameter = Helpers.MatchParam(commandParameterAst.ParameterName, parentCommandExplanation.HelpResult?.Parameters);

                    if (matchedParameter != null)
                    {

                        if (!string.Equals(commandParameterAst.ParameterName, matchedParameter.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            exp.CommandName += $" '-{matchedParameter.Name}'";
                        }

                        if (matchedParameter.SwitchParameter ?? false)
                        {
                            exp.CommandName = "Switch " + exp.CommandName;
                        }

                        if (string.Equals(matchedParameter.Required, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            exp.CommandName = "Mandatory " + exp.CommandName;
                        }

                        if (!(matchedParameter.SwitchParameter ?? false) && !string.IsNullOrEmpty(matchedParameter.TypeName))
                        {
                            exp.CommandName += $" of type [{matchedParameter.TypeName}]";
                        }

                        if (string.Equals(matchedParameter.Globbing, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            exp.CommandName += " (supports wildcards like '*' and '?')";
                        }

                        exp.Description = matchedParameter.Description;

                        if (!string.IsNullOrEmpty(
                            parentCommandExplanation
                                .HelpResult?
                                .ParameterSetNames))
                        {
                            var availableParamSets = parentCommandExplanation
                                .HelpResult?
                                .ParameterSetNames
                                .Split(", ")
                                .Append("__AllParameterSets")
                                .ToArray();

                            var paramSetData = Helpers.GetParameterSetData(matchedParameter, availableParamSets);

                            if (paramSetData.Count > 1)
                            {
                                exp.Description += $"\nThis parameter is present in more than one parameter set: {string.Join(", ", paramSetData.Select(p => p.ParameterSetName))}";
                            }
                            if (paramSetData.Count == 1)
                            {
                                var paramSetName = paramSetData.Select(p => p.ParameterSetName).FirstOrDefault();
                                if (paramSetName == "__AllParameterSets")
                                {
                                    if (availableParamSets.Length > 1)
                                    {
                                        exp.Description += $"\nThis parameter is present in all parameter sets.";
                                    }
                                }
                                else
                                {
                                    exp.Description += $"\nThis parameter is in parameter set: {paramSetName}";
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogWarning($"Failed to get Description for parameter '{commandParameterAst.ParameterName}' on command '{parentCommandExplanation.CommandName}': {e.Message}");
                }
            }

            explanations.Add(exp);
            return base.VisitCommandParameter(commandParameterAst);
        }
    }
}
