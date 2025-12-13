using System.Linq;
using explainpowershell.models;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    [TestFixture]
    public class InMemoryHelpRepositoryTests
    {
        private InMemoryHelpRepository repository;

        [SetUp]
        public void Setup()
        {
            repository = new InMemoryHelpRepository();
        }

        [Test]
        public void GetHelpForCommand_WithValidCommand_ReturnsEntity()
        {
            // Arrange
            var entity = new HelpEntity
            {
                CommandName = "Get-Process",
                Synopsis = "Gets the processes running on the local computer.",
                ModuleName = "Microsoft.PowerShell.Management"
            };
            repository.AddHelpEntity(entity);

            // Act
            var result = repository.GetHelpForCommand("get-process");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Get-Process", result.CommandName);
        }

        [Test]
        public void GetHelpForCommand_WithInvalidCommand_ReturnsNull()
        {
            // Act
            var result = repository.GetHelpForCommand("NonExistentCommand");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetHelpForCommand_WithModule_ReturnsCorrectEntity()
        {
            // Arrange
            var entity1 = new HelpEntity
            {
                CommandName = "Test-Command",
                ModuleName = "Module1",
                Synopsis = "Test command from Module1"
            };
            var entity2 = new HelpEntity
            {
                CommandName = "Test-Command",
                ModuleName = "Module2",
                Synopsis = "Test command from Module2"
            };
            repository.AddHelpEntity(entity1);
            repository.AddHelpEntity(entity2);

            // Act
            var result1 = repository.GetHelpForCommand("Test-Command", "Module1");
            var result2 = repository.GetHelpForCommand("Test-Command", "Module2");

            // Assert
            Assert.IsNotNull(result1);
            Assert.AreEqual("Module1", result1.ModuleName);
            Assert.IsNotNull(result2);
            Assert.AreEqual("Module2", result2.ModuleName);
        }

        [Test]
        public void GetHelpForCommandRange_WithMatchingPrefix_ReturnsMultipleEntities()
        {
            // Arrange
            repository.AddHelpEntity(new HelpEntity { CommandName = "Get-Process" });
            repository.AddHelpEntity(new HelpEntity { CommandName = "Get-Service" });
            repository.AddHelpEntity(new HelpEntity { CommandName = "Set-Service" });

            // Act
            var result = repository.GetHelpForCommandRange("Get-");

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(e => e.CommandName == "Get-Process"));
            Assert.IsTrue(result.Any(e => e.CommandName == "Get-Service"));
        }

        [Test]
        public void GetHelpForCommandRange_WithNoMatches_ReturnsEmptyList()
        {
            // Arrange
            repository.AddHelpEntity(new HelpEntity { CommandName = "Get-Process" });

            // Act
            var result = repository.GetHelpForCommandRange("Set-");

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Clear_RemovesAllEntities()
        {
            // Arrange
            repository.AddHelpEntity(new HelpEntity { CommandName = "Get-Process" });
            repository.AddHelpEntity(new HelpEntity { CommandName = "Get-Service" });

            // Act
            repository.Clear();
            var result = repository.GetHelpForCommand("Get-Process");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetHelpForCommand_IsCaseInsensitive()
        {
            // Arrange
            var entity = new HelpEntity { CommandName = "Get-Process" };
            repository.AddHelpEntity(entity);

            // Act
            var result1 = repository.GetHelpForCommand("GET-PROCESS");
            var result2 = repository.GetHelpForCommand("get-process");
            var result3 = repository.GetHelpForCommand("Get-Process");

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(result3);
            Assert.AreEqual(result1.CommandName, result2.CommandName);
            Assert.AreEqual(result2.CommandName, result3.CommandName);
        }
    }
}
