Push-Location $PSScriptRoot/.. # this line does not work as expected when placed in the BeforeAll block.

BeforeAll {
    $requiredDotnetVersions = Get-ChildItem *.csproj -Recurse -Depth 1 | ForEach-Object {
        [version](
            [xml](Get-Content $_)
        ).
            Project.
            PropertyGroup.
            TargetFramework.
            Replace('net','')
    } | Sort-Object -Unique
}

Describe "prerequisites" {
    It "has the right dotnet sdk installed" {
        $latestAvailableDotnetVersion = dotnet --list-sdks
        | ForEach-Object { [version]$_.split(' ')[0] }
        | Sort-Object
        | Select-Object -Last 1

        foreach ($version in $script:requiredDotnetVersions) {
            $latestAvailableDotnetVersion | Should -BeGreaterOrEqual $version
        }
    }

    It "has the right Azure Function Core Tools version installed" {
        $requiredFuncVersion = [int](Get-Content ./.vscode/settings.json | ConvertFrom-Json).'azurefunctions.projectruntime'.trim('~')
        $funcToolsVersion = [version](func --version)

        $funcToolsVersion.Major | Should -BeGreaterOrEqual $requiredFuncVersion
    }
}

Describe "dotnet sdk versions" {
    It "should be up-to-date" {
        # dotnet sdk check checks for dotnet sdk and runtimes. Should be "Up to date" for Major dotnet version (other versions don't matter)
        $dotnetMajorVersions = "$($requiredDotnetVersions.Major)"
        (dotnet sdk check) -match "[$dotnetMajorVersions](\.\d+)+\s\s+"
        | Where-Object {$_ -notmatch "Up to date"}
        | Should -BeNullOrEmpty
    }
}

Describe "dotnet package versions"{

    foreach ($package in @((dotnet list package --outdated) -match "^   >")) {
        It "in main project should be up to date" -TestCases @{'package' = $package} {
            $package | Should -BeNullOrEmpty
        }
    }

    Push-Location ./explainpowershell.analysisservice.tests
    foreach ($package in @((dotnet list package --outdated) -match "^   >")) {
        It "in testproject should be up to date" -TestCases @{'package' = $package} {
            $package | Should -BeNullOrEmpty
        }
    }
    Pop-Location
}

Pop-Location
