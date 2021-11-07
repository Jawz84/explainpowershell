[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('None', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Output = 'Detailed'
)

$c = New-PesterConfiguration -Hashtable @{
    Output = @{
        Verbosity = $Output
    }
}

$opp = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'

Push-Location $PSScriptRoot
    # Integration Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Integration tests`n"
    Invoke-Pester -Configuration $c
    # Unit Tests
    Write-Host -ForegroundColor Cyan "`n####`n#### Starting Unit tests`n"
    dotnet test --no-build --nologo
Pop-Location

$ProgressPreference = $opp