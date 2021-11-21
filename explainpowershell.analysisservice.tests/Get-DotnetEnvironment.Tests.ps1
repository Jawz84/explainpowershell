BeforeAll {
    Push-Location $PSScriptRoot/..
}

Describe "dotnet versions" {
    It "dotnet sdk should be up-to-date" {
        # dotnet sdk check checks for dotnet sdk and runtimes, three in total. Should all three be "Up to date"
        (dotnet sdk check | Select-String "Up to date").Count | Should -Be 3
    }
    It "dotnet packages should be up-to-date" {
        # Except for the Microsoft.Azure.WebJobs.Extensions.Storage package, there should be no outdated packages
        # Microsoft.Azure.WebJobs.Extensions.Storage is outdated, but the newer versions don't support Azure Tables anymore :(
        (dotnet list package --outdated | select-string "> (?!Microsoft\.Azure\.WebJobs\.Extensions\.Storage)").Count | Should -Be 0
    }
}

AfterAll {
    Pop-Location
}