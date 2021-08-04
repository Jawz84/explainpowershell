#test
'#--' * 40
$ErrorActionPreference = 'stop'
$env:MODULE_NAME = 'microsoft.powershell.core'
$fileName = "./explainpowershell.helpcollector/help.$env:MODULE_NAME.cache.json"

if (-not (Get-Module -ListAvailable -Name $env:MODULE_NAME)) {
    Install-Module -Force $env:MODULE_NAME.replace('*', '')   #### TODO: wildcard was an idea to enable handling Az* module. but gives probelems with writing to cache file with name of 'help.az*.cache.json' as a filename
}
Write-Output "Module '$env:MODULE_NAME' installed"


$helpCollectorSplat = @{
    ModulesToProcess = @(Get-Module -ListAvailable $env:MODULE_NAME)
    verbose          = $true
}
./explainpowershell.helpcollector/explainpowershell.helpcollector.ps1 @helpCollectorSplat
| ConvertTo-Json
| Out-File -path $fileName

./explainpowershell.helpcollector/helpwriter.ps1 -helpDataCacheFilename $fileName