``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.17763.2458 (1809/October2018Update/Redstone5), VM=Hyper-V
Intel Xeon CPU E5-2673 v4 2.30GHz, 1 CPU, 2 logical and 2 physical cores
.NET SDK=6.0.101
  [Host]     : .NET Core 2.1.30 (CoreCLR 4.6.30411.01, CoreFX 4.6.30411.02), X64 RyuJIT
  Job-LNYQMT : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT

Platform=X64  Runtime=.NET 5.0  Arguments=/p:platform=x64,/p:IsDotnetBuild=true  
Toolchain=netcoreapp5.0  

```
|                                        Method |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Allocated |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|-------:|----------:|
|                         QueryDefaultInterface |     21.40 ns |   0.468 ns |   0.438 ns |      - |      - |         - |
|                      QueryNonDefaultInterface |     20.89 ns |   0.457 ns |   0.562 ns |      - |      - |         - |
|                     QueryNonDefaultInterface2 |     21.86 ns |   0.408 ns |   0.340 ns |      - |      - |         - |
|              QueryDefaultInterfaceSetProperty |     15.85 ns |   0.305 ns |   0.313 ns |      - |      - |         - |
|           QueryNonDefaultInterfaceSetProperty |     15.18 ns |   0.251 ns |   0.235 ns |      - |      - |         - |
|                      QuerySDKDefaultInterface |     50.90 ns |   0.571 ns |   0.534 ns |      - |      - |         - |
|                   QuerySDKNonDefaultInterface |     54.07 ns |   1.071 ns |   1.146 ns |      - |      - |         - |
|                       DefaultObjectParameters |  4,231.06 ns |  63.539 ns |  56.326 ns | 0.0114 | 0.0038 |     384 B |
|                       DefaultStringParameters |    228.27 ns |   3.913 ns |   3.661 ns | 0.0010 |      - |      32 B |
|                                   DynamicCast | 15,626.22 ns | 301.379 ns | 358.770 ns | 0.0153 |      - |     448 B |
|    ConstructAndQueryDefaultInterfaceFirstCall |  3,572.03 ns |  69.306 ns |  85.114 ns | 0.0076 | 0.0038 |     264 B |
| ConstructAndQueryNonDefaultInterfaceFirstCall |  4,447.83 ns |  73.217 ns |  68.487 ns | 0.0114 | 0.0038 |     360 B |
|                            StaticPropertyCall |     52.46 ns |   0.944 ns |   0.788 ns |      - |      - |         - |
|                 QueryInterfaceOnManagedObject |    629.33 ns |  11.516 ns |  10.773 ns | 0.0029 |      - |      96 B |
|          QueryNativeInterfaceOnComposedObject |    868.41 ns |  11.527 ns |  10.782 ns | 0.0029 |      - |      96 B |
