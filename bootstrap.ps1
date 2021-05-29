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
dotnet restore

Write-Host -ForegroundColor Green "Checking PowerShell modules.."
$modules = 'Pester', 'Az', 'AzTable', 'Posh-Git', 'Microsoft.PowerShell.UnixCompleters'
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
    '. ./Tests/Invoke-SyntaxAnalyzer.ps1'
    '. ./Tests/Get-HelpDatabaseData.ps1'
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

Write-Host -ForegroundColor Green "Fill local database with help data.."
& $PSScriptRoot\explainpowershell.helpcollector\explainpowershell.helpwriter.ps1 -Force:$Force

Write-host -ForegroundColor Green "Running tests to see if everything works"
& $PSScriptRoot/Tests/launch.ps1

Write-host -ForegroundColor Green "Done. You now have the functions 'Get-HelpDatabaseData' and 'Invoke-SyntaxAnalyzer' available for ease of testing."