using namespace Microsoft.PowerShell.Commands

Describe "SyntaxAnalyzer" {
    BeforeAll {
        . $PSCommandPath.Replace('.Tests.ps1', '.ps1')

        function Test-IsPrerequisitesRunning {
            $result = $true
            $ports = 7071, 10002

            try {
                foreach ($port in $ports) {
                    $tcpClient = New-Object System.Net.Sockets.TcpClient
                    $result = $result -and $tcpClient.ConnectAsync('127.0.0.1', $port).Wait(100)
                }
            }
            catch {
                return $false
            }
            finally {
                $tcpClient.Dispose()
            }

            return $result
        }

        Write-Warning "Checking if function app and Azurite are running.."
        if (-not (Test-IsPrerequisitesRunning)) {
            try {
                Write-Warning "Starting Function App.."
                Start-ThreadJob -ArgumentList $PSScriptRoot {
                    Push-Location "$($args[0])/../explainpowershell.analysisservice/"
                    func host start
                }

                do {
                    Start-Sleep -Seconds 2
                } until (Test-IsPrerequisitesRunning)
            }
            catch {

            }
        }

        Write-Warning "OK - Function App and Azurite running" 
    }

    It "Explains the While statement" {
        $code = 'while ($abc -lt 29) {$abc++}' 
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Description.Count | Should -BeExactly 5 
    }

    It "In the explanation, should correctly show a splatted variable with an @ sign at the beginning" {
        $code = 'gci @splat' 
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].OriginalExtent | Should -BeExactly '@splat' 
    }

    It "Explains Try catch finally" {
        $code = 'try {gci -name} catch [ArgumentNullException], [ArgumentException]  {"blah"} catch {"foo"} finally {remove-variable a}'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Count | Should -BeExactly 9
    }

    It "Calculates Id and ParentId for Explanations" {
        $code = "if (`$abc -eq 123) {}";
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Id | Should -Not -BeNullOrEmpty
        $content.Explanations[0].ParentId | Should -BeNullOrEmpty
        $content.Explanations[1].ParentId | Should -Be $content.Explanations[0].Id
    }

    It "Gets help article links for known commands and About_.. articles" {
        $code = "if (`$abc -eq 123) {Get-ChildItem}";
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_If"
        $content.Explanations[3].HelpResult.DocumentationLink | Should -Match "get-childitem"
    }

    It "Explains numeric constants" {
        $code = "0b0110; 0xAB234F; 12e-3";
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "Binary number (value: 6)"
        $content.Explanations[1].Description | Should -BeExactly "Hexadecimal number (value: 11215695)"
        $content.Explanations[2].Description | Should -BeLikeExactly "Number (value: 0?012)"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -Match "about_Numeric_Literals"
    }

    It "Converts alias to full command name" {
        $code = 'gci'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-ChildItem'
    }

    It "Converts multiple aliases to full command names" {
        $code = 'gps | select name | ? name -like dotnet'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Select-Object name | Where-Object name -like dotnet'
    }

    It "Notifies user about detected syntax errors, while still explaining as much as possible" {
        $code = 'gps | ? {$_.id'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ParseErrorMessage | Should -BeExactly "Missing closing '}' in statement block or type definition."
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Where-Object {$_.id'
    }

    It "works with if statements" {
        $code = 'if ($abc -eq 123) {gci -name}'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ExpandedCode | Should -BeExactly 'if ($abc -eq 123) {Get-ChildItem -name}'
    }

    It "Explains variables, even complex ones" {
        $code = '$abc ; $env:path ; @splatted ; $script:myVar'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
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
            $result = SyntaxAnalyzer -PowerShellCode $PowerShellCode
            $result.StatusCode | Should -Be 200
    }

    AfterAll {
        Get-Job | Stop-Job -PassThru | Remove-Job -Force
    }
}
