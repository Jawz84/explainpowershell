$body = @{
    code="gps * | %{$_.fullname}"
} | convertto-json

Invoke-RestMethod -Uri "http://localhost:7071/api/SyntaxAnalyzer" -Method Post -Body $body