
@echo off
"%~dp0..\tools\nuget.exe" restore "%~dp0..\Gibson.sln"
"%~dp0..\tools\nuget.exe" pack "%~dp0..\src\Gibson\Gibson.csproj" -Build -Symbols -Properties Configuration=Release
PAUSE