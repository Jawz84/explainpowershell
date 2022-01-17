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
    # TODO: peek at code above for better version
    $dotnetMajorVersion = '6'
    
    It "dotnet sdk should be up-to-date" {
        # dotnet sdk check checks for dotnet sdk and runtimes. Should be "Up to date" for Major dotnet version (other versions don't matter)
        (dotnet sdk check) -match "$dotnetMajorVersion(\.\d+)+  "
        | Where-Object {$_ -notmatch "Up to date"}
        | Should -BeNullOrEmpty
    }
    It "dotnet packages should be up-to-date" {
        (dotnet list package --outdated) -match "^   >" | Should -BeNullOrEmpty
    }
}

AfterAll {
    Pop-Location
}