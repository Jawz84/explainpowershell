function Invoke-SyntaxAnalyzer {
    param(
        [string]$PowershellCode,
        [switch]$Explanations
    )

    $body = @{
        PowershellCode=$PowershellCode
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/SyntaxAnalyzer" -Method Post -Body $body

    if ($Explanations) {
        return $response.Content | ConvertFrom-Json | Select-Object -Expandproperty Explanations
    }

    return $response
}
