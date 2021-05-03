using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using explainpowershell.models;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public class SyntaxAnalyzer
    {
        private const string HelpTableName = "HelpData";
        private const string PartitionKey = "CommandHelp";
        private string extent;
        private ILogger Log;
        private readonly List<Explanation> explanations = new List<Explanation>();
        private CloudTable cloudTable;
        private int offSet = 0;
        private static readonly PowerShell powerShell = PowerShell.Create();
        private Dictionary<String, String> AliasToCmdletDictionary;
        private CommandInvocationIntrinsics InvokeCommand { get => powerShell.Runspace.SessionStateProxy.InvokeCommand; }

        [FunctionName("SyntaxAnalyzer")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Table(HelpTableName)] CloudTable cloudTbl,
            ILogger log)
        {
            cloudTable = cloudTbl;
            Log = log;
            var AnalysisResult = new AnalysisResult();
            var modules = new List<Module>();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var code = JsonConvert
                .DeserializeObject<Code>(requestBody)
                ?.PowershellCode;

            ScriptBlockAst ast = Parser.ParseInput(code, out Token[] tokens, out ParseError[] parseErrors);

            extent = ast.Extent.ToString();

            if (string.IsNullOrEmpty(extent))
                return ResponseHelper(HttpStatusCode.BadRequest, "Empty request. Pass powershell code in the request body for an AST analysis.");

            var filteredTokens = tokens.Where(o => !o.TokenFlags.HasFlag(TokenFlags.ParseModeInvariant));

            var foundAsts = ast.FindAll(a => a is ScriptBlockAst, true).Select(o => o as ScriptBlockAst);

            try
            {
                foreach (var a in foundAsts)
                {
                    foreach (var u in a.UsingStatements)
                        AstExplainer(u);

                    foreach (var att in a.Attributes)
                        AstExplainer(att);

                    AstExplainer(a.ParamBlock);
                    AstExplainer(a.BeginBlock);
                    AstExplainer(a.DynamicParamBlock);
                    AstExplainer(a.ProcessBlock);
                    AstExplainer(a.EndBlock);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "error");
                return ResponseHelper(HttpStatusCode.InternalServerError, "Oops, someting went wrong internally. Please file an issue with the PowerShell code you submitted when this occurred.");
            }

            foreach (var exp in explanations)
            {
                if (exp.HelpResult == null)
                    continue;

                if (!modules.Any(m => m.ModuleName == exp.HelpResult.ModuleName))
                {
                    modules.Add(
                        new Module()
                        {
                            ModuleName = exp.HelpResult.ModuleName
                        });
                }
            }

            AnalysisResult.ExpandedCode = extent;
            AnalysisResult.Explanations = explanations;
            AnalysisResult.DetectedModules = modules;
            AnalysisResult.ParseErrorMessage = parseErrors?.FirstOrDefault()?.Message;

            var json = System.Text.Json.JsonSerializer.Serialize(AnalysisResult);

            return ResponseHelper(HttpStatusCode.OK, json, "application/json");
        }

        private void AstExplainer(Ast ast)
        {
            switch (ast)
            {
                case AttributeBaseAst attributeBase:
                    switch (attributeBase)
                    {
                        case AttributeAst attribute:
                            // todo: NamedAttributeExpressionAst;
                            foreach (var args in attribute.PositionalArguments)
                                ExpressionExplainer(args);
                            // todo add Attribute explanation 
                            break;
                        case TypeConstraintAst typeConstraint:
                            // todo add TypeConstraintAst explanation
                            break;
                        default:
                            AstExplainer(attributeBase);
                            Log.LogWarning($"unhandled ast: {attributeBase.GetType()}, extent {extent}");
                            break;
                    }
                    break;
                case NamedBlockAst namedBlock:
                    foreach (var stmt in namedBlock.Statements)
                        StatementExplainer(stmt);
                    // todo: traps
                    break;
                case ParamBlockAst paramBlock:
                    foreach (var a in paramBlock.Attributes)
                        AstExplainer(a);
                    foreach (var p in paramBlock.Parameters)
                        ParameterExplainer(p);
                    break;
                case Ast e:
                    explanations.Add(
                        new Explanation()
                        {
                            OriginalExtent = e.Extent.Text,
                            Description = ast.GetType().Name.Replace("Ast", "")
                        });
                    break;
            }
        }

        private void ParameterExplainer(ParameterAst p)
        {
            //todo write implementation;
            AstExplainer(p);
        }

        private void StatementExplainer(StatementAst stmt)
        {
            switch (stmt)
            {
                case CommandBaseAst cmd:
                    CommandBaseExplainer(cmd);
                    break;
                case PipelineBaseAst pipelineBase:
                    PipelineBaseExplainer(pipelineBase);
                    break;
                case IfStatementAst ifStatement:
                    var expl = new Explanation()
                    {
                        OriginalExtent = ifStatement.Extent.Text,
                        Description = "if-statement, run statement lists based on the results of one or more conditional tests",
                        CommandName = "if-statement",
                        HelpResult = HelpTableQuery("about_if")
                    };

                    explanations.Add(expl);

                    foreach (var clause in ifStatement.Clauses)
                    {
                        var (pipelineBase, StatementBlock) = clause;
                        PipelineBaseExplainer(pipelineBase);
                        foreach (var statement in StatementBlock.Statements)
                            StatementExplainer(statement);
                        // todo: traps
                    }
                    if (ifStatement.ElseClause != null)
                        foreach (var statement in ifStatement.ElseClause.Statements)
                            StatementExplainer(statement);
                    break;
                default:
                    AstExplainer(stmt);
                    Log.LogWarning($"unhandled ast: {stmt.GetType()}, extent {extent}");
                    break;
            }

            /*
            (BlockStatementAst
                [workflows]
                .Body -> StatementBlockAst
                .Kind -> Token)
            BreakStatementAst
                .Label -> ExpressionAst
            CommandBaseAst
                .Redirections -> RedirectionAst
                CommandAst
                    .CommandElements -> CommandElementAst
                CommandExpressionAst
                    .Expression -> ExpressionAst
            (ConfigurationDefinitionAst
                [DSC])
            ContinueStatementAst
                .Label -> ExpressionAst
            DataStatementAst
                .Body -> StatementBlockAst
                .CommandsAllowed -> ExpressionAst..
                .Variable -> string
            DynamicKeywordStatementAst
                .CommandElements -> CommandElementAst..
            ExitStatementAst
                .Pipeline -> PipelineBaseAst
            FunctionDefinitionAst
                .Body -> ScriptBlockAst
                .Name -> string
                .Parameters -> ParameterAst..
            IfStatementAst
                .Clauses -> ReadOnlyCollection<Tuple<PipelineBaseAst,StatementBlockAst>>
                .ElseClause -> StatementBlockAst
            LabeledStatementAst
                .Label -> string
                .Condition -> PipelineBaseAst
                LoopStatementAst
                    DoUntilStatementAst
                    DoWhileStatementAst
                    ForEachStatementAst
                    ForStatementAst
                    WhileStatementAst
                SwitchStatementAst
                    .Clauses -> ReadOnlyCollection<Tuple<ExpressionAst,StatementBlockAst>>
                    .Condition -> PipelineBaseAst
                    .Default -> StatementBlockAst
            PipelineBaseAst
                AssignmentStatementAst
                    .Left -> ExpressionAst
                    .Operator -> TokenKind
                    .Right -> StatementAst
                ChainableAst
                    PipelineAst
                        .PipelineElements -> CommandBaseAst
                    PipelineChainAst
                        .LhsPipelineChain -> ChainableAst
                        .RhsPipeline -> PipelineAst
                (ErrorStatementAst
                    .Bodies -> Ast..
                    .Conditions -> Ast..)
            ReturnStatementAst
                .Pipeline -> PipelineBaseAst
            ThrowStatementAst
                .Pipeline -> PipelineBaseAst
            TrapStatementAst
                .TrapType -> TypeConstraintAst
            TryStatementAst
                .Body -> StatementBlockAst
                .CatchClauses -> CatchClauseAst..
                .Finally -> StatementBlockAst
            (TypeDefinitionAst
                [class, enum, interface])
            UsingStatementAst
                .Name, .Alias -> StringConstantExpressionAst
                .ModuleSpecification -> HashtableAst
            */
        }

        private void PipelineBaseExplainer(PipelineBaseAst pipelineBase)
        {
            switch (pipelineBase)
            {
                case ChainableAst chainable:
                    switch (chainable)
                    {
                        case PipelineAst pipeline:
                            foreach (var elem in pipeline.PipelineElements)
                                CommandBaseExplainer(elem);
                            break;
                        case PipelineChainAst pipelineChain:
                            PipelineBaseExplainer(pipelineChain.LhsPipelineChain);
                            foreach (var elem in pipelineChain.RhsPipeline.PipelineElements)
                                CommandBaseExplainer(elem);
                            break;
                        default:
                            AstExplainer(pipelineBase);
                            Log.LogWarning($"unhandled ast: {pipelineBase.GetType()}, extent {extent}");
                            break;
                    }
                    break;
                case AssignmentStatementAst assignmentStatement:
                    var operatorExplanation = Helpers.TokenExplainer(assignmentStatement.Operator);
                    explanations.Add(
                        new Explanation()
                        {
                            Description = $"{operatorExplanation} Assigns a value to '{assignmentStatement.Left.Extent.Text}'.",
                            OriginalExtent = assignmentStatement.Extent.Text
                        });
                    ExpressionExplainer(assignmentStatement.Left);
                    StatementExplainer(assignmentStatement.Right);
                    break;
                default:
                    AstExplainer(pipelineBase);
                    Log.LogWarning($"unhandled ast: {pipelineBase.GetType()}, extent {extent}");
                    break;
            }
        }

        private void CommandBaseExplainer(CommandBaseAst commandBase)
        {
            switch (commandBase)
            {
                case CommandAst element:
                    var cmd = element as CommandAst;
                    string cmdName = cmd.GetCommandName();
                    string resolvedCmd = ResolveCmd(cmdName);
                    if (string.IsNullOrEmpty(resolvedCmd))
                    {
                        resolvedCmd = cmdName;
                    }

                    HelpEntity helpResult = HelpTableQuery(resolvedCmd);
                    var description = helpResult?.Synopsis?.ToString() ?? "";

                    resolvedCmd = helpResult?.CommandName ?? resolvedCmd;

                    // TODO: Create something better for this. BindCommand only binds commands that are loaded, and it's slow.
                    // I've set it to not resolve, because that speeds things up, and a lot of the times it won't matter.
                    var bindResult = StaticParameterBinder.BindCommand(cmd, false);

                    StringBuilder boundParameters = new StringBuilder();
                    foreach (var p in bindResult.BoundParameters.Values)
                    {
                        if (p.Parameter != null)
                        {
                            boundParameters
                                .Append(" -")
                                .Append(p.Parameter.Name);

                            if (!p.Parameter.SwitchParameter)
                            {
                                boundParameters
                                    .Append(' ')
                                    .Append(p.Value.Extent.Text);
                                CommandElementExplainer(p.Value);
                            }
                        }
                        else
                        {
                            boundParameters
                                .Append(' ')
                                .Append(p.Value.Extent.Text);
                            CommandElementExplainer(p.Value);
                        }
                    }

                    ExpandAliasesInExtent(cmd, resolvedCmd);

                    explanations.Add(
                        new Explanation()
                        {
                            OriginalExtent = cmd.Extent.Text,
                            CommandName = resolvedCmd + boundParameters.ToString(),
                            Description = description,
                            HelpResult = helpResult
                        });
                    break;
                case CommandExpressionAst element:
                    ExpressionExplainer(element.Expression);
                    break;
                default:
                    AstExplainer(commandBase);
                    Log.LogWarning($"unhandled ast: {commandBase.GetType()}, extent {extent}");
                    break;
            }
        }

        private void CommandElementExplainer(CommandElementAst value)
        {
            switch (value)
            {
                case CommandParameterAst param:
                    ExpressionExplainer(param.Argument);
                    break;
                case ExpressionAst expr:
                    ExpressionExplainer(expr);
                    break;
                default:
                    AstExplainer(value);
                    Log.LogWarning($"unhandled ast: {value.GetType()}, extent {extent}");
                    break;
            }
        }

        private void ExpressionExplainer(ExpressionAst argument)
        {
            if (argument == null)
                return;

            var explanation = new Explanation();
            switch (argument)
            {
                case VariableExpressionAst var:
                    var prefix = " ";
                    var suffix = "";
                    var varName = var.VariablePath.UserPath;
                    var standard = $"named '{varName}'";

                    explanation.CommandName = "Variable";
                    explanation.HelpResult = HelpTableQuery("about_variables");

                    if (HasSpecialVars(varName))
                    {
                        if (string.Equals(varName, SpecialVars.PSDefaultParameterValues, StringComparison.OrdinalIgnoreCase))
                        {
                            suffix = ", a special variable to set parameter default values.";
                            explanation.CommandName = "PSDefaultParameterValues"; 
                            explanation.HelpResult = HelpTableQuery("about_Parameters_Default_Values");
                        }
                        else if (SpecialVars.AutomaticVariables.Contains(varName, StringComparer.OrdinalIgnoreCase)) 
                        {
                            suffix = ", an automatic variable.";
                            explanation.CommandName = "Automatic variable";
                            explanation.HelpResult = HelpTableQuery("about_automatic_variables");
                        } 
                        else if (SpecialVars.PreferenceVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
                        {
                            suffix = ", a preference variable.";
                            explanation.CommandName = "Preference variable";
                            explanation.HelpResult = HelpTableQuery("about_Preference_variables");
                        }
                        else
                        {
                            suffix = ", a special variable.";
                            explanation.CommandName = "Special variable";
                            explanation.HelpResult = HelpTableQuery("about_automatic_variables");
                        }
                    }

                    if (varName == "_" | string.Equals(varName, "PSItem", StringComparison.OrdinalIgnoreCase)) 
                    {
                        suffix = ", a built-in variable that holds the current element from the objects being passed in from the pipeline.";
                        explanation.CommandName = "Pipeline iterator variable";
                        explanation.HelpResult = HelpTableQuery("about_automatic_variables");
                    }

                    if (var.Splatted)
                    {
                        prefix = " splatted ";
                        explanation.CommandName = "Splatted variable";
                        explanation.HelpResult = HelpTableQuery("about_splatting");
                    }

                    if (var.VariablePath.IsPrivate)
                        prefix = $" private ";

                    if (!var.VariablePath.IsUnscopedVariable)
                    {
                        var split = var
                            .VariablePath
                            .UserPath
                            .Split(':');

                        var identifier = split.FirstOrDefault();
                        varName = split.LastOrDefault();
                        standard = $"named '{varName}'";

                        if (var.VariablePath.IsGlobal | var.VariablePath.IsScript)
                        {
                            suffix = $" in '{identifier}' scope ";
                            explanation.CommandName = "Scoped variable";
                            explanation.HelpResult = HelpTableQuery("about_scopes");
                        }


                        if (var.VariablePath.IsDriveQualified)
                        {
                            if (string.Equals(identifier, "Env", StringComparison.OrdinalIgnoreCase))
                            {
                                prefix = "n environment ";
                                suffix = " (on PSDrive 'env:')";
                                explanation.CommandName = "Environment variable";
                                explanation.HelpResult = HelpTableQuery("about_Environment_Variables");
                            }
                            else
                            {
                                standard = $"pointing to item '{varName}'";
                                suffix = $" on PSDrive '{identifier}:'";
                                explanation.CommandName = "PSDrive (Providers)";
                                explanation.HelpResult = HelpTableQuery("about_Providers");
                            }
                        }
                    }

                    explanation.OriginalExtent = $"${var.VariablePath}";
                    explanation.Description = $"A{prefix}variable {standard}{suffix}";
                    break;
                case BinaryExpressionAst binary:
                    explanation.Description = Helpers.TokenExplainer(binary.Operator);
                    explanation.OriginalExtent = binary.Extent.Text;
                    ExpressionExplainer(binary.Left);
                    ExpressionExplainer(binary.Right);
                    break;
                case UnaryExpressionAst unaryExpression:
                    explanation.Description = Helpers.TokenExplainer(unaryExpression.TokenKind);
                    explanation.OriginalExtent = unaryExpression.Extent.Text;
                    ExpressionExplainer(unaryExpression.Child);
                    break;
                case ConstantExpressionAst constantExpression:
                    explanation.OriginalExtent = constantExpression.Extent.Text;
                    switch (constantExpression)
                    {
                        case StringConstantExpressionAst stringConstantExpression:
                            var hasDollarSign = stringConstantExpression.Value.IndexOf('$') >= 0;
                            switch (stringConstantExpression.StringConstantType)
                            {
                                case StringConstantType.SingleQuoted:
                                    explanation.Description = "String in which variables will not be expanded.";
                                    break;
                                case StringConstantType.SingleQuotedHereString:
                                    explanation.Description = "Multiline here-string in which variables will not be expanded.";
                                    break;
                                case StringConstantType.DoubleQuoted:
                                    if (hasDollarSign)
                                    {
                                        explanation.Description = "String in which variables will be expanded.";
                                    }
                                    else
                                    {
                                        explanation.Description = "String in which variables would be expanded.";
                                    }
                                    break;
                                case StringConstantType.DoubleQuotedHereString:
                                    if (hasDollarSign)
                                    {
                                        explanation.Description = "Multiline here-string in which variables will be expanded.";
                                    }
                                    else
                                    {
                                        explanation.Description = "Multiline here-string in which variables would be expanded.";
                                    }
                                    break;
                                case StringConstantType.BareWord:
                                    explanation.Description = "String without quotation.";
                                    break;
                            }
                            explanation.CommandName = stringConstantExpression.StringConstantType.ToString();
                            explanation.HelpResult = HelpTableQuery("about_quoting_rules");
                            break;
                        default:
                            explanation.CommandName = "Numeric literal";
                            explanation.HelpResult = HelpTableQuery("about_Numeric_Literals");
                            var numberString = constantExpression.Extent.Text.ToString();
                            var rg = new Regex(@"[a-zA-Z]");

                            if (numberString.StartsWith("0b", true, null))
                            {
                                explanation.Description = $"Binary number (value: {constantExpression.SafeGetValue()})";
                            }
                            else if (numberString.StartsWith("0x", true, null))
                            {
                                explanation.Description = $"Hexadecimal number (value: {constantExpression.SafeGetValue()})";
                            }
                            else if (rg.IsMatch(constantExpression.Extent.Text))
                            {
                                explanation.Description = $"Number (value: {constantExpression.SafeGetValue()})";
                            }
                            else
                            {
                                explanation.Description = "Number";
                            }
                            break;
                    }
                    break;
                case ExpandableStringExpressionAst expandableStringExpression:
                    explanation.OriginalExtent = expandableStringExpression.Extent.Text;
                    explanation.CommandName = expandableStringExpression.StringConstantType.ToString();
                    explanation.HelpResult = HelpTableQuery("about_quoting_rules");

                    var items = new StringBuilder();
                    items.AppendJoin(", ", expandableStringExpression.NestedExpressions.Select(n => n.Extent.Text));
                    explanation.Description = $"String with expandable elements: {items}";

                    foreach (var exp in expandableStringExpression.NestedExpressions)
                        ExpressionExplainer(exp);

                    break;
                case MemberExpressionAst memberExpression:
                    explanation.OriginalExtent = memberExpression.Extent.Text;

                    switch (memberExpression)
                    {
                        case InvokeMemberExpressionAst invokeMemberExpression:
                            var args = "";
                            var argsText = " without arguments";
                            var objectOrClass = "object"; 
                            var stat = "";

                            if (invokeMemberExpression.Arguments != null)
                                args = string.Join(", ", invokeMemberExpression.Arguments.Select(args => args.Extent.Text));

                            if (invokeMemberExpression.Static){
                                objectOrClass = "class";
                                stat = "static ";
                            }

                            if (args != "")
                                argsText = $", with arguments '{args}'";

                            explanation.Description = $"Invoke the {stat}method '{invokeMemberExpression.Member}' on {objectOrClass} '{invokeMemberExpression.Expression}'{argsText}.";
                            explanation.CommandName = "Method";
                            explanation.HelpResult = HelpTableQuery("about_Methods");

                            if (invokeMemberExpression.Arguments != null)
                                foreach (var a in invokeMemberExpression.Arguments)
                                    ExpressionExplainer(a);

                            // ignoring sub class: BaseCtorInvokeMemberExpressionAst
                            break;
                        default:
                            explanation.Description = $"Access the property '{memberExpression.Member}' on object '{memberExpression.Expression}'";
                            explanation.CommandName = "Property";
                            explanation.HelpResult = HelpTableQuery("about_Properties");
                            break;
                    }
                    ExpressionExplainer(memberExpression.Expression);
                    if ((memberExpression.Member as StringConstantExpressionAst).StringConstantType != StringConstantType.BareWord)
                        CommandElementExplainer(memberExpression.Member);
                    break;
                case ParenExpressionAst parenExpression:
                    PipelineBaseExplainer(parenExpression.Pipeline);
                    break;
                case SubExpressionAst subExpression:
                    foreach (var s in subExpression.SubExpression.Statements)
                        StatementExplainer(s);
                    break;
                default:
                    AstExplainer(argument);
                    Log.LogWarning($"unhandled ast: {argument.GetType()}, extent {extent}");
                    break;
            }
            if (!string.IsNullOrEmpty(explanation.Description))
                explanations.Add(explanation);
            /*
            ArrayExpressionAst
                .SubExpression -> StatementBlockAst
            ArrayLiteralAst
                .Elements -> ExpressionAst..
            AttributedExpressionAst
                [like [Parameter()]$PassThru or [ValidateScript({$true})$abc = 42.]
                .Attribute -> AttributeBaseAst
                .Child -> ExpressionAst
                ConvertExpressionAst
                    [cast expression]
                    .StaticType -> Type
                    .Type -> TypeConstraintAst
            HashtableAst
                .KeyValuePairs -> ReadOnlyCollection<Tuple<ExpressionAst,StatementAst>>
            IndexExpressionAst
                .Index, .Target -> ExpressionAst
            ScriptBlockExpressionAst
                .ScriptBlock -> ScriptBlockAst

            TernaryExpressionAst
                .Condition, .IfFalse, .IfTrue -> ExpressionAst
            TypeExpressionAst
                .TypeName -> ITypeName
            UsingExpressionAst
                .SubExpression -> ExpressionAst
            */
        }

        private bool HasSpecialVars(string varName)
        {
            if (SpecialVars.InitializedVariables.Contains(varName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private HelpEntity HelpTableQuery(string resolvedCmd)
        {
            TableQuery<HelpEntity> query = new TableQuery<HelpEntity>()
                .Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, resolvedCmd.ToLower()))); // Azure Table query does not support StringComparer.IgnoreOrdinalCase. RowKey command names are all stored lowercase.

            var helpResult = cloudTable.ExecuteQuery(query).FirstOrDefault();
            return helpResult;
        }

        private HttpResponseMessage ResponseHelper(HttpStatusCode status, string message, string mediaType = "text/plain")
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(message, Encoding.UTF8, mediaType)
            };
        }

        private void ExpandAliasesInExtent(CommandAst cmd, string resolvedCmd)
        {
            int start = offSet + cmd.Extent.StartOffset;
            int length = offSet + cmd.CommandElements[0].Extent.EndOffset - start;
            extent = extent
                .Remove(start, length)
                .Insert(start, resolvedCmd);

            offSet = offSet + resolvedCmd.Length - length;
        }

        private string ResolveCmd(string cmdName)
        {
            if (AliasToCmdletDictionary == null)
            {
                InitializeAliasDictionary();
            }

            if (AliasToCmdletDictionary.ContainsKey(cmdName))
            {
                return AliasToCmdletDictionary[cmdName];
            }

            return InvokeCommand.GetCommandName(cmdName, false, false).FirstOrDefault();
        }

        public void InitializeAliasDictionary()
        {
            AliasToCmdletDictionary = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<CommandInfo> aliases = InvokeCommand.GetCommands("*", CommandTypes.Alias, true);

            foreach (AliasInfo aliasInfo in aliases)
            {
                AliasToCmdletDictionary.Add(aliasInfo.Name, aliasInfo.Definition);
            }
        }
    }
}
