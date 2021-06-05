. $PSScriptRoot/Test-IsPrerequisitesRunning.ps1

$IsAzuriteUp = Test-IsPrerequisitesRunning -ports 10002

if ( $IsAzuriteUp ) {
    Write-Host "OK - Azurite table service available on localhost:10002" -ForegroundColor Green
}
else {
    Write-Warning "Azurite Table service not found on localhost:10002. Make sure Azurite table service is running and accessible."
}