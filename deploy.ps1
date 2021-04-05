trap { Pop-Location}
$key = get-content .\storageaccountkey.user
$source = ".\bin\Release\net5.0\publish\wwwroot\"
$ErrorActionPreference = 'stop'

Push-Location .\explainpowershell.frontend &&
    dotnet clean -v m &&
    dotnet restore -v m &&
    dotnet publish --no-restore --configuration Release &&
    az storage blob delete-batch --source `$web --account-name storageexplainpowershell --account-key $key --output none &&
    az storage blob upload-batch --destination `$web --account-name storageexplainpowershell --destination-path / --source $source --account-key $key --output none &&
    Pop-Location

Push-Location .\explainpowershell.analysisservice &&
    dotnet clean -v m &&
    func azure functionapp publish explainpowershellsyntaxanalyzer &&
    Pop-Location