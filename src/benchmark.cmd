msbuild Benchmarks\Benchmarks.csproj -t:restore -t:build /p:platform=x64 /p:configuration=release /p:solutiondir=%~dp0 /p:IsDotnetBuild=false
rem dotnet %~dp0Benchmarks\bin\x64\Release\netcoreapp2.0\Benchmarks.dll -filter * --runtimes netcoreapp2.0 netcoreapp3.1 netcoreapp5.0
rem dotnet %~dp0Benchmarks\bin\x64\Release\netcoreapp2.0\Benchmarks.dll -p ETW -filter Benchmarks.QueryInterfacePerf.GetVector Benchmarks.QueryInterfacePerf.GetVectorInt Benchmarks.ReflectionPerf.ExecuteMarshalingForCustomObject --runtimes netcoreapp5.0
dotnet %~dp0Benchmarks\bin\x64\Release\netcoreapp2.0\Benchmarks.dll -p ETW -filter Benchmarks.ReflectionPerf.IntEventSource2* --runtimes netcoreapp5.0
