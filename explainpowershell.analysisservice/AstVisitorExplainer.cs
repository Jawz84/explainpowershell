using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;
using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

namespace ExplainPowershell.SyntaxAnalyzer
{
    class AstVisitorExplainer : AstVisitor2
    {
        private const string PartitionKey = "CommandHelp";
        private readonly List<Explanation> explanations = new List<Explanation>();
        private string extent;
        private int offSet = 0;
        private readonly CloudTable helpTable;
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
                ExpandedCode = extent
            };

            return analysisResult;
        }

        public AstVisitorExplainer(string extentText, CloudTable cloudTable, ILogger log)
        {
            helpTable = cloudTable;
            this.log = log;
            extent = extentText;
        }

        private bool HasSpecialVars(string varName)
        {
            if (SpecialVars.InitializedVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private HelpEntity HelpTableQuery(string resolvedCmd)
        {
            TableQuery<HelpEntity> query = new TableQuery<HelpEntity>()
                .Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, resolvedCmd.ToLower()))); // Azure Table query does not support StringComparer.IgnoreOrdinalCase. RowKey command names are all stored lowercase.

            var helpResult = helpTable.ExecuteQuery(query).FirstOrDefault();
            return helpResult;
        }

        private void ExpandAliasesInExtent(CommandAst cmd, string resolvedCmd)
        {
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

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            AstExplainer(arrayExpressionAst);
            return base.VisitArrayExpression(arrayExpressionAst);
        }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            AstExplainer(arrayLiteralAst);
            return base.VisitArrayLiteral(arrayLiteralAst);
        }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            var operatorExplanation = Helpers.TokenExplainer(assignmentStatementAst.Operator);
            explanations.Add(
                new Explanation()
                {
                    CommandName = $"Assignment operator '{assignmentStatementAst.Operator.Text()}'",
                    HelpResult = HelpTableQuery("about_assignment_operators"),
                    Description = $"{operatorExplanation} Assigns a value to '{assignmentStatementAst.Left.Extent.Text}'.",
                    TextToHighlight = assignmentStatementAst.Operator.Text()
                }.AddDefaults(assignmentStatementAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitAttribute(AttributeAst attributeAst)
        {
            if (attributeAst.TypeName.Name.ToLower() == "cmdletbinding")
            {
                explanations.Add(
                    new Explanation()
                    {
                        CommandName = "CmdletBinding Attribute",
                        HelpResult = HelpTableQuery("about_Functions_CmdletBindingAttribute"),
                        Description = "The CmdletBinding attribute adds common parameters to your script or function, among other things.",
                    }.AddDefaults(attributeAst, explanations));

                return AstVisitAction.Continue;
            }
            else
            {
                AstExplainer(attributeAst);
                return base.VisitAttribute(attributeAst);
            }
        }

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst)
        {
            AstExplainer(attributedExpressionAst);
            return base.VisitAttributedExpression(attributedExpressionAst);
        }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            explanations.Add(
                new Explanation()
                {
                    CommandName = $"Operator",
                    HelpResult = HelpTableQuery("about_operators"),
                    Description = Helpers.TokenExplainer(binaryExpressionAst.Operator),
                    TextToHighlight = binaryExpressionAst.Operator.Text()
                }.AddDefaults(binaryExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            AstExplainer(blockStatementAst);
            return base.VisitBlockStatement(blockStatementAst);
        }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst)
        {
            // I am ignoring .Label because it is hardly used.

            explanations.Add(
                new Explanation()
                {
                    CommandName = "break statement",
                    HelpResult = HelpTableQuery("about_break"),
                    Description = $"Breaks out of the current loop-like statement, switch statement or the current runspace."
                }.AddDefaults(breakStatementAst, explanations));

            return base.VisitBreakStatement(breakStatementAst);
        }

        public override AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst)
        {
            var exceptionText = "";
            if (!catchClauseAst.IsCatchAll)
            {
                exceptionText = $"of type '{string.Join("', '", catchClauseAst.CatchTypes.Select(c => c.TypeName.Name))}' ";
            }

            explanations.Add(
                new Explanation()
                {
                    CommandName = $"Catch block, belongs to Try statement",
                    HelpResult = HelpTableQuery("about_try_catch_finally"),
                    Description = $"Executed when an exception {exceptionText}is thrown in the Try {{}} block.",
                    TextToHighlight = "catch"
                }.AddDefaults(catchClauseAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            string cmdName = commandAst.GetCommandName();
            string resolvedCmd = Helpers.ResolveAlias(cmdName) ?? cmdName;
            if (string.IsNullOrEmpty(resolvedCmd))
            {
                resolvedCmd = cmdName;
            }

            HelpEntity helpResult = HelpTableQuery(resolvedCmd);
            var description = helpResult?.Synopsis?.ToString() ?? "";

            resolvedCmd = helpResult?.CommandName ?? resolvedCmd;

            // TODO: Create something better for this. BindCommand only binds commands that are loaded, and it's slow.
            // I've set it to not resolve, because that speeds things up, and a lot of the times it won't matter.
            var bindResult = StaticParameterBinder.BindCommand(commandAst, false);

            StringBuilder boundParameters = new StringBuilder();
            foreach (var p in bindResult.BoundParameters.Values)
            {
                if (p.Parameter != null)
                {
                    boundParameters
                        .Append(" -")
                        .Append(p.Parameter.Name);

                    if (!p.Parameter.SwitchParameter)
                    {
                        boundParameters
                            .Append(' ')
                            .Append(p.Value.Extent.Text);
                    }
                }
                else
                {
                    boundParameters
                        .Append(' ')
                        .Append(p.Value.Extent.Text);
                }
            }

            ExpandAliasesInExtent(commandAst, resolvedCmd);

            if (commandAst.InvocationOperator != TokenKind.Unknown)
            {
                string invocationOperatorExplanation = commandAst.InvocationOperator == TokenKind.Dot ?
                    "The dot source invocation operator '.'" :
                    Helpers.TokenExplainer(commandAst.InvocationOperator);

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

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst)
        {
            //AstExplainer(commandExpressionAst);
            return base.VisitCommandExpression(commandExpressionAst);
        }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst)
        {
            AstExplainer(commandParameterAst);
            return base.VisitCommandParameter(commandParameterAst);
        }

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            var explanation = new Explanation
            {
                CommandName = "Numeric literal"
            }.AddDefaults(constantExpressionAst, explanations);

            var numberString = constantExpressionAst.Extent.Text.ToString();
            var rg = new Regex(@"[a-zA-Z]");

            if (numberString.StartsWith("0b", true, null))
            {
                explanation.Description = $"Binary number (value: {constantExpressionAst.SafeGetValue()})";
            }
            else if (numberString.StartsWith("0x", true, null))
            {
                explanation.Description = $"Hexadecimal number (value: {constantExpressionAst.SafeGetValue()})";
            }
            else if (rg.IsMatch(constantExpressionAst.Extent.Text))
            {
                explanation.Description = $"Number (value: {constantExpressionAst.SafeGetValue()})";
            }
            else
            {
                // Just a plain numerical literal, no explanation needed.
                return AstVisitAction.SkipChildren;
            }

            // Moved this line to here, so it doesn't get executed when we encounter a plain number.
            explanation.HelpResult = HelpTableQuery("about_Numeric_Literals");
            explanations.Add(explanation);

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst)
        {
            explanations.Add(
                new Explanation()
                {
                    CommandName = "continue statement",
                    HelpResult = HelpTableQuery("about_continue"),
                    Description = $"Skips forward to the next iteration inside a loop."
                }.AddDefaults(continueStatementAst, explanations));

            return base.VisitContinueStatement(continueStatementAst);
        }

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            explanations.Add(
                new Explanation()
                {
                    Description = $"Converts or restricts the expression to the type '{convertExpressionAst.Type.TypeName.Name}'.",
                    CommandName = "Convert expression"
                }.AddDefaults(convertExpressionAst, explanations));

            return base.VisitConvertExpression(convertExpressionAst);
        }

        public override AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst)
        {
            explanations.Add(
                new Explanation()
                {
                    CommandName = "data statement",
                    HelpResult = HelpTableQuery("about_data_section"),
                    Description = $"A PowerShell data section, stored in the variable '${dataStatementAst.Variable}'",
                    TextToHighlight = "data"
                }.AddDefaults(dataStatementAst, explanations));

            return base.VisitDataStatement(dataStatementAst);
        }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst)
        {
            explanations.Add(
               new Explanation()
               {
                   CommandName = "do-until statement",
                   HelpResult = HelpTableQuery("about_do"),
                   Description = $"A loop that runs until '{doUntilStatementAst.Condition.Extent.Text}' evaluates to true",
                   TextToHighlight = "until"
               }.AddDefaults(doUntilStatementAst, explanations));

            return base.VisitDoUntilStatement(doUntilStatementAst);
        }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst)
        {
            explanations.Add(
                new Explanation()
                {
                    CommandName = "do-while statement",
                    HelpResult = HelpTableQuery("about_do"),
                    Description = $"A loop that runs as long as '{doWhileStatementAst.Condition.Extent.Text}' evaluates to true",
                    TextToHighlight = "while"
                }.AddDefaults(doWhileStatementAst, explanations));

            return base.VisitDoWhileStatement(doWhileStatementAst);
        }

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
        {
            AstExplainer(errorExpressionAst);
            return base.VisitErrorExpression(errorExpressionAst);
        }

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst)
        {
            AstExplainer(errorStatementAst);
            return base.VisitErrorStatement(errorStatementAst);
        }

        public override AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst)
        {
            var returning = string.IsNullOrEmpty(exitStatementAst.Pipeline?.Extent?.Text) ?
                "." :
                $", with an exit code of '{exitStatementAst.Pipeline.Extent.Text}'.";

            var helpResult = HelpTableQuery("about_language_keywords");
            helpResult.DocumentationLink += "#exit";

            explanations.Add(
                new Explanation()
                {
                    CommandName = "exit statement",
                    HelpResult = helpResult,
                    Description = $"Causes PowerShell to exit a script or a PowerShell instance{returning}",
                    TextToHighlight = "exit"
                }.AddDefaults(exitStatementAst, explanations));

            return base.VisitExitStatement(exitStatementAst);
        }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            var items = string.Join(", ", expandableStringExpressionAst.NestedExpressions.Select(n => n.Extent.Text));
            explanations.Add(
                new Explanation()
                {
                    Description = $"String with expandable elements: {items}",
                    CommandName = expandableStringExpressionAst.StringConstantType.ToString(),
                    HelpResult = HelpTableQuery("about_quoting_rules"),
                    TextToHighlight = "\""
                }.AddDefaults(expandableStringExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst)
        {
            AstExplainer(redirectionAst);
            return base.VisitFileRedirection(redirectionAst);
        }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            AstExplainer(forEachStatementAst);
            return base.VisitForEachStatement(forEachStatementAst);
        }

        public override AstVisitAction VisitForStatement(ForStatementAst forStatementAst)
        {
            AstExplainer(forStatementAst);
            return base.VisitForStatement(forStatementAst);
        }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            AstExplainer(functionDefinitionAst);
            return base.VisitFunctionDefinition(functionDefinitionAst);
        }

        public override AstVisitAction VisitHashtable(HashtableAst hashtableAst)
        {
            AstExplainer(hashtableAst);
            return base.VisitHashtable(hashtableAst);
        }

        public override AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst)
        {
            explanations.Add(
                new Explanation()
                {
                    Description = "if-statement, run statement lists based on the results of one or more conditional tests",
                    CommandName = "if-statement",
                    HelpResult = HelpTableQuery("about_if"),
                    TextToHighlight = "if"
                }.AddDefaults(ifStmtAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            AstExplainer(indexExpressionAst);
            return base.VisitIndexExpression(indexExpressionAst);
        }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst)
        {
            var args = "";
            var argsText = " without arguments";
            var objectOrClass = "object";
            var stat = "";

            if (methodCallAst.Arguments != null)
                args = string.Join(", ", methodCallAst.Arguments.Select(args => args.Extent.Text));

            if (methodCallAst.Static)
            {
                objectOrClass = "class";
                stat = "static ";
            }

            if (args != "")
                argsText = $", with arguments '{args}'";

            explanations.Add(
                new Explanation
                {
                    Description = $"Invoke the {stat}method '{methodCallAst.Member}' on {objectOrClass} '{methodCallAst.Expression}'{argsText}.",
                    CommandName = "Method",
                    HelpResult = HelpTableQuery("about_Methods")
                }.AddDefaults(methodCallAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            explanations.Add(
                new Explanation
                {
                    Description = $"Access the property '{memberExpressionAst.Member}' on object '{memberExpressionAst.Expression}'",
                    CommandName = "Property",
                    HelpResult = HelpTableQuery("about_Properties")
                }.AddDefaults(memberExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst)
        {
            AstExplainer(redirectionAst);
            return base.VisitMergingRedirection(redirectionAst);
        }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst)
        {
            AstExplainer(namedAttributeArgumentAst);
            return base.VisitNamedAttributeArgument(namedAttributeArgumentAst);
        }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst)
        {
            //AstExplainer(namedBlockAst);
            return base.VisitNamedBlock(namedBlockAst);
        }

        public override AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst)
        {
            AstExplainer(paramBlockAst);
            return base.VisitParamBlock(paramBlockAst);
        }

        public override AstVisitAction VisitParameter(ParameterAst parameterAst)
        {
            AstExplainer(parameterAst);
            return base.VisitParameter(parameterAst);
        }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            //AstExplainer(parenExpressionAst);
            return base.VisitParenExpression(parenExpressionAst);
        }

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst)
        {
            _ = Parser.ParseInput(pipelineAst.Extent.Text, out Token[] tokensInPipeline, out _);
            if (tokensInPipeline.Any(t => t.Kind == TokenKind.Pipe))
            {
                explanations.Add(new Explanation()
                {
                    Description = Helpers.TokenExplainer(TokenKind.Pipe) + $" Takes each element that results from the left hand side code, and passes it to the right hand side one by one.",
                    CommandName = "Pipeline",
                    HelpResult = HelpTableQuery("about_pipelines"),
                    TextToHighlight = "|"
                }.AddDefaults(pipelineAst, explanations));
                explanations.Last().OriginalExtent = "'|'";
            }
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst)
        {
            AstExplainer(returnStatementAst);
            return base.VisitReturnStatement(returnStatementAst);
        }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst)
        {
            //AstExplainer(scriptBlockAst);
            return base.VisitScriptBlock(scriptBlockAst);
        }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            //AstExplainer(scriptBlockExpressionAst);
            return base.VisitScriptBlockExpression(scriptBlockExpressionAst);
        }

        public override AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            if (statementBlockAst.Parent is TryStatementAst &
                // Ugly hack. Finally block is undistinguisable from the Try block, except for textual position.
                statementBlockAst.Extent.StartColumnNumber > statementBlockAst.Parent.Extent.StartColumnNumber + 5)
            {
                explanations.Add(new Explanation()
                {
                    CommandName = "Finally block, belongs to Try statement",
                    HelpResult = HelpTableQuery("about_try_catch_finally"),
                    Description = "The Finally block is always run after the Try block, regardless of Exceptions. Intended for cleanup actions that should always be run.",
                }.AddDefaults(statementBlockAst, explanations));
                explanations.Last().OriginalExtent = "Finally " + explanations.Last().OriginalExtent;

                return AstVisitAction.Continue;
            }

            //AstExplainer(statementBlockAst);
            return base.VisitStatementBlock(statementBlockAst);
        }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            var explanation = new Explanation
            {
                CommandName = stringConstantExpressionAst.StringConstantType.ToString(),
                HelpResult = HelpTableQuery("about_quoting_rules")
            }.AddDefaults(stringConstantExpressionAst, explanations);

            var hasDollarSign = stringConstantExpressionAst.Value.IndexOf('$') >= 0;
            switch (stringConstantExpressionAst.StringConstantType)
            {
                case StringConstantType.SingleQuoted:
                    explanation.Description = "String in which variables will not be expanded.";
                    break;
                case StringConstantType.SingleQuotedHereString:
                    explanation.Description = "Multiline here-string in which variables will not be expanded.";
                    break;
                case StringConstantType.DoubleQuoted:
                    if (hasDollarSign)
                    {
                        explanation.Description = "String in which variables will be expanded.";
                    }
                    else
                    {
                        explanation.Description = "String in which variables would be expanded.";
                    }
                    break;
                case StringConstantType.DoubleQuotedHereString:
                    if (hasDollarSign)
                    {
                        explanation.Description = "Multiline here-string in which variables will be expanded.";
                    }
                    else
                    {
                        explanation.Description = "Multiline here-string in which variables would be expanded.";
                    }
                    break;
                case StringConstantType.BareWord:
                    return AstVisitAction.SkipChildren;
            }

            explanations.Add(explanation);

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            AstExplainer(subExpressionAst);
            return base.VisitSubExpression(subExpressionAst);
        }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst)
        {
            AstExplainer(switchStatementAst);
            return base.VisitSwitchStatement(switchStatementAst);
        }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst)
        {
            AstExplainer(throwStatementAst);
            return base.VisitThrowStatement(throwStatementAst);
        }

        public override AstVisitAction VisitTrap(TrapStatementAst trapStatementAst)
        {
            AstExplainer(trapStatementAst);
            return base.VisitTrap(trapStatementAst);
        }

        public override AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst)
        {
            explanations.Add(new Explanation()
            {
                CommandName = "Try statement",
                HelpResult = HelpTableQuery("about_try_catch_finally"),
                Description = "If an exception is thrown in a Try block, it can be handled in a Catch block, and/or a cleanup can be done in a Finally block.",
                TextToHighlight = "try"
            }.AddDefaults(tryStatementAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
        {
            if (typeConstraintAst.Parent is CatchClauseAst)
                return base.VisitTypeConstraint(typeConstraintAst);

            var typeName = typeConstraintAst.TypeName.Name;
            var accelerator = ".";
            var cmdName = "Type constraint";
            HelpEntity help = null;

            var (acceleratorName, acceleratorFullTypeName) = Helpers.ResolveAccelerator(typeName);
            if (acceleratorName != null)
            {
                typeName = acceleratorName;
                accelerator = $", which is a type accelerator for '{acceleratorFullTypeName}'";
                help = HelpTableQuery("about_type_accelerators");
                cmdName = "Type accelerator";
            }

            explanations.Add(
                new Explanation()
                {
                    Description = $"Constrains the type to '{typeName}'{accelerator}",
                    CommandName = cmdName,
                    HelpResult = help
                }.AddDefaults(typeConstraintAst, explanations));

            return base.VisitTypeConstraint(typeConstraintAst);
        }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            // AstExplainer(typeExpressionAst);
            return base.VisitTypeExpression(typeExpressionAst);
        }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            explanations.Add(new Explanation()
            {
                Description = Helpers.TokenExplainer(unaryExpressionAst.TokenKind),
                CommandName = "Unary operator",
                HelpResult = HelpTableQuery("about_operators"),
                TextToHighlight = unaryExpressionAst.TokenKind.Text()
            }.AddDefaults(unaryExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            AstExplainer(usingExpressionAst);
            return base.VisitUsingExpression(usingExpressionAst);
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            var explanation = new Explanation
            {
                CommandName = "Variable",
                HelpResult = HelpTableQuery("about_variables"),
            }.AddDefaults(variableExpressionAst, explanations);

            var prefix = " ";
            var suffix = "";
            var varName = variableExpressionAst.VariablePath.UserPath;
            var standard = $"named '{varName}'";

            if (HasSpecialVars(varName))
            {
                if (string.Equals(varName, SpecialVars.PSDefaultParameterValues, StringComparison.OrdinalIgnoreCase))
                {
                    suffix = ", a special variable to set parameter default values.";
                    explanation.CommandName = "PSDefaultParameterValues";
                    explanation.HelpResult = HelpTableQuery("about_Parameters_Default_Values");
                }
                else if (SpecialVars.AutomaticVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                {
                    suffix = ", an automatic variable.";
                    explanation.CommandName = "Automatic variable";
                    explanation.HelpResult = HelpTableQuery("about_automatic_variables");
                }
                else if (SpecialVars.PreferenceVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                {
                    suffix = ", a preference variable.";
                    explanation.CommandName = "Preference variable";
                    explanation.HelpResult = HelpTableQuery("about_Preference_variables");
                }
                else
                {
                    suffix = ", a special variable.";
                    explanation.CommandName = "Special variable";
                    explanation.HelpResult = HelpTableQuery("about_automatic_variables");
                }
            }

            if (varName == "_" | string.Equals(varName, "PSItem", StringComparison.OrdinalIgnoreCase))
            {
                suffix = ", a built-in variable that holds the current element from the objects being passed in from the pipeline.";
                explanation.CommandName = "Pipeline iterator variable";
                explanation.HelpResult = HelpTableQuery("about_automatic_variables");
            }

            if (variableExpressionAst.Splatted)
            {
                prefix = " splatted ";
                explanation.CommandName = "Splatted variable";
                explanation.HelpResult = HelpTableQuery("about_splatting");
            }

            if (variableExpressionAst.VariablePath.IsPrivate)
                prefix = $" private ";

            if (!variableExpressionAst.VariablePath.IsUnscopedVariable)
            {
                var split = variableExpressionAst
                    .VariablePath
                    .UserPath
                    .Split(':');

                var identifier = split.FirstOrDefault();
                varName = split.LastOrDefault();
                standard = $"named '{varName}'";

                if (variableExpressionAst.VariablePath.IsGlobal | variableExpressionAst.VariablePath.IsScript)
                {
                    suffix = $" in '{identifier}' scope ";
                    explanation.CommandName = "Scoped variable";
                    explanation.HelpResult = HelpTableQuery("about_scopes");
                }


                if (variableExpressionAst.VariablePath.IsDriveQualified)
                {
                    if (string.Equals(identifier, "Env", StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = "n environment ";
                        suffix = " (on PSDrive 'env:')";
                        explanation.CommandName = "Environment variable";
                        explanation.HelpResult = HelpTableQuery("about_Environment_Variables");
                    }
                    else
                    {
                        standard = $"pointing to item '{varName}'";
                        suffix = $" on PSDrive '{identifier}:'";
                        explanation.CommandName = "PSDrive (Providers)";
                        explanation.HelpResult = HelpTableQuery("about_Providers");
                    }
                }
            }

            explanation.Description = $"A{prefix}variable {standard}{suffix}";

            explanations.Add(explanation);

            return AstVisitAction.SkipChildren;
        }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst)
        {
            explanations.Add(new Explanation()
            {
                Description = $"While '{whileStatementAst.Condition.Extent.Text}' evaluates to true, execute the code in the block {{}}.",
                CommandName = "While loop",
                HelpResult = HelpTableQuery("about_while"),
                TextToHighlight = "while"
            }.AddDefaults(whileStatementAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst)
        {
            AstExplainer(baseCtorInvokeMemberExpressionAst);
            return base.VisitBaseCtorInvokeMemberExpression(baseCtorInvokeMemberExpressionAst);
        }

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            AstExplainer(configurationDefinitionAst);
            return base.VisitConfigurationDefinition(configurationDefinitionAst);
        }

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst)
        {
            AstExplainer(dynamicKeywordStatementAst);
            return base.VisitDynamicKeywordStatement(dynamicKeywordStatementAst);
        }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            AstExplainer(functionMemberAst);
            return base.VisitFunctionMember(functionMemberAst);
        }

        public override AstVisitAction VisitPipelineChain(PipelineChainAst statementChain)
        {
            AstExplainer(statementChain);
            return base.VisitPipelineChain(statementChain);
        }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst)
        {
            AstExplainer(propertyMemberAst);
            return base.VisitPropertyMember(propertyMemberAst);
        }

        public override AstVisitAction VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst)
        {
            var helpResult = HelpTableQuery("about_if");
            helpResult.DocumentationLink += "#using-the-ternary-operator-syntax";

            explanations.Add(new Explanation()
            {
                Description = $"A condensed if-else construct, used for simple situations.",
                CommandName = "Ternary expression",
                HelpResult = helpResult,
                TextToHighlight = "?"
            }.AddDefaults(ternaryExpressionAst, explanations));

            return base.VisitTernaryExpression(ternaryExpressionAst);
        }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            var highlight = "";
            var about = "";
            var attributes = ".";
            var synopsis = "";

            if (typeDefinitionAst.Attributes.Count > 0)
            {
                attributes = $", with the attributes '{string.Join(", ", typeDefinitionAst.Attributes.Select(a => a.TypeName.Name))}'.";
            }

            if (typeDefinitionAst.IsClass)
            {
                highlight = "class";
                about = "about_classes";
                synopsis = $"A class is a blueprint for a type. Create a new instance of this type with [{typeDefinitionAst.Name}]::new().";

            }
            else if (typeDefinitionAst.IsEnum)
            {
                highlight = "enum";
                about = "about_enum";
                synopsis = "Enum is short for enumeration. An enumeration is a distinct type that consists of a set of named labels called the enumerator list.";
            }

            explanations.Add(new Explanation()
            {
                Description = $"Defines a '{highlight}', with the name '{typeDefinitionAst.Name}'{attributes} {synopsis}",
                CommandName = "Type definition",
                HelpResult = HelpTableQuery(about),
                TextToHighlight = highlight
            }.AddDefaults(typeDefinitionAst, explanations));

            return base.VisitTypeDefinition(typeDefinitionAst);
        }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst)
        {
            explanations.Add(new Explanation()
            {
                Description = $"The using statement allows you to specify which namespaces are used in the session. Adding namespaces simplifies usage of .NET classes and member and allows you to import classes from script modules and assemblies. In this case a {usingStatementAst.UsingStatementKind} is loaded.",
                CommandName = "using statement",
                HelpResult = HelpTableQuery("about_using"),
                TextToHighlight = "using"
            }.AddDefaults(usingStatementAst, explanations));

            return base.VisitUsingStatement(usingStatementAst);
        }
    }
}
