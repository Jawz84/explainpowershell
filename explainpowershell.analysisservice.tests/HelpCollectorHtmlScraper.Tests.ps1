Describe "HelpCollector HTML synopsis scraper" {
    BeforeAll {
        . "$PSScriptRoot/../explainpowershell.helpcollector/HelpCollector.Functions.ps1"
    }

    It "Normalizes docs.microsoft.com to learn.microsoft.com and forces https" {
        ConvertTo-LearnDocumentationUri -Uri 'http://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_return' |
            Should -Be 'https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_return'
    }

    It "Extracts synopsis from summary paragraph" {
        $html = @'
<!doctype html>
<html>
<head><title>t</title></head>
<body>
<h1 id="return">about_Return</h1>
<p class="summary">Returns from the current scope.</p>
</body>
</html>
'@

        Get-SynopsisFromHtml -Html $html -Cmd 'return' | Should -Be 'Returns from the current scope.'
    }

    It "Extracts synopsis from meta description" {
        $html = @'
<html>
<head>
<meta name="description" content="Throws a terminating error." />
</head>
<body>
<h1>about_Throw</h1>
</body>
</html>
'@

        Get-SynopsisFromHtml -Html $html -Cmd 'throw' | Should -Be 'Throws a terminating error.'
    }

    It "Extracts keyword synopsis from about_language_keywords section" {
        $html = @'
<html>
<body>
<h1>about_Language_Keywords</h1>
<h2 id="throw">throw</h2>
<p>Throws an exception.</p>
</body>
</html>
'@

        Get-SynopsisFromHtml -Html $html -Cmd 'throw' | Should -Be 'Throws an exception.'
    }
}
