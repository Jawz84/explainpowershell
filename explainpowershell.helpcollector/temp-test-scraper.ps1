$force = $true
$PSScriptRoot = '.'
$modulesToProcess = Get-Content "$PSScriptRoot/explainpowershell.metadata/defaultModules.json"
| ConvertFrom-Json

foreach ($module in $modulesToProcess) {
    $fileName = "$PSScriptRoot/explainpowershell.helpcollector/help.$($module.Name).cache.user"

    if ($Force -or !(Test-Path $fileName) -or ((Get-Item $fileName).Length -eq 0)) {
        Write-Host -ForegroundColor Green "Collecting help data for module '$($module.Name)'.."
        ./explainpowershell.helpcollector/helpcollector.ps1 -ModulesToProcess $module
        | ConvertTo-Json
        | Out-File -path $fileName -Force:$Force
    }
    else {
        Write-Host "Detected cache file '$fileName', skipping collecting help data. Use '-Force' or remove cache file to refresh help data."
    }

    Write-Host "Writing help for module '$($module.Name)' to local Azurite table.."
    ./explainpowershell.helpcollector/helpwriter.ps1 -HelpDataCacheFilename $fileName
}