trap {
    Pop-Location
    setAppSettings -uri 'http://localhost:7071/api/'
}

function setAppSettings ($uri) {
    Set-Content -Path .\explainpowershell.frontend\wwwroot\appsettings.json -Value (@{BaseAddress = $uri} | ConvertTo-Json)
}

$key = get-content .\storageaccountkey.user
$source = ".\bin\Release\net5.0\publish\wwwroot\"
$ErrorActionPreference = 'stop'
setAppSettings -uri 'https://explainpowershellsyntaxanalyzer.azurewebsites.net/api/'

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

setAppSettings -uri 'http://localhost:7071/api/'