<#
.SYNOPSIS
Write help data about installed powershell modules and available about_ articles to database.
.PARAMETER Force
Forces collection of About_ and Help data, even if local cache files exist.
#>

[CmdletBinding(DefaultParameterSetName="default")]
param(
    [parameter(ParameterSetName ="Production")]
    [parameter(ParameterSetName ="default")]
    [Switch]$Force,

    [parameter(ParameterSetName ="Production")]
    [Switch]$IsProduction,

    [parameter(ParameterSetName ="Production")]
    [String]$storageAccountName = 'storageexplainpowershell',

    [parameter(ParameterSetName ="Production")]
    [String]$ResourceGroupName = 'explainpowershell'
)

Push-Location $PSScriptRoot\

. .\classes.ps1

$tableName = 'HelpData'
$partitionKey = 'CommandHelp'

#TODO: Think of a way to make this script run locally for dev and in a pipeline for prod
$azureSubscriptionId = 'ced1b96d-9473-4313-b1c2-f8b30db08841' # thisConnect-AzAccount -UseDeviceAuthentication -Subscription 
$helpDataCacheFilename = 'helpdata.cache.user'

function New-SasToken {
    param(
        $ResourceGroupName,
        $StorageAccountName,
        $TableName
    )

    $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -AccountName $StorageAccountName).context

    $sasSplat = @{
        Name       = $TableName
        Context    = $context
        Permission = 'rwdlacu' # read, write, delete, list, add, copy?, update
        StartTime  = (Get-Date)
        ExpiryTime = (Get-Date).AddMinutes(30)
    }

    return New-AzStorageTableSASToken @sasSplat
}


if ($null -eq (Get-Command -Name 'Get-AzContext' -ErrorAction SilentlyContinue)) {
    Write-Host -ForegroundColor Green "Installing PowerShell Az module.."
    Install-Module Az -Force
}

if ($null -eq (Get-Command -Name 'Add-AzTableRow' -ErrorAction SilentlyContinue)) {
    Write-Host -ForegroundColor Green "Installing PowerShell AzTable module.."
    Install-Module AzTable -Force
}

if ($IsProduction) {
    Get-AzContext
    $sasToken = New-SasToken -ResourceGroupName $ResourceGroupName -StorageAccountName $storageAccountName -TableName $TableName #(Get-Content .\sastoken.user).trim()
    $storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken
}
else {
    $azuriteLocalConnectionString = 'AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'
    $storageCtx = New-AzStorageContext -ConnectionString $azuriteLocalConnectionString
}

if ($null -eq ($table = Get-AzStorageTable -Context $storageCtx -Name $tableName -ErrorAction SilentlyContinue)) {
    $table = New-AzStorageTable -Context $storageCtx -Name $tableName
}

if ($Force -or !(Test-Path $PSScriptRoot\about$helpDataCacheFilename)) {
    Write-Host -Foregroundcolor green "Collecting about_.. article data and saving to cache file 'about$helpDataCacheFilename'.."
    .\explainpowershell.aboutcollector.ps1 | ConvertTo-Json | Set-Content -Path $PSScriptRoot\about$helpDataCacheFilename -Force
}
else {
    Write-Host "Detected cache file 'about$helpDataCacheFilename', skipping collecting about_.. data. Use '-Force' or remove cache file to refresh about_.. data."
}

if ($Force -or !(Test-Path $PSScriptRoot\$helpDataCacheFilename)) {
    Write-Host -ForegroundColor Green "Collecting help data.."
    $tmp = .\explainpowershell.helpcollector.ps1
    Write-Host -ForegroundColor Green "Converting help data to JSON ($($tmp.Count) items).."
    $tmp = ConvertTo-Json -Depth 5 -InputObject $tmp
    Write-Host -ForegroundColor Green "Saving data to cache file '$helpDataCacheFilename'.."
    Set-Content -Path $PSScriptRoot\$helpDataCacheFilename -Value $tmp -Force
}
else {
    Write-Host "Detected cache file '$helpDataCacheFilename', skipping collecting help data. Use '-Force' or remove cache file to refresh help data."
}

Write-Host -ForegroundColor Green "Reading help data from cached file '$helpDataCacheFilename' ($([int]((Get-Item ./$helpDataCacheFilename).Length /1mb)) MB).."
$commandHelp = Get-Content $PSScriptRoot\$helpDataCacheFilename -Raw | ConvertFrom-Json
$commandHelp += Get-Content $PSScriptRoot\about$helpDataCacheFilename -Raw | ConvertFrom-Json

Write-Host -ForegroundColor Green "Adding help data to $(if ($IsProduction) {'Azure Storage Tables production'} else {'local Azurite developement'}) table.."
$i = 0
foreach ($help in $commandHelp) {
    Write-Progress -Activity "Adding helpdata to table.." -Status "Added: $i of $($commandHelp.Count)" -PercentComplete (($i / $commandHelp.Count)  * 100)
    $i++

    try {
        Add-AzTableRow -ErrorAction Stop -Table $table.CloudTable -PartitionKey $partitionKey -RowKey $help.CommandName.ToLower() -property @{
            CommandName       = "$($help.CommandName)"
            ModuleName        = "$($help.ModuleName)"
            Synopsis          = "$($help.Synopsis)"
            Syntax            = "$($help.Syntax)"
            # Parameters        = $help.Parameters.Name
            DocumentationLink = "$($help.DocumentationLink)"
            # RawCmdletHelp_aliases     = $help.RawCmdletHelp.Aliases
            # RawCmdletHelp_description = $help.RawCmdletHelp.Description
            # RawCmdletHelp_InputTypes = $help.RawCmdletHelp.InputTypes.Name
            # rawcmdlethelp_ = $help.rawcmdlethelp.Parameters
            # RawCommandInfo    = $help.RawCommandInfo
        } | Out-Null
    }
    catch [MethodInvocationException]{
        Write-Information "Entry $($help.CommandName) already present in database, skipping."
    }
    catch {
        $_
    }
}

Pop-Location