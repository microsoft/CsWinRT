@echo off
if /i "%cswinrt_echo%" == "on" @echo on

set CsWinRTBuildNetSDKVersion=10.0.100

set this_dir=%~dp0

if /i not "%cswinrt_setup_local_dotnet%" == "true" goto :skip_dotnet_installation

:dotnet
rem Install required .NET SDK version and add to environment
set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(x86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%DOTNET_ROOT(x86)%;%path%
set DownloadTimeout=1200

rem Install .NET Version used to build projection
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTBuildNetSDKVersion%' -InstallDir '%DOTNET_ROOT%' -Architecture 'x64' -DownloadTimeout %DownloadTimeout% ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTBuildNetSDKVersion%' -InstallDir '%DOTNET_ROOT(x86)%' -Architecture 'x86' -DownloadTimeout %DownloadTimeout% ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'
:skip_dotnet_installation

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

set cswinrt_functional_tests=JsonValueFunctionCalls, ClassActivation, Structs, Events, DynamicInterfaceCasting, Collections, Async, DerivedClassActivation, DerivedClassAsBaseClass, CCW

if "%cswinrt_platform%" EQU "x86" set run_functional_tests=true
if "%cswinrt_platform%" EQU "x64" set run_functional_tests=true

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
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /p:RestorePackagesConfig=true /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform% %this_dir%cswinrt.slnx 

:build
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% %this_dir%cswinrt.slnx 
if ErrorLevel 1 (
  echo.
  echo ERROR: Build failed
  exit /b !ErrorLevel!
)

rem skip tests for now
goto :package

if "%cswinrt_platform%" NEQ "arm" (
  if "%cswinrt_platform%" NEQ "arm64" (
    echo Restore functional tests for %cswinrt_platform% %cswinrt_configuration%
    for %%a in (%cswinrt_functional_tests%) do (
      echo Restoring %%a

      rem Do restore separately to workaround issue where specifying TargetFramework causes nuget restore to propagate it to project references causing issues.
      call :exec %msbuild_path%msbuild.exe /t:restore %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% /p:solutiondir=%this_dir% %this_dir%Tests\FunctionalTests\%%a\%%a.csproj
    )
  )
)

if "%run_functional_tests%" EQU "true" (
  echo Publishing functional tests for %cswinrt_platform% %cswinrt_configuration%
  for %%a in (%cswinrt_functional_tests%) do (
    echo Publishing %%a

    call :exec %msbuild_path%msbuild.exe /t:publish %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% /p:TargetFramework=net10.0 /p:solutiondir=%this_dir% %this_dir%Tests\FunctionalTests\%%a\%%a.csproj
  )
)

if "%cswinrt_build_only%"=="true" goto :eof

rem Tests are not yet enabled for ARM builds (not supported by Project Reunion)
if %cswinrt_platform%==arm goto :package
if %cswinrt_platform%==arm64 goto :package

:test
rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
if %cswinrt_platform%==x86 (
  set dotnet_exe="%DOTNET_ROOT(x86)%\dotnet.exe"
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
echo Running object lifetime tests for %cswinrt_platform% %cswinrt_configuration%
if '%NUGET_PACKAGES%'=='' set NUGET_PACKAGES=%USERPROFILE%\.nuget\packages
call :exec vstest.console.exe %this_dir%\Tests\ObjectLifetimeTests\bin\%cswinrt_platform%\%cswinrt_configuration%\net10.0-windows10.0.19041.0\win-%cswinrt_platform%\ObjectLifetimeTests.Lifted.build.appxrecipe /TestAdapterPath:"%NUGET_PACKAGES%\mstest.testadapter\3.10.1\build\_common" /framework:FrameworkUap10 /logger:trx;LogFileName=%this_dir%\VsTestResults.trx 
if ErrorLevel 1 (
  echo.
  echo ERROR: Lifetime test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:unittest
rem WinUI NuGet package's Microsoft.WinUI.AppX.targets attempts to import a file that does not exist, even when
rem executing "dotnet test --no-build ...", which evidently still needs to parse and load the entire project.
rem Work around by using a dummy targets file and assigning it to the MsAppxPackageTargets property.
echo Running cswinrt unit tests for %cswinrt_platform% %cswinrt_configuration%
echo ^<Project/^> > %temp%\EmptyMsAppxPackage.Targets
call :exec %dotnet_exe% test --verbosity normal --no-build --logger xunit;LogFilePath=%~dp0unittest_%cswinrt_version_string%.xml %this_dir%Tests/unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;MsAppxPackageTargets=%temp%\EmptyMsAppxPackage.Targets -- RunConfiguration.TreatNoTestsAsError=true
if ErrorLevel 1 (
  echo.
  echo ERROR: Unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:sourcegeneratortest
rem Running Source Generator Unit Tests
echo Running source generator tests for %cswinrt_platform% %cswinrt_configuration%
call :exec %dotnet_exe% test --verbosity normal --no-build --logger trx;LogFilePath=%~dp0sourcegeneratortest_%cswinrt_version_string%.trx %this_dir%Tests\SourceGeneratorTest\SourceGeneratorTest.csproj /nologo /m /p:configuration=%cswinrt_configuration% -- RunConfiguration.TreatNoTestsAsError=true
if ErrorLevel 1 (
  echo.
  echo ERROR: Source generator unit test failed, skipping NuGet pack
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
  rem Not skipping due to known issue.
  rem echo ERROR: Authoring test failed, skipping NuGet pack
  rem exit /b !ErrorLevel!
)

:functionaltest
rem Run functional tests
if "%run_functional_tests%" EQU "true" (
  echo Running cswinrt functional tests for %cswinrt_platform% %cswinrt_configuration%

  for %%a in (%cswinrt_functional_tests%) do (
    echo Running %%a

    call :exec %this_dir%Tests\FunctionalTests\%%a\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\%%a.exe
    if !errorlevel! NEQ 100 (
      echo.
      echo ERROR: Functional test '%%a' failed with !errorlevel!, skipping NuGet pack
      exit /b !ErrorLevel!
    )
  )
)

if "%cswinrt_label%"=="functionaltest" exit /b 0

:package
rem We set the properties of the CsWinRT.nuspec here, and pass them as the -Properties option when we call `nuget pack`
set cswinrt_bin_dir=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set interop_winmd=%cswinrt_bin_dir%WinRT.Interop.winmd
set net10_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\net10.0\WinRT.Runtime.dll
set source_generator_roslyn4120=%this_dir%Authoring\WinRT.SourceGenerator.Roslyn4120\bin\%cswinrt_configuration%\netstandard2.0\WinRT.SourceGenerator.dll
set winrt_host_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll
set winrt_host_resource_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll.mui
set winrt_shim=%this_dir%Authoring\WinRT.Host.Shim\bin\%cswinrt_configuration%\net10.0\WinRT.Host.Shim.dll
set cswinmd_outpath=%this_dir%Authoring\cswinmd\bin\%cswinrt_configuration%\net10.0
rem Now call pack
echo Creating nuget package
call :exec %nuget_dir%\nuget pack %this_dir%..\nuget\Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;interop_winmd=%interop_winmd%;net10_runtime=%net10_runtime%;source_generator_roslyn4120=%source_generator_roslyn4120%;cswinrt_nuget_version=%cswinrt_version_string%;winrt_host_x86=%winrt_host_x86%;winrt_host_x64=%winrt_host_x64%;winrt_host_arm=%winrt_host_arm%;winrt_host_arm64=%winrt_host_arm64%;winrt_host_resource_x86=%winrt_host_resource_x86%;winrt_host_resource_x64=%winrt_host_resource_x64%;winrt_host_resource_arm=%winrt_host_resource_arm%;winrt_host_resource_arm64=%winrt_host_resource_arm64%;winrt_shim=%winrt_shim% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
call :exec %nuget_dir%\nuget pack %this_dir%..\nuget\Microsoft.Windows.CsWinMD.nuspec -Properties cswinmd_outpath=%cswinmd_outpath%;source_generator_roslyn4120=%source_generator_roslyn4120%;cswinmd_nuget_version=%cswinrt_version_string%; -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
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
