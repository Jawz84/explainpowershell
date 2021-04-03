$key = get-content .\storageaccountkey.user
$source = .\explainpowershell.frontend\bin\Release\net5.0\publish\wwwroot\
dotnet clean -v m &&
    dotnet restore -v m &&
    dotnet publish --no-restore &&
    az storage blob upload-batch --destination `$web --account-name storageexplainpowershell --destination-path / --source $source --account-key $key &&
    Push-Location .\explainpowershell.analysisservice && 
    func azure functionapp publish explainpowershellsyntaxanalyzer --no-build &&
    Pop-Location