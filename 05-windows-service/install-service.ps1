# Run in an elevated PowerShell on Windows
dotnet publish -c Release -r win-x64 --self-contained false -o C:\Services\ReportWorker
sc.exe create "ReportDeliveryWorker" binPath= "C:\Services\ReportWorker\WindowsService.exe" start= auto
sc.exe failure "ReportDeliveryWorker" reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc.exe start "ReportDeliveryWorker"
