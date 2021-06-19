help command -full
Clear-Host
get-history | more
invoke-history 12
invoke-history | export-csv c:\temp\output.txt
start-transcript c:\temp\output.txt -IncludeInvocationHeader
stop-transcript
gip
get-netadapter
get-netadapter "MyConnection01" | get-netroute -addressfamily ipv4
Get-NetTCPConnection | ? State -eq Established | sort Localport | FT -Autosize
Get-DnsClientCache
Get-SmbMapping
test-connection DC01
test-connection DC01 -count 999999999
tnc DC01 -tr
tnc DC01 -p 3389
tnc DC01 -p 3389 -inf detailed
resolve-dnsname DC01
New-SmbMapping -LocalPath S: -RemotePath \\server\demo
. 'C:\Program Files\Microsoft\Exchange Server\V14\bin\RemoteExchange.ps1'
Get-Mailbox -ResultSize Unlimited | Get-ADPermission |  Where-Object {($_.ExtendedRights -like "*send-as*") -and  -not ($_.User -like "nt authority\self")} | Format-Table Identity, User -auto
Get-Mailbox -ResultSize Unlimited | Get-MailboxPermission |  Where-Object {($_.AccessRights -match "FullAccess") -and  -not ($_.User -like "NT AUTHORITY\SELF")} | Format-Table Identity, User
Get-Mailbox identity@dummy.com | Format-List *Quota
Get-ActiveSyncDevice -filter {deviceaccessstate -eq 'quarantined'} |  select identity, deviceid | fl
Set-Mailbox identity@dummy.com -IssueWarningQuota 950MB -ProhibitSendQuota 970MB  -ProhibitSendReceiveQuota 1GB -UseDatabaseQuotaDefaults $false
Set-Mailbox -Identity "Full Name" -DeliverToMailboxAndForward $true -ForwardingAddress "Full Name"
Set-Mailbox -Identity "Full Name" -ForwardingAddress $null -ForwardingSmtpAddress $null
Get-ActiveSyncDevice -filter {deviceaccessstate -eq 'quarantined'} |  select identity, deviceid | fl Set-CASMailbox –Identity identity@dummy.com –ActiveSyncAllowedDeviceIDs $DeviceId
$env:computername
[Management.ManagementDateTimeConverter]::ToDateTime((Get-WmiObject Win32_OperatingSystem  -Property LastBootUpTime -ComputerName DC01).LastBootUpTime)
Get-WmiObject win32_operatingsystem -ComputerName DC01 | select @{Name="Last Boot Time"; Expression={$_.ConvertToDateTime($_.LastBootUpTime)}}, PSComputerName
rename-computer -name WS001 -newname WS-001
restart-computer
restart-computer -computername DC01
stop-computer
Start-Process -FilePath cmd.exe -Wait -WindowStyle Maximized
disable-windowsoptionalfeature -FeatureName SMB1Protocol
Get-ChildItem -Path hkcu:\
Set-Location hkcu: ; cd SOFTWARE; dir
New-Item -Path hkcu:\new_key
Remove-Item -Path hkcu:\new_key
Set-ItemProperty -Path hkcu:\key -Name myProp -Value "testinggg"
Get-Service s* | sort Status
Get-Service someservice |  where Status -eq running | sort DisplayName
get-service -computername hyd-rdsh-10 | where DisplayName -match "myRegex_now_you_have_two_problems"
start-service spooler
stop-service fax
restart-service spooler
get-printer -computername hyd-rdsh-10 | where Type -eq Local |  select ComputerName, Name, ShareName, Location, Comment | Out-GridView
Get-Printer -ComputerName DC01 | where PrinterStatus -eq Error | fl Name,JobCount
Get-PrintJob -ComputerName DC01 -PrinterName $myPrinterQueueName
Get-PrintJob -ComputerName DC01 -PrinterName DC01 |  where id -eq [job number] | fl JobStatus,UserName
add-printer -connect \\computer\queue_name
Remove-Printer -Name "[Printer Path and Name]"
Get-ChildItem -Path c:\temp -Recurse -Include *.JPG
Copy-Item -Path C:\Users\myUser\desktop\test.txt -Destination ~\Documents\ -Verbose
Move-Item $myPath -Destination $myDestination -Verbose
Rename-Item ~\Desktop\MyTestFile -NewName MyTestFile.ps1
search-adaccount -accountinactive -datetime (Get-date).adddays(-30) -usersonly
set-aduser $userName -title "Senior DevOps Engineer"
disable-adaccount -identity $userName
Get-WmiObject -Class ccm_application -Namespace root\ccm\clientsdk  -ComputerName (get-content .\computers.txt) |  Where-Object { ($_.InstallState -ne "Installed")  -and ($_.ApplicabilityState -eq "Applicable")  -and ($_.IsMachineTarget -eq $True)  -and ($_.EvaluationState -ne 1)} |  select FullName,__SERVER
Get-ADDomainController –Filter * | Select Hostname, IsGlobalCatalog | Export-CSV C:\Temp\AllDomainControllerStatus.CSV -NoTypeInfo
Backup-GPO –All –Path C:\Temp\AllGPO
Get-HotFix –ID KB2877616
Get-HotFix –ID KB2877616 –Computername WindowsServer1.TechGenix.com
Get-EventLog –Log System –Newest 100 | Where-Object {$_.EventID –eq ‘1074’} | FT MachineName, UserName, TimeGenerated –AutoSize | Export-CSV C:\Temp\AllEvents.CSV -NoTypeInfo
$cred = Get-Credential; $Session = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://ps.outlook.com/powershell -Credential $cred -Authentication Basic -AllowRedirection ; Import-PSSession $Session
dir -r | % { $_.FullName.substring($pwd.Path.length+1) + $(if ($_.PsIsContainer) {'\'}) }
dir | % { New-Object PSObject -Property @{ Name = $_.Name; Size = if($_.PSIsContainer) { (gci $_.FullName -Recurse | Measure Length -Sum).Sum } else {$_.Length}; Type = if($_.PSIsContainer) {'Directory'} else {'File'} } }
Get-CimInstance -ClassName Win32_OperatingSystem | Select-Object -Property Build*,OSType,ServicePack*
$env:path -split ';' | sort -Unique