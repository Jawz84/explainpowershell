$pre = @'
using System;
using System.Collections.Generic;

namespace ExplainPowershell.SyntaxAnalyzer
{
    static partial class Helpers {
        public static string ResolveAlias(string cmdName)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
'@

$post = @'
            };

            return dict.ContainsKey(cmdName) ? dict[cmdName] : null;
        }
    }
}
'@

$body = Get-Alias | ForEach-Object {
@"
                { `"$($_.Name)`", `"$($_.Definition)`" },
"@
}


$pre, $body, $post | Out-File -FilePath "$PSScriptRoot\..\Helpers\Alias.cs" -Force