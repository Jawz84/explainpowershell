function Get-MetaData {
    param(
        [switch] $Refresh
    )

    $uri = 'http://127.0.0.1:7071/api/MetaData'

    if ( $Refresh ) {
        $uri += '?refresh=true'
    }
    
    Invoke-RestMethod -Uri $uri
}
