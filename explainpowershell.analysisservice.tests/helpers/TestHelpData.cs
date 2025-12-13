using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    internal static class TestHelpData
    {
        public static void SeedAboutTopics(InMemoryHelpRepository repository)
        {
            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Classes",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Classes"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Foreach",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Foreach"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_For",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_For"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Remote_Variables",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables",
                RelatedLinks = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Scopes",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Scopes"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Return",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Return"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Throw",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Throw"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_language_keywords",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_language_keywords"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_trap",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Trap"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Switch",
                DocumentationLink = "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Switch"
            });
        }
    }
}
