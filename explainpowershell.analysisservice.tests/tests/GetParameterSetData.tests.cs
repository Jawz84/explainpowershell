using NUnit.Framework;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using ExplainPowershell.SyntaxAnalyzer;
using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class GetParameterSetDataTests
    {
        public required HelpEntity helpItem { get; set; }
        public required List<ParameterData> doc { get; set; }

        [SetUp]
        public void Setup()
        {
            var filename = "../../../testfiles/test_get_help.json";
            var json = File.ReadAllText(filename);
            helpItem = JsonSerializer.Deserialize<HelpEntity>(json) ?? 
                throw new InvalidOperationException("Failed to deserialize test_get_help.json");
            doc = JsonSerializer.Deserialize<List<ParameterData>>(helpItem.Parameters!) ?? 
                throw new InvalidOperationException("Failed to deserialize Parameters");
        }

        [Test]
        public void ShouldReadParameterSetDetails()
        {
            var parameterData = doc[4]; // The -Full parameter
            var result = Helpers.GetParameterSetData(
                parameterData, 
                helpItem.ParameterSetNames.Split(", ")).FirstOrDefault() ??
                throw new InvalidOperationException("No parameter set data found");

            Assert.That(result.ParameterSetName, Is.EqualTo("AllUsersView"));
        }
    }
}