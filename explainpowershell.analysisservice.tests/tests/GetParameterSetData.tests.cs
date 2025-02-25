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
            if (!File.Exists(filename))
                throw new FileNotFoundException($"Test file not found: {filename}");

            var json = File.ReadAllText(filename);
            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException("Test file is empty");

            helpItem = JsonSerializer.Deserialize<HelpEntity>(json) ?? 
                throw new InvalidOperationException("Failed to deserialize test_get_help.json");

            if (string.IsNullOrEmpty(helpItem.Parameters))
                throw new InvalidOperationException("No Parameters data in test file");

            if (string.IsNullOrEmpty(helpItem.ParameterSetNames))
                throw new InvalidOperationException("No ParameterSetNames data in test file");

            doc = JsonSerializer.Deserialize<List<ParameterData>>(helpItem.Parameters) ?? 
                throw new InvalidOperationException("Failed to deserialize Parameters");

            if (doc.Count < 5)
                throw new InvalidOperationException("Test file does not contain expected parameter data");

            Assert.That(doc[4], Is.Not.Null, "Fifth parameter should not be null");
            Assert.That(doc[4].Name, Is.Not.Null, "Fifth parameter should have a name");
        }

        [Test]
        public void ShouldReadParameterSetDetails()
        {
            var parameterData = doc[4]; // The -Full parameter
            Assert.That(parameterData, Is.Not.Null, "Parameter data at index 4 should not be null");
            Assert.That(helpItem.ParameterSetNames, Is.Not.Null, "ParameterSetNames should not be null");

            var paramSetNames = helpItem?.ParameterSetNames?.Split(", ", StringSplitOptions.RemoveEmptyEntries);
            Assert.That(paramSetNames, Is.Not.Empty, "Parameter set names should not be empty");

            var parameterSets = Helpers.GetParameterSetData(parameterData!, paramSetNames!);
            Assert.That(parameterSets, Is.Not.Null, "GetParameterSetData should return non-null result");
            Assert.That(parameterSets.Count(), Is.GreaterThan(0), "GetParameterSetData should return at least one result");

            var result = parameterSets.First();
            Assert.That(result, Is.Not.Null, "First parameter set should not be null");
            Assert.That(result.ParameterSetName, Is.EqualTo("AllUsersView"));
        }
    }
}