param($scriptRoot)

$ErrorActionPreference = "Stop"

function Resolve-MsBuild {
	$msb2017 = Resolve-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild\*\bin\msbuild.exe" -ErrorAction SilentlyContinue
	if($msb2017) {
		Write-Host "Found MSBuild 2017 (or later)."
		Write-Host $msb2017
		return $msb2017
	}

	$msBuild2015 = "${env:ProgramFiles(x86)}\MSBuild\14.0\bin\msbuild.exe"

	if(-not (Test-Path $msBuild2015)) {
		throw 'Could not find MSBuild 2015 or later.'
	}

	Write-Host "Found MSBuild 2015."
	Write-Host $msBuild2015

	return $msBuild2015
}

$msBuild = Resolve-MsBuild
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\Rainbow.sln"

Remove-Item $scriptRoot\..\src\*\bin\release\*.nupkg

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$version = Read-Host "Enter version to build"

function PackNCopy($projFile) {
	& $msBuild $projFile /p:Configuration=Release /t:pack /p:Version=$version /p:IncludeSymbols=true
	$outputPath = [IO.Path]::GetDirectoryName($projFile)
	$outputPath = Resolve-Path $outputPath\bin\release\*.nupkg
	Copy-Item $outputPath $scriptRoot
}

& $nuGet pack "$scriptRoot\Rainbow.nuget\Rainbow.nuspec" -version $version
PackNCopy "$scriptRoot\..\src\Rainbow\Rainbow.csproj"
PackNCopy "$scriptRoot\..\src\Rainbow.Storage.Sc\Rainbow.Storage.Sc.csproj"
PackNCopy "$scriptRoot\..\src\Rainbow.Storage.Yaml\Rainbow.Storage.Yaml.csproj"