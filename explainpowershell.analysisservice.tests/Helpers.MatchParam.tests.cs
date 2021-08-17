using NUnit.Framework;
using System.IO;
using System;
using ExplainPowershell.SyntaxAnalyzer;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class Tests
    {
        private string filename;
        private string json;

        [SetUp]
        public void Setup()
        {
            filename = "../../../testfiles/parameterinfo.json";
            json = File.ReadAllText(filename);
        }

        [Test]
        public void ShouldThrowIfAmbiguous()
        {
            var param = "fi";
            Assert.Throws<ArgumentException>(() => Helpers.MatchParam(param, json));
        }

        [Test]
        public void ShouldResolveParameterIfAlias()
        {
            var param = "s";
            Assert.AreEqual(Helpers.MatchParam(param, json).Name, "Recurse");
        }

        [Test]
        public void ShouldNotMatchNoneAlias()
        {
            var param = "none";
            Assert.IsNull(Helpers.MatchParam(param, json));
        }

        [Test]
        public void ShouldResolveParamForUnambiguousPartialName()
        {
            var param = "sy";
            Assert.AreEqual(Helpers.MatchParam(param, json).Name, "System");
        }
    }
}