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

## Update to latest Dotnet sdk

$availableDotnetSdkDockerImages = (Invoke-RestMethod https://mcr.microsoft.com/v2/dotnet/sdk/tags/list).tags -match '0-bullseye-slim-amd64$'
$latestDotnetSdkImage = $availableDotnetSdkDockerImages | Select-Object -Last 10 | Out-ConsoleGridView -Title "Pick a DotNet SDK image version to migrate to:" -OutputMode Single
$dotNetLatestVersion = ([version]($latestDotnetSdkImage.split('-')[0]))
$dockerFilePath = "$PSScriptRoot/../.devcontainer/Dockerfile"

$dockerFile = (Get-Content -Path $dockerFilePath).split([System.Environment]::NewLine)
for ($i = 0; $i -lt $dockerFile.Length; $i++) {
    if ($dockerFile[$i] -match 'ARG VARIANT=') {
        $dockerFile[$i] = "ARG VARIANT=$latestDotnetSdkImage"
    }
}
$dockerFile | Out-File -FilePath $dockerFilePath -Force

Get-ChildItem -Path $PSScriptRoot/.. *.csproj -Recurse -Depth 2 | ForEach-Object {
    $file = $_
    $xml = [xml](Get-Content $file)
    $xml.Project.PropertyGroup.TargetFramework = "net$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)"
    for ($i = 0; $i -lt $xml.Project.ItemGroup.Reference.Length; $i++) {
        if ($null -ne $xml.Project.ItemGroup.Reference[$i].HintPath) {
            $xml.Project.ItemGroup.Reference[$i].HintPath = ($xml.Project.ItemGroup.Reference[$i].HintPath -replace '\\net\d+\.\d+\\', "\net$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)\")
        }
    }
    $xml.Save( $file.FullName )
}

$vsCodeSettingsFile = "$PSScriptRoot/../.vscode/settings.json"
$settings = Get-Content -Path $vsCodeSettingsFile | ConvertFrom-Json
$settings.'azureFunctions.deploySubpath' = "bin/Release/net$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)/publish"
$settings | ConvertTo-Json | Out-File -Path $vsCodeSettingsFile -Force

$ghActionsDeployAppFilePath = "$PSScriptRoot/../.github/workflows/deploy_app.yml"
$ghDeployAction = Get-Content -Path $ghActionsDeployAppFilePath | ConvertFrom-Yaml
$ghDeployAction.jobs.buildFrontend.steps.with[0].'dotnet-version' = "$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)"
$ghDeployAction.jobs.buildBackend.steps.with[0].'dotnet-version' = "$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)"
$ghDeployAction.jobs.buildBackend.steps.with[1].path = "./explainpowershell.analysisservice/bin/Release/net$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)"
$ghDeployAction.jobs.buildFrontend.steps.with[1].path = "./explainpowershell.frontend/bin/Release/net$($dotNetLatestVersion.Major).$($dotNetLatestVersion.Minor)/publish/wwwroot"

$ghDeployAction
| ConvertTo-Yaml
| Set-Content -Path $ghActionsDeployAppFilePath -Force

## Update packages
$outdated = dotnet list "$PSScriptRoot/../explainpowershell.sln" package --outdated
$outdated+= dotnet list "$PSScriptRoot/../explainpowershell.analysisservice.tests/explainpowershell.analysisservice.tests.csproj" package --outdated

$targetVersions = $outdated | Select-String '> ' -Raw | ForEach-Object {
    [pscustomobject]@{
        PackageName   = $_.split('  ')[1].Trim(' > ').Trim()
        LatestVersion = [version]($_.split('  ')[-1].Trim())
    }
}

Get-ChildItem -Path $PSScriptRoot/.. *.csproj -Recurse -Depth 2 | ForEach-Object {
    $file = $_
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

Write-Host -foregroundcolor Magenta "All done. Usually it is a good idea to REBUILD the dev containers now."