# Explain PowerShell

PowerShell version of [explainshell.com](explainshell.com)

On ExplainShell.com, you can enter a Linux terminal oneliner, and the site will analyze it, and return snippets from the proper man-pages, in an effort to explain the oneliner. 
I would like to create the same thing but for PowerShell. 

I have a proof of concept running [here](https://storageexplainpowershell.z6.web.core.windows.net/).

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

## Deploying to Azure

To deploy this to your own Azure environment, you can use the `deploy.ps1` script.
- Create a storage account in Azure
- In the root of the repo, create a file `storageaccountkey.user` and in that, paste the storage account key for the account you have created.
- 