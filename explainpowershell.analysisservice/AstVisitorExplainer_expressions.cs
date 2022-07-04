using System;
using System.Linq;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;
using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
         public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            var helpResult = HelpTableQuery("about_arrays");
            helpResult.DocumentationLink += "#the-array-sub-expression-operator";

            explanations.Add(
                new Explanation()
                {
                    Description = "The array sub-expression operator creates an array from the statements inside it. Whatever the statement inside the operator produces, the operator will place it in an array. Even if there is zero or one object.",
                    CommandName = "Array expression",
                    HelpResult = helpResult,
                    TextToHighlight = "@(",
                }.AddDefaults(arrayExpressionAst, explanations));

            return base.VisitArrayExpression(arrayExpressionAst);
        }


        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst)
        {
            // SKIP, because the Attribute will be listed separately also.
            AstExplainer(attributedExpressionAst);
            return base.VisitAttributedExpression(attributedExpressionAst);
        }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            var (tokenDescription, helpQuery) = Helpers.TokenExplainer(binaryExpressionAst.Operator);
            helpQuery ??= "about_operators";
            var helpResult = HelpTableQuery(helpQuery);

            explanations.Add(
                new Explanation()
                {
                    CommandName = "Operator",
                    HelpResult = helpResult,
                    Description = $"{tokenDescription} This works from left to right, so targeting '{binaryExpressionAst.Right.Extent.Text}'",
                    TextToHighlight = binaryExpressionAst.Operator.Text()
                }.AddDefaults(binaryExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst)
        {
            // IGNORE because commandExpressionAst will only have '.Expression' which resolves to one of the actual expressions.
            //AstExplainer(commandExpressionAst);
            return base.VisitCommandExpression(commandExpressionAst);
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

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
        {
            // SKIP
            AstExplainer(errorExpressionAst);
            return base.VisitErrorExpression(errorExpressionAst);
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

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            AstExplainer(indexExpressionAst);
            return base.VisitIndexExpression(indexExpressionAst);
        }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            // TODO: document why
            //AstExplainer(parenExpressionAst);
            return base.VisitParenExpression(parenExpressionAst);
        }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            // TODO: document why
            //AstExplainer(scriptBlockExpressionAst);
            return base.VisitScriptBlockExpression(scriptBlockExpressionAst);
        }


        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            var explanation = new Explanation
            {
                CommandName = stringConstantExpressionAst.StringConstantType.ToString(),
                HelpResult = HelpTableQuery("about_quoting_rules")
            }.AddDefaults(stringConstantExpressionAst, explanations);

            var hasDollarSign = stringConstantExpressionAst.Value.Contains('$');
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
            // SKIP
            AstExplainer(subExpressionAst);
            return base.VisitSubExpression(subExpressionAst);
        }


        public override AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            if (typeExpressionAst.Parent is BinaryExpressionAst ||
                typeExpressionAst.Parent is CommandExpressionAst ||
                typeExpressionAst.Parent is AssignmentStatementAst)
            {
                HelpEntity help = null;
                var description = string.Empty;

                if (typeExpressionAst.TypeName.IsArray)
                {
                    description = $"Array of '{typeExpressionAst.TypeName.Name}'";
                    help = new HelpEntity() {
                        DocumentationLink = "https://docs.microsoft.com/en-us/powershell/scripting/lang-spec/chapter-04"
                    };
                }
                else if (typeExpressionAst.TypeName.IsGeneric)
                {
                    description = $"Generic type";
                    help = new HelpEntity() {
                        DocumentationLink = "https://docs.microsoft.com/en-us/powershell/scripting/lang-spec/chapter-04#44-generic-types"
                    };
                }

                explanations.Add(new Explanation()
                {
                    Description = description,
                    CommandName = "Type expression",
                    HelpResult = help
                }.AddDefaults(typeExpressionAst, explanations));

            }
            return base.VisitTypeExpression(typeExpressionAst);
        }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            var (description, helpQuery) = Helpers.TokenExplainer(unaryExpressionAst.TokenKind);
            helpQuery ??= "about_operators";

            explanations.Add(new Explanation()
            {
                Description = description,
                CommandName = "Unary operator",
                HelpResult = HelpTableQuery(helpQuery),
                TextToHighlight = unaryExpressionAst.TokenKind.Text()
            }.AddDefaults(unaryExpressionAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // TODO: add using expression explanation
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

            if (variableExpressionAst.Parent is UsingExpressionAst)
            {
                suffix = ", with the 'using' scope modifier: a local variable used in a remote scope.";
                explanation.HelpResult = HelpTableQuery("about_Remote_Variables");
                explanation.CommandName = "Scoped variable";
                explanation.HelpResult.RelatedLinks += HelpTableQuery("about_Scopes")?.DocumentationLink;
            }

            explanation.Description = $"A{prefix}variable {standard}{suffix}";

            explanations.Add(explanation);

            return AstVisitAction.SkipChildren;
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
    }
}