$pre = @'
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExplainPowershell.SyntaxAnalyzer
{
    static partial class Helpers {
        public static (string, string) ResolveAccelerator(string typeName)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
'@

$post = @'
            };

            return dict.ContainsKey(typeName) ?
                (dict.Keys.First(k => string.Equals(typeName, k, StringComparison.OrdinalIgnoreCase)), dict[typeName]) :
                (null, null);
        }
    }
}
'@

$body = [psobject].Assembly.GetType("System.Management.Automation.TypeAccelerators")::Get.GetEnumerator() | ForEach-Object {
@"
                { `"$($_.Key)`", `"$($_.Value.FullName)`" },
"@
}


$pre, $body, $post | Out-File -FilePath "$PSScriptRoot\..\Helpers\Accelerators.cs" -Force