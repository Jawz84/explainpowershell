help command -full
Clear-Host
get-history | more
invoke-history 12
invoke-history | export-csv c:\temp\output.txt
start-transcript c:\temp\output.txt -IncludeInvocationHeader
stop-transcript
gip
get-netadapter
get-netadapter "MyConnection01" | get-netroute [[-addressfamily ipv4]]
Get-NetTCPConnection | ? State -eq Established | sort Localport | FT -Autosize
Get-DnsClientCache
Get-SmbMapping
test-connection DC01
test-connection DC01 -count 999999999
tnc DC01 -tr
tnc DC01 -p 3389
tnc DC01 -p 3389 -inf detailed
resolve-dnsname DC01
New-SmbMapping -LocalPath S: -RemotePath [UNC Path]
. 'C:\Program Files\Microsoft\Exchange Server\V14\bin\RemoteExchange.ps1'
Get-Mailbox -ResultSize Unlimited | Get-ADPermission |  Where-Object {($_.ExtendedRights -like "*send-as*") -and  -not ($_.User -like "nt authority\self")} | Format-Table Identity, User -auto
Get-Mailbox -ResultSize Unlimited | Get-MailboxPermission |  Where-Object {($_.AccessRights -match "FullAccess") -and  -not ($_.User -like "NT AUTHORITY\SELF")} | Format-Table Identity, User
Get-Mailbox [identity] | Format-List *Quota
Get-ActiveSyncDevice -filter {deviceaccessstate -eq 'quarantined'} |  select identity, deviceid | fl
Set-Mailbox [identity] -IssueWarningQuota [size] -ProhibitSendQuota [size]  -ProhibitSendReceiveQuota [size] -UseDatabaseQuotaDefaults $false
Set-Mailbox -Identity "Full Name" -DeliverToMailboxAndForward $true -ForwardingAddress "Full Name"
Set-Mailbox -Identity "Full Name" -ForwardingAddress $null -ForwardingSmtpAddress $null
Get-ActiveSyncDevice -filter {deviceaccessstate -eq 'quarantined'} |  select identity, deviceid | fl Set-CASMailbox –Identity [account] –ActiveSyncAllowedDeviceIDs [DEVICEID]
$env:computername
[Management.ManagementDateTimeConverter]::ToDateTime((Get-WmiObject Win32_OperatingSystem  -Property LastBootUpTime -ComputerName DC01).LastBootUpTime)
Get-WmiObject win32_operatingsystem -ComputerName DC01 |  select @{Name="Last Boot Time"; Expression={$_.ConvertToDateTime($_.LastBootUpTime)}}, PSComputerName
rename-computer -name [original_name] -newname [new_name]
restart-computer
restart-computer -computername DC01
stop-computer
Start-Process -FilePath [executable] -Wait -WindowStyle Maximized
disable-windowsoptionalfeature -feature [featurename]
Get-ChildItem -Path [path e.g. hkcu:\]
Set-Location hkcu: cd SOFTWARE dir
New-Item -Path [path and key, e.g. hkcu:\new_key]
Remove-Item -Path [path and key, e.g. hkcu:\new_key]
Set-ItemProperty -Path [path and key, e.g. hkcu:\key] -Name [PropertyName] -Value [New Value]
Get-Service [optional wildcard search on service name (not display name)] |  sort [Status, Name or Displayname]
Get-Service [[optional wildcard search on service name (not display name)] |  where Status -eq running | sort [Status, Name or Displayname]
get-service -computername hyd-rdsh-10 | where DisplayName -match [search_expression]
start-service [service name]
stop-service [service name]
restart-service [service name]
get-printer -computername [Computer Name] | where Type -eq Local |  select ComputerName, Name, ShareName, Location, Comment | Out-GridView
Get-Printer -ComputerName DC01 | where PrinterStatus -eq Error | fl Name,JobCount
Get-PrintJob -ComputerName DC01 -PrinterName [print queue]
Get-PrintJob -ComputerName DC01 -PrinterName DC01 |  where id -eq [job number] | fl JobStatus,UserName
add-printer -connect [path to printer, e.g. \\computer\queue_name]
Remove-Printer -Name "[Printer Path and Name]"
Get-ChildItem -Path [Drive]:[Path] -Recurse -Include *.JPG
Copy-Item [path, optionally including file reference] -Destination [path] -Verbose
Move-Item [path] -Destination [path] -Verbose
Rename-Item [path] -NewName [path]
search-adaccount -accountinactive -datetime "[date]" -usersonly
set-aduser [name] -title [new_title]
disable-adaccount -identity "[AD user]"
Get-WmiObject -Class ccm_application -Namespace root\ccm\clientsdk  -ComputerName (get-content [path_to_computers_file]) |  Where-Object { ($_.InstallState -ne "Installed")  -and ($_.ApplicabilityState -eq "Applicable")  -and ($_.IsMachineTarget -eq $True)  -and ($_.EvaluationState -ne 1)} |  select FullName,__SERVER
