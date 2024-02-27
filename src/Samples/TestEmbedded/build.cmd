@echo off
if /i "%echo_build%" == "on" @echo on

set this_dir=%~dp0

set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(x86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%DOTNET_ROOT(x86)%;%path%

:params
set build_platform=%1
set build_configuration=%2

if "%build_platform%"=="" set build_platform=x64
if "%build_configuration%"=="" set build_configuration=Release

set nuget_dir=%this_dir%.nuget

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
call :exec %nuget_dir%\nuget.exe restore %nuget_params% %this_dir%TestEmbedded.sln

:build
echo Building TestEmbedded for %build_platform% %build_configuration%
call :exec msbuild.exe %embed_build_params% /p:platform=%build_platform%;configuration=%build_configuration% /bl:embeddedsample.binlog %this_dir%TestEmbedded.sln 
if ErrorLevel 1 (
  echo.
  echo ERROR: TestEmbedded Build failed
  exit /b !ErrorLevel!
)
if "%only_build%"=="true" goto :eof

:test
rem Build/Run xUnit tests, generating xml output report for Azure Devops reporting, via XunitXml.TestLogger NuGet
if %build_platform%==x86 (
  set dotnet_exe="%DOTNET_ROOT(x86)%\dotnet.exe"
) else (
  set dotnet_exe="%DOTNET_ROOT%\dotnet.exe"
)
if not exist %dotnet_exe% (
  if %build_platform%==x86 (
    set dotnet_exe="%ProgramFiles(x86)%\dotnet\dotnet.exe"
  ) else (
    set dotnet_exe="%ProgramFiles%\dotnet\dotnet.exe"
  )
)

call :exec %dotnet_exe% test --verbosity normal --no-build --logger xunit;LogFilePath=%~dp0embedunittest.xml %this_dir%UnitTestEmbedded/UnitTestEmbedded.csproj /nologo /m /p:platform=%build_platform%;configuration=%build_configuration%
if ErrorLevel 1 (
  echo.
  echo ERROR: TestEmbedded unit test failed
  exit /b !ErrorLevel!
)

:exec
%*
goto :eof