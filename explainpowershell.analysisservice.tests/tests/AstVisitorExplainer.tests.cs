using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using System.Threading.Tasks;

using Azure.Data.Tables;
using explainpowershell.models;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class GetAstVisitorExplainerTests
    {
        private AstVisitorExplainer explainer = null!;

        [SetUp]
        public void Setup()
        {
            var mockILogger = new LoggerDouble<LogEntry>();
            var tableClient = new TableClient(
                "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;", 
                "HelpData");

            explainer = new(
                extentText: string.Empty,
                client: tableClient,
                log: mockILogger,
                tokens: Array.Empty<Token>());
        }

        [Test]
        public void ShoudGenerateHelpForClasses()
        {
            ScriptBlock.Create("class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School; Child([int]$a, [string]$s ) : base($a) { $this.School = $s}}").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(res.Explanations[0].Description, Is.EqualTo(
                "Defines a 'class', with the name 'Person'. A class is a blueprint for a type. Create a new instance of this type with [Person]::new()."));
        }

        [Test]
        public void ShoudGenerateHelpForConstructors()
        {
            ScriptBlock.Create("class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School; Child([int]$a, [string]$s ) : base($a) { $this.School = $s}}").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(res.Explanations[3].Description, Does.StartWith("A constructor, a special method"));
        }

        [Test]
        public void ShouldGenerateHelpForUsingExpressions()
        {
            ScriptBlock.Create("$using:var").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(res.Explanations[1].Description, Is.EqualTo(
                "A variable named 'var', with the 'using' scope modifier: a local variable used in a remote scope."));
            Assert.That(res.Explanations[1].HelpResult?.DocumentationLink, Is.EqualTo(
                "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables"));
            Assert.That(res.Explanations[1].CommandName, Is.EqualTo("Scoped variable"));
            Assert.That(res.Explanations[1].HelpResult?.RelatedLinks, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ShoudGenerateHelpForForeachStatements()
        {
            ScriptBlock.Create("foreach ($i in $array) { $i }").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(res.Explanations[0].Description, Is.EqualTo(
                "Executes the code in the script block for each element '$i' in '$array'"));

            Assert.That(res.Explanations[0].HelpResult?.DocumentationLink, Is.EqualTo(
                "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Foreach"));
        }

        [Test]
        public void ShoudGenerateHelpForForStatements()
        {
            ScriptBlock.Create("for ($i=0; $i -lt 10; $i++) { $i }").Ast.Visit(explainer);
            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(res.Explanations[0].Description, Is.EqualTo(
                "Executes the code in the script block for as long as adding '$i++' on '$i=0' results in '$i -lt 10' being true."));
            Assert.That(res.Explanations[0].HelpResult?.DocumentationLink, Is.EqualTo(
                "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_For"));
        }
    }
}