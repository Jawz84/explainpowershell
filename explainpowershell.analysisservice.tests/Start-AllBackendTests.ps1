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

$opp = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'

Push-Location /workspace/explainpowershell.analysisservice.tests
    # Integration Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Integration tests`n"
    Invoke-Pester -Configuration $c
    # Unit Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Unit tests`n"
    dotnet test --no-build --nologo --verbosity $Output
Pop-Location

$ProgressPreference = $opp