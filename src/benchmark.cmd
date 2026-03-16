@echo off
setlocal

set this_dir=%~dp0

:: ================================================================
:: Benchmark runner for CsWinRT
::
:: Usage:
::   benchmark.cmd [mode] [baseline_dir]
::
:: Modes:
::   compare-versions  - Compare CsWinRT 2.x (baseline) vs 3.x (default)
::   regression        - Compare known-good 3.x (baseline) vs current 3.x
::
:: Environment variables (optional overrides):
::   BENCHMARK_CSWINRT3_VERSION         - CsWinRT 3.x version (compare-versions)
::   BENCHMARK_CSWINRT2_VERSION         - CsWinRT 2.x version (compare-versions)
::   BENCHMARK_CSWINRT_CURRENT_VERSION  - Currently-built CsWinRT version (regression)
::   BENCHMARK_CSWINRT_BASELINE_VERSION - Known-good CsWinRT version (regression)
::   BENCHMARK_WINDOWS_SDK_VERSION      - Windows SDK package version
:: ================================================================

set MODE=%1
if "%MODE%"=="" set MODE=compare-versions

:: Build BenchmarkComponent if not already built
if not exist "%this_dir%_build\x64\Release\BenchmarkComponent\bin\BenchmarkComponent\BenchmarkComponent.dll" (
    echo Building BenchmarkComponent...
    nuget restore TestWinRT\Test.sln
    msbuild -t:restore -t:BenchmarkComponent /p:RestorePackagesConfig=true /p:platform=x64 /p:configuration=release /p:solutiondir=%this_dir% %this_dir%cswinrt.slnx
)

:: Copy BenchmarkComponent artifacts to BenchmarkProjection
echo Copying BenchmarkComponent artifacts...
copy /Y "%this_dir%_build\x64\Release\BenchmarkComponent\bin\BenchmarkComponent\BenchmarkComponent.dll" "%this_dir%Benchmarks\BenchmarkProjection\" >nul
copy /Y "%this_dir%_build\x64\Release\BenchmarkComponent\bin\BenchmarkComponent\BenchmarkComponent.winmd" "%this_dir%Benchmarks\BenchmarkProjection\" >nul

:: Run benchmarks
echo Running benchmarks in %MODE% mode...
dotnet run --project "%this_dir%Benchmarks\Benchmarks\Benchmarks.csproj" -c Release --framework net10.0-windows10.0.26100.1 -- --mode %MODE% --filter *

:: Run ResultsComparer
set baselinedir=%2
if "%baselinedir%"=="" set baselinedir=Perf\BenchmarkBaseline

if exist "%this_dir%Perf\ResultsComparer\ResultsComparer.sln" (
    echo Running ResultsComparer...
    nuget restore "%this_dir%Perf\ResultsComparer\ResultsComparer.sln"
    msbuild "%this_dir%Perf\ResultsComparer\ResultsComparer.sln" -t:restore -t:build /p:platform="Any CPU" /p:configuration=release
    "%this_dir%Perf\ResultsComparer\bin\Release\net8.0\ResultsComparer.exe" --base %baselinedir% --diff BenchmarkDotNet.Artifacts\results\ --threshold 5%% --xml BenchmarkDotNet.Artifacts\results\comparison.xml
)