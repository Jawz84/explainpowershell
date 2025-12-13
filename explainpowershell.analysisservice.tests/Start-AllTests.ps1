[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('None', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Output = 'Normal',
    [switch]$SkipIntegrationTests,
    [switch]$SkipUnitTests,

    # By default, keep test runs deterministic and fast by disabling outbound AI calls.
    # Opt-in for real AI calls when needed (e.g., manual/local integration).
    [switch]$EnableAiCalls
)

$c = New-PesterConfiguration -Hashtable @{
    Output = @{
        Verbosity = $Output
    }
}

$PSScriptRoot

# Save/restore environment so running tests doesn't permanently affect the user's session.
$script:originalAiEnabled = $env:AiExplanation__Enabled
$script:originalAiEndpoint = $env:AiExplanation__Endpoint
$script:originalAiApiKey = $env:AiExplanation__ApiKey
$script:originalAiDeploymentName = $env:AiExplanation__DeploymentName

try {
    if (-not $EnableAiCalls) {
        $env:AiExplanation__Enabled = $false
        $env:AiExplanation__Endpoint = ''
        $env:AiExplanation__ApiKey = ''
        $env:AiExplanation__DeploymentName = ''
    }

    # Run all code generators
    Get-ChildItem -Path $PSScriptRoot/../explainpowershell.analysisservice/ -Recurse -Filter *_code_generator.ps1 | ForEach-Object { & $_.FullName }

    Push-Location -Path $PSScriptRoot/
    if (-not $SkipIntegrationTests) {
        # Integration Tests
        Write-Host -ForegroundColor Cyan "`n####`n#### Starting Integration tests`n"
        . ./Test-IsPrerequisitesRunning.ps1
        $werePrerequisitesAlreadyRunning = Test-IsPrerequisitesRunning -ports 7071
        Invoke-Pester -Configuration $c
        if (-not $werePrerequisitesAlreadyRunning) {
            Get-Job | Stop-Job -PassThru | Remove-Job -Force
        }
    }
    if (-not $SkipUnitTests) {
        # Unit Tests
        Write-Host -ForegroundColor Cyan "`n####`n#### Starting Unit tests`n"
        Write-Host -ForegroundColor Green "Building tests.."
        Set-Location $PSScriptRoot/..
        # we want the verbosity for the build step to be quiet
        dotnet build --verbosity quiet --nologo 
        Write-Host -ForegroundColor Green "Running tests.."
        # for the test step we want to be able to adjust the verbosity
        dotnet test --no-build --nologo --verbosity $Output 
    }
    Pop-Location

}
finally {
    if ($null -ne $script:originalAiEnabled) { $env:AiExplanation__Enabled = $script:originalAiEnabled } else { Remove-Item Env:AiExplanation__Enabled -ErrorAction SilentlyContinue }
    if ($null -ne $script:originalAiEndpoint) { $env:AiExplanation__Endpoint = $script:originalAiEndpoint } else { Remove-Item Env:AiExplanation__Endpoint -ErrorAction SilentlyContinue }
    if ($null -ne $script:originalAiApiKey) { $env:AiExplanation__ApiKey = $script:originalAiApiKey } else { Remove-Item Env:AiExplanation__ApiKey -ErrorAction SilentlyContinue }
    if ($null -ne $script:originalAiDeploymentName) { $env:AiExplanation__DeploymentName = $script:originalAiDeploymentName } else { Remove-Item Env:AiExplanation__DeploymentName -ErrorAction SilentlyContinue }
}