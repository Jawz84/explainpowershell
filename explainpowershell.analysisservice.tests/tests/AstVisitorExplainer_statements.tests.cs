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
    public class GetAstVisitorExplainer_statementTests
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
                extentText: string.Empty,
                client: tableClient,
                log: mockILogger,
                tokens: null);
        }

        [Test]
        public void ShouldHelpWithSwitch()
        {
            ScriptBlock
                .Create("switch (Get-Command ) { 'get-childitem' { $_ | select -exp moduleName  } Default {'.'}}")
                .Ast
                .Visit(explainer);

            AnalysisResult res = explainer.GetAnalysisResult();

            Assert.AreEqual(
                "Defines a 'class', with the name 'Person'. A class is a blueprint for a type. Create a new instance of this type with [Person]::new().",
                res.Explanations[0].Description);
        }
    }
}