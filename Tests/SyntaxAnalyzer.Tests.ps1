using namespace Microsoft.PowerShell.Commands

Describe "SyntaxAnalyzer" {
    BeforeAll {
        . $PSCommandPath.Replace('.Tests.ps1', '.ps1')

        function Test-IsPrerequisitesRunning {
            $result = $true
            $ports = 7071, 10002

            foreach ($port in $ports) {
                $tcpClient = New-Object System.Net.Sockets.TcpClient
                $result = $result -and $tcpClient.ConnectAsync('127.0.0.1', $port).Wait(100)
            }

            $tcpClient.Dispose()

            return $result
        }

        Write-Warning "Checking if function app and storage emulator are running.."
        if (-not (Test-IsPrerequisitesRunning)) {
            Write-Warning "Starting Function App and Azure Storage Emulator.."
            Start-Job {
                AzureStorageEmulator.exe start
            }

            Start-Job {
                Push-Location .\explainpowershell.analysisservice\
                func host start .\explainpowershell.analysisservice
            }

            do {
                Start-Sleep -Seconds 2
            } until (Test-IsPrerequisitesRunning)
        }

        Write-Warning "OK - Function App and Azure Storage Emulator running"
    }

    It "Explains numeric constants" {
        $code = "0b0110; 0xAB234F; 12e-3";
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "Binary number (value: 6)"
        $content.Explanations[1].Description | Should -BeExactly "Hexadecimal number (value: 11215695)"
        $content.Explanations[2].Description | Should -BeExactly "Number (value: 0,012)"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -BeExactly "https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Numeric_Literals"
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
        #$content.ParseErrorMessage | Should -BeExactly ""
    }

    It "Explains variables, even complex ones" {
        $code = '$abc | $env:path | @splatted | $script:myVar'
        [BasicHtmlWebResponseObject]$result = SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "A variable named 'abc'"
        $content.Explanations[1].Description | Should -BeExactly "An environment variable named 'path' (on PSDrive 'env:')"
        $content.Explanations[2].Description | Should -BeExactly "A <a href=`"https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_splatting`">splatted</a> variable named 'splatted'"
    }

    $testCase = (Get-Content .\oneliners.ps1).split("`n") | ForEach-Object {
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
