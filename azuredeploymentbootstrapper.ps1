# Use this script to set up your Azure environment and GitHub Actions, so you can deploy explain powershell to Azure with GitHub Actions.
# You can also use this 
[cmdletbinding()]
param(
    [parameter(mandatory)]
    $SubscriptionId,
    [parameter(mandatory)]
    $ResourceGroupName,
    [parameter(mandatory, HelpMessage="For valid values, see 'az account list-locations'")]
    $AzureLocation,
    [string]$FunctionAppName,
    [string]$StorageAccountName,
    [switch]$TestEnv,
    [switch]$RemoveTestEnv,
    [string]$TestEnvName
)
$ErrorActionPreference = "Stop"
$testEnvMetadataPath = Join-Path $PSScriptRoot 'explainpowershell.azureinfra/test-environments.json'

function Get-TestEnvironmentRecords {
    if (Test-Path $testEnvMetadataPath) {
        return (Get-Content $testEnvMetadataPath | ConvertFrom-Json)
    }
    return @()
}

function Save-TestEnvironmentRecords {
    param([array]$records)
    if (-not $records -or $records.Count -eq 0) {
        if (Test-Path $testEnvMetadataPath) {
            Remove-Item $testEnvMetadataPath -Force
        }
        return
    }

    $records | ConvertTo-Json -Depth 5 | Set-Content -Path $testEnvMetadataPath -Encoding utf8
}

function New-StorageSafeName {
    param(
        [parameter(Mandatory)] [string]$Prefix,
        [parameter(Mandatory)] [string]$Seed
    )
    $safe = ("$Prefix$Seed".ToLower() -replace '[^a-z0-9]', '')
    if ($safe.Length -gt 24) {
        $safe = $safe.Substring(0,24)
    }
    if ($safe.Length -lt 3) {
        $safe = $safe.PadRight(3,'0')
    }
    return $safe
}

function New-TestEnvironmentNames {
    param(
        [parameter(Mandatory)] [string]$BaseResourceGroup,
        [parameter(Mandatory)] [string]$Suffix
    )

    $resolvedSuffix = $Suffix.ToLower()
    $rgName = ($BaseResourceGroup + "-test-" + $resolvedSuffix).ToLower()
    $functionAppSeed = ($rgName -replace '[^a-z0-9-]', '')
    $functionAppName = ("fa-$functionAppSeed" -replace '[^a-z0-9-]', '').ToLower()
    if ($functionAppName.Length -gt 60) {
        $functionAppName = $functionAppName.Substring(0,60)
    }
    $appServicePlanName = ("asp-$functionAppSeed" -replace '[^a-z0-9-]', '').ToLower()
    $storageAccountName = New-StorageSafeName -Prefix 'sa' -Seed $functionAppSeed

    return [pscustomobject]@{
        Environment = $resolvedSuffix
        ResourceGroupName = $rgName
        FunctionAppName = $functionAppName
        AppServicePlanName = $appServicePlanName
        StorageAccountName = $storageAccountName
    }
}

if ($TestEnv -and $RemoveTestEnv) {
    throw "Use either -TestEnv or -RemoveTestEnv, not both."
}

function New-TestEnvironment {
    param(
        [parameter(Mandatory)] [string]$SubscriptionId,
        [parameter(Mandatory)] [string]$AzureLocation,
        [parameter(Mandatory)] [pscustomobject]$Names
    )

    az account set --subscription $SubscriptionId
    az group create --location $AzureLocation --name $Names.ResourceGroupName --output none

    $templatePath = Join-Path $PSScriptRoot 'explainpowershell.azureinfra/template.bicep'
    $parameters = @(
        "functionAppName=$($Names.FunctionAppName)",
        "appServicePlanName=$($Names.AppServicePlanName)",
        "storageAccountName=$($Names.StorageAccountName)",
        "location=$AzureLocation"
    )

    az deployment group create `
        --resource-group $Names.ResourceGroupName `
        --template-file $templatePath `
        --parameters $parameters | Out-Null

    $records = @(Get-TestEnvironmentRecords | Where-Object { $_.Environment -ne $Names.Environment })
    $records += [pscustomobject]@{
        Environment = $Names.Environment
        ResourceGroupName = $Names.ResourceGroupName
        FunctionAppName = $Names.FunctionAppName
        StorageAccountName = $Names.StorageAccountName
        AppServicePlanName = $Names.AppServicePlanName
        CreatedOn = (Get-Date).ToString('u')
    }
    Save-TestEnvironmentRecords -records $records

    Write-Host -ForegroundColor Green "Test environment '$($Names.Environment)' deployed."
    Write-Host "Resource group: $($Names.ResourceGroupName)"
    Write-Host "Function App:   $($Names.FunctionAppName)"
    Write-Host "Storage acct:   $($Names.StorageAccountName)"
    Write-Host "To remove it later, run:`n  ./azuredeploymentbootstrapper.ps1 -SubscriptionId $SubscriptionId -ResourceGroupName $ResourceGroupName -AzureLocation $AzureLocation -RemoveTestEnv -TestEnvName $($Names.Environment)"
}

function Remove-TestEnvironment {
    param(
        [parameter(Mandatory)] [string]$SubscriptionId,
        [parameter(Mandatory)] [string]$AzureLocation,
        [parameter(Mandatory)] [string]$EnvironmentName
    )

    $lookup = Get-TestEnvironmentRecords
    $target = $lookup | Where-Object { $_.Environment -eq $EnvironmentName.ToLower() }
    if (-not $target) {
        throw "No test environment metadata found for '$EnvironmentName'."
    }

    az account set --subscription $SubscriptionId
    az group delete --name $target.ResourceGroupName --yes

    $remaining = $lookup | Where-Object { $_.Environment -ne $target.Environment }
    Save-TestEnvironmentRecords -records $remaining

    Write-Host -ForegroundColor Green "Removed test environment '$EnvironmentName' (resource group '$($target.ResourceGroupName)')."
}

if ($RemoveTestEnv) {
    if (-not $TestEnvName) {
        throw "Specify -TestEnvName when using -RemoveTestEnv."
    }
    Remove-TestEnvironment -SubscriptionId $SubscriptionId -AzureLocation $AzureLocation -EnvironmentName $TestEnvName
    return
}

if ($TestEnv) {
    $suffix = if ($TestEnvName) { $TestEnvName } else { (Get-Date -Format 'yyyyMMddHHmmss') }
    $names = New-TestEnvironmentNames -BaseResourceGroup $ResourceGroupName -Suffix $suffix
    if ($FunctionAppName) { $names.FunctionAppName = $FunctionAppName }
    if ($StorageAccountName) { $names.StorageAccountName = $StorageAccountName }
    az account set --subscription $SubscriptionId
    New-TestEnvironment -SubscriptionId $SubscriptionId -AzureLocation $AzureLocation -Names $names
    return
}

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

$spn = az ad sp create-for-rbac --name $spnName --role contributor --scopes /subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName --sdk-auth
$spn | gh secret set AZURE_SERVICE_PRINCIPAL
