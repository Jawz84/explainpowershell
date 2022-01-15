BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
    . $PSScriptRoot/Start-FunctionApp.ps1
    . $PSScriptRoot/Test-IsAzuriteUp.ps1
}

Describe "Get-MetaData" {
    It "Calculates Data about the database" {
        $metaData = Get-MetaData -Refresh

        $metaData.psobject.Properties.Name | Should -Be @(
            'NumberOfCommands'
            'NumberOfAboutArticles'
            'NumberOfModules'
            'ModuleNames'
            'LastPublished'
            'PartitionKey'
            'RowKey'
            'Timestamp'
            'ETag')
        $metaData.NumberOfCommands | Should -Not -Be 0
        $metaData.LastPublished | Should -Not -BeNullOrEmpty
    }

    It "Returns cached Data about the database" {
        $metaData = Get-MetaData

        $metaData.psobject.Properties.Name | Should -Be @(
            'NumberOfCommands'
            'NumberOfAboutArticles'
            'NumberOfModules'
            'ModuleNames'
            'LastPublished'
            'PartitionKey'
            'RowKey'
            'Timestamp'
            'ETag')
        $metaData.NumberOfCommands | Should -Not -Be 0
        $metaData.LastPublished | Should -Not -BeNullOrEmpty
    }
}
