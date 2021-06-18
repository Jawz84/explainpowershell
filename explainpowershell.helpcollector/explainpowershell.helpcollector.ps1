
# Do parameterset calculations based on this with: 
# $a = ./explainpowershell.helpcollector.ps1 | select -first 1
# $n = 2; $a.parameters | ? {$_.parameterSets.Keys -eq '__AllParameterSets' -or $_.parameterSets.Keys -contains $a.ParameterSetNames[$n] } | select name, {$_.parametersets.keys}, {$_.parametersets[$a.ParameterSetNames[$n]].IsMandatory}
# In this, `$a.ParameterSetNames[$n]` is the parameterSetName.

#using namespace Microsoft.PowerShell.Commands

[cmdletbinding()]
param(
    $ModulesToProcess
)

#. .\classes.ps1

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

$ModulesToProcess = $ModulesToProcess | Sort-Object -Unique -Property Name
$total = $ModulesToProcess.Count

Write-Host -ForegroundColor Green 'Get raw command info for commands in all installed modules..'
$(
    foreach ($mod in $ModulesToProcess) {
        write-host "Processing module '$($mod.Name)' (total: $($ModulesToProcess.IndexOf($mod)+1)/$total)"
        [string]$moduleProjectUri = $mod.ProjectUri

        foreach ($cmd in Get-Command -Module $mod.name) {
            $help = Get-Help $cmd.Name

            $relatedLinks = $help.relatedLinks.navigationLink.where{ $_.uri -match '^http' }.uri ?? $cmd.HelpUri ?? $moduleProjectUri
            $DocumentationLink = $relatedLinks | Select-Object -First 1
    
            $synopsis = $help.synopsis.trim()

            if ($synopsis.startswith($cmd.Name)) {
                $synopsis = Get-SynopsisFromUri $DocumentationLink ?? $synopsis
            }

            $parameterData = $help.parameters.parameter.foreach{
                $cmdParam = $cmd.Parameters[$_.name]
                [pscustomobject]@{
                    Aliases         = $_.aliases ?? $cmdParam.Aliases
                    DefaultValue    = $_.defaultValue
                    Description     = $_.Description.Text -join ''
                    Globbing        = $_.globbing
                    IsDynamic       = $cmdParam.IsDynamic
                    Name            = $_.name
                    ParameterSets   = $cmdParam.ParameterSets
                    PipelineInput   = $_.pipelineInput
                    Position        = $_.position
                    Required        = $_.required
                    SwitchParameter = $cmdParam.SwitchParameter
                    TypeName        = $_.parameterValue ?? $cmdParam.ParameterType.FullName
                }
            }

            [pscustomobject]@{
                Aliases             = $help.Aliases
                CommandName         = $cmd.Name
                DefaultParameterSet = $cmd.DefaultParameterSet
                Description         = $help.Description.Text -join ''
                DocumentationLink   = $DocumentationLink
                InputTypes          = $help.InputTypes.inputType.type.name
                ModuleName          = $cmd.ModuleName
                ModuleProjectUri    = $moduleProjectUri
                Parameters          = $parameterData
                ParameterSetNames   = $parameterData.ParameterSets.Keys | Where-Object {$_ -ne '__AllParameterSets' } | Sort-Object -Unique
                RelatedLinks        = $relatedLinks
                ReturnValues        = $help.ReturnValues.returnValue.type.name
                Synopsis            = $synopsis
                Syntax              = (Get-Command $cmd.Name -Syntax).Trim()
            }
        }
    }
)
