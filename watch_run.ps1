Push-Location -Path $PSScriptRoot
    Push-Location -Path .\explainpowershell.frontend
        Start-Process -FilePath dotnet -argumentlist watch, run &
    Pop-Location
    Push-Location -Path .\explainpowershell.analysisservice
        Start-Process -filepath dotnet -argumentlist watch, msbuild, /t:RunFunctions &
    Pop-Location
Pop-Location