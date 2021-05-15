if ($IsLinux && $env:DOTNET_RUNNING_IN_CONTAINER) {
    Write-Host -ForegroundColor Green "We are running in a container, make sure we have permissions on all folders in the repo, to be able to build and run the application."
    $testOwnershipAndPermissions = ls -l $PSScriptRoot | Select-String bootstrap -Raw 

    if ($testOwnershipAndPermissions | Select-String root) {
        Write-Host "Making 'vscode' owner of all files in '$PSScriptRoot/' (recursive)."
        sudo chown -R vscode:vscode $PSScriptRoot/
    }

    # if ($testOwnershipAndPermissions[8] -eq '-') {
    #     Write-Host "Granting 777 permission on all files in '$PSScriptRoot/' (recursive)."
    #     sudo chmod 777 -R $PSScriptRoot/
    # }
}

Write-Host -ForegroundColor Green "Downloading all C# dependencies (dotnet restore).."
dotnet restore

Write-Host -ForegroundColor Green "Fill local database with help data.."
& $PSScriptRoot\explainpowershell.helpcollector\explainpowershell.helpwriter.ps1

Write-Host -ForegroundColor Green "Install Pester test framework.."
Install-Module Pester -Force

Write-host -ForegroundColor Green "Running tests to see if everything works"
& $PSScriptRoot/Tests/launch.ps1
