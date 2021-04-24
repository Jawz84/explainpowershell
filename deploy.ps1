function setAppSettings ($uri) {
    Set-Content -Path .\explainpowershell.frontend\wwwroot\appsettings.json -Value (@{BaseAddress = $uri} | ConvertTo-Json)
}
function writeGreen ($string) {
    Write-Host -ForegroundColor Green -Object "`n$string"
}

function handleJobErrorStream ($JobErrors) {
    $JobErrors = $JobErrors | Where-Object {$_ -notmatch $normalOutputFilter}
    if ($null -ne $JobErrors) {
        Write-Warning "Errors:"
        $JobErrors | Out-Default
    }
}

$date = Get-Date
$ErrorActionPreference = 'stop'
$key = get-content .\storageaccountkey.user
$source = ".\bin\Release\net5.0\publish\wwwroot\"
$productionApiUri = 'https://explainpowershellsyntaxanalyzer.azurewebsites.net/api/'
$developmentApiUri = 'http://localhost:7071/api/'
$storageAccountName = 'storageexplainpowershell'
$functionAppName = 'explainpowershellsyntaxanalyzer'
$normalOutputFilter = "^\d{1,3}/\d{3}|^NotSpecified: \(:String\) \[\], RemoteException|^Finished\["

writeGreen "Setting Api to production url '$productionApiUri'"
setAppSettings -uri $productionApiUri

writeGreen "Start job for creating AzViz image of Azure environment"
$azVizJob = Start-Job -ScriptBlock { Export-AzViz -ResourceGroup explainpowershell -LabelVerbosity 2 -Direction left-to-right -SuppressSubscriptionName -OutputFilePath ./AzViz.png }

writeGreen "Start job deploying Blazor wasm app to Azure Storage blob account '$storageAccountName' as static website"
$blazorJob = Start-Job -ArgumentList $storageAccountName, $key, $source {
    $storageAccountName = $args[0]
    $key = $args[1]
    $source = $args[2]

    Push-Location .\explainpowershell.frontend &&
    dotnet clean -v m &&
    dotnet restore -v m &&
    dotnet publish --no-restore --configuration Release &&
    az storage blob delete-batch --source `$web --account-name $storageAccountName --account-key $key --output none --only-show-errors &&
    az storage blob upload-batch --destination `$web --account-name $storageAccountName --destination-path / --source $source --account-key $key --output none --only-show-errors &&
    Pop-Location
}

writeGreen "Start job deploying FunctionApp '$functionAppName' to Azure"
$functionAppJob = Start-Job -ArgumentList $functionAppName -ScriptBlock {
    $functionAppName = $args[0]
    Push-Location .\explainpowershell.analysisservice &&
    dotnet clean -v m &&
    func azure functionapp publish $functionAppName &&
    Pop-Location
}

writeGreen "Waiting for deploy jobs to finish.."
$date = get-date
$blazorJob, $functionAppJob | Wait-Job -ErrorAction SilentlyContinue -ErrorVariable waitErrors | Out-Null

if (-not (($null -eq $waitErrors -or $waitErrors.Count -eq 0))) {
    throw $waitErrors
}

$azVizJob | Remove-Job

writeGreen "Blazor deployment result:"
Receive-Job -Job $blazorJob -Wait -AutoRemoveJob -ErrorAction SilentlyContinue -ErrorVariable blazorErrors
handleJobErrorStream -JobErrors $blazorErrors

writeGreen "Blazor deployment result:"
Receive-Job -Job $functionAppJob -Wait -AutoRemoveJob -ErrorAction SilentlyContinue -ErrorVariable functionAppErrors
handleJobErrorStream -JobErrors $functionAppErrors

writeGreen "Setting Api back to developement url '$developmentApiUri'"
setAppSettings -uri $developmentApiUri

writeGreen "Done - OK"
"{0:mm}:{0:ss} minutes" -f ((Get-Date) - $date)

trap {
    Write-Warning "Something went wrong, aborting.  $_"
    Pop-Location

    writeGreen "Setting Api back to developement url '$developmentApiUri'"
    setAppSettings -uri $developmentApiUri
    Write-Warning "Done, with errors."
    "{0:mm}:{0:ss} minutes" -f ((Get-Date) - $date)
}
