@echo off

setlocal ENABLEDELAYEDEXPANSION

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
 set cswinrt_configuration=Debug
)

echo Building cswinrt for %cswinrt_platform% %cswinrt_configuration%
msbuild cswinrt.sln /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%;BuildTestProjection=true

rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
echo Running cswinrt unit tests for %cswinrt_platform% %cswinrt_configuration%
if %cswinrt_configuration% == x86 (
set dotnet_exe="%ProgramFiles(x86)%\dotnet\dotnet.exe"
) else (
set dotnet_exe="%ProgramFiles%\dotnet\dotnet.exe"
)
%dotnet_exe% test --no-build --logger xunit;LogFilePath=test_cswinrt_unittest.xml unittest/UnitTest.csproj /nologo /m /p:platform=%cswinrt_platform%;configuration=%cswinrt_configuration%