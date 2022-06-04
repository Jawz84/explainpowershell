[cmdletbinding()]
param(
    [pscustomobject[]]
    $ModulesToProcess
)

#region functions
function Get-SynopsisFromUri {
    [CmdletBinding()]
    param(
        $uri,
        $cmd
    )

    try {
        $html = (Invoke-RestMethod $uri).Trim().Split("`n").split('<div')
        $title = ($html | Select-String -Pattern '<h1.*>(.*)</h1').Matches.Groups[-1].Value
        $temp = $html | Select-String 'class="summary">' -Context 1
        if ($null -eq $temp) {
            $temp = $html | Select-String "h2 id=`"$cmd" -Context 1
        }
        return @($true, $temp.Context.PostContext.Trim() -replace '<p>|</p>', '')
    }
    catch {
        return @($false, $title)
    }
}

function Get-Synopsis {
    param(
        $Help,
        $Cmd,
        $DocumentationLink,
        $description
    )

    if ($null -eq $help) {
        return @($null, $null)
    }

    $synopsis = $help.Synopsis.Trim()

    if ($synopsis -like '') {
        Write-Verbose "$($cmd.name) - Empty synopsis, trying to get synopsis from description."
        $description = $help.description.Text
        if ([string]::IsNullOrEmpty($description)) {
            Write-Verbose "$($cmd.name) - Empty description."
        }
        else {
            $synopsis = $description.Trim().Split('.')[0].Trim()
        }
    }

    if ($synopsis -match "^$($cmd.Name) .*[-\[\]<>]" -or $synopsis -like '') {
        # If synopsis starts with the name of the verb, it's not a synopsis.
        $synopsis = $null

        if ([string]::IsNullOrEmpty($DocumentationLink) -or $DocumentationLink -in $script:badUrls) {
        }
        else {
            Write-Verbose "$($cmd.name) - Trying to get missing synopsis from Uri"
            $succes, $synopsis = Get-SynopsisFromUri $DocumentationLink -cmd $cmd.Name -verbose:$false

            if ($null -eq $synopsis -or -not $success) {
                if ($synopsis -notmatch "^$($cmd.Name) .*[-\[\]<>]") {
                    Write-Warning "!!$($cmd.name) - Bad online help uri, '$DocumentationLink' is about '$synopsis'"
                    $script:badUrls += $DocumentationLink
                    $DocumentationLink = $null
                    $synopsis = $null
                }
            }
        }
    }

    if ($null -ne $synopsis -and $synopsis -match "^$($cmd.Name) .*[-\[\]<>]") {
        $synopsis = $null
    }

    return @($synopsis, $DocumentationLink)
}
#endregion functions

$ModulesToProcess = $ModulesToProcess | Sort-Object -Unique -Property Name

$script:badUrls = @()

#TODO - reconsider this, seems to throw null errors
$defaultParameterSetSettings = '{"IsMandatory":true,"Position":-2147483648,"ValueFromPipeline":false,"ValueFromPipelineByPropertyName":false,"ValueFromRemainingArguments":false,"HelpMessage":null,"HelpMessageBaseName":null,"HelpMessageResourceId":null}'

foreach ($mod in $ModulesToProcess) {
    if (-not $PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent) {
        Write-Progress -Id 1 -Activity "Processing '$($ModulesToProcess.Count)' modules." -CurrentOperation "Processing module '$($mod.Name)'" -PercentComplete ((@($ModulesToProcess).IndexOf($mod) + 1) / $ModulesToProcess.Count * 100) 
    }
    else {
        Write-Verbose "Processing module '$($mod.Name)'"
    }

    try {
        Update-Help -Module $mod.Name -Force -ErrorAction stop
    }
    catch {
        Write-Warning "$_"
    }

    [string]$moduleProjectUri = $mod.ProjectUri

    $commandsToProcess = (Get-Command -Module $mod.name).where{ $_.CommandType -ne 'Alias' }

    foreach ($cmd in $commandsToProcess) {
        Write-Debug $cmd.Name

        if (-not $PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent) {
            Write-Progress -ParentId 1 -Activity "Processing '$($commandsToProcess.Count)' commands" -CurrentOperation "Processing command '$($cmd.Name)'" -PercentComplete ((@($commandsToProcess).IndexOf($cmd) + 1) / $commandsToProcess.Count * 100)
        }

        try {
            $help = Get-Help $cmd.Name -ErrorAction stop
        }
        catch {
            Write-Warning "!!$($cmd.name) - Unexpected error in Get-Help: $_"
            $help = $null
        }
        $null = $help ?? $(Write-Warning "!!$($cmd.name) - No local help present, limited results")

        $relatedLinks = $help.relatedLinks.navigationLink.where{ $_.uri -match '^http' }.uri ?? $cmd.HelpUri ?? $moduleProjectUri
        $documentationLink = $relatedLinks | Select-Object -First 1

        $synopsis, $documentationLink = Get-Synopsis -Help $help -Cmd $cmd -DocumentationLink $DocumentationLink

        try {
            $syntax = ($help.syntax | Out-String).Trim()
            if ($syntax -like '*syntaxItem*') {
                $syntax = Get-Command $cmd.Name -Syntax
            }
        }
        catch {
            Write-Warning "!!$($cmd.name) - Something went wrong getting syntax info: $_"
        }

        $parameterData = $help.parameters.parameter.foreach{
            try {
                $cmdParam = $cmd.Parameters[$_.name]
            }
            catch {
                Write-Verbose "$($cmd.name) - No command parameter data"
            }

            [pscustomobject]@{
                Aliases         = ($_.aliases -join ', ') ?? ($cmdParam.Aliases -join ', ')
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
            Aliases             = @($help.Aliases) -join ', '
            CommandName         = $cmd.Name
            DefaultParameterSet = $cmd.DefaultParameterSet
            Description         = $help.Description.Text -join ''
            DocumentationLink   = $documentationLink
            InputTypes          = $help.InputTypes.inputType.type.name -join ', '
            ModuleName          = $cmd.ModuleName
            ModuleVersion       = "$($cmd.Module.Version ?? $cmd.Version ?? '')"
            ModuleProjectUri    = $moduleProjectUri
            Parameters          = ($parameterData | ConvertTo-Json -Compress -Depth 4).replace($defaultParameterSetSettings,'null')
            ParameterSetNames   = @($parameterData.ParameterSets.Keys | Where-Object { $_ -ne '__AllParameterSets' } | Sort-Object -Unique) -join ', '
            RelatedLinks        = $relatedLinks -join ', '
            ReturnValues        = $help.ReturnValues.returnValue.type.name -join ', '
            Synopsis            = $synopsis
            Syntax              = $syntax
        }
    }

    if (-not $PSCmdlet.MyInvocation.BoundParameters['Verbose'].IsPresent) {
        Write-Progress -ParentId 1 -Activity "Processing '$($commandsToProcess.Count)' commands" -CurrentOperation 'Completed' -Completed
        Write-Progress -Id 1 -Activity "Processing '$($ModulesToProcess.Count)' modules." -CurrentOperation 'Completed' -Completed
    }
}
