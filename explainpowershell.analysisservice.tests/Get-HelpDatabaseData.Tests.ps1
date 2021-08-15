BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
    . $PSScriptRoot/Test-IsAzuriteUp.ps1
}

Describe 'Get-HelpDatabaseData' {
    It 'Has help about_.. article data in database' {
        $data = Get-HelpDatabaseData -RowKey 'about_pwsh'

        $data.Properties.CommandName | Should -BeExactly 'about_Pwsh'
        $data.Properties.DocumentationLink | Should -Match 'https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_Pwsh'
        $data.Properties.ModuleName | Should -BeNullOrEmpty
        $data.Properties.Synopsis | Should -BeExactly 'Explains how to use the pwsh command-line interface. Displays the command-line parameters and describes the syntax.'
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
        $data = Get-HelpDatabaseData -RowKey $CommandName.ToLower()
        $data | Should -Not -BeNullOrEmpty 
    }
}
