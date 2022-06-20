<#
.SYNOPSIS
    A function that is ALSO a testfunction, just like myTestModule\Get-TestInfo.
.DESCRIPTION
    A funnction to test our clobber support with
.LINK
    https://www.explainpowershell.com
.EXAMPLE
    Get-TestInfo -IsAlsoADummy
    Get-TestInfo will return if the switch parameter 'IsAlsoADummy' is present or not. This will return $true
#>
function Get-TestInfo {
    [CmdletBinding()]
    param (
        [Switch]$IsAlsoADummy
    )
    return $IsAlsoADummy
}