using System;
using System.Collections.Generic;
using System.Linq;
using explainpowershell.models;
using ExplainPowershell.SyntaxAnalyzer.Repositories;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    /// <summary>
    /// In-memory implementation of IHelpRepository for testing
    /// </summary>
    public class InMemoryHelpRepository : IHelpRepository
    {
        private readonly Dictionary<string, HelpEntity> helpData = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Add help data for testing
        /// </summary>
        public void AddHelpEntity(HelpEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            var key = entity.CommandName ?? string.Empty;
            if (!string.IsNullOrEmpty(entity.ModuleName))
            {
                key = $"{key} {entity.ModuleName}";
            }
            
            helpData[key] = entity;
        }

        /// <summary>
        /// Clear all help data
        /// </summary>
        public void Clear()
        {
            helpData.Clear();
        }

        /// <inheritdoc/>
        public HelpEntity GetHelpForCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return null;
            }

            // First try exact match
            if (helpData.TryGetValue(commandName, out var entity))
            {
                return entity;
            }

            // If not found, try to find a command with this name in any module
            var key = helpData.Keys.FirstOrDefault(k => 
                k.Equals(commandName, StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith($"{commandName} ", StringComparison.OrdinalIgnoreCase));
            
            return key != null ? helpData[key] : null;
        }

        /// <inheritdoc/>
        public HelpEntity GetHelpForCommand(string commandName, string moduleName)
        {
            if (string.IsNullOrEmpty(commandName) || string.IsNullOrEmpty(moduleName))
            {
                return null;
            }

            var key = $"{commandName} {moduleName}";
            return helpData.TryGetValue(key, out var entity) ? entity : null;
        }

        /// <inheritdoc/>
        public List<HelpEntity> GetHelpForCommandRange(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return new List<HelpEntity>();
            }

            return helpData
                .Where(kvp => kvp.Key.StartsWith(commandName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value)
                .ToList();
        }
    }
}
