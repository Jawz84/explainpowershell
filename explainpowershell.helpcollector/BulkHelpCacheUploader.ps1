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