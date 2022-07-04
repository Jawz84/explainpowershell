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
        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            var (operatorExplanation, tokenHelpQuery) = Helpers.TokenExplainer(assignmentStatementAst.Operator);
            explanations.Add(
                new Explanation()
                {
                    CommandName = $"Assignment operator '{assignmentStatementAst.Operator.Text()}'",
                    HelpResult = HelpTableQuery(tokenHelpQuery),
                    Description = $"{operatorExplanation} Assigns a value to '{assignmentStatementAst.Left.Extent.Text}'.",
                    TextToHighlight = assignmentStatementAst.Operator.Text()
                }.AddDefaults(assignmentStatementAst, explanations));

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            // SKIP, because there is not much to explain.
            AstExplainer(blockStatementAst);
            return base.VisitBlockStatement(blockStatementAst);
        }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst)
        {
            // I am ignoring '.Label' because it is hardly used.

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


        public override AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst)
        {
            // SKIP
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

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            explanations.Add(
                new Explanation() {
                    Description = $"Executes the code in the script block for each element '{forEachStatementAst.Variable.Extent.Text}' in '{forEachStatementAst.Condition}'",
                    CommandName = "foreach statement",
                    HelpResult = HelpTableQuery("about_foreach"),
                    TextToHighlight = "foreach"
                }.AddDefaults(forEachStatementAst, explanations));

            return base.VisitForEachStatement(forEachStatementAst);
        }

        public override AstVisitAction VisitForStatement(ForStatementAst forStatementAst)
        {
            explanations.Add(
                new Explanation() {
                    Description = $"Executes the code in the script block for as long as adding '{forStatementAst.Iterator.Extent.Text}' on '{forStatementAst.Initializer.Extent.Text}' results in '{forStatementAst.Condition.Extent.Text}' being true.",
                    CommandName = "for statement",
                    HelpResult = HelpTableQuery("about_for"),
                    TextToHighlight = "for"
                }.AddDefaults(forStatementAst, explanations));
            return base.VisitForStatement(forStatementAst);
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

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst)
        {
            // TODO: add return statement explanation
            AstExplainer(returnStatementAst);
            return base.VisitReturnStatement(returnStatementAst);
        }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst)
        {
            // TODO: add switch statement explanation
            AstExplainer(switchStatementAst);
            return base.VisitSwitchStatement(switchStatementAst);
        }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst)
        {
            // TODO: add throw statement explanation
            AstExplainer(throwStatementAst);
            return base.VisitThrowStatement(throwStatementAst);
        }

        public override AstVisitAction VisitTrap(TrapStatementAst trapStatementAst)
        {
            // TODO: add trap explanation
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

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst)
        {
            // SKIP
            // Apparently one can add keywords with [System.Management.Automation.Language.DynamicKeyword]::AddKeyword.
            // This won't work in static analysis.
            // I've never seen a dynamic keyword in the wild yet anyway.
            // Update: it appears in DSC, dynamic keywords are used: 
            // Example to test: Configuration cnf { Import-DscResource -Module nx; Node 'lx.a.com' { nxFile ExampleFile { DestinationPath = '/tmp/example'; Contents = "hello world `n"; Ensure = 'Present'; Type = 'File'; } } }; cnf -OutputPath:'C:\temp'
            AstExplainer(dynamicKeywordStatementAst);
            return base.VisitDynamicKeywordStatement(dynamicKeywordStatementAst);
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
