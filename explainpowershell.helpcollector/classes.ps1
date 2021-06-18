using namespace System.Management.Automation

class RawHelpParameterData {
    [string] $aliases
    [string] $defaultValue
    [PSObject[]] $Description
    [string] $name
    [string] $parameterSetName
    [string] $parameterValue
    [string] $pipelineInput
    [string] $position
    [string] $required
    [string] $typeName
}

class RawHelpData {
    [string[]] $Aliases
    [string] $Description
    [string] $Synopsis
    [PSObject[]] $Syntax
    [PSObject] $Parameters
    [PSTypeName[]] $InputTypes
    [string[]] $RelatedLinks
    [PSTypeName[]] $ReturnValues
}

Class HelpData {
    [string] $ModuleName
    [string] $CommandName
    [string] $HelpUri
    [string] $Synopsis
    [string] $Syntax
    [string[]] $Parameters
    [RawHelpData] $RawCmdletHelp
    [string] $DocumentationLink
}