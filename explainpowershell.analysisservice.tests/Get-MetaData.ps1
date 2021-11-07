function Get-MetaData {
    param(
        [switch] $Refresh
    )

    $uri = 'http://localhost:7071/api/MetaData'

    if ( $Refresh ) {
        $uri += '?refresh=true'
    }

    Invoke-RestMethod -Uri $uri
}
