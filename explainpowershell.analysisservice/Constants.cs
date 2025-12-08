namespace ExplainPowershell.SyntaxAnalyzer
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Azure Table Storage constants
        /// </summary>
        public static class TableStorage
        {
            /// <summary>
            /// The partition key used for command help entries in Azure Table Storage
            /// </summary>
            public const string CommandHelpPartitionKey = "CommandHelp";

            /// <summary>
            /// The default table name for help data
            /// </summary>
            public const string HelpDataTableName = "HelpData";

            /// <summary>
            /// Character used for filtering in range queries (ASCII 33 = '!')
            /// </summary>
            public const char RangeFilterChar = '!';

            /// <summary>
            /// Separator character used between command name and module name (ASCII 32 = ' ')
            /// </summary>
            public const char CommandModuleSeparator = ' ';
        }

        /// <summary>
        /// PowerShell documentation link constants
        /// </summary>
        public static class Documentation
        {
            public const string MicrosoftDocsBase = "https://docs.microsoft.com/en-us/powershell/scripting/lang-spec";
            public const string Chapter04TypeSystem = MicrosoftDocsBase + "/chapter-04";
            public const string Chapter04GenericTypes = Chapter04TypeSystem + "#44-generic-types";
            public const string Chapter08PipelineStatements = MicrosoftDocsBase + "/chapter-08#82-pipeline-statements";
        }

        /// <summary>
        /// PowerShell about topics
        /// </summary>
        public static class AboutTopics
        {
            public const string AboutClasses = "about_classes";
            public const string AboutEnum = "about_enum";
            public const string AboutFunctions = "about_functions";
            public const string AboutFunctionsCmdletBindingAttribute = "about_Functions_CmdletBindingAttribute";
            public const string AboutHashTables = "about_hash_tables";
            public const string AboutOperators = "about_operators";
            public const string AboutRedirection = "about_redirection";
            public const string AboutTypeAccelerators = "about_type_accelerators";
        }
    }
}
