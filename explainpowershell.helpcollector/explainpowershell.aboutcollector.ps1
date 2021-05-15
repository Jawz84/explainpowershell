using namespace Microsoft.PowerShell.Commands

. .\classes.ps1

if (!(Test-Path '~/.local/share/powershell/Help/en-US/about_History.help.txt')) {
    Write-Host -Foregroundcolor green "Updating local PowerShell Help files.."
    Update-Help -Force -ErrorAction SilentlyContinue
}

# about_.. articles

Write-Host -Foregroundcolor green "Get all built-in 'about_..' articles, and get missing short descriptions from text.."
$aboutArticles = Get-Help About_*
# filter only Microsoft built-in ones
$abouts = $aboutArticles | Where-Object {-not $_.synopsis} 

foreach ($about in $abouts) {
    $baseUrl = 'https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/'
    [BasicHtmlWebResponseObject]$result = $null
    try {
        $result = Invoke-WebRequest -Uri ($baseUrl + $about.name) -ErrorAction SilentlyContinue -TimeoutSec 1
    }
    catch {
        try {
            $baseUrl = $baseUrl.replace("microsoft.powershell.core/about","Microsoft.PowerShell.Security/About")
            $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name) -TimeoutSec 1
        }
        catch {
            $baseUrl = $baseUrl.replace("Microsoft.PowerShell.Security/About","microsoft.wsman.management/about")
            $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name) -TimeoutSec 1
        }
    }

    $selectedContext = $about.ToString().Split("`n") 
        | Select-String "short description" -Context 5

    if ($selectedContext) {
        $synopsis = (
            (
                    $selectedContext.Context.PostContext.Split("`n")
                    | Where-Object {-not [string]::IsNullOrWhiteSpace($_) }
            ) -join ' '
        ).Replace("`r", '').Replace("`n", '')

        $hasLongSubscription = $synopsis.IndexOf("long description", [stringcomparison]::OrdinalIgnoreCase)
        if ($hasLongSubscription -gt 0) {
            $synopsis = $synopsis.Substring(0, $hasLongSubscription)
        }
    }

    [HelpData]@{
        CommandName = $about.name
        DocumentationLink = if ($null -ne $result) {$baseUrl + $about.name}
        Synopsis = $synopsis
    }
}