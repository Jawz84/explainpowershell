BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
    . $PSScriptRoot/Start-FunctionApp.ps1
    . $PSScriptRoot/Test-IsAzuriteUp.ps1
}

Describe "Get-MetaData" {
    It "Returns Data about the database" {
        $metaData = Get-MetaData

        $metaData.psobject.Properties.Name | Should -Be @(
            'NumberOfCommands'
            'NumberOfAboutArticles'
            'NumberOfModules'
            'ModuleNames'
            'LastPublished')
        $metaData.LastPublished | Should -Not -BeNullOrEmpty
    }
}
