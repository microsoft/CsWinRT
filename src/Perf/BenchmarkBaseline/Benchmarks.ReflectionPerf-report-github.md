``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.17763.2458 (1809/October2018Update/Redstone5), VM=Hyper-V
Intel Xeon CPU E5-2673 v4 2.30GHz, 1 CPU, 2 logical and 2 physical cores
.NET SDK=6.0.101
  [Host]     : .NET Core 2.1.30 (CoreCLR 4.6.30411.01, CoreFX 4.6.30411.02), X64 RyuJIT
  Job-LNYQMT : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT

Platform=X64  Runtime=.NET 5.0  Arguments=/p:platform=x64,/p:IsDotnetBuild=true  
Toolchain=netcoreapp5.0  

```
|                                    Method |            Mean |         Error |        StdDev |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------------------------------------ |----------------:|--------------:|--------------:|-------:|-------:|-------:|----------:|
|       ExecuteMarshalingForNewKeyValuePair |    21,003.47 ns |    415.573 ns |    444.658 ns | 0.0610 | 0.0305 |      - |   1,780 B |
|              ExecuteMarshalingForNewArray |     5,664.70 ns |    110.265 ns |    122.560 ns | 0.0153 | 0.0076 |      - |     614 B |
|           ExecuteMarshalingForNewNullable |     4,091.17 ns |     80.949 ns |    220.227 ns | 0.0076 |      - |      - |     332 B |
|  ExecuteMarshalingForExistingKeyvaluePair |       293.50 ns |      4.507 ns |      4.216 ns |      - |      - |      - |         - |
|         ExecuteMarshalingForExistingArray |       290.04 ns |      1.777 ns |      1.484 ns |      - |      - |      - |         - |
|      ExecuteMarshalingForExistingNullable |       277.94 ns |      5.576 ns |      6.638 ns |      - |      - |      - |         - |
|                ExecuteMarshalingForString |        37.57 ns |      0.509 ns |      0.451 ns |      - |      - |      - |         - |
|          ExecuteMarshalingForCustomObject |     3,776.91 ns |     74.047 ns |     88.147 ns | 0.0114 | 0.0038 |      - |     304 B |
|              ExecuteMarshalingForDelegate |     3,273.40 ns |     33.174 ns |     27.701 ns | 0.0038 |      - |      - |     192 B |
|                            IntEventSource |    38,727.84 ns |    432.396 ns |    361.070 ns | 0.0610 |      - |      - |   2,073 B |
|                  ExistingDictionaryLookup |   128,791.62 ns |  1,678.273 ns |  1,569.858 ns | 0.7324 |      - |      - |  22,400 B |
|            ExistingDictionaryLookupCached | 1,251,361.03 ns | 23,367.016 ns | 21,857.522 ns | 7.8125 |      - |      - | 224,003 B |
|                 ExistingDictionaryLookup2 |     1,245.38 ns |     14.283 ns |     12.661 ns | 0.0076 |      - |      - |     224 B |
|                 ExistingDictionaryLookup3 |     1,786.09 ns |     33.499 ns |     35.844 ns | 0.0076 |      - |      - |     224 B |
|                            GetNullableInt |     3,869.86 ns |     75.492 ns |     70.615 ns | 0.0076 | 0.0038 | 0.0038 |     204 B |
|                            SetNullableInt |     3,460.40 ns |     66.314 ns |     65.129 ns | 0.0076 |      - |      - |     224 B |
|                 GetNullableBittableStruct |     7,534.79 ns |     96.753 ns |     85.769 ns | 0.0305 | 0.0153 |      - |     977 B |
|                 SetNullableBittableStruct |     4,978.66 ns |     91.828 ns |     85.896 ns | 0.0229 |      - |      - |     624 B |
|                       GetNullableTimeSpan |     4,277.63 ns |     83.683 ns |    144.350 ns | 0.0076 | 0.0038 |      - |     238 B |
|                       SetNullableTimeSpan |     3,610.86 ns |     72.211 ns |     64.013 ns | 0.0076 |      - |      - |     224 B |
|              GetNullableNonBittableStruct |     8,189.02 ns |    162.982 ns |    384.167 ns | 0.0305 | 0.0153 |      - |   1,038 B |
|              SetNullableNonBittableStruct |     5,369.13 ns |    105.815 ns |    108.665 ns | 0.0229 |      - |      - |     664 B |
|                       SetNullableDelegate |     3,254.21 ns |     62.461 ns |     58.426 ns | 0.0076 |      - |      - |     208 B |
|                    SetNullableIntDelegate |     1,926.40 ns |     36.056 ns |     37.027 ns | 0.0076 | 0.0038 |      - |     248 B |
|                    GetNullableIntDelegate |     7,398.12 ns |    141.490 ns |    125.427 ns | 0.0305 | 0.0153 | 0.0076 |     646 B |
|                         GetNewIntDelegate |     2,753.28 ns |     53.246 ns |     49.806 ns | 0.0076 | 0.0038 |      - |     208 B |
|                    GetExistingIntDelegate |       218.66 ns |      2.144 ns |      2.005 ns |      - |      - |      - |         - |
|                      CreateAndIterateList |    16,753.73 ns |    241.076 ns |    201.310 ns | 0.0916 | 0.0305 |      - |   2,696 B |
|                                    GetUri |     1,579.90 ns |     12.653 ns |     11.835 ns | 0.0038 |      - |      - |     120 B |
|                                    SetUri |     1,532.37 ns |     16.530 ns |     12.906 ns | 0.0057 |      - |      - |     152 B |
|                            GetExistingUri |       398.85 ns |      5.775 ns |      5.402 ns | 0.0043 |      - |      - |     120 B |
|                              GetWinRTType |       202.15 ns |      1.395 ns |      1.237 ns | 0.0045 |      - |      - |     120 B |
|                              SetWinRTType |       247.73 ns |      5.011 ns |      4.921 ns |      - |      - |      - |         - |
|                          SetPrimitiveType |       206.68 ns |      2.491 ns |      2.208 ns |      - |      - |      - |         - |
|                           SetNonWinRTType |       252.72 ns |      3.289 ns |      2.916 ns |      - |      - |      - |         - |
|                      GetExistingWinRTType |       195.94 ns |      1.902 ns |      1.779 ns | 0.0045 |      - |      - |     120 B |
|           GetWeakReferenceOfManagedObject |     3,462.65 ns |     47.934 ns |     42.492 ns | 0.0038 |      - |      - |     168 B |
| GetAndResolveWeakReferenceOfManagedObject |     3,764.02 ns |     44.410 ns |     39.368 ns | 0.0038 |      - |      - |     168 B |
|            GetWeakReferenceOfNativeObject |       496.09 ns |      4.769 ns |      4.461 ns | 0.0005 |      - |      - |      24 B |
