[CmdletBinding()]
param(
    [switch] $Force
)

foreach ($file in (Get-ChildItem -Path $PSScriptRoot/ -Filter '*.user' )) {
    $fileName = $file.FullName

    if (-not $Force) {
        $item = Get-Content $fileName | ConvertFrom-Json | Select-Object -First 1 -ExpandProperty CommandName | ForEach-Object ToLower
    }

    if ($force -or -not (Get-HelpDatabaseData -RowKey $item -IsProduction).Properties.ModuleVersion) {
        Write-Host "Writing help for module '$($File.Name)' to Azure table.."
        try {
            ./helpwriter.ps1 -HelpDataCacheFilename $fileName -IsProduction
        }
        catch {
            Write-Warning "Error in processing module '$($file.Name)': $($_.Exception.Message)"
        }
    }
    else {
        Write-Host "Skipping '$($File.Name)', because that data is already present in Azure. (use -Force switch to overwrite)"
    }
}

# Trigger refresh of database metadata, so ExplainPowerShell can show the newly added modules and updated cmdlet count.
if (-not (Get-Module -ListAvailable Az.Functions)) {
    Install-Module Az.Functions -Force
}

Get-AzFunctionApp
    | Where-Object { $_.Name -match 'powershellexplainer' -and $_.Status -eq 'running' }
    | ForEach-Object { Invoke-RestMethod -Uri "https://$($_.DefaultHostName)/api/MetaData?refresh=true" }