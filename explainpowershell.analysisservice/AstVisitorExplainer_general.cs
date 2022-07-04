using System.Linq;
using System.Management.Automation.Language;
using explainpowershell.models;
using explainpowershell.SyntaxAnalyzer.ExtensionMethods;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public partial class AstVisitorExplainer : AstVisitor2
    {
        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            // SKIP
            AstExplainer(arrayLiteralAst);
            return base.VisitArrayLiteral(arrayLiteralAst);
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

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst)
        {
            var redirectsOrAppends = redirectionAst.Append ? "Appends" : "Redirects";
            var fromStream = redirectionAst.FromStream == RedirectionStream.Output ? "" :
                $"from stream '{redirectionAst.FromStream}' ";

            explanations.Add(
                new Explanation()
                {
                    Description = $"{redirectsOrAppends} output {fromStream}to location '{redirectionAst.Location}'.",
                    CommandName = "File redirection operator",
                    HelpResult = HelpTableQuery("about_redirection"),
                    TextToHighlight = ">"
                }.AddDefaults(redirectionAst, explanations));

            return base.VisitFileRedirection(redirectionAst);
        }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            // TODO: add function definition explanation
            AstExplainer(functionDefinitionAst);
            return base.VisitFunctionDefinition(functionDefinitionAst);
        }

        public override AstVisitAction VisitHashtable(HashtableAst hashtableAst)
        {
            var keys = string.Join(", ", hashtableAst.KeyValuePairs.Select(p => p.Item1.ToString()));
            var keysString = keys == null ? "" : $" This hash table has the following keys: '{keys}'";

            explanations.Add(
                new Explanation()
                {
                    Description = $"An object that holds key-value pairs, optimized for hash-searching for keys.{keysString}",
                    CommandName = "Hash table",
                    HelpResult = HelpTableQuery("about_hash_tables"),
                    TextToHighlight = "@{"
                }.AddDefaults(hashtableAst, explanations));

            return base.VisitHashtable(hashtableAst);
        }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst)
        {
            explanations.Add(
                new Explanation()
                {
                    Description = $"Redirects output from stream '{redirectionAst.FromStream}' to stream '{redirectionAst.ToStream}'.",
                    CommandName = "Stream redirection operator",
                    HelpResult = HelpTableQuery("about_redirection"),
                    TextToHighlight = ">&"
                }.AddDefaults(redirectionAst, explanations));

            return base.VisitMergingRedirection(redirectionAst);
        }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst)
        {
            // TODO: add named attribute argument explanation
            AstExplainer(namedAttributeArgumentAst);
            return base.VisitNamedAttributeArgument(namedAttributeArgumentAst);
        }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst)
        {
            // TODO: document why commented out
            //AstExplainer(namedBlockAst);
            return base.VisitNamedBlock(namedBlockAst);
        }

        public override AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst)
        {
            // TODO: add param block explanation
            AstExplainer(paramBlockAst);
            return base.VisitParamBlock(paramBlockAst);
        }

        public override AstVisitAction VisitParameter(ParameterAst parameterAst)
        {
            // TODO: add parameter explanation
            AstExplainer(parameterAst);
            return base.VisitParameter(parameterAst);
        }

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst)
        {
            _ = Parser.ParseInput(pipelineAst.Extent.Text, out Token[] tokensInPipeline, out _);
            if (tokensInPipeline.Any(t => t.Kind == TokenKind.Pipe))
            {
                var (tokenDescription, tokenHelpQuery) = Helpers.TokenExplainer(TokenKind.Pipe);

                explanations.Add(new Explanation()
                {
                    Description = $"{tokenDescription} Takes each element that results from the left hand side code, and passes it to the right hand side one by one.",
                    CommandName = "Pipeline",
                    HelpResult = HelpTableQuery(tokenHelpQuery),
                    TextToHighlight = "|"
                }.AddDefaults(pipelineAst, explanations));
                explanations.Last().OriginalExtent = "'|'";
            }
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst)
        {
            // TODO: document why
            //AstExplainer(scriptBlockAst);
            return base.VisitScriptBlock(scriptBlockAst);
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
        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
        {
            if (typeConstraintAst.Parent is CatchClauseAst)
            {
                return base.VisitTypeConstraint(typeConstraintAst);
            }

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
            else if (typeConstraintAst.Parent is ConvertExpressionAst)
            {
                return base.VisitTypeConstraint(typeConstraintAst);
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
        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            // SKIP
            // configuration MyDscConfig {..} -> throws exception in devcontainer, just works in cloud.
            // Example to test: Configuration cnf { Import-DscResource -Module nx; Node 'lx.a.com' { nxFile ExampleFile { DestinationPath = '/tmp/example'; Contents = "hello world `n"; Ensure = 'Present'; Type = 'File'; } } }; cnf -OutputPath:'C:\temp'
            // #35
            AstExplainer(configurationDefinitionAst);
            return base.VisitConfigurationDefinition(configurationDefinitionAst);
        }

        public override AstVisitAction VisitPipelineChain(PipelineChainAst statementChain)
        {
            var operatorString = statementChain.Operator.ToString() == "AndAnd" ? "&&" : "||";

            explanations.Add(new Explanation()
            {
                Description = $"The '{operatorString}' operator executes the right-hand pipeline, if the left-hand pipeline succeeded.",
                CommandName = "Pipeline chain",
                HelpResult = HelpTableQuery("about_Pipeline_Chain_Operators"),
                TextToHighlight = operatorString
            }.AddDefaults(statementChain, explanations));

            return base.VisitPipelineChain(statementChain);
        }
    }
}
