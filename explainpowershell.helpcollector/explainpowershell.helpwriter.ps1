[CmdletBinding()]
param(
    [Switch]$IsProduction
)

. .\classes.ps1

$tableName = 'HelpData'
$partitionKey = 'CommandHelp'

if ($IsProduction) {
    $storageAccountName = 'storageexplainpowershell'
    $sasToken = (Get-Content .\sastoken.user).trim()
    $storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken
}
else {
    $azuriteLocalConnectionString = 'AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'
    $storageCtx = New-AzStorageContext -ConnectionString $azuriteLocalConnectionString
}

if ($null -eq ($table = Get-AzStorageTable -Context $storageCtx -Name $tableName -ErrorAction SilentlyContinue)) {
    $table = New-AzStorageTable -Context $storageCtx -Name $tableName
}

$commandHelp = .\explainpowershell.helpcollector.ps1

foreach ($help in $commandHelp) {
    $hlp = $help -As [HelpData]
    Add-AzTableRow -Table $table.CloudTable -PartitionKey $partitionKey -RowKey $hlp.CommandName.ToLower() -property @{
        CommandName       = "$($hlp.CommandName)"
        ModuleName        = "$($hlp.ModuleName)"
        Synopsis          = "$($hlp.Synopsis)"
        Syntax            = "$($hlp.Syntax)"
        # Parameters        = $hlp.Parameters.Name
        DocumentationLink = "$($hlp.DocumentationLink)"
        # RawCmdletHelp_aliases     = $hlp.RawCmdletHelp.Aliases
        # RawCmdletHelp_description = $hlp.RawCmdletHelp.Description
        # RawCmdletHelp_InputTypes = $hlp.RawCmdletHelp.InputTypes.Name
        # rawcmdlethelp_ = $hlp.rawcmdlethelp.Parameters
        # RawCommandInfo    = $hlp.RawCommandInfo
    } | Out-Null
}

# Read-Host "press enter to delete table $tableName"
# Remove-AzStorageTable -Name $tableName -Context $storageCtx