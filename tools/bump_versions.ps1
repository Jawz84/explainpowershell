$ErrorActionPreference = 'stop'

[CmdletBinding()]
param(
    [string]$TargetDotnetVersion
)

$currentGitBranch = git branch --show-current
if ($currentGitBranch -eq 'main') {
    throw "You are on 'main' branch, create a new branch first"
}

function Ensure-Module {
    param([string]$Name)
    if (-not (Get-Module -ListAvailable $Name)) {
        Install-Module -Name $Name -Force
    }
}

Ensure-Module -Name 'powershell-yaml'

function Get-TargetDotnetVersion {
    param([string]$Override)
    if ($Override) { return [version]$Override }
    $sdks = dotnet --list-sdks | ForEach-Object { [version]($_.Split(' ')[0]) }
    if (-not $sdks) {
        throw 'No .NET SDKs found. Install a recent SDK or provide -TargetDotnetVersion.'
    }
    return ($sdks | Sort-Object | Select-Object -Last 1)
}

$dotNetVersion = Get-TargetDotnetVersion -Override $TargetDotnetVersion
$dotNetShortVersion = '{0}.{1}' -f $dotNetVersion.Major, $dotNetVersion.Minor
$functionsToolsVersion = try { [version](func --version) } catch { [version]'4.0.0' }

Write-Host "Updating project files to net$dotNetShortVersion"

Get-ChildItem -Path "$PSScriptRoot/.." -Filter *.csproj -Recurse -Depth 2 | ForEach-Object {
    $xml = [xml](Get-Content $_.FullName)
    foreach ($group in @($xml.Project.PropertyGroup)) {
        if ($group.TargetFramework) {
            $group.TargetFramework = "net$dotNetShortVersion"
        }
    }
    foreach ($itemGroup in @($xml.Project.ItemGroup)) {
        foreach ($reference in @($itemGroup.Reference)) {
            if ($reference.HintPath) {
                $reference.HintPath = $reference.HintPath -replace '\\net\d+\.\d+\\', "\net$dotNetShortVersion\"
            }
        }
    }
    $xml.Save($_.FullName)
}

$vsCodeSettingsFile = "$PSScriptRoot/../.vscode/settings.json"
if (Test-Path $vsCodeSettingsFile) {
    $settings = Get-Content -Path $vsCodeSettingsFile | ConvertFrom-Json
    $settings.'azureFunctions.deploySubpath' = "bin/Release/net$dotNetShortVersion/publish"
    $settings.'azureFunctions.projectRuntime' = "~$($functionsToolsVersion.Major)"
    $settings | ConvertTo-Json -Depth 5 | Out-File -Path $vsCodeSettingsFile -Force
}

$deployAppWorkflow = "$PSScriptRoot/../.github/workflows/deploy_app.yml"
if (Test-Path $deployAppWorkflow) {
    $ghDeployAction = Get-Content -Path $deployAppWorkflow | ConvertFrom-Yaml
    foreach ($jobName in 'buildFrontend','buildBackend') {
        $job = $ghDeployAction.jobs.$jobName
        if ($null -eq $job) { continue }
        foreach ($step in $job.steps) {
            if ($step.with.'dotnet-version') {
                $step.with.'dotnet-version' = $dotNetShortVersion
            }
            if ($step.with.path) {
                $step.with.path = $step.with.path -replace 'net\d+\.\d+', "net$dotNetShortVersion"
            }
        }
    }
    $ghDeployAction
    | ConvertTo-Yaml
    | Set-Content -Path $deployAppWorkflow -Force
}

$deployInfraWorkflow = "$PSScriptRoot/../.github/workflows/deploy_azure_infra.yml"
if (Test-Path $deployInfraWorkflow) {
    $ghDeployInfra = Get-Content -Path $deployInfraWorkflow | ConvertFrom-Yaml
    foreach ($step in $ghDeployInfra.jobs.deploy.steps) {
        if ($step.run) {
            $step.run = $step.run -replace 'FUNCTIONS_EXTENSION_VERSION=~\d+', "FUNCTIONS_EXTENSION_VERSION=~$($functionsToolsVersion.Major)"
        }
    }
    $ghDeployInfra
    | ConvertTo-Yaml
    | Set-Content -Path $deployInfraWorkflow -Force
}

## Update packages (mirrors Pester checks: dotnet list package --outdated)
$outdated = dotnet list "$PSScriptRoot/../explainpowershell.sln" package --outdated
$outdated += dotnet list "$PSScriptRoot/../explainpowershell.analysisservice.tests/explainpowershell.analysisservice.tests.csproj" package --outdated

$targetVersions = $outdated | Select-String '^   >' | ForEach-Object {
    $parts = ($_ -split '\s{2,}') | Where-Object { $_ }
    [pscustomobject]@{
        PackageName   = $parts[0].Trim('>',' ')
        LatestVersion = [version]$parts[-1]
    }
}

Get-ChildItem -Path "$PSScriptRoot/.." -Filter *.csproj -Recurse -Depth 2 | ForEach-Object {
    $xml = [xml](Get-Content $_.FullName)
    foreach ($itemGroup in @($xml.Project.ItemGroup)) {
        foreach ($package in @($itemGroup.PackageReference)) {
            if ($package.Include -in $targetVersions.PackageName) {
                $latest = ($targetVersions | Where-Object PackageName -EQ $package.Include).LatestVersion
                if ($latest) {
                    $package.Version = $latest.ToString()
                }
            }
        }
    }
    $xml.Save($_.FullName)
}

Push-Location $PSScriptRoot/..
dotnet restore
dotnet clean --verbosity minimal
Pop-Location

Write-Host -ForegroundColor Magenta 'Version bump complete.'