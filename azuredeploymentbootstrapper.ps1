# Use this script to set up your Azure environment and GitHub Actions, so you can deploy explain powershell to Azure with GitHub Actions.
# You can also use this 
[cmdletbinding()]
param(
    [parameter(mandatory)]
    $SubscriptionId,
    [parameter(mandatory)]
    $ResourceGroupName,
    [parameter(mandatory, HelpMessage="For valid values, see 'az account list-locations'")]
    $AzureLocation
)
$ErrorActionPreferencea = "Stop"
$spnName = $ResourceGroupName

az account show --output none
if ($LASTEXITCODE) {
    az login --output none
}

$ghStatus = gh auth status
if ($ghStatus.StartsWith("You are not logged into")) {
    gh auth login --hostname github.com --git-protocol https --web
}

az account set --subscription $SubscriptionId
az group create --location $AzureLocation --name $ResourceGroupName --output none

$existingFunctionAppName = az functionapp list --resource-group $resourcegroupname --query '[].name' --output tsv | Where-Object { $_ -like "fa$resourcegroupname*" }
if (-not $existingFunctionAppName) {
    "Generating new names for FunctionApp and Storage Account.."
    $rnd = Get-Random -Maximum 1000

    if ($null -eq $FunctionAppName) {
        $FunctionAppName = "fa$ResourceGroupName$rnd"
    }

    if ($null -eq $StorageAccountName) {
        $StorageAccountName = "sa$ResourceGroupName$rnd"
    }

    $ResourceGroupName | gh secret set RESOURCE_GROUP_NAME
    $FunctionAppName | gh secret set FUNCTION_APP_NAME
    $StorageAccountName | gh secret set STORAGE_ACCOUNT_NAME
    $explanation = @"

You can now go to your explainpowershell fork on GitHub. Under Actions, run the 'Deploy Azure Infra' workflow, then the 'Deploy app to Azure' workflow and run `./explainpowershell.helpwriter.ps1 -Force -IsProduction -ResourceGroupName $ResourceGroupName -StorageAccountName $StorageAccountName`. The Url where you can reach your version of the project can be found in the Azure Portal. Go to resource group '`$ResourceGroupName'. Under the storage account resource that was deployed, find the 'Static Website' entry in the menu. It is the Url for 'Primary Endpoint'. Alternatively, you can retrieve it with `az`:

`$myStorageAccountName = '$StorageAccountName'
(az storage account show --name `$myStorageAccountName | convertfrom-json).primaryEndpoints.web

"@

    Write-Host -ForegroundColor Green $explanation
}
else {
    "Found existing FunctionApp set-up '$existingFunctionAppName', only refreshing Azure Service Principal."
}

$spn = az ad sp create-for-rbac --name $spnName --role contributor --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName --sdk-aut
$spn | gh secret set AZURE_SERVICE_PRINCIPAL
