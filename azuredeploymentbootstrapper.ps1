# Use this script to set up your Azure environment and GitHub Actions, so you can deploy explain powershell to Azure with GitHub Actions.
[cmdletbinding()]
param(
    [parameter(mandatory)]
    $SubscriptionId,
    [parameter(mandatory)]
    $ResourceGroupName,
    [parameter(mandatory, HelpMessage="For valid values, see 'az account list-locations'")]
    $AzureLocation,
    $FunctionAppName,
    $StorageAccountName
)

az login --output none
gh auth login --hostname github.com

az account set --subscription $SubscriptionId
az group create --location $AzureLocation --name $ResourceGroupName

$rnd = Get-Random -Maximum 1000
$spnName = $ResourceGroupName

if ($null -eq $FunctionAppName) {
    $FunctionAppName = "fa$ResourceGroupName$rnd"
}

if ($null -eq $StorageAccountName) {
    $StorageAccountName = "sa$ResourceGroupName$rnd"
}

$spn = az ad sp create-for-rbac --name $spnName --role contributor --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName --sdk-aut

$spn | gh secret set AZURE_SERVICE_PRINCIPAL
$ResourceGroupName | gh secret set RESOURCE_GROUP_NAME
$FunctionAppName | gh secret set FUNCTION_APP_NAME
$StorageAccountName | gh secret set STORAGE_ACCOUNT_NAME

$explanation = @"

You can now go to your explainpowershell fork on GitHub. Under Actions, run the 'Deploy Azure Infra' workflow, then the 'Deploy app to Azure' workflow and lastly the 'Fill help database' workflow. The Url where you can reach your version of the project can be found in the Azure Portal. Go to resource group '`$ResourceGroupName'. Under the storage account resource that was deployed, find the 'Static Website' entry in the menu. It is the Url for 'Primary Endpoint'. Alternatively, you can retrieve it with `az`:

`$myStorageAccountName = '$StorageAccountName'
(az storage account show --name `$myStorageAccountName | convertfrom-json).primaryEndpoints.web

"@

Write-Host -ForegroundColor Green $explanation