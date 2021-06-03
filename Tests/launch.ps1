[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('None', 'Normal', 'Detailed', 'Diagnostic')]
    [string]$Output = 'Detailed'
)

$c = New-PesterConfiguration
$c.Output.Verbosity.Value = $Output

$opp = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'

Push-Location $PSScriptRoot
Invoke-Pester -Configuration $c
Pop-Location

$ProgressPreference = $opp