BeforeAll {
    Push-Location $PSScriptRoot/..
}

Describe "prerequisites" {
    It "has the right dotnet sdk installed" {
        $requiredDotnetVersions = Get-ChildItem *.csproj -Recurse -Depth 1 | ForEach-Object {
            [version](
                [xml](Get-Content $_)
            ).
                Project.
                PropertyGroup.
                TargetFramework.
                Replace('net','')
        } | Sort-Object -Unique

        $latestAvailableDotnetVersion = dotnet --list-sdks | ForEach-Object { [version]$_.split(' ')[0] } | Sort-Object | Select-Object -Last 1

        foreach ($version in $requiredDotnetVersions) {
            $latestAvailableDotnetVersion | Should -BeGreaterOrEqual $version
        }
    }

    It "has the right Azure Function Core Tools version installed" {
        $requiredFuncVersion = [int](Get-Content ./.vscode/settings.json | ConvertFrom-Json).'azurefunctions.projectruntime'.trim('~')
        $funcToolsVersion = [version](func --version)

        $funcToolsVersion.Major | Should -BeGreaterOrEqual $requiredFuncVersion
    }
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