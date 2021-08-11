using namespace Microsoft.Azure.Cosmos.Table

[cmdletbinding()]
param(
    $HelpDataCacheFilename = '../explainpowershell.helpcollector/help.az.accounts.cache.json'
)

if (-not $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
    Write-Progress -id 2 -Activity "Upload help information.." -CurrentOperation "Preparing..." -PercentComplete 0
}

Write-Verbose "Preparing to upload data to Azure table.."

$tableName = 'TestHelpData'
$partitionKey = 'CommandHelp'
$StorageAccountName = 'explainpowershell'
$ResourceGroupName = 'powershellexplainer'

Push-Location $PSScriptRoot
. ./New-SasToken.ps1
Pop-Location

$sasToken = New-SasToken -ResourceGroupName $ResourceGroupName -StorageAccountName $storageAccountName
$storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken

$table = Get-AzStorageTable -Context $storageCtx -Name $tableName -ErrorAction SilentlyContinue
if ($null -eq $table) {
    $table = New-AzStorageTable -Context $storageCtx -Name $tableName
}
$table = $table.CloudTable

$commandHelp = Get-Content $helpDataCacheFilename -Raw
| ConvertFrom-Json -AsHashtable

foreach ($help in $commandHelp) {
    if (-not $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        Write-Progress -Id 2 -Activity "Uploading '$($commandHelp.Count)' Help items." -CurrentOperation "Uploading help for command '$($help.CommandName)'" -PercentComplete ((@($commandHelp).IndexOf($help) + 1) / $commandHelp.Count * 100) 
    }

    $RowKey = $help.CommandName.ToLower()

    $entity = New-Object -TypeName DynamicTableEntity -ArgumentList $PartitionKey, $RowKey

    foreach ($prop in $help.Keys) {
        if ($help[$prop] -notlike $null) {
            try {
                $entity.Properties.Add($prop, $help.Item($prop))
            }
            catch {
                $entity.Properties.Add([string]$prop, [string]($help.Item($prop) | ConvertTo-Json -depth 5))
            }
        }
    }

    try {
        $res = $Table.Execute([TableOperation]::InsertOrReplace($entity))
    }
    catch {
        Write-Warning "Couldn't write '$($entity.RowKey)': $($_.Exception.Message)"
        continue
    }

    while ($null -eq $res) {
        Write-Warning 'No response writing '$($entity.RowKey)', retrying in 2 seconds'
        Start-Sleep -Seconds 2
        $res = $Table.Execute([TableOperation]::InsertOrReplace($entity))
    }

    Write-Verbose ("{2,-5} {0,-20} {1}" -f $res.Result.Properties.ModuleName, $res.Result.Properties.CommandName, $res.HttpStatusCode)
}