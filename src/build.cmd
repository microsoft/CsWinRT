@echo off
if /i "%cswinrt_echo%" == "on" @echo on

set CsWinRTNet5SdkVersion=6.0.100-preview.6.21355.2
set this_dir=%~dp0

:dotnet
rem Install required .NET 5 SDK version and add to environment
set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%path%


powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTNet5SdkVersion%' -InstallDir '%DOTNET_ROOT%' -Architecture 'x64' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTNet5SdkVersion%' -InstallDir '%DOTNET_ROOT(86)%' -Architecture 'x86' ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'

:globaljson
rem Create global.json for current .NET SDK, and with allowPrerelease=true
set global_json=%this_dir%global.json
echo { > %global_json%
echo   "sdk": { >> %global_json%
echo     "version": "%CsWinRTNet5SdkVersion%", >> %global_json%
echo     "allowPrerelease": true >> %global_json%
echo   } >> %global_json%
echo } >> %global_json%

rem Preserve above for Visual Studio launch inheritance
setlocal ENABLEDELAYEDEXPANSION

:params
set cswinrt_platform=%1
set cswinrt_configuration=%2
set cswinrt_version_number=%3
set cswinrt_version_string=%4
set cswinrt_assembly_version=%5
set "%6"!="" set cswinrt_label=%6

if "%cswinrt_platform%"=="" set cswinrt_platform=x64

if /I "%cswinrt_platform%" equ "all" (
  if "%cswinrt_configuration%"=="" (
    set cswinrt_configuration=all
  )
  call %0 x86 !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  call %0 x64 !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  call %0 arm !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  call %0 arm64 !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  goto :eof
)

if /I "%cswinrt_configuration%" equ "all" (
  call %0 %cswinrt_platform% Debug !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  call %0 %cswinrt_platform% Release !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  goto :eof
)

if "%cswinrt_configuration%"=="" (
  set cswinrt_configuration=Release
)

if "%cswinrt_version_number%"=="" set cswinrt_version_number=0.0.0.0
if "%cswinrt_version_string%"=="" set cswinrt_version_string=0.0.0-private.0
if "%cswinrt_assembly_version%"=="" set cswinrt_assembly_version=0.0.0.0

if "%cswinrt_baseline_breaking_compat_errors%"=="" set cswinrt_baseline_breaking_compat_errors=false
if "%cswinrt_baseline_assembly_version_compat_errors%"=="" set cswinrt_baseline_assembly_version_compat_errors=false

rem Generate prerelease targets file to exercise build warnings
set prerelease_targets=%this_dir%..\nuget\Microsoft.Windows.CsWinRT.Prerelease.targets
rem Create default %prerelease_targets%
echo ^<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" InitialTargets="CsWinRTVerifyPrerelease"^> > %prerelease_targets%
echo   ^<Target Name="CsWinRTVerifyPrerelease" >> %prerelease_targets%
echo     Condition="'$(NetCoreSdkVersion)' ^!= '%CsWinRTNet5SdkVersion%' and '$(Net5SdkVersion)' ^!= '%CsWinRTNet5SdkVersion%'"^> >> %prerelease_targets%
echo     ^<Warning Text="This C#/WinRT prerelease is designed for .Net SDK %CsWinRTNet5SdkVersion%. Other prereleases may be incompatible due to breaking changes." /^> >> %prerelease_targets%
echo   ^</Target^> >> %prerelease_targets%
echo ^</Project^> >> %prerelease_targets%

goto :skip_build_tools
rem VS 16.X BuildTools support (when a prerelease VS is required, until it is deployed to Azure Devops agents) 
msbuild -ver | findstr 16.X >nul
if ErrorLevel 1 (
  echo Using VS Build Tools 16.X 
  if %cswinrt_platform%==x86 (
    set msbuild_path="%this_dir%.buildtools\MSBuild\Current\Bin\\"
  ) else (
    set msbuild_path="%this_dir%.buildtools\MSBuild\Current\Bin\amd64\\"
  )
  if not exist !msbuild_path! (
    if not exist .buildtools md .buildtools 
    powershell -NoProfile -ExecutionPolicy unrestricted -File .\get_buildtools.ps1
  )
  set nuget_params=-MSBuildPath !msbuild_path!
) else (
  set msbuild_path=
  set nuget_params= 
)
:skip_build_tools

set nuget_dir=%this_dir%.nuget

if not "%cswinrt_label%"=="" goto %cswinrt_label%

