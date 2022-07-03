$ErrorActionPreference = 'stop'
$currentGitBranch = git branch --show-current
if ($currentGitBranch -eq 'main') {
    throw "You are on 'main' branch, create a new branch first"
}

## Update to latest Azurite
$dockerAzuriteImageTagList = Invoke-RestMethod 'https://mcr.microsoft.com/v2/azure-storage/azurite/tags/list'
$latestAvailableAzuriteVersionTag = $dockerAzuriteImageTagList.tags
| ForEach-Object { $_ -as [version] }
| Measure-Object -Maximum
$latestAvailableAzuriteVersionString = $latestAvailableAzuriteVersionTag.Maximum.ToString()
$dockerComposeFile = "$PSScriptRoot/../.devcontainer/docker-compose.yml"

$requiredModules = 'Microsoft.PowerShell.ConsoleGuiTools', 'powershell-yaml'

foreach ($module in $requiredModules) {
    if (-not (Get-Module -ListAvailable $module)) {
        Install-Module -Name $module -Force
    }
}

$compose = Get-Content -Path $dockerComposeFile | ConvertFrom-Yaml

$compose.services.database.image = "mcr.microsoft.com/azure-storage/azurite:$latestAvailableAzuriteVersionString"

$compose
| ConvertTo-Yaml
| Set-Content -Path $dockerComposeFile -Force

## Update to latest Dotnet sdk, Func Host Core Tools and PowerShell

