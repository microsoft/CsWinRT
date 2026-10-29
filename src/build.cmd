@echo off
if /i "%cswinrt_echo%" == "on" @echo on

set CsWinRTBuildNetSDKVersion=10.0.402

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
if not "%~6"=="" set "cswinrt_label=%~6"

if "%cswinrt_platform%"=="" set cswinrt_platform=x64

if /I "%cswinrt_label%"=="product" goto :product
if /I "%cswinrt_label%"=="productpackage" goto :productpackage

if /I "%cswinrt_platform%" equ "all" (
  if "%cswinrt_configuration%"=="" (
    set cswinrt_configuration=all
  )
  call %0 x86 !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
  call %0 x64 !cswinrt_configuration! !cswinrt_version_number! !cswinrt_version_string! !cswinrt_assembly_version!
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

set "cswinrt_solution=%this_dir%cswinrt.slnx"
set cswinrt_generate_test_projection=true
set "cswinrt_build_tools_arch=%cswinrt_platform%"
set cswinrt_solution_build_params=
set cswinrt_reference_build_params=
set cswinrt_reference_output=

set cswinrt_functional_tests=JsonValueFunctionCalls, ClassActivation, CustomPropertyProvider, DefaultCustomPropertyProviderDisabled, DefaultCustomPropertyProviderSuppressed, DefaultCustomPropertyProviderWinUI, Structs, Events, DynamicInterfaceCasting, Collections, Async, DerivedClassActivation, DerivedClassAsBaseClass, CCW, NativeExposedType, ForwardedTypeIdentities

if "%cswinrt_platform%" EQU "x86" set run_functional_tests=true
if "%cswinrt_platform%" EQU "x64" set run_functional_tests=true

rem Require Visual Studio 2026 rather than silently using an older developer prompt.
%msbuild_path%msbuild.exe -version -nologo >nul 2>&1
if errorlevel 1 (
  echo ERROR: Visual Studio 2026 MSBuild is required. Run this script from its Developer Command Prompt.
  exit /b 1
)
%msbuild_path%msbuild.exe -version -nologo | findstr /r /b "18\." >nul
if errorlevel 1 (
  echo ERROR: Visual Studio 2026 MSBuild is required. Run this script from its Developer Command Prompt.
  exit /b 1
)

set nuget_dir=%this_dir%.nuget

if not "%cswinrt_label%"=="" goto %cswinrt_label%

:restore
rem Ensure nuget.exe is available.
if not exist %nuget_dir% md %nuget_dir%
if not exist %nuget_dir%\nuget.exe powershell -Command "Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile %nuget_dir%\nuget.exe"
%nuget_dir%\nuget update -self

rem Get TestWinRT repo
call %this_dir%get_testwinrt.cmd

call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /p:RestorePackagesConfig=true /t:restore /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform% %this_dir%cswinrt.slnx 

:build
call :buildsolution
if errorlevel 1 exit /b !errorlevel!
call :buildruntimereference
if errorlevel 1 exit /b !errorlevel!

rem skip tests for now
goto :package

if "%cswinrt_platform%" NEQ "arm64" (
  echo Restore functional tests for %cswinrt_platform% %cswinrt_configuration%
  for %%a in (%cswinrt_functional_tests%) do (
    echo Restoring %%a

    rem Do restore separately to workaround issue where specifying TargetFramework causes nuget restore to propagate it to project references causing issues.
    call :exec %msbuild_path%msbuild.exe /t:restore %cswinrt_build_params% /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;RuntimeIdentifier=win-%cswinrt_platform%;VersionNumber=%cswinrt_version_number%;VersionString=%cswinrt_version_string%;AssemblyVersionNumber=%cswinrt_assembly_version%;GenerateTestProjection=true;BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors%;BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% /p:solutiondir=%this_dir% %this_dir%Tests\FunctionalTests\%%a\%%a.csproj
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

rem Tests are not yet enabled for ARM64 builds (not supported by Project Reunion)
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
call :exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%this_dir%..\build\scripts\Run-ObjectLifetimeInProcess.ps1" -LayoutDir "%this_dir%Tests\ObjectLifetimeTests\bin\%cswinrt_platform%\%cswinrt_configuration%\net10.0-windows10.0.26100.1\win-%cswinrt_platform%" -OutputDir "%this_dir%TestResults\ObjectLifetime"
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

:sourcegenerator2test
rem Running Source Generator 2 Unit Tests
echo Running source generator 2 tests for %cswinrt_platform% %cswinrt_configuration%
call :exec %dotnet_exe% test --verbosity normal --no-build --logger trx;LogFilePath=%~dp0sourcegenerator2test_%cswinrt_version_string%.trx %this_dir%Tests\SourceGenerator2Test\SourceGenerator2Test.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration% -- RunConfiguration.TreatNoTestsAsError=true
if ErrorLevel 1 (
  echo.
  echo ERROR: Source generator 2 unit test failed, skipping NuGet pack
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

if /I "%cswinrt_platform%"=="x64" (
  call :exec %this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\AuthoringConsumptionTest2\bin\AuthoringConsumptionTest2.exe --gtest_output=xml:%this_dir%authoringtest2_%cswinrt_version_string%.xml
  if ErrorLevel 1 (
    echo.
    rem Not skipping due to known issue.
    rem echo ERROR: Multi-component authoring test failed, skipping NuGet pack
    rem exit /b !ErrorLevel!
  )
)
if /I "%cswinrt_platform%"=="x86" (
  call :exec %this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\AuthoringConsumptionTest2\bin\AuthoringConsumptionTest2.exe --gtest_output=xml:%this_dir%authoringtest2_%cswinrt_version_string%.xml
  if ErrorLevel 1 (
    echo.
    rem Not skipping due to known issue.
    rem echo ERROR: Multi-component authoring test failed, skipping NuGet pack
    rem exit /b !ErrorLevel!
  )
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
set cswinrt_bin_dir=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\cswinrt\bin
set interop_winmd=%this_dir%WinRT.Internal\bin\%cswinrt_configuration%\net10.0-windows10.0.26100.1\WindowsRuntime.Internal.winmd
set net10_runtime=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Runtime\lib\WinRT.Runtime.dll
set net10_runtime_ref=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Runtime\ref\WinRT.Runtime.dll
set net10_runtime_ref_xml=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Runtime\ref\WinRT.Runtime.xml
set source_generator_roslyn4120=%this_dir%Authoring\WinRT.SourceGenerator.Roslyn4120\bin\%cswinrt_configuration%\netstandard2.0\WinRT.SourceGenerator.dll
set source_generator=%this_dir%Authoring\WinRT.SourceGenerator2\bin\%cswinrt_configuration%\net10.0\WinRT.SourceGenerator.dll
set winrt_host_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll
set winrt_host_resource_%cswinrt_platform%=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\WinRT.Host.dll.mui
set winrt_shim=%this_dir%Authoring\WinRT.Host.Shim\bin\%cswinrt_configuration%\net10.0\WinRT.Host.Shim.dll
set cswinrtinteropgen_%cswinrt_platform%=%this_dir%WinRT.Interop.Generator\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\cswinrtinteropgen.exe
set cswinrtimplgen_%cswinrt_platform%=%this_dir%WinRT.Impl.Generator\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\cswinrtimplgen.exe
set cswinrtprojectiongen_%cswinrt_platform%=%this_dir%WinRT.Projection.Generator\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\cswinrtprojectiongen.exe
set cswinrtprojectionrefgen_%cswinrt_platform%=%this_dir%WinRT.Projection.Ref.Generator\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\cswinrtprojectionrefgen.exe
set cswinrtwinmdgen_%cswinrt_platform%=%this_dir%WinRT.WinMD.Generator\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\cswinrtwinmdgen.exe
set run_cswinrt_generator_task=%this_dir%WinRT.Generator.Tasks\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Generator.Tasks.dll

set "cswinrt_nuspec=%this_dir%..\nuget\Microsoft.Windows.CsWinRT.nuspec"
set "cswinrt_nuget_exe=%nuget_dir%\nuget.exe"
set cswinrt_repository_properties=
call :packnuget
if errorlevel 1 exit /b !errorlevel!
goto :smoketest

:packnuget
rem Now call pack
echo Creating nuget package
rem NuGet expects all nuspec substitutions in one semicolon-separated argument
set "cswinrt_package_properties=cswinrt_nuget_version=%cswinrt_version_string%;"
for %%p in (
  interop_winmd
  net10_runtime net10_runtime_ref net10_runtime_ref_xml
  source_generator winrt_shim run_cswinrt_generator_task
  winrt_host_x86 winrt_host_x64 winrt_host_arm winrt_host_arm64
  winrt_host_resource_x86 winrt_host_resource_x64
  winrt_host_resource_arm winrt_host_resource_arm64
  cswinrtinteropgen_x64 cswinrtinteropgen_arm64
  cswinrtimplgen_x64 cswinrtimplgen_arm64
  cswinrtprojectiongen_x64 cswinrtprojectiongen_arm64
  cswinrtprojectionrefgen_x64 cswinrtprojectionrefgen_arm64
  cswinrtwinmdgen_x64 cswinrtwinmdgen_arm64
) do set "cswinrt_package_properties=!cswinrt_package_properties!%%p=!%%p!;"
call :exec "%cswinrt_nuget_exe%" pack "%cswinrt_nuspec%" ^
  -BasePath "%this_dir%..\nuget" ^
  -Properties "%cswinrt_package_properties%%cswinrt_repository_properties%" ^
  -OutputDirectory "%cswinrt_bin_dir%" ^
  -NonInteractive ^
  -Verbosity Detailed ^
  -NoPackageAnalysis
exit /b %errorlevel%

:smoketest
rem Build and run the end-to-end smoke tests against the just-built NuGet package. These
rem verify that the real package (ref/lib assemblies, generators, and build targets) works
rem for a consuming app, a component author, and a projection author, fully isolated from the
rem repo build infrastructure. They run only on x64 (matching the native build tools packaged
rem for the host architecture) and can be skipped by setting 'cswinrt_run_smoke_tests=false'.
rem They run on PowerShell 7 ('pwsh'), which is what the CI tasks use as well: the tests inspect
rem the built assemblies with 'System.Reflection.Metadata', which Windows PowerShell lacks.
if /I not "%cswinrt_platform%"=="x64" goto :eof
if /I "%cswinrt_run_smoke_tests%"=="false" goto :eof

where /q pwsh.exe
if ErrorLevel 1 (
 echo.
 echo ERROR: The smoke tests require PowerShell 7 ^('pwsh'^), which was not found on PATH.
 echo Install it from https://aka.ms/powershell, or skip the smoke tests with 'cswinrt_run_smoke_tests=false'.
 exit /b 1
)

echo Running smoke tests for %cswinrt_platform% %cswinrt_configuration%
call :exec pwsh.exe -NoProfile -ExecutionPolicy Bypass -File "%this_dir%Tests\SmokeTests\run-smoke-tests.ps1" -PackageSource "%cswinrt_bin_dir%" -PackageVersion %cswinrt_version_string% -Configuration %cswinrt_configuration%
if ErrorLevel 1 (
 echo.
 echo ERROR: Smoke tests failed
 exit /b !ErrorLevel!
)

rem Preinitialization is an AOT-only check, so run it separately from the CoreCLR smoke tests.
call :exec pwsh.exe -NoProfile -ExecutionPolicy Bypass -File "%this_dir%Tests\SmokeTests\run-smoke-tests.ps1" -PackageSource "%cswinrt_bin_dir%" -PackageVersion %cswinrt_version_string% -Configuration %cswinrt_configuration% -Test Preinitialization -Runtime NativeAot
if ErrorLevel 1 (
 echo.
 echo ERROR: Preinitialization smoke test failed
 exit /b !ErrorLevel!
)
goto :eof

:productsettings
if "%cswinrt_configuration%"=="" set cswinrt_configuration=Release
if "%cswinrt_version_number%"=="" set cswinrt_version_number=0.0.0.0
if "%cswinrt_version_string%"=="" set cswinrt_version_string=0.0.0-private.0
if "%cswinrt_assembly_version%"=="" set cswinrt_assembly_version=0.0.0.0
if "%cswinrt_baseline_breaking_compat_errors%"=="" set cswinrt_baseline_breaking_compat_errors=false
if "%cswinrt_baseline_assembly_version_compat_errors%"=="" set cswinrt_baseline_assembly_version_compat_errors=false
if "%cswinrt_artifacts_dir%"=="" set "cswinrt_artifacts_dir=%this_dir%..\artifacts"
if /I not "%cswinrt_configuration%"=="Release" (
  echo ERROR: Product-only builds require the Release configuration.
  exit /b 1
)
if not exist "%cswinrt_artifacts_dir%\logs" mkdir "%cswinrt_artifacts_dir%\logs"
exit /b %errorlevel%

:product
rem Product orchestration is separate from the full developer build; compilation and packing are shared
call :productsettings
if errorlevel 1 exit /b !errorlevel!
if defined cswinrt_unit_test_runtime (
  if /I not "%cswinrt_unit_test_runtime%"=="jit" if /I not "%cswinrt_unit_test_runtime%"=="aot" (
    echo ERROR: cswinrt_unit_test_runtime must be jit or aot.
    exit /b 1
  )
)
if /I "%cswinrt_platform%"=="all" (
  rem Defer packaging until every architecture is staged, preserving the caller's skip choice
  set "cswinrt_original_skip_package=!cswinrt_skip_package!"
  set cswinrt_skip_package=true
  for %%a in (x86 x64 arm64) do (
    call "%~f0" %%a %cswinrt_configuration% %cswinrt_version_number% %cswinrt_version_string% %cswinrt_assembly_version% product
    if errorlevel 1 exit /b !errorlevel!
  )
  if /I "!cswinrt_original_skip_package!"=="true" exit /b 0
  goto :productpackage
)
call :setupvisualstudio
if errorlevel 1 exit /b !errorlevel!
set "PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer;%PATH%"
set msbuild_path=
set "cswinrt_solution=%this_dir%cswinrt.product.slnf"
set cswinrt_generate_test_projection=false
set "cswinrt_build_tools_arch=%cswinrt_platform%"
rem x86 targets still use x64 generators on the build host
if /I "%cswinrt_platform%"=="x86" set cswinrt_build_tools_arch=x64
set "cswinrt_build_params=%cswinrt_build_params% /m /nr:false /nologo /v:minimal /p:CIBuildReason=CI;ContinuousIntegrationBuild=true"
set "cswinrt_solution_build_params=/restore /p:RestorePackagesConfig=true /bl:"%cswinrt_artifacts_dir%\logs\product.binlog""
call :buildsolution
if errorlevel 1 exit /b !errorlevel!
rem Unit-test builds retain the implementation runtime and do not stage a NuGet payload
if defined cswinrt_unit_test_runtime goto :buildunittest
rem Architecture-neutral outputs are produced once, on the x64 leg
if /I "%cswinrt_platform%"=="x64" (
  if /I "%cswinrt_generate_sdk_projection%"=="true" (
    for %%v in (10.0.17763.1000 10.0.18362.2007 10.0.19041.2 10.0.22000.197 10.0.22621.3233 10.0.26100.7705) do (
      call :buildsdkprojection %%v
      if errorlevel 1 exit /b !errorlevel!
    )
  )
  rem Isolate the reference output from the implementation used by projections and tests
  set "cswinrt_reference_output=%cswinrt_artifacts_dir%\runtime-reference\%cswinrt_configuration%\net10.0\"
  set "cswinrt_reference_build_params=/p:BaseOutputPath="%cswinrt_artifacts_dir%\runtime-reference\\" /bl:"%cswinrt_artifacts_dir%\logs\runtime-reference.binlog""
  call :buildruntimereference
  if errorlevel 1 exit /b !errorlevel!
)
call :stageproduct
if errorlevel 1 exit /b !errorlevel!
if /I "%cswinrt_skip_package%"=="true" exit /b 0
goto :productpackage

:productpackage
rem Packaging consumes merged artifacts and does not need the compiler environment
call :productsettings
if errorlevel 1 exit /b !errorlevel!
set "cswinrt_bin_dir=%cswinrt_artifacts_dir%\packages"
set cswinrt_nuget_exe=nuget.exe
set "cswinrt_nuspec=%this_dir%..\nuget\Microsoft.Windows.CsWinRT.nuspec"
set "interop_winmd=%cswinrt_artifacts_dir%\product\x64\native\WindowsRuntime.Internal.winmd"
set "net10_runtime=%cswinrt_artifacts_dir%\product\x64\WinRT.Runtime.dll"
set "net10_runtime_ref=%cswinrt_artifacts_dir%\product\x64\ref\WinRT.Runtime.dll"
set "net10_runtime_ref_xml=%cswinrt_artifacts_dir%\product\x64\ref\WinRT.Runtime.xml"
set "source_generator=%cswinrt_artifacts_dir%\product\x64\WinRT.SourceGenerator.dll"
set "winrt_shim=%cswinrt_artifacts_dir%\product\x64\WinRT.Host.Shim.dll"
set "run_cswinrt_generator_task=%cswinrt_artifacts_dir%\product\x64\WinRT.Generator.Tasks.dll"
for %%a in (x86 x64 arm64) do (
  set "winrt_host_%%a=%cswinrt_artifacts_dir%\product\%%a\native\WinRT.Host.dll"
  set "winrt_host_resource_%%a=%cswinrt_artifacts_dir%\product\%%a\native\WinRT.Host.dll.mui"
)
for %%a in (x64 arm64) do (
  for %%t in (cswinrtinteropgen cswinrtimplgen cswinrtprojectiongen cswinrtprojectionrefgen cswinrtwinmdgen) do (
    set "%%t_%%a=%cswinrt_artifacts_dir%\product\%%a\tools\%%t.exe"
  )
)
set "cswinrt_repository_properties=branch=%GITHUB_REF_NAME%;commit=%GITHUB_SHA%;"
if /I "%cswinrt_generate_sdk_projection%"=="true" (
  rem Add both projections for each SDK to a temporary nuspec, leaving the checked-in file untouched
  powershell -NoProfile -Command ^
    "$ErrorActionPreference='Stop';" ^
    "$n=[xml](Get-Content -LiteralPath $env:cswinrt_nuspec -Raw);" ^
    "foreach($sdk in 17763,18362,19041,22000,22621,26100) {" ^
    "  foreach($assembly in 'WinRT.Sdk.Projection','WinRT.Sdk.Xaml.Projection') {" ^
    "    $source=Join-Path $env:cswinrt_artifacts_dir ('product\x64\impl\'+$sdk+'\'+$assembly+'.dll');" ^
    "    if(-not(Test-Path -LiteralPath $source)) { throw ('Missing SDK projection: '+$source) };" ^
    "    $file=$n.CreateElement('file',$n.DocumentElement.NamespaceURI);" ^
    "    $file.SetAttribute('src',$source);" ^
    "    $file.SetAttribute('target',('impl\net10\'+$sdk));" ^
    "    [void]$n.package.files.AppendChild($file);" ^
    "  }" ^
    "};" ^
    "$n.Save((Join-Path $env:cswinrt_artifacts_dir 'Microsoft.Windows.CsWinRT.nuspec'));"
  if errorlevel 1 exit /b !errorlevel!
  set "cswinrt_nuspec=%cswinrt_artifacts_dir%\Microsoft.Windows.CsWinRT.nuspec"
)
call :packnuget
exit /b %errorlevel%

:buildsolution
echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% ^
  /p:platform=%cswinrt_platform% ^
  /p:configuration=%cswinrt_configuration% ^
  /p:VersionNumber=%cswinrt_version_number% ^
  /p:VersionString=%cswinrt_version_string% ^
  /p:AssemblyVersionNumber=%cswinrt_assembly_version% ^
  /p:GenerateTestProjection=%cswinrt_generate_test_projection% ^
  /p:BaselineAllAPICompatError=%cswinrt_baseline_breaking_compat_errors% ^
  /p:BaselineAllMatchingRefApiCompatError=%cswinrt_baseline_assembly_version_compat_errors% ^
  /p:PublishBuildTool=true ^
  /p:BuildToolArch=%cswinrt_build_tools_arch% ^
  "%cswinrt_solution%" %cswinrt_solution_build_params%
exit /b %errorlevel%

:buildruntimereference
rem Stage the implementation before recompiling with implementation-only APIs stripped out
set "winrt_runtime_bin=%this_dir%WinRT.Runtime2\bin\%cswinrt_configuration%\net10.0\"
set "winrt_runtime_staging=%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Runtime\"
if "%cswinrt_reference_output%"=="" set "cswinrt_reference_output=%winrt_runtime_bin%"
echo Staging WinRT.Runtime implementation assembly for %cswinrt_platform% %cswinrt_configuration%
call :copyproduct "%winrt_runtime_bin%WinRT.Runtime.dll" "%winrt_runtime_staging%lib"
if errorlevel 1 exit /b !errorlevel!
echo Building WinRT.Runtime reference assembly for %cswinrt_platform% %cswinrt_configuration%
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /restore ^
  /p:configuration=%cswinrt_configuration% ^
  /p:VersionNumber=%cswinrt_version_number% ^
  /p:VersionString=%cswinrt_version_string% ^
  /p:AssemblyVersionNumber=%cswinrt_assembly_version% ^
  /p:CsWinRTBuildReferenceAssembly=true ^
  "%this_dir%WinRT.Runtime2\WinRT.Runtime.csproj" %cswinrt_reference_build_params%
if errorlevel 1 exit /b !errorlevel!
echo Staging WinRT.Runtime reference assembly for %cswinrt_platform% %cswinrt_configuration%
call :copyproduct "%this_dir%WinRT.Runtime2\obj\%cswinrt_configuration%\net10.0\ref\WinRT.Runtime.dll" "%winrt_runtime_staging%ref"
if errorlevel 1 exit /b !errorlevel!
call :copyproduct "%cswinrt_reference_output%WinRT.Runtime.xml" "%winrt_runtime_staging%ref"
exit /b %errorlevel%

:setupvisualstudio
set cswinrt_vs_dir=
"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" ^
  -latest -prerelease -products * ^
  -version "[18.0,19.0)" ^
  -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 ^
  -property installationPath >"%cswinrt_artifacts_dir%\logs\visual-studio.txt"
if errorlevel 1 exit /b %errorlevel%
set /p cswinrt_vs_dir=<"%cswinrt_artifacts_dir%\logs\visual-studio.txt"
if not defined cswinrt_vs_dir (
  echo ERROR: Visual Studio 2026 with the C++ build tools is required.
  exit /b 1
)
call "%cswinrt_vs_dir%\Common7\Tools\VsDevCmd.bat" -no_logo -host_arch=amd64 -arch=amd64
exit /b %errorlevel%

:buildsdkprojection
rem The Contracts package version identifies the SDK build; generate base before XAML, which references it
for /f "tokens=3 delims=." %%s in ("%~1") do set "cswinrt_sdk_build=%%s"
for %%x in (false true) do (
  call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% /restore ^
    /p:Configuration=%cswinrt_configuration% ^
    /p:VersionNumber=%cswinrt_version_number% ^
    /p:VersionString=%cswinrt_version_string% ^
    /p:AssemblyVersionNumber=%cswinrt_assembly_version% ^
    /p:BuildToolArch=x64 ^
    /p:WindowsSdkBuild=%cswinrt_sdk_build% ^
    /p:SdkPackageVersion=%~1 ^
    /p:WindowsSdkXaml=%%x ^
    "%this_dir%WinRT.Sdk.Projection\WinRT.Sdk.Projection.csproj" ^
    /bl:"%cswinrt_artifacts_dir%\logs\sdk-%cswinrt_sdk_build%-xaml-%%x.binlog"
  if errorlevel 1 exit /b !errorlevel!
)
for %%a in (WinRT.Sdk.Projection WinRT.Sdk.Xaml.Projection) do (
  call :copyproduct "%this_dir%WinRT.Sdk.Projection\bin\%cswinrt_configuration%\%cswinrt_sdk_build%\%%a.dll" "%cswinrt_artifacts_dir%\product\x64\impl\%cswinrt_sdk_build%"
  if errorlevel 1 exit /b !errorlevel!
)
exit /b 0

:stageproduct
set "cswinrt_stage=%cswinrt_artifacts_dir%\product\%cswinrt_platform%"
for %%f in (WinRT.Host.dll WinRT.Host.dll.mui) do (
  call :copyproduct "%this_dir%_build\%cswinrt_platform%\%cswinrt_configuration%\WinRT.Host\bin\%%f" "%cswinrt_stage%\native"
  if errorlevel 1 exit /b !errorlevel!
)
rem The package ships generators for x64 and ARM64 hosts, not x86
if /I not "%cswinrt_platform%"=="x86" (
  for %%t in (
    "WinRT.Interop.Generator,cswinrtinteropgen"
    "WinRT.Impl.Generator,cswinrtimplgen"
    "WinRT.Projection.Generator,cswinrtprojectiongen"
    "WinRT.Projection.Ref.Generator,cswinrtprojectionrefgen"
    "WinRT.WinMD.Generator,cswinrtwinmdgen"
  ) do (
    for /f "tokens=1,2 delims=," %%p in ("%%~t") do (
      call :copyproduct "%this_dir%%%p\bin\%cswinrt_configuration%\net10.0\win-%cswinrt_platform%\publish\%%q.exe" "%cswinrt_stage%\tools"
      if errorlevel 1 exit /b !errorlevel!
    )
  )
)
if /I "%cswinrt_platform%"=="x64" (
  call :copyproduct "%winrt_runtime_staging%lib\WinRT.Runtime.dll" "%cswinrt_stage%"
  if errorlevel 1 exit /b !errorlevel!
  for %%f in (WinRT.Runtime.dll WinRT.Runtime.xml) do (
    call :copyproduct "%winrt_runtime_staging%ref\%%f" "%cswinrt_stage%\ref"
    if errorlevel 1 exit /b !errorlevel!
  )
  call :copyproduct "%this_dir%Authoring\WinRT.SourceGenerator2\bin\%cswinrt_configuration%\net10.0\WinRT.SourceGenerator.dll" "%cswinrt_stage%"
  if errorlevel 1 exit /b !errorlevel!
  call :copyproduct "%this_dir%Authoring\WinRT.Host.Shim\bin\%cswinrt_configuration%\net10.0\WinRT.Host.Shim.dll" "%cswinrt_stage%"
  if errorlevel 1 exit /b !errorlevel!
  call :copyproduct "%this_dir%WinRT.Generator.Tasks\bin\%cswinrt_configuration%\netstandard2.0\WinRT.Generator.Tasks.dll" "%cswinrt_stage%"
  if errorlevel 1 exit /b !errorlevel!
  call :copyproduct "%this_dir%WinRT.Internal\bin\%cswinrt_configuration%\net10.0-windows10.0.26100.1\WindowsRuntime.Internal.winmd" "%cswinrt_stage%\native"
  if errorlevel 1 exit /b !errorlevel!
)
exit /b 0

:copyproduct
if not exist "%~1" (
  echo ERROR: Missing product output: %~1
  exit /b 1
)
if not exist "%~2" mkdir "%~2"
copy /y "%~1" "%~2\" >nul
exit /b %errorlevel%

:buildunittest
rem Restore TestWinRT's native NuGet dependencies into its solution-local packages directory
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% ^
  /t:Restore ^
  /p:RestorePackagesConfig=true ^
  /p:Platform=%cswinrt_platform% ^
  /p:Configuration=%cswinrt_configuration% ^
  "%this_dir%TestWinRT\Test.sln" ^
  /bl:"%cswinrt_artifacts_dir%\logs\testwinrt-restore.binlog"
if errorlevel 1 exit /b !errorlevel!
set "cswinrt_test_params=/p:Platform=%cswinrt_platform%"
set "cswinrt_test_params=%cswinrt_test_params% /p:Configuration=%cswinrt_configuration%"
set "cswinrt_test_params=%cswinrt_test_params% /p:RuntimeIdentifier=win-%cswinrt_platform%"
set "cswinrt_test_params=%cswinrt_test_params% /p:BuildToolArch=%cswinrt_build_tools_arch%"
set "cswinrt_test_params=%cswinrt_test_params% /p:GenerateTestProjection=true"
set "cswinrt_test_params=%cswinrt_test_params% /p:VersionNumber=%cswinrt_version_number%"
set "cswinrt_test_params=%cswinrt_test_params% /p:VersionString=%cswinrt_version_string%"
set "cswinrt_test_params=%cswinrt_test_params% /p:AssemblyVersionNumber=%cswinrt_assembly_version%"
set "cswinrt_test_params=%cswinrt_test_params% /p:SolutionDir="%this_dir%\""
rem Restore without TargetFramework so it does not propagate to the netstandard2.0 task project.
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% %cswinrt_test_params% ^
  /t:Restore ^
  /p:RestorePackagesConfig=true ^
  "%this_dir%Tests\UnitTest\UnitTest.csproj" ^
  /bl:"%cswinrt_artifacts_dir%\logs\unit-test-restore.binlog"
if errorlevel 1 exit /b !errorlevel!
set cswinrt_test_target=/t:Build
if /I "%cswinrt_unit_test_runtime%"=="aot" set cswinrt_test_target=/t:Publish /p:TargetFramework=net10.0
call :exec %msbuild_path%msbuild.exe %cswinrt_build_params% %cswinrt_test_params% ^
  %cswinrt_test_target% ^
  "%this_dir%Tests\UnitTest\UnitTest.csproj" ^
  /bl:"%cswinrt_artifacts_dir%\logs\unit-test-%cswinrt_unit_test_runtime%.binlog"
exit /b %errorlevel%

:exec
if /i "%cswinrt_echo%" == "only" (
echo Command Line:
echo %*
echo.
) else (
%*
)

goto :eof
