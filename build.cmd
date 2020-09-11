@echo off
if /i "%cswinrt_echo%" == "on" @echo on

rem set CsWinRTNet5SdkVersion=5.0.100-preview.7.20366.15
set CsWinRTNet5SdkVersion=5.0.100-preview.8.20417.9

:dotnet
rem Install required .NET 5 SDK version and add to environment
set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%path%
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTNet5SdkVersion%' -InstallDir "%DOTNET_ROOT%" -Architecture 'x64' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet' "
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTNet5SdkVersion%' -InstallDir "%DOTNET_ROOT(86)%" -Architecture 'x86' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet' "

:globaljson
rem User expected to provide global.json with allowPrerelease=true
if not exist %~dp0global.json (
  echo Creating default global.json to allowPrelease for unit test project builds
  echo { > global.json
  echo   "sdk": { >> global.json
  echo     "version": "%CsWinRTNet5SdkVersion%", >> global.json
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
set "%5"!="" set cswinrt_label=%5

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

rem Generate prerelease targets file to exercise build warnings
set prerelease_targets=nuget\Microsoft.Windows.CsWinRT.Prerelease.targets
if not exist %prerelease_targets% (
  echo Creating default %prerelease_targets%
  echo ^<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" InitialTargets="CsWinRTVerifyPrerelease"^> > %prerelease_targets%
  echo   ^<Target Name="CsWinRTVerifyPrerelease" >> %prerelease_targets%
  echo     Condition="'$(NetCoreSdkVersion)' ^!= '%CsWinRTNet5SdkVersion%' and '$(Net5SdkVersion)' ^!= '%CsWinRTNet5SdkVersion%'"^> >> %prerelease_targets%
  echo     ^<Warning Text="This C#/WinRT prerelease is designed for .Net SDK %CsWinRTNet5SdkVersion%. Other prereleases may be incompatible due to breaking changes." /^> >> %prerelease_targets%
  echo   ^</Target^> >> %prerelease_targets%
  echo ^</Project^> >> %prerelease_targets%
)

rem Xcopy MSBuild support (temporary, until VS 16.8 is deployed to Azure Devops agents in 12/2020) 
if %cswinrt_platform%==x86 (
  set msbuild_path="%cd%\.msbuild\tools\MSBuild\Current\Bin\\"
) else (
  set msbuild_path="%cd%\.msbuild\tools\MSBuild\Current\Bin\amd64\\"
)
if exist %msbuild_path% (
set nuget_params=-MSBuildPath %msbuild_path%
rem TargetFrameworkRootPath="D:\git\cswinrt2\.msbuild\RoslynTools.ReferenceAssemblies.0.1.3.nupkg";^
set msbuild_params=/p:UseXcopyMSBuild=true;^
TargetFrameworkRootPath="D:\git\xcopy-msbuild\binaries\RoslynTools.MSBuild.16.8.0-preview1.nupkg";^
VCTargetsPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Microsoft\VC\v160\\";^
MSBuildExtensionsPath="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\\";^
MSBuildExtensionsPath32="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild";^
MSBuildExtensionsPath64="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild" 
set dotnet_params=/p:^
MSBuildExtensionsPath="C:\Program Files\dotnet\sdk\%CsWinRTNet5SdkVersion%\\";^
MSBuildExtensionsPath32="C:\Program Files\dotnet\sdk\%CsWinRTNet5SdkVersion%";^
MSBuildExtensionsPath64="C:\Program Files\dotnet\sdk\%CsWinRTNet5SdkVersion%"
) else (
set msbuild_path=
set nuget_params=
set msbuild_params=
set dotnet_params=
)

if not "%cswinrt_label%"=="" goto %cswinrt_label%

:restore
if not exist .nuget md .nuget
if not exist .nuget\nuget.exe powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/v5.6.0/nuget.exe -OutFile .nuget\nuget.exe"
.nuget\nuget update -self
call :exec .nuget\nuget.exe restore %nuget_params%

:build
call get_testwinrt.cmd
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %msbuild_params% %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;GenerateTestProjection=true cswinrt.sln 

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
call :exec %dotnet_exe% test --no-build --logger xunit;LogFilePath=%~dp0unittest_%cswinrt_version_string%.xml unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;MsAppxPackageTargets=%temp%\EmptyMsAppxPackage.Targets %dotnet_params%
if ErrorLevel 1 (
  echo.
  echo ERROR: Unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

rem Run WinRT.Host tests
echo Running cswinrt host tests for %cswinrt_platform% %cswinrt_configuration%
call :exec %~dp0_build\%cswinrt_platform%\%cswinrt_configuration%\HostTest\bin\HostTest.exe --gtest_output=xml:%~dp0hosttest_%cswinrt_version_string%.xml 
if ErrorLevel 1 (
  echo.
  echo ERROR: Host test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:package
set cswinrt_bin_dir=%~dp0_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set netstandard2_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Runtime.dll
set net5_runtime=%~dp0WinRT.Runtime\bin\%cswinrt_configuration%\net5.0\WinRT.Runtime.dll
set source_generator=%~dp0Authoring\WinRT.SourceGenerator\bin\%cswinrt_configuration%\netstandard2.0\WinRT.SourceGenerator.dll
echo Creating nuget package
call :exec .nuget\nuget pack nuget/Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;netstandard2_runtime=%netstandard2_runtime%;net5_runtime=%net5_runtime%;source_generator=%source_generator%;cswinrt_nuget_version=%cswinrt_version_string% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
goto :eof

:exec
if /i "%cswinrt_echo%" == "only" (
echo Command Line:
echo %*
echo.
) else (
%*
)
goto :eof