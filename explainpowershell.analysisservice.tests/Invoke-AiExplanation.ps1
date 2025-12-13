# Ensure we use the repo-local (optimized) helper implementation.
# This avoids accidentally calling a stale `Invoke-SyntaxAnalyzer` already loaded in the caller's session.
$invokeSyntaxAnalyzerPath = Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-SyntaxAnalyzer.ps1'
if (Test-Path -LiteralPath $invokeSyntaxAnalyzerPath) {
    . $invokeSyntaxAnalyzerPath
}

function Invoke-AiExplanation {
    param(
        [Parameter(Mandatory)]
        [string]$PowershellCode,

        [Parameter()]
        [object]$AnalysisResult,

        [Parameter()]
        [string]$BaseUri = 'http://127.0.0.1:7071/api',

        [Parameter()]
        [int]$TimeoutSec = 30,

        [Parameter()]
        [switch]$AsObject,

        [Parameter()]
        [switch]$AiExplanation
    )

    $ErrorActionPreference = 'stop'

    if (-not $AnalysisResult) {
        if (Get-Command -Name Invoke-SyntaxAnalyzer -ErrorAction SilentlyContinue) {
            $analysisResponse = Invoke-SyntaxAnalyzer -PowershellCode $PowershellCode -BaseUri $BaseUri -TimeoutSec $TimeoutSec
            $AnalysisResult = $analysisResponse.Content | ConvertFrom-Json
        }
        else {
            $analysisBody = @{ PowershellCode = $PowershellCode } | ConvertTo-Json

            $analysisResponse = Invoke-WebRequest -Uri "$BaseUri/SyntaxAnalyzer" -Method Post -Body $analysisBody -ContentType 'application/json' -TimeoutSec $TimeoutSec
            $AnalysisResult = $analysisResponse.Content | ConvertFrom-Json
        }
    }

    $body = @{
        PowershellCode = $PowershellCode
        AnalysisResult  = $AnalysisResult
    } | ConvertTo-Json -Depth 20

    # Note: the function route is `AiExplanation`, but the Functions host is case-insensitive.

    $response = Invoke-WebRequest -Uri "$BaseUri/aiexplanation" -Method Post -Body $body -ContentType 'application/json' -TimeoutSec $TimeoutSec
   
    if ($AsObject -or $AiExplanation) {
        $result = $response.Content | ConvertFrom-Json

        if ($AiExplanation) {
            return $result.AiExplanation
        }

        return $result
    }

    return $response
}
