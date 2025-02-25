function Convert-PackageVersions {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string[]]$Lines
    )

    process {
        foreach ($line in $Lines) {
            # Skip empty lines
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }
            
            # Use regex to parse the line and extract package name and versions
            if ($line -match '^\s*>\s*(?<package>[\w\.]+)\s+(?<installed>[\d\.]+)\s+(?<resolved>[\d\.]+)\s+(?<latest>[\d\.]+)\s*$') {
                [pscustomobject]@{
                    Package = [string]$matches['package']
                    InstalledVersion = [version]$matches['installed']
                    LatestVersion = [version]$matches['latest']
                }
            }
        }
    }
}