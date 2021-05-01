# TokenKind.cs code generator

$pre = @'
using System.Management.Automation.Language;

namespace ExplainPowershell.SyntaxAnalyzer
{
    static class Helpers {
       public static string TokenExplainer(TokenKind tokenKind)
        {
            var suffix = "";
            switch (tokenKind)
            {
'@

$post = @'
            }
            return suffix;
        }
    }
}
'@

$tokenKind = Import-Csv -Delimiter ';' -Path "$PSScriptRoot\TokenKind.csv"

$generatedCode = $tokenKind | ForEach-Object {
@"
                case TokenKind.$($_.Name):
                    suffix = "$($_.Explanation.Replace('"',''''))";
                    break;
"@
}

$pre, $generatedCode, $post | Out-File -FilePath "$PSScriptRoot\TokenKind.cs" -Force