<#
.SYNOPSIS
Invokes the SyntaxAnalyzer HTTP endpoint used by analysis service tests.

.DESCRIPTION
Posts PowerShell source text to the analysis service and returns the raw web response.
When -Explanations is specified, returns only the Explanations property from the JSON
response payload.

.PARAMETER PowershellCode
The PowerShell source text to analyze.

.PARAMETER BaseUri
Base URI for the analysis service API. Defaults to the local Azure Functions host.

.PARAMETER TimeoutSec
Timeout, in seconds, for the HTTP request.

.PARAMETER Explanations
If specified, returns only the parsed Explanations array/object from the JSON response.

.OUTPUTS
System.Net.HttpWebResponse / Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject
By default, returns the response object from Invoke-WebRequest.

.OUTPUTS
System.Object
When -Explanations is specified, returns the deserialized Explanations property.

.EXAMPLE
Invoke-SyntaxAnalyzer -PowershellCode 'Get-Date'

.EXAMPLE
Invoke-SyntaxAnalyzer -PowershellCode 'Get-ChildItem' -Explanations
#>
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
