@echo off

set Net5SdkVersion=5.0.100-preview.4.20227.10

:dotnet
rem Install required .NET 5 SDK version and add to environment
set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%path%
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%Net5SdkVersion%' -InstallDir "%DOTNET_ROOT%" -Architecture 'x64' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet' "
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%Net5SdkVersion%' -InstallDir "%DOTNET_ROOT(86)%" -Architecture 'x86' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet' "

:globaljson
rem User expected to provide global.json with allowPrerelease=true
if not exist %~dp0global.json (
  echo global.json not found, creating one to allowPrelease for unit test project builds
  echo { > global.json
  echo   "sdk": { >> global.json
  echo     "version": "%Net5SdkVersion%", >> global.json
  echo     "rollForward": "patch", >> global.json
  echo     "allowPrerelease": true >> global.json
  echo   } >> global.json
  echo } >> global.json
)

rem Preserve above for Visual Studio launch inheritance
setlocal ENABLEDELAYEDEXPANSION

:params
set cswinrt_platform=%1
set cswinrt_configuration=%2
set cswinrt_version_number=%3
set cswinrt_version_string=%4
set cswinrt_label=%5

if "%cswinrt_platform%"=="" set cswinrt_platform=x64

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

if "%cswinrt_version_number%"=="" set cswinrt_version_number=0.0.0.0
if "%cswinrt_version_string%"=="" set cswinrt_version_string=0.0.0-private.0

if not "%cswinrt_label%"=="" goto %cswinrt_label%

:restore
if not exist .nuget md .nuget
if not exist .nuget\nuget.exe powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/v5.6.0/nuget.exe -OutFile .nuget\nuget.exe"
.nuget\nuget update -self
.nuget\nuget.exe restore

:build
call get_testwinrt.cmd
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
msbuild cswinrt.sln /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;GenerateTestProjection=true

:test
rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
echo Running cswinrt unit tests for %cswinrt_platform% %cswinrt_configuration%
set dotnet_exe="%DOTNET_ROOT%\dotnet.exe"
if not exist %dotnet_exe% (
  if %cswinrt_platform%==x86 (
    set dotnet_exe="%ProgramFiles(x86)%\dotnet\dotnet.exe"
  ) else (
    set dotnet_exe="%ProgramFiles%\dotnet\dotnet.exe"
  )
)

rem WinUI NuGet package's Microsoft.WinUI.AppX.targets attempts to import a file that does not exist, even when
rem executing "dotnet test --no-build ...", which evidently still needs to parse and load the entire project.
rem Work around by using a dummy targets file and assigning it to the MsAppxPackageTargets property.
echo ^<Project/^> > %temp%\EmptyMsAppxPackage.Targets

%dotnet_exe% test --no-build --logger xunit;LogFilePath=%~dp0test_%cswinrt_version_string%.xml unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;MsAppxPackageTargets=%temp%\EmptyMsAppxPackage.Targets
if ErrorLevel 1 (
  echo.
  echo ERROR: Unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

rem todo remove all this
:package
set cswinrt_bin_dir=%~dp0_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set netstandard2_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Runtime.dll
set net5_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\net5.0\WinRT.Runtime.dll
.nuget\nuget pack nuget/Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;netstandard2_runtime=%netstandard2_runtime%;net5_runtime=%net5_runtime%;cswinrt_nuget_version=%cswinrt_version_string% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
