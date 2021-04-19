function setAppSettings ($uri) {
    Set-Content -Path .\explainpowershell.frontend\wwwroot\appsettings.json -Value (@{BaseAddress = $uri} | ConvertTo-Json)
}
function writeGreen ($string) {
    Write-Host -ForegroundColor Green -Object "`n$string"
}

$ErrorActionPreference = 'stop'
$key = get-content .\storageaccountkey.user
$source = ".\bin\Release\net5.0\publish\wwwroot\"
$productionApiUri = 'https://explainpowershellsyntaxanalyzer.azurewebsites.net/api/'
$developmentApiUri = 'http://localhost:7071/api/'
$storageAccountName = 'storageexplainpowershell'
$functionAppName = 'explainpowershellsyntaxanalyzer'


writeGreen "Setting Api to production url '$productionApiUri'"
setAppSettings -uri $productionApiUri

writeGreen "Start job for creating AzViz image of Azure environment"
$null = Start-Job -ScriptBlock { Export-AzViz -ResourceGroup explainpowershell -LabelVerbosity 2 -Direction left-to-right -SuppressSubscriptionName -OutputFilePath ./AzViz.png }

writeGreen "Start deploying Blazor wasm app to Azure Storage blob account '$storageAccountName' as static website"
Push-Location .\explainpowershell.frontend &&
    dotnet clean -v m &&
    dotnet restore -v m &&
    dotnet publish --no-restore --configuration Release &&
    az storage blob delete-batch --source `$web --account-name $storageAccountName --account-key $key --output none &&
    az storage blob upload-batch --destination `$web --account-name $storageAccountName --destination-path / --source $source --account-key $key --output none &&
    Pop-Location

writeGreen "Start deploying FunctionApp '$functionAppName' to Azure"
Push-Location .\explainpowershell.analysisservice &&
    dotnet clean -v m &&
    func azure functionapp publish $functionAppName &&
    Pop-Location

writeGreen "Setting Api back to developement url '$developmentApiUri'"
setAppSettings -uri $developmentApiUri

writeGreen "Done - OK"

trap {
    Write-Warning "Something went wrong, aborting."
    Pop-Location
    write-green "Setting Api back to developement url '$developmentApiUri'"
    setAppSettings -uri $developmentApiUri
    Write-Warning "Done, with errors."
}