:restore
rem When a preview nuget is required, update -self doesn't work, so manually update 
if exist %nuget_dir%\nuget.exe (
  %nuget_dir%\nuget.exe | findstr 5.9 >nul
  if ErrorLevel 1 (
    echo Updating to nuget 5.9
    rd /s/q %nuget_dir% >nul 2>&1
  )
)
if not exist %nuget_dir% md %nuget_dir%
if not exist %nuget_dir%\nuget.exe powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/v5.8.0-preview.2/nuget.exe -OutFile %nuget_dir%\nuget.exe"
%nuget_dir%\nuget update -self
rem Note: packages.config-based (vcxproj) projects do not support msbuild /t:restore
call %this_dir%get_testwinrt.cmd
set NUGET_RESTORE_MSBUILD_ARGS=/p:platform="%cswinrt_platform%"
call :exec %nuget_dir%\nuget.exe restore %nuget_params% %this_dir%cswinrt.sln
rem: Calling nuget restore again on ObjectLifetimeTests.Lifted.csproj to prevent .props from \microsoft.testplatform.testhost\build\netcoreapp2.1 from being included. Nuget.exe erroneously imports props files. https://github.com/NuGet/Home/issues/9672
call :exec %msbuild_path%msbuild.exe %this_dir%\Tests\ObjectLifetimeTests\ObjectLifetimeTests.Lifted.csproj /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%

:build
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% %this_dir%cswinrt.sln 
if ErrorLevel 1 (
  echo.
  echo ERROR: Build failed
  exit /b !ErrorLevel!
)
if "%cswinrt_build_only%"=="true" goto :eof

:test
:unittest
rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
echo Running cswinrt unit tests for %cswinrt_platform% %cswinrt_configuration%
if %cswinrt_platform%==x86 (
  set dotnet_exe="%DOTNET_ROOT(86)%\dotnet.exe"
) else (
  set dotnet_exe="%DOTNET_ROOT%\dotnet.exe"
)
if not exist %dotnet_exe% (
  if %cswinrt_platform%==x86 (
    set dotnet_exe="%ProgramFiles(x86)%\dotnet\dotnet.exe"
  ) else (
    set dotnet_exe="%ProgramFiles%\dotnet\dotnet.exe"
  )
)

:objectlifetimetests
rem Running Object Lifetime Unit Tests
pushd .
cd %this_dir%\Tests\ObjectLifetimeTests\bin\%cswinrt_platform%\%cswinrt_configuration%\net5.0-windows10.0.19041.0\win10-%cswinrt_platform%
sn -Vr Microsoft.Windows.SDK.NET.dll
vstest.console.exe ObjectLifetimeTests.Lifted.build.appxrecipe /TestAdapterPath:"%USERPROFILE%\.nuget\packages\mstest.testadapter\2.2.4-preview-20210513-02\build\_common" /framework:FrameworkUap10 /logger:trx;LogFileName=%this_dir%\VsTestResults.trx 
popd



rem WinUI NuGet package's Microsoft.WinUI.AppX.targets attempts to import a file that does not exist, even when
rem executing "dotnet test --no-build ...", which evidently still needs to parse and load the entire project.
rem Work around by using a dummy targets file and assigning it to the MsAppxPackageTargets property.
echo ^<Project/^> > %temp%\EmptyMsAppxPackage.Targets
call :exec %dotnet_exe% test --verbosity normal --no-build --logger xunit;LogFilePath=%~dp0unittest_%cswinrt_version_string%.xml %this_dir%Tests/unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;MsAppxPackageTargets=%temp%\EmptyMsAppxPackage.Targets 
if ErrorLevel 1 (
  echo.
  echo ERROR: Unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:hosttest
rem Run WinRT.Host tests
echo Running cswinrt host tests for %cswinrt_platform% %cswinrt_configuration%
call :exec %this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\HostTest\bin\HostTest.exe --gtest_output=xml:%this_dir%hosttest_%cswinrt_version_string%.xml 
if ErrorLevel 1 (
  echo.
  echo ERROR: Host test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)
 

:authortest
rem Run Authoring tests
echo Running cswinrt authoring tests for %cswinrt_platform% %cswinrt_configuration%
call :exec %this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\AuthoringConsumptionTest\bin\AuthoringConsumptionTest.exe --gtest_output=xml:%this_dir%hosttest_%cswinrt_version_string%.xml 
if ErrorLevel 1 (
  echo.
  echo ERROR: Authoring test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:package
set cswinrt_bin_dir=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set interop_winmd=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\obj\merged\WinRT.Interop.winmd
set netstandard2_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Runtime.dll
set net5_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\net5.0\WinRT.Runtime.dll
set source_generator=%this_dir%Authoring\WinRT.SourceGenerator\bin\%cswinrt_configuration%\netstandard2.0\WinRT.SourceGenerator.dll
set winrt_host_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll
set winrt_shim=%this_dir%Authoring\WinRT.Host.Shim\bin\%cswinrt_configuration%\net5.0\WinRT.Host.Shim.dll
echo Creating nuget package
call :exec %nuget_dir%\nuget pack %this_dir%..\nuget\Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;interop_winmd=%interop_winmd%;netstandard2_runtime=%netstandard2_runtime%;net5_runtime=%net5_runtime%;source_generator=%source_generator%;cswinrt_nuget_version=%cswinrt_version_string%;winrt_host_x86=%winrt_host_x86%;winrt_host_x64=%winrt_host_x64%;winrt_host_arm=%winrt_host_arm%;winrt_host_arm64=%winrt_host_arm64%;winrt_shim=%winrt_shim% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
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

