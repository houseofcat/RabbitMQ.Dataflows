dotnet restore
dotnet publish -c Release -o C:\Services\MyWorkerService

sc.exe create MyWorkerService binpath=C:\Services\MyWorkerService\MyWorkerService.exe
sc.exe start MyWorkerService

sc.exe stop MyWorkerService
sc.exe delete MyWorkerService