@ECHO off

SET scriptRoot=%~dp0

start /b powershell.exe -ExecutionPolicy Unrestricted -NoExit .\PackageNuGet.ps1 %scriptRoot%