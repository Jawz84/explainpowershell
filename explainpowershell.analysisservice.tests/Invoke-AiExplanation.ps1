function Invoke-AiExplanation {
    param(
        [Parameter(Mandatory)]
        [string]$PowershellCode,

        [Parameter()]
        [object]$AnalysisResult,

        [Parameter()]
        [string]$BaseUri = 'http://localhost:7071/api',

        [Parameter()]
        [switch]$AsObject,

        [Parameter()]
        [switch]$AiExplanation
    )

    $ErrorActionPreference = 'stop'

    if (-not $AnalysisResult) {
        if (Get-Command -Name Invoke-SyntaxAnalyzer -ErrorAction SilentlyContinue) {
            $analysisResponse = Invoke-SyntaxAnalyzer -PowershellCode $PowershellCode
            $AnalysisResult = $analysisResponse.Content | ConvertFrom-Json
        }
        else {
            $analysisBody = @{ PowershellCode = $PowershellCode } | ConvertTo-Json
            $analysisResponse = Invoke-WebRequest -Uri "$BaseUri/SyntaxAnalyzer" -Method Post -Body $analysisBody -ContentType 'application/json'
            $AnalysisResult = $analysisResponse.Content | ConvertFrom-Json
        }
    }

    $body = @{
        PowershellCode = $PowershellCode
        AnalysisResult  = $AnalysisResult
    } | ConvertTo-Json -Depth 20

    # Note: the function route is `AiExplanation`, but the Functions host is case-insensitive.
    $response = Invoke-WebRequest -Uri "$BaseUri/aiexplanation" -Method Post -Body $body -ContentType 'application/json'

    if ($AsObject -or $AiExplanation) {
        $result = $response.Content | ConvertFrom-Json

        if ($AiExplanation) {
            return $result.AiExplanation
        }

        return $result
    }

    return $response
}
