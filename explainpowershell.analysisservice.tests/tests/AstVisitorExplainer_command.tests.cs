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
    public class GetAstVisitorExplainer_commandTests
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
        public void ShoudGenerateHelpForUnknownCommand()
        {
            ScriptBlock.Create("myUnknownCommand")
                .Ast
                .Visit(explainer);

            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Unrecognized command.",
                res.Explanations[0].Description);
        }

        [Test]
        public void ShoudGenerateHelpForUnknownCmdLet()
        {
            ScriptBlock.Create("get-myunknownCmdlet")
                .Ast
                .Visit(explainer);

            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.That(
                res
                .Explanations[0]
                .Description
                .StartsWith(
                    "Unrecognized cmdlet. Try finding the module that contains this cmdlet and add it to my database."));
        }

        [Test]
        public void ShoudGenerateHelpForUnknownCommandWithDashInName()
        {
            ScriptBlock.Create("dotnet-watch")
                .Ast
                .Visit(explainer);

            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Unrecognized command.",
                res.Explanations[0].Description);
        }
    }
}