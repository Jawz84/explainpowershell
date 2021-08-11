function Get-ParameterSetNames {
    param(
        $CommandName
    )

    $cmd = Get-Command -Name $commandName
    $parameterData = $cmd.Parameters.Keys | ForEach-Object {
        [pscustomobject]@{
            ParameterSets = $cmd.Parameters[$_].ParameterSets
        }
    }

    return $parameterData.ParameterSets.Keys | Where-Object { $_ -ne '__AllParameterSets' } | Sort-Object -Unique
}


function Get-ParameterSets {
    param(
        $CommandName = 'Add-AzAdGroupMember',
        [switch]$Full
    )

    $cmd = Get-Command -Name $commandName
    $parameterData = $cmd.Parameters.Keys | ForEach-Object {
        [pscustomobject]@{
            Name          = $_
            ParameterSets = $cmd.Parameters[$_].ParameterSets
        }
    }

    $parameterSetNames = $parameterData.ParameterSets.Keys | Where-Object { $_ -ne '__AllParameterSets' } | Sort-Object -Unique

    foreach ($parameterSetName in $parameterSetNames) {
        $parameterData
        | Where-Object {
            if ($Full) {
                $_.parameterSets.Keys -eq '__AllParameterSets' -or $_.parameterSets.Keys -contains $parameterSetName
            }
            else {
                $_.parameterSets.Keys -contains $parameterSetName
            }
        }
        | ForEach-Object {
            [pscustomobject]@{
                ParameterSetName                = $parameterSetName
                ParameterName                   = $_.Name
                IsMandatory                     = $_.parametersets[$parameterSetName].IsMandatory
                Position                        = $_.parametersets[$parameterSetName].Position
                HelpMessage                     = $_.parametersets[$parameterSetName].HelpMessage
                ValueFromPipeline               = $_.parametersets[$parameterSetName].ValueFromPipeline
                ValueFromPipelineByPropertyName = $_.parametersets[$parameterSetName].ValueFromPipelineByPropertyName
                ValueFromRemainingArguments     = $_.parametersets[$parameterSetName].ValueFromRemainingArguments
            }
        }
    }
}

Get-parameterSets