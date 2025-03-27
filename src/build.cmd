@echo off
if /i "%cswinrt_echo%" == "on" @echo on

set CsWinRTBuildNetSDKVersion=6.0.424
set CsWinRTBuildNet8SDKVersion=8.0.303
set this_dir=%~dp0

:dotnet
rem Install required .NET SDK version and add to environment
if "%CIBuildReason%"=="CI" goto :params

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
rem Install .NET 8 used to build projection
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTBuildNet8SDKVersion%' -InstallDir '%DOTNET_ROOT%' -Architecture 'x64' -DownloadTimeout %DownloadTimeout% ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'
powershell -NoProfile -ExecutionPolicy unrestricted -Command ^
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) ^
-Version '%CsWinRTBuildNet8SDKVersion%' -InstallDir '%DOTNET_ROOT(x86)%' -Architecture 'x86' -DownloadTimeout %DownloadTimeout% ^
-AzureFeed 'https://dotnetcli.blob.core.windows.net/dotnet'

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
set cswinrt_aot_functional_tests=JsonValueFunctionCalls, ClassActivation, Structs, Events, DynamicInterfaceCasting, Collections, Async, DerivedClassActivation, DerivedClassAsBaseClass, CCW

if "%cswinrt_platform%" EQU "x86" set run_functional_tests=true
if "%cswinrt_platform%" EQU "x64" (
  if "%CIBuildReason%" NEQ "CI" (
    set run_functional_tests=true
  )
)

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
if not exist %nuget_dir%\nuget.exe powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile %nuget_dir%\nuget.exe"
%nuget_dir%\nuget update -self
rem Note: packages.config-based (vcxproj) projects do not support msbuild /t:restore
call %this_dir%get_testwinrt.cmd
set NUGET_RESTORE_MSBUILD_ARGS=/p:platform="%cswinrt_platform%"
if "%CIBuildReason%"=="CI" set NUGET_RESTORE_MSBUILD_ARGS=%NUGET_RESTORE_MSBUILD_ARGS%;CIBuildReason=%CIBuildReason%
call :exec %nuget_dir%\nuget.exe restore %nuget_params% %this_dir%cswinrt.sln
rem: Calling nuget restore again on ObjectLifetimeTests.Lifted.csproj to prevent .props from \microsoft.testplatform.testhost\build\netcoreapp2.1 from being included. Nuget.exe erroneously imports props files. https://github.com/NuGet/Home/issues/9672
call :exec %msbuild_path%msbuild.exe %this_dir%\Tests\ObjectLifetimeTests\ObjectLifetimeTests.Lifted.csproj /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%

if "%cswinrt_platform%" EQU "x64" (
  if /I "%cswinrt_configuration%" EQU "release" (
    rem We restore here as NAOT needs its own restore to pull in ILC
    call :exec %msbuild_path%msbuild.exe %this_dir%\Tests\AuthoringTest\AuthoringTest.csproj /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%
  )
)

:build
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% %this_dir%cswinrt.sln 
if ErrorLevel 1 (
  echo.
  echo ERROR: Build failed
  exit /b !ErrorLevel!
)

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

    call :exec %msbuild_path%msbuild.exe /t:publish %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% /p:TargetFramework=net6.0 /p:solutiondir=%this_dir% %this_dir%Tests\FunctionalTests\%%a\%%a.csproj -bl:CCWBin%%a.binlog
  )
)

if "%cswinrt_platform%" EQU "x64" (
  if /I "%cswinrt_configuration%" EQU "release" (
    echo Publishing AOT functional tests for %cswinrt_platform% %cswinrt_configuration%
    for %%a in (%cswinrt_aot_functional_tests%) do (
      echo Publishing %%a

      rem No restore needed here as the previous run for .NET 6 did a restore without target framework.
      call :exec %msbuild_path%msbuild.exe /t:publish %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% /p:TargetFramework=net8.0 /p:solutiondir=%this_dir% %this_dir%Tests\FunctionalTests\%%a\%%a.csproj
    )
  )
)

if "%cswinrt_build_only%"=="true" goto :eof

:buildembedded
echo Building embedded sample for %cswinrt_platform% %cswinrt_configuration%
call :exec %nuget_dir%\nuget.exe restore %nuget_params% %this_dir%Samples\TestEmbedded\TestEmbedded.sln
call :exec %msbuild_path%msbuild.exe %this_dir%\Samples\TestEmbedded\TestEmbedded.sln /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %this_dir%\Samples\TestEmbedded\TestEmbedded.sln /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration% /bl:embeddedsample.binlog
if ErrorLevel 1 (
  echo.
  echo ERROR: Embedded build failed
  exit /b !ErrorLevel!
)

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

:embeddedtests
:: build the embedded sample and run the unittest 
call :exec %dotnet_exe% test --verbosity normal --no-build --logger xunit;LogFilePath=%~dp0embedunittest_%cswinrt_version_string%.xml %this_dir%Samples/TestEmbedded/UnitTestEmbedded/UnitTestEmbedded.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration% -- RunConfiguration.TreatNoTestsAsError=true
if ErrorLevel 1 (
  echo.
  echo ERROR: Embedded unit test failed, skipping NuGet pack
  exit /b !ErrorLevel!
)

