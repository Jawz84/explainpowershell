using System.Text.Json;
using Azure.Data.Tables;
using explainpowershell.models;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    [TestFixture]
    public class HelpEntitySynopsisBindingTests
    {
        [Test]
        public void HelpEntity_Synopsis_ShouldNotBeNullWhenPresent()
        {
            // Arrange - simulate what the helpcollector produces
            var jsonFromHelpcollector = @"{
                ""CommandName"": ""Get-NetIPConfiguration"",
                ""ModuleName"": ""NetTCPIP"",
                ""ModuleVersion"": ""1.0.0.0"",
                ""Synopsis"": ""Gets the IP address configuration."",
                ""Syntax"": ""Get-NetIPConfiguration [[-InterfaceIndex] <Int32[]>]""
            }";

            // Act - deserialize like the helpwriter would read it
            var entity = JsonSerializer.Deserialize<HelpEntity>(jsonFromHelpcollector);

            // Assert
            Assert.IsNotNull(entity);
            Assert.IsNotNull(entity!.Synopsis, "Synopsis should not be null");
            Assert.AreEqual("Get-NetIPConfiguration", entity.CommandName);
            Assert.AreEqual("NetTCPIP", entity.ModuleName);
            Assert.AreEqual("Gets the IP address configuration.", entity.Synopsis);
        }

        [Test]
        public void HelpEntity_Synopsis_ToString_ShouldReturnValue()
        {
            // Arrange
            var entity = new HelpEntity
            {
                CommandName = "Get-Test",
                Synopsis = "Test synopsis"
            };

            // Act
            var description = entity.Synopsis?.ToString();

            // Assert
            Assert.AreEqual("Test synopsis", description);
        }

        [Test]
        public void HelpEntity_Synopsis_NullCheck_ShouldWorkCorrectly()
        {
            // Arrange
            var entityWithSynopsis = new HelpEntity { Synopsis = "Has synopsis" };
            var entityWithoutSynopsis = new HelpEntity { Synopsis = null };
            var entityWithEmptySynopsis = new HelpEntity { Synopsis = "" };

            // Act & Assert
            Assert.IsFalse(string.IsNullOrEmpty(entityWithSynopsis.Synopsis?.ToString()));
            Assert.IsTrue(string.IsNullOrEmpty(entityWithoutSynopsis.Synopsis?.ToString()));
            Assert.IsTrue(string.IsNullOrEmpty(entityWithEmptySynopsis.Synopsis?.ToString()));
        }

        [Test]
        public void TableEntity_CanStoreAndRetrieveSynopsis()
        {
            // This test simulates how Azure.Data.Tables handles the Synopsis property
            // Arrange
            var tableEntity = new TableEntity("CommandHelp", "get-test")
            {
                ["CommandName"] = "Get-Test",
                ["ModuleName"] = "TestModule",
                ["Synopsis"] = "This is a test synopsis"
            };

            // Verify the property is stored correctly
            Assert.IsTrue(tableEntity.ContainsKey("Synopsis"));
            Assert.AreEqual("This is a test synopsis", tableEntity["Synopsis"]);
        }
    }
}
