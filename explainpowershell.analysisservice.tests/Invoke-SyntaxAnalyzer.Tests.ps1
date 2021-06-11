using namespace Microsoft.PowerShell.Commands

Describe "Invoke-SyntaxAnalyzer" {
    BeforeAll {
        . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
        . $PSScriptRoot/Start-FunctionApp.ps1
        . $PSScriptRoot/Test-IsAzuriteUp.ps1
    }

    It "Explains class declaration" {
        $code = 'class foo {}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'class'.*'foo'\..*\[foo\]::new\(\)"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_classes"
    }

    It "Explains enum declaration" {
        $code = 'enum foo {bar = 1}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'enum'.*'foo'\..*enumeration"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_enum"
    }

    It "Explains enum declaration" {
        $code = '[flags()] enum foo {bar = 1}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'enum'.*'foo'.*'flags'.*enumeration"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_enum"
    }

    It "Explains using statements" {
        $code = 'using namespace system.text'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "a namespace"
        $content.Explanations[0].CommandName | Should -BeExactly "using statement"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_using"
    }

    It "Explains CmdletBinding attribute" {
        $code = '[CmdletBinding()] param()'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "The CmdletBinding attribute adds common parameters to your script or function, among other things."
        $content.Explanations[1].CommandName | Should -BeExactly "CmdletBinding Attribute"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_Functions_CmdletBindingAttribute"
    }

    It "Explains type accelerators" {
        $code = '[string]$mytext = 123' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[2].Description | Should -BeExactly "Constrains the type to 'string', which is a type accelerator for 'System.String'"
        $content.Explanations[2].CommandName | Should -BeExactly "Type accelerator"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -Match "about_type_accelerators"
    }

    It "Explains the While statement" {
        $code = 'while ($abc -lt 29) {$abc++}' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Description.Count | Should -BeExactly 5 
    }

    It "In the explanation, should correctly show a splatted variable with an @ sign at the beginning" {
        $code = 'gci @splat' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].OriginalExtent | Should -BeExactly '@splat' 
    }

    It "Explains Try catch finally" {
        $code = 'try {gci -name} catch [ArgumentNullException], [ArgumentException]  {"blah"} catch {"foo"} finally {remove-variable a}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Count | Should -BeExactly 9
    }

    It "Calculates Id and ParentId for Explanations" {
        $code = "if (`$abc -eq 123) {}";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Id | Should -Not -BeNullOrEmpty
        $content.Explanations[0].ParentId | Should -BeNullOrEmpty
        $content.Explanations[1].ParentId | Should -Be $content.Explanations[0].Id
    }

    It "Gets help article links for known commands and About_.. articles" {
        $code = "if (`$abc -eq 123) {Get-ChildItem}";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_If"
        $content.Explanations[3].HelpResult.DocumentationLink | Should -Match "get-childitem"
    }

    It "Explains numeric constants" {
        $code = "0b0110; 0xAB234F; 12e-3";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "Binary number (value: 6)"
        $content.Explanations[1].Description | Should -BeExactly "Hexadecimal number (value: 11215695)"
        $content.Explanations[2].Description | Should -BeLikeExactly "Number (value: 0?012)"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -Match "about_Numeric_Literals"
    }

    It "Converts alias to full command name" {
        $code = 'gci'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-ChildItem'
    }

    It "Converts multiple aliases to full command names" {
        $code = 'gps | select name | ? name -like dotnet'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Select-Object name | Where-Object name -like dotnet'
    }

    It "Notifies user about detected syntax errors, while still explaining as much as possible" {
        $code = 'gps | ? {$_.id'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ParseErrorMessage | Should -BeExactly "Missing closing '}' in statement block or type definition."
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Where-Object {$_.id'
    }

    It "works with if statements" {
        $code = 'if ($abc -eq 123) {gci -name}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ExpandedCode | Should -BeExactly 'if ($abc -eq 123) {Get-ChildItem -name}'
    }

    It "Explains variables, even complex ones" {
        $code = '$abc ; $env:path ; @splatted ; $script:myVar'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "A variable named 'abc'"
        $content.Explanations[1].Description | Should -BeExactly "An environment variable named 'path' (on PSDrive 'env:')"
        $content.Explanations[2].Description | Should -BeExactly "A splatted variable named 'splatted'"
    }

    $testCase = (Get-Content $PSScriptRoot\oneliners.ps1).split("`n") | ForEach-Object {
        [psobject]@{
            PowerShellCode = $_
        }
    }

    It "Can handle all kinds of different oneliners without freaking out: <PowerShellCode>" -ForEach $testCase {
            $result = Invoke-SyntaxAnalyzer -PowerShellCode $PowerShellCode
            $result.StatusCode | Should -Be 200
    }

    AfterAll {
        Get-Job | Stop-Job -PassThru | Remove-Job -Force
    }
}
