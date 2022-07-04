using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    public class GetAstVisitorExplainer_helpersTests
    {
        [Test]
        public void ShouldReturnListOfVerbs()
        {
            var verbList = AstVisitorExplainer.GetApprovedVerbs();

            Assert.That(verbList.Contains("Get"));
            Assert.That(verbList.Contains("Convert"));
            Assert.That(verbList.Contains("Enable"));
            Assert.That(verbList.Count > 90); // there are ~100 approved verbs
        }
    }
}