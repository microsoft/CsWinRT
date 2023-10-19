rem Create global.json for current .NET SDK, and with allowPrerelease=true
set global_json=%this_dir%global.json
echo { > %global_json%
echo   "sdk": { >> %global_json%
echo     "version": "8.0.100-rc.2.23502.2", >> %global_json%
echo     "allowPrerelease": true >> %global_json%
echo   } >> %global_json%
echo } >> %global_json%

nuget restore TestWinRT\Test.sln
nuget restore cswinrt.sln
msbuild Benchmarks\Benchmarks.csproj -t:restore -t:build /p:platform=x64 /p:configuration=release /p:solutiondir=%~dp0 /p:IsDotnetBuild=false
dotnet %~dp0Benchmarks\bin\x64\Release\net8.0\Benchmarks.dll -filter * --runtimes net6.0 net8.0 nativeaot8.0
nuget restore Perf\ResultsComparer\ResultsComparer.sln
msbuild Perf\ResultsComparer\ResultsComparer.sln -t:restore -t:build /p:platform="Any CPU" /p:configuration=release
set baselinedir=%1
IF "%baselinedir%" == "" set baselinedir="Perf\BenchmarkBaseline"
Perf\ResultsComparer\bin\Release\net6.0\ResultsComparer.exe --base %baselinedir% --diff BenchmarkDotNet.Artifacts\results\ --threshold 5%% --xml BenchmarkDotNet.Artifacts\results\comparison.xml