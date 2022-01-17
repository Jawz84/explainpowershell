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
using ExplainPowershell.SyntaxAnalyzer;
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
            var tableClient = new TableClient(
                "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;", 
                "HelpData");

            explainer = new(
                string.Empty,
                tableClient,
                mockILogger);
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
                "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables",
                res.Explanations[1].HelpResult?.DocumentationLink);
            Assert.AreEqual(
                "Scoped variable",
                res.Explanations[1].CommandName);
            Assert.That(
                res.Explanations[1].HelpResult?.RelatedLinks, 
                Is.Not.Null.And.Not.Empty);
        }
    }
}