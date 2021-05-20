if ($IsLinux && $env:DOTNET_RUNNING_IN_CONTAINER) {
    Write-Host -ForegroundColor Green "We are running in a container, make sure we have permissions on all folders in the repo, to be able to build and run the application."
    $testOwnershipAndPermissions = ls -l $PSScriptRoot | Select-String bootstrap -Raw 

    if ($testOwnershipAndPermissions | Select-String root) {
        Write-Host "Making 'vscode' owner of all files in '$PSScriptRoot/' (recursive)."
        sudo chown -R vscode:vscode $PSScriptRoot/
    }
}

Write-Host -ForegroundColor Green "Downloading all C# dependencies (dotnet restore).."
dotnet restore


$modules = 'Pester', 'Az', 'AzTable', 'Posh-Git'
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

$commandsToAddToProfile = @(
    'Import-Module Posh-Git', 
    'Set-PSReadLineOption -EditMode Windows'
)

if ( !(Test-Path -path $profile.CurrentUserAllHosts) ) {
    New-Item -path $profile.CurrentUserAllHosts -Force -ItemType file | Out-Null
}
if ((Get-Content -path $profile.CurrentUserAllHosts).split("`n") -notcontains $commandsToAddToProfile[0]) {
    Add-Content -Path $profile.CurrentUserAllHosts -Value $commandsToAddToProfile
}

Write-Host -ForegroundColor Green "Fill local database with help data.."
& $PSScriptRoot\explainpowershell.helpcollector\explainpowershell.helpwriter.ps1

Write-host -ForegroundColor Green "Running tests to see if everything works"
& $PSScriptRoot/Tests/launch.ps1
