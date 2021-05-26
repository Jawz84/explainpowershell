$graph = graph {
    edge Ast -To Ast.Parent, Ast.Extent, AttributeBaseAst, CatchClauseAst, CommandElementAst, MemberAst, NamedAttributeArgumentAst, NamedBlockAst, ParamBlockAst, ParameterAst, RedirectionAst, ScriptBlockAst, StatementBlockAst;
        edge Ast.Parent -to Ast
        edge Ast.Extent -to IScriptExtent
            # edge IScriptExtent
        edge AttributeBaseAst -to AttributeAst, TypeConstraintAst
            edge AttributeAst -to AttributeAst.NamedArguments, AttributeAst.PositionalArguments
                edge AttributeAst.NamedArguments -to NamedAttributeExpressionAst
                edge AttributeAst.PositionalArguments -to ExpressionAst
        edge CatchClauseAst -to CatchClauseAst.Body, CatchClauseAst.CatchTypes, CatchClauseAst.IsCatchAll
            edge CatchClauseAst.Body -to StatementBlockAst
            edge CatchClauseAst.CatchTypes -To TypeConstraintAst
        edge CommandElementAst -to CommandParameterAst, ExpressionAst
            edge CommandParameterAst -to CommandParameterAst.Argument, CommandParameterAst.ParameterName
                edge CommandParameterAst.Argument -to ExpressionAst
                # edge CommandParameterAst.ParameterName -to string
                    # edge string
            edge ExpressionAst -to ExpressionAst.StaticType, ArrayExpressionAst, ArrayLiteralAst, AttributedExpressionAst, BinaryExpressionAst, ConstantExpressionAst, '(ErrorExpressionAst)', ExpandableStringExpressionAst, HashtableAst, IndexExpressionAst, MemberExpressionAst, ParenExpressionAst, ScriptBlockExpressionAst, SubExpressionAst, TernaryExpressionAst, TypeExpressionAst, UnaryExpressionAst, UsingExpressionAst, VariableExpressionAst
                edge ArrayExpressionAst -to ArrayExpressionAst.SubExpression
                    edge ArrayExpressionAst.SubExpression -to StatementBlockAst
                edge ArrayLiteralAst -to ArrayLiteralAst.Elements
                    edge ArrayLiteralAst.Elements -to ExpressionAst
                edge AttributedExpressionAst -to AttributedExpressionAst.Attribute, AttributedExpressionAst.Child, ConvertExpressionAst
                    edge AttributedExpressionAst.Attribute -to AttributeBaseAst
                    edge AttributedExpressionAst.Child -to ExpressionAst
                    edge ConvertExpressionAst -to ConvertExpressionAst.StaticType, ConvertExpressionAst.Type
                        # edge ConvertExpressionAst.StaticType -to Type
                            # edge Type
                        edge ConvertExpressionAst.Type -to TypeConstraintAst
                edge BinaryExpressionAst -to BinaryExpressionAst.Left, BinaryExpressionAst.Operator, BinaryExpressionAst.Right
                    edge BinaryExpressionAst.Left -to ExpressionAst
                    # edge BinaryExpressionAst.Operator -to TokenKind
                        # edge TokenKind
                    edge BinaryExpressionAst.Right -to ExpressionAst
                edge ConstantExpressionAst -to ConstantExpressionAst.Value, StringConstantExpressionAst
                    # edge ConstantExpressionAst.Value -to string
                    # edge StringConstantExpressionAst -to StringConstantExpressionAst.StringConstantType
                        # edge StringConstantType
                # edge '(ErrorExpressionAst)'
                edge ExpandableStringExpressionAst -to ExpandableStringExpressionAst.NestedExpressions, ExpandableStringExpressionAst.StringConstantType, ExpandableStringExpressionAst.Value
                    edge ExpandableStringExpressionAst.NestedExpressions -to VariableExpressionAst, SubExpressionAst
                    edge ExpandableStringExpressionAst.StringConstantType -to StringConstantType
                    # edge ExpandableStringExpressionAst.Value -to string
                edge HashtableAst -to HashtableAst.KeyValuePairs
                    edge HashtableAst.KeyValuePairs -to ExpressionAst, StatementAst
                edge IndexExpressionAst -to IndexExpressionAst.Index, IndexExpressionAst.Target
                    edge IndexExpressionAst.Index, IndexExpressionAst.Target -to ExpressionAst
                edge MemberExpressionAst -to MemberExpressionAst.Expression, MemberExpressionAst.Member, InvokeMemberExpressionAst
                    edge MemberExpressionAst.Expression -to ExpressionAst
                    edge MemberExpressionAst.Member -to CommandElementAst
                    edge InvokeMemberExpressionAst -to InvokeMemberExpressionAst.Arguments, BaseCtorInvokeMemberExpressionAst
                        edge InvokeMemberExpressionAst.Arguments -to ExpressionAst
                        edge BaseCtorInvokeMemberExpressionAst
                edge ParenExpressionAst -to ParenExpressionAst.Pipeline
                    edge ParenExpressionAst.Pipeline -to PipelineBaseAst
                edge ScriptBlockExpressionAst -to ScriptBlockExpressionAst.ScriptBlock
                    edge ScriptBlockExpressionAst.ScriptBlock -to ScriptBlockAst
                edge SubExpressionAst -to SubExpressionAst.SubExpression
                    edge SubExpressionAst.SubExpression -to StatementBlockAst
                edge TernaryExpressionAst -to TernaryExpressionAst.Condition, TernaryExpressionAst.IfFalse, TernaryExpressionAst.IfTrue
                    edge TernaryExpressionAst.Condition, TernaryExpressionAst.IfFalse, TernaryExpressionAst.IfTrue -to ExpressionAst
                edge TypeExpressionAst -to TypeExpressionAst.TypeName
                    # edge TypeExpressionAst.TypeName -to ITypeName
                        # edge ITypeName
                edge UnaryExpressionAst -to UnaryExpressionAst.Child, UnaryExpressionAst.TokenChild
                    edge UnaryExpressionAst.Child -to ExpressionAst
                    # edge UnaryExpressionAst.TokenChild -to Token
                        # edge Token
                edge UsingExpressionAst -to UsingExpressionAst.SubExpression
                    edge UsingExpressionAst.SubExpression -to ExpressionAst
                edge VariableExpressionAst -to VariableExpressionAst.Splatted, VariableExpressionAst.VariablePath
                    # edge VariableExpressionAst.Splatted -to bool
                        # edge bool
                    # edge VariableExpressionAst.VariablePath -to VariablePath
                        # edge VariablePath
        edge MemberAst -to MemberAst.Name, FunctionMemberAst, PropertyMemberAst
            # edge MemberAst.Name -to string
            edge FunctionMemberAst -to FunctionMemberAst.Attributes, FunctionMemberAst.Body, FunctionMemberAst.Name, FunctionMemberAst.Parameters, FunctionMemberAst.ReturnType
                edge FunctionMemberAst.Attributes -to AttributeAst
                edge FunctionMemberAst.Body -To ScriptBlockAst
                # edge FunctionMemberAst.Name -to string
                edge FunctionMemberAst.Parameters -to ParameterAst 
                edge FunctionMemberAst.ReturnType -to TypeConstraintAst
            edge PropertyMemberAst -to PropertyMemberAst.Attributes, PropertyMemberAst.InitialValue, PropertyMemberAst.Name, PropertyMemberAst.PropertyType
                edge PropertyMemberAst.Attributes -to AttributeAst
                edge PropertyMemberAst.InitialValue -to ExpressionAst
                # edge PropertyMemberAst.Name -to string
                edge PropertyMemberAst.PropertyType -to TypeConstraintAst
        edge NamedAttributeArgumentAst -to NamedAttributeArgumentAst.Argument, NamedAttributeArgumentAst.ArgumentName, NamedAttributeArgumentAst.ExpressionOmitted
            edge NamedAttributeArgumentAst.Argument -to ExpressionAst
            # edge NamedAttributeArgumentAst.ArgumentName -to string
            # edge NamedAttributeArgumentAst.ExpressionOmitted -to bool
        edge NamedBlockAst -to NamedBlockAst.BlockKind, NamedBlockAst.Statements, NamedBlockAst.Traps, NamedBlockAst.Unnamed
            # edge NamedBlockAst.BlockKind -to Token
            edge NamedBlockAst.Statements -to StatementAst
            edge NamedBlockAst.Traps -to TrapStatementAst
            # edge NamedBlockAst.Unnamed -to bool
        edge ParamBlockAst -to ParamBlockAst.Attributes, ParamBlockAst.Parameters
            edge ParamBlockAst.Attributes -to AttributeAst
            edge ParamBlockAst.Parameterse -to ParameterAst
        edge ParameterAst -to ParameterAst.Attributes, ParameterAst.DefaultValue, ParameterAst.Name, ParameterAst.StaticType
            edge ParameterAst.Attributes -to AttributeBaseAst
            edge ParameterAst.DefaultValue -to ExpressionAst
            # edge ParameterAst.Name -to string
            # edge ParameterAst.StaticType -to Type
        edge RedirectionAst -to RedirectionAst.FromStream, FileRedirectionAst, MergingRedirectionAst
            # edge RedirectionAst.FromStream -to RedirectionStream
                # edge RedirectionStream
            edge FileRedirectionAst -to FileRedirectionAst.Append, FileRedirectionAst.Location
                # edge FileRedirectionAst.Append -to bool
                edge FileRedirectionAst.Location -to ExpressionAst
            edge MergingRedirectionAst -to MergingRedirectionAst.ToStream
                # edge MergingRedirectionAst.ToStream -to RedirectionStream
        edge ScriptBlockAst -to ScriptBlockAst.Attributes, ScriptBlockAst.BeginBlock, ScriptBlockAst.DynamicParamBlock, ScriptBlockAst.EndBlock, ScriptBlockAst.ProcessBlock, ScriptBlockAst.ParamBlock, ScriptBlockAst.UsingStatements
            edge ScriptBlockAst.Attributes -to AttrubuteAst
            edge ScriptBlockAst.BeginBlock, ScriptBlockAst.DynamicParamBlock, ScriptBlockAst.EndBlock, ScriptBlockAst.ProcessBlock -to NamedBlockAst
            edge ScriptBlockAst.ParamBlock -to ParamBlockAst
            edge ScriptBlockAst.UsingStatements -to UsingStatementAst
        edge StatementAst -to '(BlockStatementAst)', BreakStatementAst, CommandBaseAst, '(ConfigurationDefinitionAst)', ContinueStatementAst, DataStatementAst, DynamicKeywordStatementAst, ExitStatementAst, FunctionDefinitionAst, IfStatementAst, LabeledStatementAst, PipelineBaseAst, ReturnStatementAst, ThrowStatementAst, TrapStatementAst, TryStatementAst, '(TypeDefinitionAst)', UsingStatementAst
            edge BreakStatementAst -to BreakStatementAst.Label
                edge BreakStatementAst.Label -to ExpressionAst
            edge CommandBaseAst -to CommandBaseAst.Redirections, CommandAst, CommandExpressionAst
                edge CommandBaseAst.Redirections -to RedirectionAst
                edge CommandAst -to CommandAst.CommandElements
                    edge CommandAst.CommandElements -to CommandElementAst
                edge CommandExpressionAst -to CommandExpressionAst.Expression
                    edge CommandExpressionAst.Expression -to ExpressionAst
            edge ContinueStatementAst -to ContinueStatementAst.Label
                edge ContinueStatementAst.Label -to ExpressionAst
            edge DataStatementAst -to DataStatementAst.Body, DataStatementAst.CommandsAllowed, DataStatementAst.Variable
                edge DataStatementAst.Body -to StatementBlockAst
                edge DataStatementAst.CommandsAllowed -to ExpressionAst
                # edge DataStatementAst.Variable -to string
            edge DynamicKeywordStatementAst -to DynamicKeywordStatementAst.CommandElements
                edge DynamicKeywordStatementAst.CommandElements -to CommandElementAst
            edge ExitStatementAst -to ExitStatementAst.Pipeline
                edge ExitStatementAst.Pipeline -to PipelineBaseAst
            edge FunctionDefinitionAst -to  FunctionDefinitionAst.Body, FunctionDefinitionAst.Name, FunctionDefinitionAst.Parameters
                edge FunctionDefinitionAst.Body -to ScriptBlockAst
                # edge FunctionDefinitionAst.Name -to string
                edge FunctionDefinitionAst.Parameters -to ParameterAst
            edge IfStatementAst -to IfStatementAst.Clauses, IfStatementAst.ElseClause
                edge IfStatementAst.Clauses -to PipelineBaseAst, StatementBlockAst
                edge IfStatementAst.ElseClause -to StatementBlockAst
            edge LabeledStatementAst -to LabeledStatementAst.Label, LabeledStatementAst.Condition, LoopStatementAst, SwitchStatementAst
                # edge LabeledStatementAst.Label -to string
                edge LabeledStatementAst.Condition -to PipelineBaseAst
                edge LoopStatementAst -to DoUntilStatementAst, DoWhileStatementAst, ForeEachStatementAst, ForStatementAst, DoWhileStatementAst
                    # edge DoUntilStatementAst, DoWhileStatementAst, ForeEachStatementAst, ForStatementAst, DoWhileStatementAst
                edge SwitchStatementAst -to SwitchStatementAst.Clauses, SwitchStatementAst.Condition, SwitchStatementAst.Default
                    edge SwitchStatementAst.Clauses -to ExpressionAst, StatementBlockAst
                    edge SwitchStatementAst.Condition -to PipelineBaseAst
                    edge SwitchStatementAst.Default -to StatementBlockAst
            edge PipelineBaseAst -to AssignmentStatementAst, ChainableAst, '(ErrorStatementAst)'
                edge AssignmentStatementAst -to AssignmentStatementAst.Left, AssignmentStatementAst.Operator, AssignmentStatementAst.Right
                    edge AssignmentStatementAst.Left -to ExpressionAst
                    # edge AssignmentStatementAst.Operator -to TokenKind
                    edge AssignmentStatementAst.Right -to StatementAst
                edge ChainableAst -to PipelineAst, PipelineChainAst
                    edge PipelineAst -to PipelineAst.PipelineElements
                        edge PipelineAst.PipelineElements -to CommandBaseAst
                    edge PipelineChainAst -to PipelineChainAst.LhsPipelineChain, PipelineChainAst.RhsPipeline
                        edge PipelineChainAst.LhsPipelineChain -to ChainableAst
                        edge PipelineChainAst.RhsPipeline -to PipelineAst
                # edge '(ErrorStatementAst)'
            edge ReturnStatementAst -to ReturnStatementAst.Pipeline
                edge ReturnStatementAst.Pipeline -to PipelineBaseAst
            edge ThrowStatementAst -to ThrowStatementAst.Pipeline
                edge ThrowStatementAst.Pipeline -to PipelineBaseAst
            edge TrapStatementAst -to TrapStatementAst.TrapType
                edge TrapStatementAst.TrapType -to TypeConstraintAst
            edge TryStatementAst -to TryStatementAst.Body, TryStatementAst.CacheClauses, TryStatementAst.Finally
                edge TryStatementAst.Body -to StatementBlockAst
                edge TryStatementAst.CacheClauses -to CatchClauseAst
                edge TryStatementAst.Finally -to StatementBlockAst
            # edge '(TypeDefinitionAst)'
            edge UsingStatementAst -to UsingStatementAst.Name, UsingStatementAst.Alias, UsingStatementAst.ModuleSpecification
                edge UsingStatementAst.Name, UsingStatementAst.Alias -to StringConstantExpressionAst
                edge UsingStatementAst.ModuleSpecification -to HashtableAst
        edge StatementBlockAst -to StatementBlockAst.Statements, StatementBlockAst.Traps
            edge StatementBlockAst.Statements -to StatementAst
            edge StatementBlockAst.Traps -to TrapStatementAst
} 

$tempfile = New-TemporaryFile 
Set-Content -Path $tempfile.FullName -Value $graph

. "$env:ProgramFiles\Graphviz\bin\sfdp.exe" -Goverlap=true $tempfile | Export-PSGraph -ShowGraph

