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

    function New-SasToken {
        param(
            $ResourceGroupName,
            $StorageAccountName
        )

        $context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -AccountName $StorageAccountName).context

        $sasSplat = @{
            Service = 'Table'
            ResourceType = 'Service', 'Container', 'Object'
            Permission = 'racwdlup' # https://docs.microsoft.com/en-us/powershell/module/az.storage/new-azstorageaccountsastoken
            StartTime  = (Get-Date)
            ExpiryTime = (Get-Date).AddMinutes(30)
            Context    = $context
        }

        return New-AzStorageAccountSASToken @sasSplat
    }

    if ($IsProduction) {
        Get-AzContext
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
        Get-AzTableRow -Table $table -partitionKey $partitionKey -RowKey $rowKey
    }
}
