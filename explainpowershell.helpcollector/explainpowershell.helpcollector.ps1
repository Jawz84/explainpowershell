using namespace Microsoft.PowerShell.Commands

. .\classes.ps1

$cmds = New-Object -TypeName 'System.Collections.Generic.Dictionary[[string],[HelpData]]'

$modulesToProcess = Get-Module -ListAvailable

$modulesToProcess += @{
    Name = "Microsoft.PowerShell.Core"
    ProjectUri = "https://docs.microsoft.com/en-us/powershell/"
}

Write-Host -ForegroundColor Green "Get raw command info for commands in all installed modules.."
$modulesToProcess
| Sort-Object -Unique -Property Name
| ForEach-Object {
    $moduleProjectUri = $_.ProjectUri
    Get-Command -Module $_.name | ForEach-Object {
        try {
            $cmds.Add(
                $_.Name,
                [HelpData]@{
                    ModuleName        = $_.ModuleName
                    CommandName       = $_.Name
                    #RawCommandInfo    = $_
                    Syntax            = (Get-Command $_.Name -Syntax).trim()
                    DocumentationLink = $moduleProjectUri
                }
            )
        }
        catch {
            # suppress duplicate key errors
        }
    }
} 

function Get-SynopsisFromUri ($uri) {
    try {
        $html = Invoke-RestMethod $uri
        ($html.Trim().Split("`n")
            | Select-String 'class="summary">' -Context 1
            | Select-Object -ExpandProperty context
            | Select-Object -ExpandProperty postcontext
        ).Trim() -replace '<p>|</p>', ''
    }
    catch {
        return
    }
}

Write-Host -ForegroundColor Green "Get help info for all detected commands, get synopis from internet if needed.."
foreach ($cmd in $cmds.Keys) {
    $help = Get-Help $cmd
    $relatedLinks = $help.relatedLinks.navigationLink.where{ $_.uri -match '^http' }.uri
    if ($null -eq $relatedLinks) {
        $relatedLinks = $cmds[$cmd].RawCommandInfo.HelpUri
    }

    $synopsis = $help.synopsis.trim()

    if ($synopsis.startswith($cmd)) {
        $uri = $relatedLinks | Select-Object -First 1
        if (-not ($synopsis = Get-SynopsisFromUri $uri)) {
            $synopsis = 'Not found'
        }
    }

    $parameterData = $help.parameters.parameter | ForEach-Object {
        [RawHelpParameterData]@{
            aliases          = $_.aliases
            defaultValue     = $_.defaultValue
            Description      = $_.Description.text
            name             = $_.name
            parameterSetName = $_.parameterSetName
            parameterValue   = $_.parameterValue
            pipelineInput    = $_.pipelineInput
            position         = $_.position
            required         = $_.required
            typeName         = $_.typeName
        }
    }

    $link = $relatedLinks | Select-Object -First 1

    if (-not [string]::IsNullOrEmpty($link)) {
        $cmds[$cmd].DocumentationLink = $link
    }
    $cmds[$cmd].Synopsis = $synopsis
    $cmds[$cmd].Parameters = $cmds[$cmd].RawCommandInfo.Parameters.Keys
    $cmds[$cmd].RawCmdletHelp = [RawHelpData]@{
        Aliases      = $help.Aliases
        Description  = $help.Description
        InputTypes   = [PSTypeName[]]$help.InputTypes.inputType.type.name
        Parameters   = $parameterData
        RelatedLinks = $relatedLinks
        ReturnValues = [PSTypeName[]]$help.ReturnValues.returnValue.type.name
        Synopsis     = $help.synopsis.trim()
        Syntax       = $help.syntax
    }

    $cmds[$cmd]
}
