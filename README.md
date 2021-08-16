# Explain PowerShell

PowerShell version of [explainshell.com](explainshell.com)

On ExplainShell.com, you can enter a Linux terminal oneliner, and the site will analyze it, and return snippets from the proper man-pages, in an effort to explain the oneliner. 

On https://www.explainpowershell.com you can do the same, but for PowerShell :)
Currently all basic commands are supported, as wel as a lot of mainstream modules. You can request support for your module [via this issue](https://github.com/Jawz84/explainpowershell/issues/43).

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
- MAKE SURE THAT THE FILE `.devcontainer\library-scripts\azcli-debian.sh` HAS 'LF' LINE ENDINGS (NOT CRLF).
  You can very easily convert them to LF in VSCode by opening the file, going to the statusbar in the bottom right, you will find it saying either LF or CRLF. If it says CRLF, change it to LF and save the file.
- Open in VSCode, and accept the offer to open in development container
- Container is built, and automatically runs the `bootstrap.ps1` script which will:
    - Check permissions on your repo so dotnet works without sudo
    - Perform `dotnet restore`
    - Install all necessary PowerShell modules
    - Fill local Azurite Table emulator with the necessary database
    - Run all tests for you, so you know everything is working

There are multiple preconfigured launch configurations and tasks. Use the `Watch run ..` tasks if you want to iterate quickly without debugging (these use dotnet watch under the hood).

### Access to local emulated db

The local emulated db lives in the Azurite container. This container is automatically started when you open the repository in a Development Container. It should be accessible through `http://localhost:10002/devstoreaccount1/HelpData` with for instance [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/), with the default development keys. See [Azurite documentation](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite) for more info. 
Keep in mind that the Azurite container access works with timing based auth. If the docker container clock deviates from the system clock, you cannot authenticate. On Windows, this has been a bug, that is fixed in WSL2 kernel `5.10.16.3`. To see your WSL2 kernel version, use `uname -r`. [Read more information](https://devblogs.microsoft.com/commandline/servicing-the-windows-subsystem-for-linux-wsl-2-linux-kernel/#bug-fix-clock-sync)

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

In the current production environment, I also have App Insights enabled, and set up an Azure CDN, DNS zone and an App Service Domain. This is enough to set up a `www.explainpowershell.com` website, also set up HTTPS through CDN manged HTTPS in Azure. To also allow access to the naked domain `explainpowerhell.com` (without the `www.`), I followed [Onboard a root or apex domain](https://docs.microsoft.com/en-us/azure/cdn/onboard-apex-domain). Alas, CDN managed HTTPS support for apex domains is no more. See #40.
