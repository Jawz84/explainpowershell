using namespace Microsoft.Azure.Cosmos.Table

function Get-HelpDatabaseData {
    [CmdletBinding(DefaultParameterSetName="local")]
    param(
        [parameter(ParameterSetName="local", Position="0")]
        [parameter(ParameterSetName="production", Position="0")]
        [string]$RowKey,

        [parameter(ParameterSetName="local")]
        [parameter(ParameterSetName="production")]
        [switch]$ReturnTable,

        [parameter(ParameterSetName="production")]
        [switch]$IsProduction,

        [parameter(ParameterSetName ="production")]
        [String]$StorageAccountName = 'storageexplainpowershell',

        [parameter(ParameterSetName ="production")]
        [String]$ResourceGroupName = 'explainpowershell'
    )

    $tableName = 'HelpData'
    $partitionKey = 'CommandHelp'

    if ($IsProduction) {
        Get-AzContext
        . ../explainpowershell.helpcollector/New-SasToken.ps1
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
