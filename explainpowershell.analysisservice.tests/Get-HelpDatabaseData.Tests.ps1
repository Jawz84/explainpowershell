BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
    . $PSScriptRoot/Test-IsAzuriteUp.ps1
}

Describe 'Get-HelpDatabaseData' {
    It 'Has help data in database' {
        $gciData = Get-HelpDatabaseData -RowKey 'get-childitem'

        $gciData.CommandName | Should -BeExactly 'Get-ChildItem'
        $gciData.DocumentationLink | Should -Match 'get-childitem'
        $gciData.ModuleName | Should -BeExactly 'Microsoft.PowerShell.Management'
        $gciData.Syntax | Should -Not -BeNullOrEmpty 
        $gciData.Synopsis | Should -BeExactly 'Gets the items and child items in one or more specified locations.'
    }
}
