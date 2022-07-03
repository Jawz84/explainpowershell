using namespace Microsoft.PowerShell.Commands

Write-Host -Foregroundcolor green "Get all built-in 'about_..' articles, and get missing short descriptions from text.."
$aboutArticles = Get-Help About_*

# filter only Microsoft built-in ones
$abouts = $aboutArticles | Where-Object {-not $_.synopsis} 

foreach ($about in $abouts) {
    $baseUrl = 'https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/'
    [BasicHtmlWebResponseObject]$result = $null
    try {
        $result = Invoke-WebRequest -Uri ($baseUrl + $about.name) -ErrorAction SilentlyContinue
    }
    catch {
        try {
            $baseUrl = $baseUrl.replace("microsoft.powershell.core/about","Microsoft.PowerShell.Security/About")
            $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name)
        }
        catch {
            try {
                $baseUrl = $baseUrl.replace("Microsoft.PowerShell.Security/About","microsoft.wsman.management/about")
                $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name)
            }
            catch {
                Write-Warning "Cannot find online help for about_.. article '$($about.name)'"
            }
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

    [PSCustomObject]@{
        CommandName = $about.name
        DocumentationLink = if ($null -ne $result) {$baseUrl + $about.name}
        Synopsis = $synopsis
    }
}