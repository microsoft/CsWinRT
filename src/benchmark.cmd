nuget restore TestWinRT\Test.sln
nuget restore cswinrt.sln
msbuild Benchmarks\Benchmarks.csproj -t:restore -t:build /p:platform=x64 /p:configuration=release /p:solutiondir=%~dp0 /p:IsDotnetBuild=false
dotnet %~dp0Benchmarks\bin\x64\Release\netcoreapp2.0\Benchmarks.dll -filter * --runtimes netcoreapp5.0