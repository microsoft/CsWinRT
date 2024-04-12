set DOTNET_ROOT=%LocalAppData%\Microsoft\dotnet
set DOTNET_ROOT(x86)=%LocalAppData%\Microsoft\dotnet\x86
set path=%DOTNET_ROOT%;%DOTNET_ROOT(x86)%;%path%

nuget restore TestWinRT\Test.sln
nuget restore cswinrt.sln
msbuild Benchmarks\Benchmarks.csproj -t:restore -t:build /p:platform=x64 /p:configuration=release /p:solutiondir=%~dp0 /p:IsDotnetBuild=false
dotnet %~dp0Benchmarks\bin\x64\Release\net6.0\Benchmarks.dll -filter * --runtimes net6.0
nuget restore Perf\ResultsComparer\ResultsComparer.sln
msbuild Perf\ResultsComparer\ResultsComparer.sln -t:restore -t:build /p:platform="Any CPU" /p:configuration=release
set baselinedir=%1
IF "%baselinedir%" == "" set baselinedir="Perf\BenchmarkBaseline"
Perf\ResultsComparer\bin\Release\net6.0\ResultsComparer.exe --base %baselinedir% --diff BenchmarkDotNet.Artifacts\results\ --threshold 5%% --xml BenchmarkDotNet.Artifacts\results\comparison.xml