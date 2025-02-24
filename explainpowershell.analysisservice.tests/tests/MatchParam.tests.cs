using NUnit.Framework;
using System.IO;
using System;
using ExplainPowershell.SyntaxAnalyzer;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class MatchParamTests
    {
        public required string filename { get; set; }
        public required string json { get; set; }

        [SetUp]
        public void Setup()
        {
            filename = "../../../testfiles/parameterinfo.json";
            if (!File.Exists(filename))
                throw new FileNotFoundException("Test file not found", filename);
                
            json = File.ReadAllText(filename);
            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException("Test file is empty");
        }

        [Test]
        public void ShouldThrowIfAmbiguous()
        {
            var param = "a";
            Assert.That(() => Helpers.MatchParam(param, json), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ShouldResolveParameterIfAlias()
        {
            var param = "s";
            var result = Helpers.MatchParam(param, json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Recurse"));
        }

        [Test]
        public void ShouldNotMatchNoneAlias()
        {
            var param = "none";
            Assert.That(Helpers.MatchParam(param, json), Is.Null);
        }

        [Test]
        public void ShouldResolveParamForUnambiguousPartialName()
        {
            var param = "sy";
            var result = Helpers.MatchParam(param, json);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("System"));
        }
    }
}
