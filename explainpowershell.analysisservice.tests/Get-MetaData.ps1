function Get-MetaData {
    Invoke-RestMethod -Uri 'http://localhost:7071/api/MetaData'
}
