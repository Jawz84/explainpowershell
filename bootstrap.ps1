[CmdletBinding()]
param(
    [Switch]$Force
)

if ($IsLinux -and $env:DOTNET_RUNNING_IN_CONTAINER) {
    Write-Host -ForegroundColor Green 'We are running in a container, make sure we have permissions on all folders in the repo, to be able to build and run the application.'
    $testOwnershipAndPermissions = ls -l $PSScriptRoot | Select-String bootstrap -Raw

    if ($Force -or ($testOwnershipAndPermissions | Select-String root)) {
        Write-Host "Making 'vscode' owner of all files in '$PSScriptRoot/' (recursive)."
        sudo chown -R vscode:vscode $PSScriptRoot/
    }
}

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
$modules = 'Pester', 'Az.Storage', 'Posh-Git', 'Microsoft.PowerShell.UnixCompleters'
foreach ($module in $modules) {
    if (($m = Get-Module -ListAvailable $module)) {
        if (!$Force) {
            continue
        }
        else {
            Write-Host "Module '$module' version $($m.Version) already installed. Use -Force to update."
        }
    }

    Write-Host -ForegroundColor Green "Install PowerShell module '$module'.."
    Install-Module -Name $module -Force
}

Import-Module Posh-Git
if ($IsLinux) {
    dotnet dev-certs https
    Import-UnixCompleters
}
else {
    dotnet dev-certs https --trust
}

Write-Host -ForegroundColor Green "Checking PowerShell '`$profile.CurrentUserAllHosts'.."
$commandsToAddToProfile = @(
    'Import-Module Posh-Git',
    'if ($isLinux) {'
    '  Set-PSReadLineOption -EditMode Windows'
    '  Set-PSReadLineKeyHandler -Chord tab -Function MenuComplete'
    '  Set-UnixCompleter -ShellType Bash'
    '  Import-UnixCompleters'
    '}'
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Invoke-SyntaxAnalyzer.ps1"
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-HelpDatabaseData.ps1"
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Get-MetaData.ps1"
)

if ( !(Test-Path -Path $profile.CurrentUserAllHosts) ) {
    New-Item -Path $profile.CurrentUserAllHosts -Force -ItemType file | Out-Null
}

$profileContents = Get-Content -Path $profile.CurrentUserAllHosts
if ($null -eq $profileContents -or
    $profileContents.split("`n") -notcontains $commandsToAddToProfile[0]) {
    Write-Host -ForegroundColor Green 'Add settings to PowerShell profile'
    Add-Content -Path $profile.CurrentUserAllHosts -Value $commandsToAddToProfile
    # Copy profile contents to VSCode profile too: Microsoft.VSCode_profile.ps1
    Get-Content -Path $profile.CurrentUserAllHosts
    | Set-Content -Path ($profile.CurrentUserAllHosts
        | Split-Path -Parent
        | Join-Path -ChildPath 'Microsoft.VSCode_profile.ps1') -Force
}

if ($Force -or -not [bool](Get-ChildItem -Path / -Filter 'about_Pwsh.help.txt' -Recurse -Depth 3 -ErrorAction SilentlyContinue)) {
    Write-Host -ForegroundColor green 'Updating local PowerShell Help files..'
    # The `-UICulture en-us` param is important, because at container build, this script is called, but the UICulture is 
    # `Invariant Culture` which results in no help available. Setting it to `en-us` manually makes sure we have updated help.
    Update-Help -Force -ErrorAction SilentlyContinue -ErrorVariable updateErrors -UICulture en-us
    if ($updateErrors) {
        Write-Warning "$($updateErrors -join `"`n`")"
    }
}

$fileName = "$PSScriptRoot/explainpowershell.helpcollector/help.about_articles.cache.user"
if ($Force -or !(Test-Path $fileName)) {
    Write-Host -ForegroundColor green "Collecting about_.. article data and saving to cache file '$fileName'.."
    ./explainpowershell.helpcollector/aboutcollector.ps1
    | ConvertTo-Json
    | Set-Content -Path $fileName -Force
}
else {
    Write-Host "Detected cache file '$fileName', skipping collecting about_.. data. Use '-Force' or remove cache file to refresh about_.. data."
}

Write-Host 'Writing about_.. help article data to local Azurite table..'
./explainpowershell.helpcollector/helpwriter.ps1 -HelpDataCacheFilename $fileName

Import-Module "$PSScriptRoot/explainpowershell.analysisservice.tests/testfiles/myTestModule.psm1"

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

    if ($module.name -eq 'myTestModule') {
        Remove-Module myTestModule
        Import-Module "$PSScriptRoot/explainpowershell.analysisservice.tests/testfiles/myConflictingTestModule.psm1"
    }
}

Write-Host -ForegroundColor Green 'Running tests to see if everything works'
& $PSScriptRoot/explainpowershell.analysisservice.tests/Start-AllBackendTests.ps1

Write-Host -ForegroundColor Green "Done. You now have the functions 'Get-HelpDatabaseData', 'Invoke-SyntaxAnalyzer' and 'Get-MetaData' available for ease of testing."