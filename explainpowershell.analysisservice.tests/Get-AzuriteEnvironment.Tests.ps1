Push-Location $PSScriptRoot/.. # this line does not work as expected when placed in the BeforeAll block.

BeforeAll {
    $requestedAzuriteVersionTag = (Get-Content ../.devcontainer/docker-compose.yml | Select-String -Raw 'mcr\.microsoft\.com/azure-storage/azurite:.*$' ).Split(':') | Select-Object -Last 1
    $requestedAzuriteVersion = $requestedAzuriteVersionTag -as [version]
}

Describe "Azurite docker image label" {
    It "Should not be set to 'latest'" {
        $requestedAzuriteVersionTag | Should -Not -Be 'latest'
    }

    It "Is up-to-date" {
        $latestAvailableAzuriteVersion = ((Invoke-RestMethod 'https://mcr.microsoft.com/v2/azure-storage/azurite/tags/list').tags | ForEach-Object { $_ -as [version] } | Measure-Object -Maximum).Maximum
        $requestedAzuriteVersion | Should -BeGreaterOrEqual $latestAvailableAzuriteVersion
    }
}

Pop-Location
