@echo off

setlocal ENABLEDELAYEDEXPANSION

rem Add per-user dotnet path to find pre-release versions
set path=%LocalAppData%\Microsoft\dotnet;%path%

rem User expected to provide global.json with allowPrerelease=true
if not exist %~dp0global.json (
  echo global.json not found, creating one to allowPrelease for unit test project builds
  echo { > global.json
  echo   "sdk": { >> global.json
  echo     "version": "5.0.100-preview.4.20213.16", >> global.json
  echo     "rollForward": "patch", >> global.json
  echo     "allowPrerelease": true >> global.json
  echo   } >> global.json
  echo } >> global.json
)

set cswinrt_platform=%1
set cswinrt_configuration=%2
set cswinrt_version=%3

if "%cswinrt_platform%"=="" set cswinrt_platform=x64

if "%cswinrt_version%"=="" set cswinrt_version=1.0.0.0

if /I "%cswinrt_platform%" equ "all" (
  if "%cswinrt_configuration%"=="" (
    set cswinrt_configuration=all
  )
  call %0 x86 !cswinrt_configuration! !cswinrt_version!
  call %0 x64 !cswinrt_configuration! !cswinrt_version!
  call %0 arm !cswinrt_configuration! !cswinrt_version!
  call %0 arm64 !cswinrt_configuration! !cswinrt_version!
  goto :eof
)

if /I "%cswinrt_configuration%" equ "all" (
  call %0 %cswinrt_platform% Debug !cswinrt_version!
  call %0 %cswinrt_platform% Release !cswinrt_version!
  goto :eof
)

if "%cswinrt_configuration%"=="" (
  set cswinrt_configuration=Release
)

:build
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
msbuild cswinrt.sln /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;GenerateTestProjection=true

:test
rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
echo Running cswinrt unit tests for %cswinrt_platform% %cswinrt_configuration%
set dotnet_exe="%LocalAppData%\Microsoft\dotnet\dotnet.exe"
if not exist %dotnet_exe% (
  if %cswinrt_platform%==x86 (
    set dotnet_exe="%ProgramFiles(x86)%\dotnet\dotnet.exe"
  ) else (
    set dotnet_exe="%ProgramFiles%\dotnet\dotnet.exe"
  )
)
%dotnet_exe% test --no-build --logger xunit;LogFilePath=%~dp0test_%cswinrt_version%.xml unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%
if ErrorLevel 1 (
  echo.
  echo ERROR: Unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:package
set cswinrt_bin_dir=%~dp0_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set netstandard2_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Runtime.dll
set netcoreapp5_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\netcoreapp5.0\WinRT.Runtime.dll
nuget pack nuget/Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;netstandard2_runtime=%netstandard2_runtime%;netcoreapp5_runtime=%netcoreapp5_runtime% -Version %cswinrt_version% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed
