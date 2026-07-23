@echo off
rem Builds WinRT.Generator.Tasks (the custom MSBuild task DLL) via cswinrt.buildtasks.slnf.
rem Run this once before opening cswinrt.dev.slnf in Visual Studio, and again after editing
rem the task sources. The dev filter excludes this project so VS never rebuilds (and locks) the DLL.
rem
rem Usage: build-tasks.cmd [Configuration] [Platform]   (defaults: Debug x64)

setlocal
set _config=%1
set _platform=%2
if "%_config%"=="" set _config=Debug
if "%_platform%"=="" set _platform=x64

dotnet build "%~dp0cswinrt.buildtasks.slnf" -p:Configuration=%_config% -p:Platform=%_platform% --nologo
exit /b %ERRORLEVEL%
