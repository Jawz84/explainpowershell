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
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Classes"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Foreach",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Foreach"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_For",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_For"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Remote_Variables",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables",
                RelatedLinks = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Remote_Variables"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Scopes",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Scopes"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Return",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Return"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_Throw",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Throw"
            });

            repository.AddHelpEntity(new HelpEntity
            {
                CommandName = "about_language_keywords",
                DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_language_keywords"
            });
        }
    }
}
