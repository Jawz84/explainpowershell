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
        private HelpEntity helpItem;
        private List<ParameterData> doc;

        [SetUp]
        public void Setup()
        {
            var filename = "../../../testfiles/test_get_help.json";
            var json = File.ReadAllText(filename);
            helpItem = JsonSerializer.Deserialize<HelpEntity>(json);
            doc = JsonSerializer.Deserialize<List<ParameterData>>(helpItem.Parameters);
        }

        [Test]
        public void ShouldReadParameterSetDetails()
        {
            var parameterData = doc[4]; // The -Full parameter
            
            Assert.AreEqual(
                Helpers.GetParameterSetData(
                    parameterData, 
                    helpItem.ParameterSetNames.Split(", ")).FirstOrDefault().ParameterSetName,
                "AllUsersView"
            );
        }
   }
}