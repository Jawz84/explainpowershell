using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer.Repositories
{
    /// <summary>
    /// Repository interface for accessing PowerShell help data
    /// </summary>
    public interface IHelpRepository
    {
        /// <summary>
        /// Query help data for a specific command
        /// </summary>
        /// <param name="commandName">The command name to query</param>
        /// <returns>The help entity if found, null otherwise</returns>
        HelpEntity? GetHelpForCommand(string commandName);

        /// <summary>
        /// Query help data for a specific command in a specific module
        /// </summary>
        /// <param name="commandName">The command name to query</param>
        /// <param name="moduleName">The module name containing the command</param>
        /// <returns>The help entity if found, null otherwise</returns>
        HelpEntity? GetHelpForCommand(string commandName, string moduleName);

        /// <summary>
        /// Query help data for commands matching a prefix
        /// </summary>
        /// <param name="commandName">The command name prefix to query</param>
        /// <returns>List of matching help entities</returns>
        List<HelpEntity> GetHelpForCommandRange(string commandName);
    }
}
