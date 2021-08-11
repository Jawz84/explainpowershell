BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
    . $PSScriptRoot/Test-IsAzuriteUp.ps1
}

Describe 'Get-HelpDatabaseData' {
    It 'Has help data in database' {
        $gciData = Get-HelpDatabaseData -RowKey 'get-childitem'

        $gciData.Properties.CommandName | Should -BeExactly 'Get-ChildItem'
        $gciData.Properties.DocumentationLink | Should -Match 'get-childitem'
        $gciData.Properties.ModuleName | Should -BeExactly 'Microsoft.PowerShell.Management'
        $gciData.Properties.Syntax | Should -Not -BeNullOrEmpty 
        $gciData.Properties.Synopsis | Should -BeExactly 'Gets the items and child items in one or more specified locations.'
    }

    $commandsToCheck = Get-Content "$PSScriptRoot/../explainpowershell.metadata/defaultModules.json"
    | ConvertFrom-Json
    | ForEach-Object {
        $cmd = Get-Command -Module $_.name | Select-Object -First 1
        [psobject]@{
            CommandName = $cmd.Name
            ModuleName = $cmd.ModuleName
        }
    }

    It "For default module '<ModuleName>', there should be help information available for at least one command: '<CommandName>'" -ForEach $commandsToCheck {
        $gciData = Get-HelpDatabaseData -RowKey $CommandName.ToLower()
        $gciData | Should -Not -BeNullOrEmpty 
    }
}
