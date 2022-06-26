function Test-IsPrerequisitesRunning {
    param(
        [parameter(mandatory)]
        [int[]]$ports
    )

    $result = $true

    try {
        foreach ($port in $ports) {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $result = $result -and $tcpClient.ConnectAsync('127.0.0.1', $port).Wait(100)
        }
    }
    catch {
        return $false
    }
    finally {
        $tcpClient.Dispose()
    }

    return $result
}