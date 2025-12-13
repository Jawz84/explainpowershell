using Azure.Data.Tables;
using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer.Repositories
{
    /// <summary>
    /// Implementation of IHelpRepository using Azure Table Storage
    /// </summary>
    public class TableStorageHelpRepository : IHelpRepository
    {
        private readonly TableClient tableClient;

        public TableStorageHelpRepository(TableClient tableClient)
        {
            this.tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        }

        /// <inheritdoc/>
        public HelpEntity? GetHelpForCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return null;
            }

            string filter = TableServiceClient.CreateQueryFilter(
                $"PartitionKey eq {Constants.TableStorage.CommandHelpPartitionKey} and RowKey eq {commandName.ToLower()}");
            var entities = tableClient.Query<HelpEntity>(filter: filter);
            return entities.FirstOrDefault();
        }

        /// <inheritdoc/>
        public HelpEntity? GetHelpForCommand(string commandName, string moduleName)
        {
            if (string.IsNullOrEmpty(commandName) || string.IsNullOrEmpty(moduleName))
            {
                return null;
            }

            var rowKey = $"{commandName.ToLower()}{Constants.TableStorage.CommandModuleSeparator}{moduleName.ToLower()}";
            return GetHelpForCommand(rowKey);
        }

        /// <inheritdoc/>
        public List<HelpEntity> GetHelpForCommandRange(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return new List<HelpEntity>();
            }

            // Getting a range from Azure Table storage works based on ascii char filtering. You can match prefixes. 
            // We use a space ' ' (char)32 as a divider between the name of a command and the name of its module 
            // for commands that appear in more than one module. Filtering this way makes sure we only match 
            // entries with '<myCommandName> <myModuleName>'.
            // filterChar = (char)33 = '!'.
            string rowKeyFilter = $"{commandName.ToLower()}{Constants.TableStorage.RangeFilterChar}";
            string filter = TableServiceClient.CreateQueryFilter(
                $"PartitionKey eq {Constants.TableStorage.CommandHelpPartitionKey} and RowKey ge {commandName.ToLower()} and RowKey lt {rowKeyFilter}");
            var entities = tableClient.Query<HelpEntity>(filter: filter);
            return entities.ToList();
        }
    }
}
