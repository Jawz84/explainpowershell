[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('None', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Output = 'Normal',
    [switch]$SkipIntegrationTests,
    [switch]$SkipUnitTests
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
    if (-not $SkipIntegrationTests) {
        # Integration Tests
        Write-Host -ForegroundColor Cyan "`n####`n#### Starting Integration tests`n"
        Invoke-Pester -Configuration $c
    }
    if (-not $SkipUnitTests) {
        # Unit Tests
        Write-Host -ForegroundColor Cyan "`n####`n#### Starting Unit tests`n"
        Write-Host -ForegroundColor Green "Building tests.."
        # we want the verbosity for the build step to be quiet
        dotnet build --verbosity quiet --nologo 
        Write-Host -ForegroundColor Green "Running tests.."
        # for the test step we want to be able to adjust the verbosity
        dotnet test --no-build --nologo --verbosity $Output 
    }
Pop-Location

$ProgressPreference = $opp