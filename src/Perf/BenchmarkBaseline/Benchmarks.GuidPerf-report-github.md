``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.17763.2458 (1809/October2018Update/Redstone5), VM=Hyper-V
Intel Xeon CPU E5-2673 v4 2.30GHz, 1 CPU, 2 logical and 2 physical cores
.NET SDK=6.0.101
  [Host]     : .NET Core 2.1.30 (CoreCLR 4.6.30411.01, CoreFX 4.6.30411.02), X64 RyuJIT
  Job-LNYQMT : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT

Platform=X64  Runtime=.NET 5.0  Arguments=/p:platform=x64,/p:IsDotnetBuild=true  
Toolchain=netcoreapp5.0  

```
|                            Method |       Mean |     Error |    StdDev |  Gen 0 | Allocated |
|---------------------------------- |-----------:|----------:|----------:|-------:|----------:|
|                      GetClassGuid | 2,036.2 ns |  24.71 ns |  23.12 ns |      - |      32 B |
|                   GetDelegateGuid |   539.1 ns |   6.16 ns |   5.76 ns | 0.0010 |      32 B |
|                    CreateListGuid | 4,666.8 ns |  91.45 ns | 115.65 ns | 0.0687 |   1,960 B |
| CreateDictionaryWithStringKeyGuid | 4,783.2 ns |  92.15 ns | 126.14 ns | 0.0839 |   2,232 B |
|   CreateDictionaryWithBoolKeyGuid | 4,719.4 ns |  91.10 ns |  89.47 ns | 0.0763 |   2,192 B |
|        CreateReadOnlyEnumListGuid | 8,192.4 ns | 124.80 ns | 110.63 ns | 0.0610 |   1,784 B |
|    CreateReadOnlyFlagEnumListGuid | 4,805.3 ns |  95.44 ns |  89.28 ns | 0.0534 |   1,496 B |
|      CreateReadOnlyStructListGuid | 4,132.2 ns |  80.69 ns | 102.04 ns | 0.0534 |   1,568 B |
|   CreateReadOnlyInterfaceListGuid | 3,705.8 ns |  47.92 ns |  44.82 ns | 0.0496 |   1,392 B |
|       CreateReadOnlyClassListGuid | 4,346.2 ns |  86.17 ns | 115.04 ns | 0.0687 |   1,864 B |
|    CreateReadOnlyDelegateListGuid | 3,698.2 ns |  53.09 ns |  47.06 ns | 0.0534 |   1,472 B |
