<#
.SYNOPSIS
    A dummy function to test stuff with.
.DESCRIPTION
    A longer description of the dummy test function, its purpose, common use cases, etc.
.LINK
    https://www.explainpowershell.com
.EXAMPLE
    Get-TestInfo -IsDummy
    Get-TestInfo will return if the switch parameter 'IsDummy' is present or not. This will return $true
#>
function Get-TestInfo {
    [CmdletBinding()]
    param (
        [Switch]$IsDummy
    )
    return $IsDummy
}