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

            var foundAsts = ast.FindAll(a => a is NamedBlockAst, true).Select(o => o as NamedBlockAst);

            try
            {
                foreach (var a in foundAsts)
                {
                    AstExplainer(a);
                }
            }
            catch (Exception e)
            {
                return ResponseHelper(HttpStatusCode.InternalServerError, e.Message);
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
                // case PipelineAst p:
                //     foreach (CommandBaseAst element in p.PipelineElements)
                //     {
                //         AstExplainer(ast);
                //     }
                //     break;
                // case CommandBaseAst cmd:
                //     CommandBaseExplainer(cmd);
                //     break;
                // case ExpressionAst expr:
                //     ExpressionExplainer(expr);
                //     break;
                // case StatementAst stmt:
                //     StatementExplainer(stmt);
                //     break;
                case NamedBlockAst namedBlock:
                    foreach (var stmt in namedBlock.Statements)
                    {
                        StatementExplainer(stmt);
                    }
                    // traps
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
                            CommandBaseExplainer(pipeline.PipelineElements);
                            break;
                        case PipelineChainAst pipelineChain:
                            PipelineBaseExplainer(pipelineChain.LhsPipelineChain);
                            CommandBaseExplainer(pipelineChain.RhsPipeline.PipelineElements);
                            break;
                    }
                    break;
                case AssignmentStatementAst assignmentStatement:
                    explanations.Add(
                        new Explanation()
                        {
                            Description = $"Operator {assignmentStatement.Operator}",
                            OriginalExtent = assignmentStatement.Extent.Text
                        });
                    ExpressionExplainer(assignmentStatement.Left);
                    StatementExplainer(assignmentStatement.Right);
                    break;
            }
        }

        private void CommandBaseExplainer(IEnumerable<CommandBaseAst> elements)
        {
            foreach (var element in elements)
            {
                CommandBaseExplainer(element);
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

                    TableQuery<HelpEntity> query = new TableQuery<HelpEntity>().Where(
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, resolvedCmd.ToLower()))); // Azure Table query does not support StringComparer.IgnoreOrdinalCase. RowKey command names are all stored lowercase.

                    var helpResult = cloudTable.ExecuteQuery(query).FirstOrDefault();
                    var description = helpResult?.Synopsis?.ToString() ?? "";

                    resolvedCmd = helpResult?.CommandName ?? resolvedCmd;

                    var bindResult = StaticParameterBinder.BindCommand(cmd);

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
            }
        }

        private void ExpressionExplainer(ExpressionAst argument)
        {
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
                    suffix = Helpers.TokenExplainer(binary.Operator);
                    explanation.Description = $"Operator {binary.Operator}. {suffix} See <a href=\"https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.language.tokenkind\">documentation</a>.";
                    explanation.OriginalExtent = binary.Extent.Text;
                    ExpressionExplainer(binary.Left);
                    ExpressionExplainer(binary.Right);
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
