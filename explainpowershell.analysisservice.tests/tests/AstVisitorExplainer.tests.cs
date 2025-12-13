using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using System.Threading.Tasks;
using explainpowershell.models;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class GetAstVisitorExplainerTests
    {
        AstVisitorExplainer explainer;

        [SetUp]
        public void Setup()
        {
            var mockILogger = new LoggerDouble<LogEntry>();

            // Unit tests should not depend on Azurite/Table Storage. Seed only the help topics
            // required by these assertions.
            var helpRepository = new InMemoryHelpRepository();
            TestHelpData.SeedAboutTopics(helpRepository);

            explainer = new(
                extentText: string.Empty,
                helpRepository: helpRepository,
                log: mockILogger,
                tokens: null);
        }

        [Test]
        public void ShoudGenerateHelpForClasses()
        {
            ScriptBlock.Create("class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School; Child([int]$a, [string]$s ) : base($a) { $this.School = $s}}").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Defines a 'class', with the name 'Person'. A class is a blueprint for a type. Create a new instance of this type with [Person]::new().",
                res.Explanations[0].Description);
        }

        [Test]
        public void ShoudGenerateHelpForConstructors()
        {
            ScriptBlock.Create("class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School; Child([int]$a, [string]$s ) : base($a) { $this.School = $s}}").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.IsTrue(res.Explanations[3].Description.StartsWith("A constructor, a special method"));
        }

        [Test]
        public void ShouldGenerateHelpForUsingExpressions()
        {
            ScriptBlock.Create("$using:var").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "A variable named 'var', with the 'using' scope modifier: a local variable used in a remote scope.",
                res.Explanations[1].Description);
            Assert.AreEqual(
                "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables",
                res.Explanations[1].HelpResult?.DocumentationLink);
            Assert.AreEqual(
                "Scoped variable",
                res.Explanations[1].CommandName);
            Assert.That(
                res.Explanations[1].HelpResult?.RelatedLinks,
                Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ShoudGenerateHelpForForeachStatements()
        {
            ScriptBlock.Create("foreach ($i in $array) { $i }").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Executes the code in the script block for each element '$i' in '$array'",
                res.Explanations[0].Description);

            Assert.AreEqual(
                "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Foreach",
                res.Explanations[0].HelpResult?.DocumentationLink);
        }

        [Test]
        public void ShoudGenerateHelpForForStatements()
        {
            ScriptBlock.Create("for ($i=0; $i -lt 10; $i++) { $i }").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Executes the code in the script block for as long as adding '$i++' on '$i=0' results in '$i -lt 10' being true.",
                res.Explanations[0].Description);

            Assert.AreEqual(
                "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_For",
                res.Explanations[0].HelpResult?.DocumentationLink);
        }

        [Test]
        public void ShouldGenerateHelpForReturnStatement()
        {
            ScriptBlock.Create("return 42").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            var explanation = res.Explanations.SingleOrDefault(e => e.TextToHighlight == "return");

            Assert.That(explanation, Is.Not.Null);
            Assert.That(explanation.CommandName, Is.EqualTo("return statement"));
            Assert.That(explanation.HelpResult?.DocumentationLink, Does.Contain("about_Return"));
            Assert.That(explanation.HelpResult?.RelatedLinks, Does.Contain("about_language_keywords").And.Contain("#return"));
        }

        [Test]
        public void ShouldGenerateHelpForThrowStatement()
        {
            ScriptBlock.Create("throw 'boom'").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            var explanation = res.Explanations.SingleOrDefault(e => e.TextToHighlight == "throw");

            Assert.That(explanation, Is.Not.Null);
            Assert.That(explanation.CommandName, Is.EqualTo("throw statement"));
            Assert.That(explanation.HelpResult?.DocumentationLink, Does.Contain("about_Throw"));
            Assert.That(explanation.HelpResult?.RelatedLinks, Does.Contain("about_language_keywords").And.Contain("#throw"));
        }

        [Test]
        public void ShouldGenerateHelpForTrapStatement()
        {
            ScriptBlock.Create("trap { continue }").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            var explanation = res.Explanations.SingleOrDefault(e => e.TextToHighlight == "trap");

            Assert.That(explanation, Is.Not.Null);
            Assert.That(explanation.CommandName, Is.EqualTo("trap statement"));
            Assert.That(explanation.HelpResult?.DocumentationLink, Does.Contain("about_Trap"));
            Assert.That(explanation.Description, Does.Contain("trap handler"));
        }
    }
}