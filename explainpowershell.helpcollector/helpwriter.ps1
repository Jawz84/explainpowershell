using namespace Microsoft.Azure.Cosmos.Table
using namespace explainpowershell.helpcollector.tools

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

$counter = 0

foreach ($help in $commandHelp) {
    if (-not $PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        Write-Progress -Id 2 -Activity "Uploading '$($commandHelp.Count)' Help items." -CurrentOperation "Uploading help for command '$($help.CommandName)'" -PercentComplete ((@($commandHelp).IndexOf($help) + 1) / $commandHelp.Count * 100) 
    }

    if (([System.Text.ASCIIEncoding]::Unicode.GetByteCount(@($help.Parameters)) / 1kb)  -gt 64) {
        # Parameter data is json, and can become too big for the 64Kb limit of Azure Table storage. If it is too big, compress it before storing.
        Write-Verbose "Compressing Parameter help for '$($help.CommandName)', because it's bigger than 64kb, and wouldn't fit Azure Table storage otherwise."
        try {
            $null = [DeCompress]
        }
        catch {
            $typeDef = (Get-Content $PSScriptRoot\tools\DeCompress.cs -Raw)
            Add-Type -TypeDefinition $typeDef
        }

        $help.Parameters = [DeCompress]::Compress($help.Parameters)
    }

    $RowKey = $help.CommandName.ToLower()

    $entity = New-Object -TypeName DynamicTableEntity -ArgumentList $PartitionKey, $RowKey

    foreach ($prop in $help.Keys) {
        if ($help[$prop] -notlike $null) {
            try {
                $null = $entity.Properties.Add($prop, $help.Item($prop))
            }
            catch {
                $null = $entity.Properties.Add([string]$prop, [string]($help.Item($prop) | ConvertTo-Json -depth 3 -Compress))
            }
        }
    }

    try {
        $res = $Table.Execute([TableOperation]::InsertOrReplace($entity))
    }
    catch {
        Write-Host "Couldn't write '$($entity.RowKey)': $($_.Exception.Message)" -ForegroundColor Yellow
        continue
    }

    while ($null -eq $res) {
        Write-Host 'No response writing '$($entity.RowKey)', retrying in 2 seconds' -ForegroundColor Yellow
        Start-Sleep -Seconds 2
        $res = $Table.Execute([TableOperation]::InsertOrReplace($entity))
    }

    $counter++

    Write-Verbose ("{2,-5} {0,-20} {1}" -f $res.Result.Properties.ModuleName, $res.Result.Properties.CommandName, $res.HttpStatusCode)
}

if ($counter -eq 0) {
    throw "No items were sucessfully written to DB!"
}
elseif ($counter -lt $commandHelp.Count) {
    $succesPercentage = "{0:f1} %" -f ($counter/$commandHelp.Count*100)
    Write-Host -ForegroundColor Yellow "Wrote $counter of $($commandHelp.Count) items to DB, that's $succesPercentage."
}
else {
    Write-Host -ForegroundColor Cyan "Succesfully wrote $counter of $($commandHelp.Count) items to DB, 100% complete!"
}
