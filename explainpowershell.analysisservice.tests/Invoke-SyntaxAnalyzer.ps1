function Invoke-SyntaxAnalyzer {
    param(
        [Parameter(Mandatory)]
        [string]$PowershellCode,

        [Parameter()]
        [string]$BaseUri = 'http://127.0.0.1:7071/api',

        [Parameter()]
        [int]$TimeoutSec = 30,

        [switch]$Explanations
    )

    $ErrorActionPreference = 'stop'

    $body = @{ PowershellCode = $PowershellCode } | ConvertTo-Json

    # Invoke-WebRequest can emit expensive per-request progress UI, which adds significant overhead
    # in tight test loops on Windows. Suppress progress only for this request to keep tests fast.
    $originalProgressPreference = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try {
        $response = Invoke-WebRequest -Uri "$BaseUri/SyntaxAnalyzer" -Method Post -Body $body -ContentType 'application/json' -TimeoutSec $TimeoutSec
    }
    finally {
        $ProgressPreference = $originalProgressPreference
    }

    if ($Explanations) {
        return $response.Content | ConvertFrom-Json | Select-Object -Expandproperty Explanations
    }

    return $response
}
