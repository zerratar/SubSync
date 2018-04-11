rem rmdir .\build\self-contained-win10-x86 /S /Q
rem rmdir .\build\self-contained-win10-x64 /S /Q
rmdir .\build\win10-x86 /S /Q
rmdir .\build\win10-x64 /S /Q
rem dotnet publish -o ..\..\build\self-contained-win10-x86 -r win10-x86 --self-contained
rem dotnet publish -o ..\..\build\self-contained-win10-x64 -r win10-x64 --self-contained
dotnet publish -o ..\..\build\win10-x86 -r win10-x86 --self-contained false
dotnet publish -o ..\..\build\win10-x64 -r win10-x64 --self-contained false
pause