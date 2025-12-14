[CmdletBinding()]
<#
.SYNOPSIS
Bootstraps the explainpowershell repository for local development.

.DESCRIPTION
Validates required tooling (PowerShell, .NET SDK, Azure Functions Core Tools),
runs code generators, restores/cleans .NET projects, ensures required
PowerShell modules are installed, optionally updates your PowerShell profile
with helper imports, refreshes local PowerShell help, populates local Azurite
tables with collected help data, and finally runs the test suite.

Run this script from the repository root for correct relative paths.

.PARAMETER Force
Forces updates and refreshes (for example: reinstalling modules, updating help,
and rebuilding cache files) even when existing installations/cache files are
detected.

.PARAMETER UpdateProfile
Updates $profile.CurrentUserAllHosts with helper imports used by the test and
analysis scripts. In interactive sessions, the script will prompt before
updating the profile unless this switch is provided.

.EXAMPLE
PS> ./bootstrap.ps1
Runs the bootstrap process and prompts before updating your profile (when
interactive).

.EXAMPLE
PS> ./bootstrap.ps1 -Force
Forces module/help/cache refresh and re-runs the bootstrap process.

.EXAMPLE
PS> ./bootstrap.ps1 -UpdateProfile
Runs bootstrap and updates your PowerShell profile with helper imports without
prompting.

.NOTES
Requires PowerShell 7.4+ and .NET 10 SDK.
On Windows, dev-certs are trusted; on Linux, dev-certs are generated.
#>
param(
    [Switch]$Force,
    [Switch]$UpdateProfile
)

$minPwsh = [Version]'7.4'
if ($PSVersionTable.PSVersion -lt $minPwsh) {
    throw "PowerShell $minPwsh or newer is required. Current: $($PSVersionTable.PSVersion). ``winget install Microsoft.PowerShell``"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw "dotnet CLI not found. Install .NET 10 SDK: ``winget install Microsoft.DotNet.SDK.10``" }

$hasNet10 = & $dotnet.Source --list-sdks | Select-String '^10\.' 
if (-not $hasNet10) { throw ".NET 10 SDK missing. ``winget install Microsoft.DotNet.SDK.10``" }

$funcCli = Get-Command func -ErrorAction SilentlyContinue
if (-not $funcCli -or -not (& $funcCli.Source --version) -match '^4\.') {
    Write-Warning "Azure Functions Core Tools v4 not detected. ``winget install Microsoft.Azure.FunctionsCoreTools``"
}

Write-Host -ForegroundColor Green 'Run all code generators..'
Get-ChildItem -Path $PSScriptRoot/explainpowershell.analysisservice/ -Recurse -Filter *_code_generator.ps1 | ForEach-Object { & $_.FullName }


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
$modules = @('Pester', 'Az.Storage', 'Posh-Git')
if ($IsLinux) {
    $modules += 'Microsoft.PowerShell.UnixCompleters'
}
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
    if (Get-Module -ListAvailable Microsoft.PowerShell.UnixCompleters) {
        Import-UnixCompleters
    }
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
    ". $PSScriptRoot/explainpowershell.analysisservice.tests/Invoke-AiExplanation.ps1"
)

$isInteractive = $null -ne $Host.UI -and $null -ne $Host.UI.RawUI -and -not $env:CI -and -not $env:GITHUB_ACTIONS

$profileNeedsUpdate = $false
if (Test-Path -Path $profile.CurrentUserAllHosts) {
    $profileContents = Get-Content -Path $profile.CurrentUserAllHosts
    if ($null -eq $profileContents -or $profileContents.split("`n") -notcontains $commandsToAddToProfile[0]) {
        $profileNeedsUpdate = $true
    }
}
else {
    $profileNeedsUpdate = $true
}

$shouldUpdateProfile = $UpdateProfile
if (-not $shouldUpdateProfile -and $profileNeedsUpdate) {
    if ($isInteractive) {
        $answer = Read-Host "Update PowerShell profile '$($profile.CurrentUserAllHosts)' with helper imports? (y/N)"
        $shouldUpdateProfile = $answer -imatch '^(y|yes)$'
    }
    else {
        Write-Host "Skipping profile update (non-interactive). Re-run with -UpdateProfile to enable." 
    }
}

if ($shouldUpdateProfile -and $profileNeedsUpdate) {
    if ( !(Test-Path -Path $profile.CurrentUserAllHosts) ) {
        New-Item -Path $profile.CurrentUserAllHosts -Force -ItemType file | Out-Null
    }

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
& $PSScriptRoot/explainpowershell.analysisservice.tests/Start-AllTests.ps1 -Output Detailed

Write-Host -ForegroundColor Green "Done. You now have the functions 'Get-HelpDatabaseData', 'Invoke-SyntaxAnalyzer' and 'Get-MetaData' available for ease of testing."