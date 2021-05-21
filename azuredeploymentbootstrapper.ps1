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
    $StorageAccountName = "sa$StorageAccountName$rnd"
}

$spn = az ad sp create-for-rbac --name $spnName --role contributor --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName --sdk-aut

$spn | gh secret set AZURE_SERVICE_PRINCIPAL
$ResourceGroupName | gh secret set RESOURCE_GROUP_NAME
$FunctionAppName | gh secret set FUNCTION_APP_NAME
$StorageAccountName | gh secret set STORAGE_ACCOUNT_NAME