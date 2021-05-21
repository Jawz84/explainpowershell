# Explain PowerShell

PowerShell version of [explainshell.com](explainshell.com)

On ExplainShell.com, you can enter a Linux terminal oneliner, and the site will analyze it, and return snippets from the proper man-pages, in an effort to explain the oneliner. 
I would like to create the same thing but for PowerShell. 

I have a [proof of concept running here](https://explainpowershell.z6.web.core.windows.net/).

## Goal

I want to make it easy for anyone to find out what a certain line of PowerShell code does.
I envision something like this:

![mock](./Mockup.png)

## Azure Resources overview

* C# Azure Function backend API that analyzes PowerShell oneliners.
* Azure Storage that hosts Blazor Wasm pages as a static website.
* Azure Storage Table in which all help metadata is for currently supported modules.

![azure resources](./AzViz.png)

## Development

This repo offers a development container, with a bootstrap script to get you fully up and running.
- Clone repo
- Open in VSCode, and accept the offer to open in development container
- Container is built, and automatically runs the `bootstrap.ps1` script which will:
    - Check permissions on your repo so dotnet works without sudo
    - Perform `dotnet restore`
    - Install all necessary PowerShell modules
    - Fill local Azurite Table emulator with the necessary database
    - Run all tests for you, so you know everything is working

There are multiple preconfigured launch configurations, and there is also a `watch_run.ps1` script that you can use if you want to iterate quickly without debugging.

## Deploying to Azure

Deploying to Azure is done using GitHub Actions. To set everything up, you will need to create an Azure resource group, an service principal for that group. Also you will need to save the secret for that service principal in GitHub as a secret. Lastly you need to give your resources a name. 
I wrote a script so you can sit back and relax while all that is being done for you. Just make sure you log on to GitHub an Azure when prompted.

```powershell
./azuredeploymentbootstrapper.ps1 -SubscriptionId 12345678-91e7-42d9-bb2d-09876543321 -ResourceGroupName MyExplainPowerShell -AzureLocation westeurope
```

After this, go to your explainpowershell fork on GitHub. Under Actions, run the `Deploy Azure Infra` workflow, then the `Deploy app to Azure` workflow and run `./explainpowershell.helpwriter.ps1 -Force -IsProduction -ResourceGroupName $ResourceGroupName -StorageAccountName $StorageAccountName`.
The Url where you can reach your version of the project can be found in the Azure Portal. Under the storage account resource that was deployed, find the `Static Website` entry in the menu. It is the Url for `Primary Endpoint`. 
Alternatively, you can retrieve it with `az`:

```powershell
$myStorageAccountName = ".."
(az storage account show --name $myStorageAccountName | convertfrom-json).primaryEndpoints.web
```
