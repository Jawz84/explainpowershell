$a = (az monitor app-insights query --apps 8ae245a8-42fa-48bc-9ac0-d1d55326f12b --analytics-query 'traces | where message startswith ''PowerShell''|project message' --offset 30d |convertfrom-json).tables.rows
$commands = $a.foreach{$_.split(':',1, $null)[1].trim()}| Sort-Object -Unique
$requestedCommands = $commands.foreach{if($_ -match "\w+-\w+") {$matches[0]}}| Sort-Object -Unique
#$requestedCommands
$unknownCommdands = $requestedCommands.where{-not (Get-Command $_ -ea silentlycontinue)}
$unknownCommdands