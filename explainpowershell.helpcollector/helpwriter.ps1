using namespace Microsoft.Azure.Cosmos.Table

[CmdletBinding()]
param(
    [string] $HelpDataCacheFilename,
    [switch] $IsProduction
)

$tableName = 'HelpData'
$partitionKey = 'CommandHelp'
$StorageAccountName = 'explainpowershell'
$ResourceGroupName = 'powershellexplainer'

if (-not $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
    Write-Progress -id 2 -Activity "Upload help information.." -CurrentOperation "Preparing..." -PercentComplete 0
}

if ($IsProduction) {
    Write-Verbose "Preparing to upload data to Azure table.."
    Push-Location $PSScriptRoot
    . ./New-SasToken.ps1
    Pop-Location
    $sasToken = New-SasToken -ResourceGroupName $ResourceGroupName -StorageAccountName $storageAccountName
    $storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken
}
else {
    Write-Verbose "Preparing to upload data to local Azurite table.."
    $azuriteLocalConnectionString = 'AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'
    $storageCtx = New-AzStorageContext -ConnectionString $azuriteLocalConnectionString
}

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