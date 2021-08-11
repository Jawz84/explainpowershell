[CmdletBinding()]
param(
    [Switch]$Force
)

if ($IsLinux && $env:DOTNET_RUNNING_IN_CONTAINER) {
    Write-Host -ForegroundColor Green "We are running in a container, make sure we have permissions on all folders in the repo, to be able to build and run the application."
    $testOwnershipAndPermissions = ls -l $PSScriptRoot | Select-String bootstrap -Raw

    if ($Force -or ($testOwnershipAndPermissions | Select-String root)) {
        Write-Host "Making 'vscode' owner of all files in '$PSScriptRoot/' (recursive)."
        sudo chown -R vscode:vscode $PSScriptRoot/
    }
}

Write-Host -ForegroundColor Green "Downloading all C# dependencies (dotnet restore).."
$env:DOTNET_NOLOGO='true'
dotnet restore

Write-Host -ForegroundColor Green "Checking PowerShell modules.."
$modules = 'Pester', 'Az', 'Posh-Git', 'Microsoft.PowerShell.UnixCompleters'
foreach ($module in $modules) {
    if (($m = Get-Module -ListAvailable $module)) {
        Write-Host "Module '$module' version $($m.Version) already installed. Use -Force to update."
        if (!$Force) {
            continue
        }
    }

    Write-Host -ForegroundColor Green "Install PowerShell module '$module'.."
    Install-Module -Name $module -Force
}

Import-Module Posh-Git
Import-UnixCompleters

Write-Host -ForegroundColor Green "Checking PowerShell '`$profile.CurrentUserAllHosts'.."
$commandsToAddToProfile = @(
    'Import-Module Posh-Git',
    'Set-PSReadLineOption -EditMode Windows'
    'Set-PSReadLineKeyHandler -Chord tab -Function MenuComplete'
    'Set-UnixCompleter -ShellType Bash'
    'Import-UnixCompleters'
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Invoke-SyntaxAnalyzer.ps1"
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-HelpDatabaseData.ps1"
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-MetaData.ps1"
)

if ( !(Test-Path -path $profile.CurrentUserAllHosts) ) {
    New-Item -path $profile.CurrentUserAllHosts -Force -ItemType file | Out-Null
}

$profileContents = Get-Content -path $profile.CurrentUserAllHosts
if ($null -eq $profileContents -or
    $profileContents.split("`n") -notcontains $commandsToAddToProfile[0])
{
    Write-Host -ForegroundColor Green "Add settings to PowerShell profile"
    Add-Content -Path $profile.CurrentUserAllHosts -Value $commandsToAddToProfile
    # Copy profile contents to VSCode profile too: Microsoft.VSCode_profile.ps1
    Get-Content -Path $profile.CurrentUserAllHosts
        | Set-Content -Path ($profile.CurrentUserAllHosts
        | Split-Path -Parent
        | Join-Path -ChildPath 'Microsoft.VSCode_profile.ps1') -Force
}

if (!(Test-Path '~/.local/share/powershell/Help/en-US/about_History.help.txt')) {
    Write-Host -Foregroundcolor green "Updating local PowerShell Help files.."
    Update-Help -Force -ErrorAction SilentlyContinue -ErrorVariable updateerrors
    Write-Warning "$($updateerrors -join `"`n`")"
}

# TODO:
# Add explainpowershell.aboutcollector.ps1

$modulesToProcess = Get-Content "$PSScriptRoot/explainpowershell.metadata/defaultModules.json"
| ConvertFrom-Json

foreach ($module in $modulesToProcess) {
    $fileName = "$PSScriptRoot/explainpowershell.helpcollector/help.$($module.Name).cache.user"

    if ($Force -or !(Test-Path $fileName) -or ((Get-Item $fileName).Length -eq 0)) {
        Write-Host -ForegroundColor Green "Collecting help data for module '$($module.Name)'.."
        ./explainpowershell.helpcollector/helpcollector.ps1 -ModulesToProcess $module
        | ConvertTo-Json
        | Out-File -path $fileName -Force:$Force
    }
    else {
        Write-Host "Detected cache file '$fileName', skipping collecting help data. Use '-Force' or remove cache file to refresh help data."
    }

    Write-Host "Writing help for module '$($module.Name)' to local Azurite table.."
    ./explainpowershell.helpcollector/helpwriter.ps1 -HelpDataCacheFilename $fileName
}

Write-host -ForegroundColor Green "Running tests to see if everything works"
& $PSScriptRoot/explainpowershell.analysisservice.tests/Start-BackendIntegrationTests.ps1

Write-host -ForegroundColor Green "Done. You now have the functions 'Get-HelpDatabaseData', 'Invoke-SyntaxAnalyzer' and 'Get-MetaData' available for ease of testing."