$availableDotnetSdkDockerImages = (Invoke-RestMethod https://mcr.microsoft.com/v2/dotnet/sdk/tags/list).tags -match '0-bullseye-slim-amd64$'
$latestDotnetSdkImage = $availableDotnetSdkDockerImages | Select-Object -Last 10 | Out-ConsoleGridView -Title 'Pick a DotNet SDK image version to migrate to:' -OutputMode Single
$dotNetLatestVersion = ([version]($latestDotnetSdkImage.split('-')[0]))
$dotNetShortVersion = "$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)"
$dockerFilePath = "$PSScriptRoot/../.devcontainer/Dockerfile"
$funcUrl = [System.Net.HttpWebRequest]::Create('https://github.com/Azure/azure-functions-core-tools/releases/latest').GetResponse().ResponseUri.AbsoluteUri
$latestFuncVersion = [version]($funcUrl | Split-Path -Leaf)
$pwshUrl = [System.Net.HttpWebRequest]::Create('https://github.com/PowerShell/PowerShell/releases/latest').GetResponse().ResponseUri.AbsoluteUri
$latestPwshVersion = [version]($pwshUrl | Split-Path -Leaf).trim('v')

$dockerFile = (Get-Content -Path $dockerFilePath).split([System.Environment]::NewLine)
for ($i = 0; $i -lt $dockerFile.Length; $i++) {
    if ($dockerFile[$i] -match 'ARG VARIANT=') {
        $dockerFile[$i] = "ARG VARIANT=$latestDotnetSdkImage"
    }
    elseif ($dockerFile[$i] -match 'ARG FUNCTOOLSVERSION=') {
        $dockerFile[$i] = "ARG FUNCTOOLSVERSION=$latestFuncVersion"
    }
    elseif ($dockerFile[$i] -match 'ARG PWSHVERSION=') {
        $dockerFile[$i] = "ARG PWSHVERSION=$latestPwshVersion"
    }
}
$dockerFile | Out-File -FilePath $dockerFilePath -Force

Get-ChildItem -Path $PSScriptRoot/.. *.csproj -Recurse -Depth 2 | ForEach-Object {
    $file = $_
    $xml = [xml](Get-Content $file)
    $xml.Project.PropertyGroup.TargetFramework = "net$dotNetShortVersion"
    for ($i = 0; $i -lt $xml.Project.ItemGroup.Reference.Length; $i++) {
        if ($null -ne $xml.Project.ItemGroup.Reference[$i].HintPath) {
            $xml.Project.ItemGroup.Reference[$i].HintPath = ($xml.Project.ItemGroup.Reference[$i].HintPath -replace '\\net\d+\.\d+\\', "\net$dotNetShortVersion\")
        }
    }
    $xml.Save( $file.FullName )
}

$vsCodeSettingsFile = "$PSScriptRoot/../.vscode/settings.json"
$settings = Get-Content -Path $vsCodeSettingsFile | ConvertFrom-Json
$settings.'azureFunctions.deploySubpath' = "bin/Release/net$dotNetShortVersion/publish"
$settings.'azureFunctions.projectRuntime' = "~$($latestFuncVersion.Major)"
$settings | ConvertTo-Json | Out-File -Path $vsCodeSettingsFile -Force

$ghActionsDeployAppFilePath = "$PSScriptRoot/../.github/workflows/deploy_app.yml"
$ghDeployAction = Get-Content -Path $ghActionsDeployAppFilePath | ConvertFrom-Yaml
for ($i = 0; $i -lt $ghDeployAction.jobs.buildFrontend.steps.with.Count; $i++) {
    if ( $ghDeployAction.jobs.buildFrontend.steps.with[$i].'dotnet-version' ) {
        $ghDeployAction.jobs.buildFrontend.steps.with[$i].'dotnet-version' = $dotNetShortVersion
    }
}
for ($i = 0; $i -lt $ghDeployAction.jobs.buildBackend.steps.with.Count; $i++) {
    if ( $ghDeployAction.jobs.buildBackend.steps.with[$i].'dotnet-version' ) {
        $ghDeployAction.jobs.buildBackend.steps.with[$i].'dotnet-version' = $dotNetShortVersion
    }
}
for ($i = 0; $i -lt $ghDeployAction.jobs.buildFrontend.steps.with.Count; $i++) {
    if ( $ghDeployAction.jobs.buildFrontend.steps.with[$i].path ) {
        $ghDeployAction.jobs.buildFrontend.steps.with[$i].path = $ghDeployAction.jobs.buildFrontend.steps.with[$i].path -replace 'net\d+\.\d+', "net$dotNetShortVersion"
    }
}
for ($i = 0; $i -lt $ghDeployAction.jobs.buildBackend.steps.with.Count; $i++) {
    if ( $ghDeployAction.jobs.buildBackend.steps.with[$i].path ) {
        $ghDeployAction.jobs.buildBackend.steps.with[$i].path = $ghDeployAction.jobs.buildBackend.steps.with[$i].path -replace 'net\d+\.\d+', "net$dotNetShortVersion"
    }
}

$ghDeployAction
| ConvertTo-Yaml
| Set-Content -Path $ghActionsDeployAppFilePath -Force

$ghActionsDeployAzureInfraFilePath = "$PSScriptRoot/../.github/workflows/deploy_azure_infra.yml"
$ghDeployAction = Get-Content -Path $ghActionsDeployAzureInfraFilePath | ConvertFrom-Yaml
for ($i = 0; $i -lt $ghDeployAction.jobs.deploy.steps.Count; $i++) {
    if ( $ghDeployAction.jobs.deploy.steps[$i].run ) {
        $ghDeployAction.jobs.deploy.steps[$i].run = $ghDeployAction.jobs.deploy.steps[$i].run -replace 'FUNCTIONS_EXTENSION_VERSION=~\d+', "FUNCTIONS_EXTENSION_VERSION=~$($latestFuncVersion.Major)"
    }
}

$ghDeployAction
| ConvertTo-Yaml
| Set-Content -Path $ghActionsDeployAzureInfraFilePath -Force

## Update packages
$outdated = dotnet list "$PSScriptRoot/../explainpowershell.sln" package --outdated
$outdated += dotnet list "$PSScriptRoot/../explainpowershell.analysisservice.tests/explainpowershell.analysisservice.tests.csproj" package --outdated

$targetVersions = $outdated | Select-String '> ' -Raw | ForEach-Object {
    [pscustomobject]@{
        PackageName   = $_.split('  ')[1].Trim(' > ').Trim()
        LatestVersion = [version]($_.split('  ')[-1].Trim())
    }
}

$files = Get-ChildItem -Path $PSScriptRoot/.. *.csproj -Recurse -Depth 2 
foreach ($file in $files) {
    $xml = [xml](Get-Content $file)

    for ($i = 0; $i -lt $xml.project.ItemGroup.PackageReference.Length; $i++) {
        $package = @($xml.project.ItemGroup.PackageReference)[$i]
        if ($package.Include -in $targetVersions.PackageName) {
            $latest = ( $targetVersions | Where-Object PackageName -EQ $package.Include ).LatestVersion
            if ($latest) {
                $package.Version = $latest.ToString()
            }
        }
    }

    $xml.Save( $file.FullName )
}

Write-Host -ForegroundColor Magenta 'All done. Usually it is a good idea to REBUILD the dev containers now.'