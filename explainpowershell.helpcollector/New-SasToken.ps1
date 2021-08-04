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