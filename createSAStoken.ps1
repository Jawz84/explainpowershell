# create SAS token
[cmdletBinding()]
param(
    $ResourceGroupName = 'explainpowershell',
    $StorageAccountName = 'storageexplainpowershell',
    $TableName = 'HelpData'
)

$context = (Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -AccountName $StorageAccountName).context

$sasSplat = @{
    Name       = $TableName
    Context    = $context
    Permission = 'rwdlacu' # read, write, delete, list, add, copy?, update
    StartTime  = (Get-Date)
    ExpiryTime = (Get-Date).AddMinutes(30)
}

New-AzStorageTableSASToken @sasSplat
