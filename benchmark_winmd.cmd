msbuild Benchmarks\Benchmarks.csproj -t:restore -t:clean;rebuild /p:BenchmarkWinmdSupport=true /p:platform=x64 /p:configuration=release /p:TargetFramework=netcoreapp3.1
S:\CsWinRT\Benchmarks\bin\x64\Release\netcoreapp3.1\Benchmarks.exe -filter *
rem Clean project to prevent mismatch scenarios with the typical benchmark.cmd scenario.
msbuild Benchmarks\Benchmarks.csproj -t:restore -t:clean /p:BenchmarkWinmdSupport=true /p:platform=x64 /p:configuration=release /p:TargetFramework=netcoreapp3.1 >nul