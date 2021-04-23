function SyntaxAnalyzer ($PowershellCode){
    $body = @{
        PowershellCode=$PowershellCode
    } | convertto-json

    return Invoke-WebRequest -Uri "http://localhost:7071/api/SyntaxAnalyzer" -Method Post -Body $body
}
