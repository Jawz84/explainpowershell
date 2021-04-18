. .\classes.ps1

$storageAccountName = 'storageexplainpowershell'
$tableName = 'HelpData'
$sasToken = (Get-Content .\sastoken.user).trim()
$partitionKey = 'CommandHelp'
$storageCtx = New-AzStorageContext -StorageAccountName $storageAccountName -SasToken $sasToken

if ($null -eq ($table = Get-AzStorageTable -Context $storageCtx -Name $tableName -ErrorAction SilentlyContinue)) {
    $table = New-AzStorageTable -Context $storageCtx -Name $tableName
}

$commandHelp = .\explainpowershell.helpcollector.ps1

foreach ($help in $commandHelp) {
    $hlp = $help -As [HelpData]
    Add-AzTableRow -Table $table.CloudTable -PartitionKey $partitionKey -RowKey $hlp.CommandName.ToLower() -property @{
        CommandName       = $hlp.CommandName
        ModuleName        = $hlp.ModuleName
        Synopsis          = $hlp.Synopsis
        Syntax            = $hlp.Syntax
        # Parameters        = $hlp.Parameters.Name
        DocumentationLink = $hlp.DocumentationLink
        # RawCmdletHelp_aliases     = $hlp.RawCmdletHelp.Aliases
        # RawCmdletHelp_description = $hlp.RawCmdletHelp.Description
        # RawCmdletHelp_InputTypes = $hlp.RawCmdletHelp.InputTypes.Name
        # rawcmdlethelp_ = $hlp.rawcmdlethelp.Parameters
        # RawCommandInfo    = $hlp.RawCommandInfo
    } | Out-Null
}

Read-Host "press enter to delete table $tableName"
Remove-AzStorageTable -Name $tableName -Context $storageCtx