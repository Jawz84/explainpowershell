# Get an idea of what Ast's occur most often in oneliners, to guide development.

$lines = get-content -Path .\oneliners.ps1

$lines.foreach{
    [System.Management.Automation.Language.Parser]::ParseInput( $_, [ref]$null, [ref]$null) 
}.FindAll({$true} ,$true) 
    | % gettype 
    | group name -NoElement 
    | sort count -Descending 
    | ft -a

<#

Count Name
----- ----
~~ 282 StringConstantExpressionAst ~~
~~ 119 CommandAst ~~
~~ 107 PipelineAst ~~
~~ 98 CommandParameterAst ~~
~~ 80 ScriptBlockAst ~~
~~ 80 NamedBlockAst ~~
~~ 35 VariableExpressionAst ~~
~~ 27 CommandExpressionAst ~~
~~ 16 BinaryExpressionAst ~~
~~ 11 ConstantExpressionAst ~~
~~ 3 IfStatementAst ~~
~~ 2 UnaryExpressionAst ~~
~~ 2 AssignmentStatementAst ~~

~~21 MemberExpressionAst ~~
~~12 ParenExpressionAst ~~
  11 ArrayLiteralAst
    9 ScriptBlockExpressionAst
    6 StatementBlockAst
    4 InvokeMemberExpressionAst
    2 HashtableAst
    1 SubExpressionAst
    1 TypeExpressionAst

    ParenExpressionAst
    ArrayLiteralAst
    ScriptBlockExpressionAst
    InvokeMemberExpressionAst
    HashtableAst
    SubExpressionAst

#>