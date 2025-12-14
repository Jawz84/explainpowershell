function Get-MetaData {
    <#
    .SYNOPSIS
    Gets metadata from the local Analysis Service.

    .DESCRIPTION
    Calls the Analysis Service HTTP endpoint to retrieve metadata used by tests.
    Use -Refresh to request regenerated metadata.

    .PARAMETER Refresh
    When specified, adds '?refresh=true' to the request URI.

    .EXAMPLE
    Get-MetaData
    Retrieves metadata from the default endpoint.

    .EXAMPLE
    Get-MetaData -Refresh
    Retrieves metadata and forces a refresh on the service.
    #>
    param(
        [switch] $Refresh
    )

    $uri = 'http://127.0.0.1:7071/api/MetaData'

    if ( $Refresh ) {
        $uri += '?refresh=true'
    }
    
    Invoke-RestMethod -Uri $uri
}
