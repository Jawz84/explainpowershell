using namespace Microsoft.PowerShell.Commands

. .\classes.ps1

# Update-Help -Force -ErrorAction SilentlyContinue
$cmds = New-Object -TypeName 'System.Collections.Generic.Dictionary[[string],[HelpData]]'

$modulesToProcess = Get-Module 

$modulesToProcess += @{
    Name = "Microsoft.PowerShell.Core"
    ProjectUri = "https://docs.microsoft.com/en-us/powershell/"
}

$modulesToProcess
| Sort-Object -Unique -Property Name
| ForEach-Object {
    $moduleProjectUri = $_.ProjectUri
    Get-Command -Module $_.name | ForEach-Object {
        $cmds.Add(
            $_.Name,
            [HelpData]@{
                ModuleName        = $_.ModuleName
                CommandName       = $_.Name
                RawCommandInfo    = $_
                Syntax            = (Get-Command $_.Name -Syntax).trim()
                DocumentationLink = $moduleProjectUri
            }
        )
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
}

$cmds[$($cmds.Keys)] 


# about_.. articles

$aboutArticles = Get-Help About_*
# filter only Microsoft built-in ones
$abouts = $aboutArticles | Where-Object {-not $_.synopsis} 

foreach ($about in $abouts) {
    $baseUrl = 'https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/'
    [BasicHtmlWebResponseObject]$result = $null
    try {
        $result = Invoke-WebRequest -Uri ($baseUrl + $about.name) -ErrorAction SilentlyContinue
    }
    catch {
        try {
            $baseUrl = $baseUrl.replace("microsoft.powershell.core/about","Microsoft.PowerShell.Security/About")
            $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name)
        }
        catch {
            $baseUrl = $baseUrl.replace("Microsoft.PowerShell.Security/About","microsoft.wsman.management/about")
            $result = Invoke-WebRequest -ErrorAction SilentlyContinue -Uri ($baseUrl + $about.name)
        }
    }

    $selectedContext = $about.ToString().Split("`n") 
        | Select-String "short description" -Context 5

    if ($selectedContext) {
        $synopsis = (
            (
                    $selectedContext.Context.PostContext.Split("`n")
                    | Where-Object {-not [string]::IsNullOrWhiteSpace($_) }
            ) -join ' '
        ).Replace("`r", '').Replace("`n", '')

        $hasLongSubscription = $synopsis.IndexOf("long description", [stringcomparison]::OrdinalIgnoreCase)
        if ($hasLongSubscription -gt 0) {
            $synopsis = $synopsis.Substring(0, $hasLongSubscription)
        }
    }

    [HelpData]@{
        CommandName = $about.name
        DocumentationLink = if ($null -ne $result) {$baseUrl + $about.name}
        Synopsis = $synopsis
    }
}