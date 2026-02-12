nuget restore TestWinRT\Test.sln
msbuild -t:restore -t:Benchmarks /p:platform=x64 /p:configuration=release /p:solutiondir=%~dp0 /p:IsDotnetBuild=false %this_dir%cswinrt.slnx
dotnet %~dp0Benchmarks\bin\x64\Release\net10.0\Benchmarks.dll -filter * --runtimes net10.0
nuget restore Perf\ResultsComparer\ResultsComparer.sln
msbuild Perf\ResultsComparer\ResultsComparer.sln -t:restore -t:build /p:platform="Any CPU" /p:configuration=release
set baselinedir=%1
IF "%baselinedir%" == "" set baselinedir="Perf\BenchmarkBaseline"
Perf\ResultsComparer\bin\Release\net8.0\ResultsComparer.exe --base %baselinedir% --diff BenchmarkDotNet.Artifacts\results\ --threshold 5%% --xml BenchmarkDotNet.Artifacts\results\comparison.xml