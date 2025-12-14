using namespace Microsoft.Azure.Cosmos.Table

<#
.SYNOPSIS
Gets help data from the HelpData Azure Table (local Azurite or production).

.DESCRIPTION
Retrieves entities from the HelpData table in the CommandHelp partition.

By default, the function uses a local Azurite Table endpoint via a connection string.
When -IsProduction is specified, it creates a Storage context using a SAS token and queries
the production Storage Account.

If -ReturnTable is specified, the underlying CloudTable object is returned instead of data.

.PARAMETER RowKey
Optional RowKey to filter for a single entity. When omitted, all entities in the CommandHelp
partition are returned.

.PARAMETER ReturnTable
Returns the underlying CloudTable object for the HelpData table.

.PARAMETER IsProduction
Uses the production Storage Account instead of the local Azurite Table endpoint.

.PARAMETER StorageAccountName
Production-only. The Azure Storage Account name.

.PARAMETER ResourceGroupName
Production-only. The Azure Resource Group name containing the Storage Account.

.EXAMPLE
Get-HelpDatabaseData

Returns all entities in the CommandHelp partition from the local Azurite table.

.EXAMPLE
Get-HelpDatabaseData -RowKey 'Get-Process'

Returns the entity with RowKey 'Get-Process' from the local Azurite table.

.EXAMPLE
Get-HelpDatabaseData -IsProduction -RowKey 'Get-Process' -ResourceGroupName 'powershellexplainer' -StorageAccountName 'explainpowershell'

Returns the entity with RowKey 'Get-Process' from the production table.

.EXAMPLE
$table = Get-HelpDatabaseData -ReturnTable

Returns the CloudTable object.

.OUTPUTS
Microsoft.Azure.Cosmos.Table.DynamicTableEntity[]
Microsoft.Azure.Cosmos.Table.CloudTable
#>
function Get-HelpDatabaseData {
    [CmdletBinding(DefaultParameterSetName = 'local')]
    param(
        [parameter(ParameterSetName = 'local', Position = '0')]
        [parameter(ParameterSetName = 'production', Position = '0')]
        [string]$RowKey,

        [parameter(ParameterSetName = 'local')]
        [parameter(ParameterSetName = 'production')]
        [switch]$ReturnTable,

        [parameter(ParameterSetName = 'production')]
        [switch]$IsProduction,

        [parameter(ParameterSetName = 'production')]
        [String]$StorageAccountName = 'explainpowershell',

        [parameter(ParameterSetName = 'production')]
        [String]$ResourceGroupName = 'powershellexplainer'
    )

    $tableName = 'HelpData'
    $partitionKey = 'CommandHelp'

    if ($IsProduction) {
        . /workspace/explainpowershell.helpcollector/New-SasToken.ps1
        $sasToken = New-SasToken -ResourceGroupName $ResourceGroupName -StorageAccountName $storageAccountName
        $storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken
    }
    else {
        $azuriteLocalConnectionString = 'AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'
        $storageCtx = New-AzStorageContext -ConnectionString $azuriteLocalConnectionString
    }

    $table = (Get-AzStorageTable -Context $storageCtx -Name $tableName).CloudTable
    if ($ReturnTable) {
        return $table
    }
    elseif (-not $rowKey) {
        $query = [TableQuery]@{
            FilterString = "PartitionKey eq '$partitionKey'"
        }

        return $table.ExecuteQuery($query)
    }
    else {
        $query = [TableQuery]@{
            FilterString = [TableQuery]::CombineFilters(
                [TableQuery]::GenerateFilterCondition(
                    'PartitionKey',
                    [QueryComparisons]::Equal,
                    $partitionKey
                ),
                'and',
                [TableQuery]::GenerateFilterCondition(
                    'RowKey',
                    [QueryComparisons]::Equal,
                    $rowKey
                )
            )
        }

        return $table.ExecuteQuery($query)
    }
}
