[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('None', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Output = 'Normal'
)

$c = New-PesterConfiguration -Hashtable @{
    Output = @{
        Verbosity = $Output
    }
}

$PSScriptRoot

# Run all code generators
Get-ChildItem -Path $PSScriptRoot/../explainpowershell.analysisservice/ -Recurse -Filter *_code_generator.ps1 | ForEach-Object { & $_.FullName }

$opp = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'

Push-Location -Path $PSScriptRoot/
    # Integration Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Integration tests`n"
    Invoke-Pester -Configuration $c
    # Unit Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Unit tests`n"
    dotnet test --no-build --nologo --verbosity $Output
Pop-Location

$ProgressPreference = $opp