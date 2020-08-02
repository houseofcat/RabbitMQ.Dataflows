dotnet restore
dotnet publish -c Release -o "C:\Services\Windows.NetCoreService"

sc.exe create Windows.NetCoreService binpath="C:\Services\Windows.NetCoreService\Windows.NetCoreService.exe"
sc.exe start Windows.NetCoreService

sc.exe stop Windows.NetCoreService
sc.exe delete Windows.NetCoreService
