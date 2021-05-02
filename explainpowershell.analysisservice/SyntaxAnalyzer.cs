using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using Microsoft.Azure.Cosmos.Table;

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
                        HelpResult = HelpTableQuery("if")
                    };
                    // todo: write code to add about_... articles to help db, remove stuff below:
                    if (expl.HelpResult == null)
                        expl.HelpResult = new HelpEntity();
                    expl.HelpResult.DocumentationLink = "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_if";
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

                    if (varName == "_" | string.Equals(varName, "PSItem", StringComparison.OrdinalIgnoreCase))
                        suffix = ", a built-in variable that holds the current element from the objects being passed in from the pipeline. See <a href=\"https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables\">Get-Help about_automatic_variables</a>).";

                    if (var.Splatted)
                        prefix = " <a href=\"https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_splatting\">splatted</a> ";
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
                            suffix = $" in '{identifier}' <a href=\"https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_scopes\">scope</a> ";

                        if (var.VariablePath.IsDriveQualified)
                        {
                            if (string.Equals(identifier, "Env", StringComparison.OrdinalIgnoreCase))
                            {
                                prefix = "n environment ";
                                suffix = " (on PSDrive 'env:')";
                            }
                            else {
                                standard = $"pointing to item '{varName}'";
                                suffix = $" on <a href=\"https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_providers\">PSDrive</a> '{identifier}:'";
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
            BinaryExpressionAst
                .Left -> ExpressionAst
                .Operator -> TokenKind
                .Right -> ExpressionAst
            ConstantExpressionAst
                .Value -> string
                StringConstantExpressionAst
                    .StringConstantType -> StringConstantType
            (ErrorExpressionAst)
            ExpandableStringExpressionAst
                .NestedExpressions -> ExpressionAst [always either VariableExpressionAst or SubExpressionAst]
                .StringConstantType -> StringConstantType
                .Value -> string
            HashtableAst
                .KeyValuePairs -> ReadOnlyCollection<Tuple<ExpressionAst,StatementAst>>
            IndexExpressionAst
                .Index, .Target -> ExpressionAst
            MemberExpressionAst
                .Expression -> ExpressionAst
                .Member -> CommandElementAst
                InvokeMemberExpressionAst
                    .Arguments -> ExpressionAst..
                    BaseCtorInvokeMemberExpressionAst
            ParenExpressionAst
                .Pipeline -> PipelineBaseAst
            ScriptBlockExpressionAst
                .ScriptBlock -> ScriptBlockAst
            SubExpressionAst
                .SubExpression -> StatementBlockAst
            TernaryExpressionAst
                .Condition, .IfFalse, .IfTrue -> ExpressionAst
            TypeExpressionAst
                .TypeName -> ITypeName
            UnaryExpressionAst
                .Child -> ExpressionAst
                .TokenKind -> Token
            UsingExpressionAst
                .SubExpression -> ExpressionAst
            */
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
