function ConvertTo-LearnDocumentationUri {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Uri
    )

    $normalized = $Uri.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $Uri
    }

    # Prefer HTTPS
    $normalized = $normalized -replace '^http://', 'https://'

    # docs.microsoft.com redirects to learn.microsoft.com; normalize for consistency.
    $normalized = $normalized -replace '^https://docs\.microsoft\.com', 'https://learn.microsoft.com'

    return $normalized
}

function Get-TitleFromHtml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Html
    )

    $m = [regex]::Match($Html, '<h1[^>]*>(.*?)</h1>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($m.Success) {
        return ([System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value) -replace '<[^>]+>', '').Trim()
    }

    return $null
}

function Get-SynopsisFromHtml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Html,

        [Parameter()]
        [string]$Cmd
    )

    if ([string]::IsNullOrWhiteSpace($Html)) {
        return $null
    }

    $regexOptions = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline

    # 1) Learn pages usually expose a meta description.
    $meta = [regex]::Match($Html, '<meta\s+name=["\'']description["\'']\s+content=["\'']([^"\'']+)["\'']\s*/?>', $regexOptions)
    if ($meta.Success) {
        return ([System.Net.WebUtility]::HtmlDecode($meta.Groups[1].Value)).Trim()
    }

    # 2) Learn pages often have a summary paragraph.
    $summary = [regex]::Match($Html, '<p[^>]*class=["\'']summary["\''][^>]*>(.*?)</p>', $regexOptions)
    if ($summary.Success) {
        $text = $summary.Groups[1].Value
        $text = [System.Net.WebUtility]::HtmlDecode($text)
        $text = ($text -replace '<[^>]+>', '').Trim()
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            return $text
        }
    }

    # 3) About_language_keywords sometimes has a per-keyword section.
    if (-not [string]::IsNullOrWhiteSpace($Cmd)) {
        $escapedCmd = [regex]::Escape($Cmd)
        $pattern = '<h2[^>]*id=[''"]' + $escapedCmd + '[''"][^>]*>.*?</h2>\s*<p[^>]*>(.*?)</p>'
        $section = [regex]::Match($Html, $pattern, $regexOptions)
        if ($section.Success) {
            $text = $section.Groups[1].Value
            $text = [System.Net.WebUtility]::HtmlDecode($text)
            $text = ($text -replace '<[^>]+>', '').Trim()
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                return $text
            }
        }
    }

    return $null
}

function Get-SynopsisFromUri {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Uri,

        [Parameter()]
        [string]$Cmd
    )

    $normalizedUri = ConvertTo-LearnDocumentationUri -Uri $Uri

    try {
        # Use Invoke-WebRequest to reliably get HTML content; IRM sometimes tries to parse.
        $response = Invoke-WebRequest -Uri $normalizedUri -ErrorAction Stop
        $html = $response.Content

        $synopsis = Get-SynopsisFromHtml -Html $html -Cmd $Cmd
        if (-not [string]::IsNullOrWhiteSpace($synopsis)) {
            return @($true, $synopsis)
        }

        $title = Get-TitleFromHtml -Html $html
        return @($false, $title)
    }
    catch {
        return @($false, $null)
    }
}

function Get-Synopsis {
    param(
        $Help,
        $Cmd,
        $DocumentationLink,
        $description
    )

    if ($null -eq $help) {
        return @($null, $null)
    }

    $synopsis = $help.Synopsis.Trim()

    if ($synopsis -like '') {
        Write-Verbose "$($cmd.name) - Empty synopsis, trying to get synopsis from description."
        $description = $help.description.Text
        if ([string]::IsNullOrEmpty($description)) {
            Write-Verbose "$($cmd.name) - Empty description."
        }
        else {
            $synopsis = $description.Trim().Split('.')[0].Trim()
        }
    }

    if ($synopsis -match "^$($cmd.Name) .*[-\[\]<>]" -or $synopsis -like '') {
        # If synopsis starts with the name of the verb, it's not a synopsis.
        $synopsis = $null

        if ([string]::IsNullOrEmpty($DocumentationLink) -or $DocumentationLink -in $script:badUrls) {
        }
        else {
            Write-Verbose "$($cmd.name) - Trying to get missing synopsis from Uri"
            $success, $synopsis = Get-SynopsisFromUri -Uri $DocumentationLink -Cmd $cmd.Name -verbose:$false

            if ($null -eq $synopsis -or -not $success) {
                if ($synopsis -notmatch "^$($cmd.Name) .*[-\[\]<>]") {
                    Write-Warning "!!$($cmd.name) - Bad online help uri, '$DocumentationLink' is about '$synopsis'"
                    $script:badUrls += $DocumentationLink
                    $DocumentationLink = $null
                    $synopsis = $null
                }
            }
        }
    }

    if ($null -ne $synopsis -and $synopsis -match "^$($cmd.Name) .*[-\[\]<>]") {
        $synopsis = $null
    }

    return @($synopsis, $DocumentationLink)
}
