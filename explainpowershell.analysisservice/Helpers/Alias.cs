using System;
using System.Collections.Generic;

namespace ExplainPowershell.SyntaxAnalyzer
{
    static partial class Helpers {
        public static string ResolveAlias(string cmdName)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "?", "Where-Object" },
                { "%", "ForEach-Object" },
                { "cd", "Set-Location" },
                { "chdir", "Set-Location" },
                { "clc", "Clear-Content" },
                { "clhy", "Clear-History" },
                { "cli", "Clear-Item" },
                { "clp", "Clear-ItemProperty" },
                { "cls", "Clear-Host" },
                { "clv", "Clear-Variable" },
                { "copy", "Copy-Item" },
                { "cpi", "Copy-Item" },
                { "cvpa", "Convert-Path" },
                { "dbp", "Disable-PSBreakpoint" },
                { "del", "Remove-Item" },
                { "dir", "Get-ChildItem" },
                { "ebp", "Enable-PSBreakpoint" },
                { "echo", "Write-Output" },
                { "epal", "Export-Alias" },
                { "epcsv", "Export-Csv" },
                { "erase", "Remove-Item" },
                { "etsn", "Enter-PSSession" },
                { "exsn", "Exit-PSSession" },
                { "fc", "Format-Custom" },
                { "fhx", "Format-Hex" },
                { "fl", "Format-List" },
                { "foreach", "ForEach-Object" },
                { "ft", "Format-Table" },
                { "fw", "Format-Wide" },
                { "gal", "Get-Alias" },
                { "gbp", "Get-PSBreakpoint" },
                { "gc", "Get-Content" },
                { "gcb", "Get-Clipboard" },
                { "gci", "Get-ChildItem" },
                { "gcm", "Get-Command" },
                { "gcs", "Get-PSCallStack" },
                { "gdr", "Get-PSDrive" },
                { "gerr", "Get-Error" },
                { "ghy", "Get-History" },
                { "gi", "Get-Item" },
                { "gjb", "Get-Job" },
                { "gl", "Get-Location" },
                { "gm", "Get-Member" },
                { "gmo", "Get-Module" },
                { "gp", "Get-ItemProperty" },
                { "gps", "Get-Process" },
                { "gpv", "Get-ItemPropertyValue" },
                { "group", "Group-Object" },
                { "gsn", "Get-PSSession" },
                { "gtz", "Get-TimeZone" },
                { "gu", "Get-Unique" },
                { "gv", "Get-Variable" },
                { "h", "Get-History" },
                { "history", "Get-History" },
                { "icm", "Invoke-Command" },
                { "iex", "Invoke-Expression" },
                { "ihy", "Invoke-History" },
                { "ii", "Invoke-Item" },
                { "ipal", "Import-Alias" },
                { "ipcsv", "Import-Csv" },
                { "ipmo", "Import-Module" },
                { "irm", "Invoke-RestMethod" },
                { "iwr", "Invoke-WebRequest" },
                { "md", "mkdir" },
                { "measure", "Measure-Object" },
                { "mi", "Move-Item" },
                { "move", "Move-Item" },
                { "mp", "Move-ItemProperty" },
                { "nal", "New-Alias" },
                { "ndr", "New-PSDrive" },
                { "ni", "New-Item" },
                { "nmo", "New-Module" },
                { "nsn", "New-PSSession" },
                { "nv", "New-Variable" },
                { "oh", "Out-Host" },
                { "popd", "Pop-Location" },
                { "pushd", "Push-Location" },
                { "pwd", "Get-Location" },
                { "r", "Invoke-History" },
                { "rbp", "Remove-PSBreakpoint" },
                { "rcjb", "Receive-Job" },
                { "rcsn", "Receive-PSSession" },
                { "rd", "Remove-Item" },
                { "rdr", "Remove-PSDrive" },
                { "ren", "Rename-Item" },
                { "ri", "Remove-Item" },
                { "rjb", "Remove-Job" },
                { "rmo", "Remove-Module" },
                { "rni", "Rename-Item" },
                { "rnp", "Rename-ItemProperty" },
                { "rp", "Remove-ItemProperty" },
                { "rsn", "Remove-PSSession" },
                { "rv", "Remove-Variable" },
                { "rvpa", "Resolve-Path" },
                { "sajb", "Start-Job" },
                { "sal", "Set-Alias" },
                { "saps", "Start-Process" },
                { "sbp", "Set-PSBreakpoint" },
                { "scb", "Set-Clipboard" },
                { "select", "Select-Object" },
                { "set", "Set-Variable" },
                { "si", "Set-Item" },
                { "sl", "Set-Location" },
                { "sls", "Select-String" },
                { "sp", "Set-ItemProperty" },
                { "spjb", "Stop-Job" },
                { "spps", "Stop-Process" },
                { "sv", "Set-Variable" },
                { "type", "Get-Content" },
                { "where", "Where-Object" },
                { "wjb", "Wait-Job" },
            };

            return dict.ContainsKey(cmdName) ? dict[cmdName] : null;
        }
    }
}