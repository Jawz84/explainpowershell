# TokenKind.cs code generator

$pre = @'
// NB: This file is auto-generated by 'TokenKind.cs_code_generator.ps1'. Your changes here will be overwritten.

using System.Management.Automation.Language;

namespace ExplainPowershell.SyntaxAnalyzer
{
    public static partial class Helpers {
        public static (string, string) TokenExplainer(TokenKind tokenKind)
        {
            var description = string.Empty;
            var helpQuery = string.Empty;

            switch (tokenKind)
            {
'@

$post = @'
            }
            return (description, helpQuery);
        }
    }
}
'@

$tokenKind = Import-Csv -Delimiter ';' -Path "$PSScriptRoot\TokenKind.csv"

$generatedCode = $tokenKind | ForEach-Object {
@"
                case TokenKind.$($_.Name):
                    description = "$($_.Explanation)";
                    helpQuery = "$($_.HelpQuery)";
                    break;
"@
}

$pre, $generatedCode, $post | Out-File -FilePath "$PSScriptRoot\..\Helpers\TokenKind.cs" -Force