:objectlifetimetests
rem Running Object Lifetime Unit Tests
echo Running object lifetime tests for %cswinrt_platform% %cswinrt_configuration%
if '%NUGET_PACKAGES%'=='' set NUGET_PACKAGES=%USERPROFILE%\.nuget\packages
call :exec vstest.console.exe %this_dir%\Tests\ObjectLifetimeTests\bin\%cswinrt_platform%\%cswinrt_configuration%\net8.0-windows10.0.19041.0\win10-%cswinrt_platform%\ObjectLifetimeTests.Lifted.build.appxrecipe /TestAdapterPath:"%NUGET_PACKAGES%\mstest.testadapter\2.2.4-preview-20210513-02\build\_common" /framework:FrameworkUap10 /logger:trx;LogFileName=%this_dir%\VsTestResults.trx 
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

:functionaltest
rem Run functional tests
if "%run_functional_tests%" EQU "true" (
  echo Running cswinrt functional tests for %cswinrt_platform% %cswinrt_configuration%

  for %%a in (%cswinrt_functional_tests%) do (
    echo Running %%a

    call :exec %this_dir%Tests\FunctionalTests\%%a\bin\%cswinrt_configuration%\net6.0\win-%cswinrt_platform%\publish\%%a.exe
    if %ErrorLevel% NEQ 100 (
      echo.
      echo ERROR: Functional test '%%a' failed with !ErrorLevel!, skipping NuGet pack
      exit /b !ErrorLevel!
    )
  )
)

if "%cswinrt_platform%" EQU "x64" (
  if /I "%cswinrt_configuration%" EQU "release" (
    echo Running cswinrt AOT functional tests for %cswinrt_platform% %cswinrt_configuration%

    for %%a in (%cswinrt_aot_functional_tests%) do (
      echo Running %%a

      call :exec %this_dir%Tests\FunctionalTests\%%a\bin\%cswinrt_configuration%\net8.0\win-%cswinrt_platform%\publish\%%a.exe
      if %ErrorLevel% NEQ 100 (
        echo.
        echo ERROR: AOT Functional test '%%a' failed with !ErrorLevel!, skipping NuGet pack
        exit /b !ErrorLevel!
      )
    )
  )
)


if "%cswinrt_label%"=="functionaltest" exit /b 0

:package
rem We set the properties of the CsWinRT.nuspec here, and pass them as the -Properties option when we call `nuget pack`
set cswinrt_bin_dir=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin\
set cswinrt_exe=%cswinrt_bin_dir%cswinrt.exe
set interop_winmd=%cswinrt_bin_dir%WinRT.Interop.winmd
set netstandard2_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Runtime.dll
set net6_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\net6.0\WinRT.Runtime.dll
set net8_runtime=%this_dir%WinRT.Runtime\bin\%cswinrt_configuration%\net8.0\WinRT.Runtime.dll
set source_generator=%this_dir%Authoring\WinRT.SourceGenerator\bin\%cswinrt_configuration%\netstandard2.0\WinRT.SourceGenerator.dll
set winrt_host_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll
set winrt_host_resource_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll.mui
set winrt_shim=%this_dir%Authoring\WinRT.Host.Shim\bin\%cswinrt_configuration%\net6.0\WinRT.Host.Shim.dll
set guid_patch=%this_dir%Perf\IIDOptimizer\bin\%cswinrt_configuration%\net6.0\*.*
set cswinmd_outpath=%this_dir%Authoring\cswinmd\bin\%cswinrt_configuration%\net6.0
rem Now call pack
echo Creating nuget package
call :exec %nuget_dir%\nuget pack %this_dir%..\nuget\Microsoft.Windows.CsWinRT.nuspec -Properties cswinrt_exe=%cswinrt_exe%;interop_winmd=%interop_winmd%;netstandard2_runtime=%netstandard2_runtime%;net6_runtime=%net6_runtime%;net8_runtime=%net8_runtime%;source_generator=%source_generator%;cswinrt_nuget_version=%cswinrt_version_string%;winrt_host_x86=%winrt_host_x86%;winrt_host_x64=%winrt_host_x64%;winrt_host_arm=%winrt_host_arm%;winrt_host_arm64=%winrt_host_arm64%;winrt_host_resource_x86=%winrt_host_resource_x86%;winrt_host_resource_x64=%winrt_host_resource_x64%;winrt_host_resource_arm=%winrt_host_resource_arm%;winrt_host_resource_arm64=%winrt_host_resource_arm64%;winrt_shim=%winrt_shim%;guid_patch=%guid_patch% -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
call :exec %nuget_dir%\nuget pack %this_dir%..\nuget\Microsoft.Windows.CsWinMD.nuspec -Properties cswinmd_outpath=%cswinmd_outpath%;source_generator=%source_generator%;cswinmd_nuget_version=%cswinrt_version_string%; -OutputDirectory %cswinrt_bin_dir% -NonInteractive -Verbosity Detailed -NoPackageAnalysis
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