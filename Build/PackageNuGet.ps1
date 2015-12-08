param($scriptRoot)

$ErrorActionPreference = "Stop"
$programFilesx86 = ${Env:ProgramFiles(x86)}
$msBuild = "$programFilesx86\MSBuild\14.0\bin\msbuild.exe"
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\Rainbow.sln"

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$synthesisAssembly = Get-Item "$scriptRoot\..\src\Rainbow\bin\Release\Rainbow.dll" | Select-Object -ExpandProperty VersionInfo
$targetAssemblyVersion = $synthesisAssembly.ProductVersion

& $nuGet pack "$scriptRoot\Rainbow.nuget\Rainbow.nuspec" -version $targetAssemblyVersion
& $nuGet pack "$scriptRoot\..\src\Rainbow\Rainbow.csproj" -Symbols -Prop Configuration=Release
& $nuGet pack "$scriptRoot\..\src\Rainbow.Storage.Sc\Rainbow.Storage.Sc.csproj" -Symbols -Prop Configuration=Release
& $nuGet pack "$scriptRoot\..\src\Rainbow.Storage.Yaml\Rainbow.Storage.Yaml.csproj" -Symbols -Prop Configuration=Release