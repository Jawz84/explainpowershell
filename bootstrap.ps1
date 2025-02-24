[CmdletBinding()]
param(
    [Switch]$Force
)

Write-Host -ForegroundColor Green 'Performing dotnet cleanup and setup..'
$env:DOTNET_NOLOGO = 'true'
dotnet restore
dotnet clean --verbosity minimal

try {
    $ErrorActionPreference = 'stop'
    Push-Location ./explainpowershell.analysisservice.tests/
    dotnet restore
    dotnet clean --verbosity minimal
    Pop-Location
}
catch {
    Write-Warning 'For a correct result, run this script from the root of the project.'
    break
}
finally {
    $ErrorActionPreference = 'continue'
}

Write-Host -ForegroundColor Green 'Checking PowerShell modules..'
$modules = @(
    @{Name = 'Pester'; MinimumVersion = '5.0.0'; AllowClobber = $true; Force = $true},
    'Az.Storage',
    'Posh-Git'
)
foreach ($module in $modules) {
    $moduleName = $module.GetType().Name -eq 'String' ? $module : $module.Name
    $version = $module.GetType().Name -eq 'Hashtable' ? $module.MinimumVersion : $null
    $force = $module.GetType().Name -eq 'Hashtable' ? $module.Force : $false
    $allowClobber = $module.GetType().Name -eq 'Hashtable' ? $module.AllowClobber : $false
    
    if (($m = Get-Module -ListAvailable $moduleName)) {
        if (!$Force) {
            if ($version -and ($m.Version -lt [Version]$version)) {
                Write-Host "Module '$moduleName' version $($m.Version) is below required version $version. Installing..."
                Remove-Module $moduleName -Force -ErrorAction SilentlyContinue
                Uninstall-Module $moduleName -AllVersions -Force -ErrorAction SilentlyContinue
                Install-Module -Name $moduleName -MinimumVersion $version -Force -Scope CurrentUser -AllowClobber:$allowClobber
            } else {
                continue
            }
        }
        else {
            Write-Host "Module '$moduleName' version $($m.Version) already installed. Force installing latest version..."
            Remove-Module $moduleName -Force -ErrorAction SilentlyContinue
            Uninstall-Module $moduleName -AllVersions -Force -ErrorAction SilentlyContinue
            Install-Module -Name $moduleName -Force -Scope CurrentUser -AllowClobber:$allowClobber
        }
    } else {
        Write-Host -ForegroundColor Green "Install PowerShell module '$moduleName'.."
        $params = @{
            Name = $moduleName
            Force = $force
            Scope = 'CurrentUser'  
            AllowClobber = $allowClobber
        }
        if ($version) {
            $params.MinimumVersion = $version
        }
        Install-Module @params
    }
}

Import-Module Posh-Git
dotnet dev-certs https --trust

Write-Host -ForegroundColor Green "Checking PowerShell '`$profile.CurrentUserAllHosts'.."
$commandsToAddToProfile = @(
    'Import-Module Posh-Git',
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Invoke-SyntaxAnalyzer.ps1",
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-HelpDatabaseData.ps1",
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-MetaData.ps1"
)

if (!(Test-Path -Path $profile.CurrentUserAllHosts)) {
    New-Item -Path $profile.CurrentUserAllHosts -Force -ItemType file | Out-Null
}

$profileContents = Get-Content -Path $profile.CurrentUserAllHosts
if ($null -eq $profileContents -or
    $profileContents.split("`n") -notcontains $commandsToAddToProfile[0]) {
    Write-Host -ForegroundColor Green 'Add settings to PowerShell profile'
    Add-Content -Path $profile.CurrentUserAllHosts -Value $commandsToAddToProfile
    # Copy profile contents to VSCode profile too
    Get-Content -Path $profile.CurrentUserAllHosts |
        Set-Content -Path ($profile.CurrentUserAllHosts | Split-Path -Parent | Join-Path -ChildPath 'Microsoft.VSCode_profile.ps1') -Force
}

if ($Force -or -not (Get-Help about_PowerShell -ErrorAction SilentlyContinue)) {
    Write-Host -ForegroundColor green 'Updating local PowerShell Help files..'
    Update-Help -Force -ErrorAction SilentlyContinue -ErrorVariable updateErrors -UICulture en-us
    if ($updateErrors) {
        Write-Warning "$($updateErrors -join `"`n`")"
    }
}

# Process help data
$fileName = "$PSScriptRoot/explainpowershell.helpcollector/help.about_articles.cache.user"
if ($Force -or !(Test-Path $fileName)) {
    Write-Host -ForegroundColor green "Collecting about_.. article data and saving to cache file '$fileName'.."
    ./explainpowershell.helpcollector/aboutcollector.ps1 |
        ConvertTo-Json |
        Set-Content -Path $fileName -Force
}
else {
    Write-Host "Detected cache file '$fileName', skipping collecting about_.. data. Use '-Force' or remove cache file to refresh about_.. data."
}

Write-Host 'Writing about_.. help article data to local Azurite table..'
./explainpowershell.helpcollector/helpwriter.ps1 -HelpDataCacheFilename $fileName

Import-Module "$PSScriptRoot/explainpowershell.analysisservice.tests/testfiles/myTestModule.psm1"
$modulesToProcess = Get-Content "$PSScriptRoot/explainpowershell.metadata/defaultModules.json" |
    ConvertFrom-Json

foreach ($module in $modulesToProcess) {
    $fileName = "$PSScriptRoot/explainpowershell.helpcollector/help.$($module.Name).cache.user"
    if ($Force -or !(Test-Path $fileName) -or ((Get-Item $fileName).Length -eq 0)) {
        Write-Host -ForegroundColor Green "Collecting help data for module '$($module.Name)'.."
        ./explainpowershell.helpcollector/helpcollector.ps1 -ModulesToProcess $module |
            ConvertTo-Json |
            Out-File -path $fileName -Force:$Force
    }
    else {
        Write-Host "Detected cache file '$fileName', skipping collecting help data. Use '-Force' or remove cache file to refresh help data."
    }
    Write-Host "Writing help for module '$($module.Name)' to local Azurite table.."
    ./explainpowershell.helpcollector/helpwriter.ps1 -HelpDataCacheFilename $fileName
    if ($module.name -eq 'myTestModule') {
        Remove-Module myTestModule
        Import-Module "$PSScriptRoot/explainpowershell.analysisservice.tests/testfiles/myConflictingTestModule.psm1"
    }
}

Write-Host -ForegroundColor Green 'Running tests to see if everything works'
& $PSScriptRoot/explainpowershell.analysisservice.tests/Start-AllBackendTests.ps1

Write-Host -ForegroundColor Green "Done. You now have the functions 'Get-HelpDatabaseData', 'Invoke-SyntaxAnalyzer' and 'Get-MetaData' available for ease of testing."