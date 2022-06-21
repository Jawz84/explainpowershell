[CmdletBinding()]
param(
    [switch] $Force,
    $modulesToProcess = ( Get-Module -ListAvailable ) #| Where-Object Name -notmatch "az.*|micrsoft.*|pester|editorservices|plaser|threadjob|posh-git"
)

foreach ($module in $modulesToProcess) {
    $fileName = "$PSScriptRoot/help.$($module.Name).cache.user"

    if ($Force -or !(Test-Path $fileName) -or ((Get-Item $fileName).Length -eq 0)) {
        Write-Host -ForegroundColor Green "Collecting help data for module '$($module.Name)'.."
        ./helpcollector.ps1 -ModulesToProcess $module
        | ConvertTo-Json
        | Out-File -path $fileName -Force:$Force
    }
    else {
        Write-Host "Detected cache file '$fileName', skipping collecting help data. Use '-Force' or remove cache file to refresh help data."
    }
}