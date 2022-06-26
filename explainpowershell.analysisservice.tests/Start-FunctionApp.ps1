. $PSScriptRoot/Test-IsPrerequisitesRunning.ps1

function IsTimedOut {
    param(
        [datetime]$Start,
        [int]$TimeOut
    )

    $IsTimedOut = (Get-Date) -gt $Start.AddSeconds($TimeOut)

    if ($IsTimedOut) {
        Write-Warning "Timed out after '$TimeOut' seconds."
    }

    return $IsTimedOut
}

$global:_isPrerequisitesRunning = $false
$timeOut = 120

Write-Host "Checking if function app is running.."
if (-not (Test-IsPrerequisitesRunning -ports 7071)) {
    $start = Get-Date
    try {
        Write-Host "Starting Function App.."
        Start-ThreadJob -ArgumentList $PSScriptRoot {
            Push-Location "$($args[0])/../explainpowershell.analysisservice/"
            func host start
        }

        do {
            Start-Sleep -Seconds 2
        } until ((IsTimedOut -Start $start -TimeOut $timeOut) -or (Test-IsPrerequisitesRunning -ports 7071))
    }
    catch {
        throw $_
        Write-Warning "Error: $($_.Message)"
    }
}
else {
    $global:_isPrerequisitesRunning = $true
}

Write-Host "OK - Function App running" -ForegroundColor Green