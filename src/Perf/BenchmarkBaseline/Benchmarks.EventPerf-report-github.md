``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.17763.2458 (1809/October2018Update/Redstone5), VM=Hyper-V
Intel Xeon CPU E5-2673 v4 2.30GHz, 1 CPU, 2 logical and 2 physical cores
.NET SDK=6.0.101
  [Host]     : .NET Core 2.1.30 (CoreCLR 4.6.30411.01, CoreFX 4.6.30411.02), X64 RyuJIT
  Job-LNYQMT : .NET 5.0.13 (5.0.1321.56516), X64 RyuJIT

Platform=X64  Runtime=.NET 5.0  Arguments=/p:platform=x64,/p:IsDotnetBuild=true  
Toolchain=netcoreapp5.0  

```
|                                         Method |        Mean |     Error |      StdDev |      Median |  Gen 0 |  Gen 1 | Allocated |
|----------------------------------------------- |------------:|----------:|------------:|------------:|-------:|-------:|----------:|
|                               IntEventOverhead | 12,071.7 ns | 175.66 ns |   155.72 ns | 12,089.0 ns |      - |      - |     328 B |
|                    AddIntEventToNewEventSource | 27,510.0 ns | 534.30 ns |   675.71 ns | 27,530.6 ns | 0.0610 |      - |   1,900 B |
|              AddMultipleEventsToNewEventSource | 38,441.6 ns | 710.65 ns |   664.74 ns | 38,497.0 ns | 0.0610 |      - |   3,054 B |
|           AddAndInvokeIntEventOnNewEventSource | 30,660.6 ns | 593.43 ns |   869.84 ns | 30,256.4 ns | 0.0610 |      - |   1,914 B |
| AddAndInvokeMultipleIntEventsToSameEventSource | 29,986.9 ns | 591.90 ns |   633.32 ns | 30,079.4 ns | 0.0610 |      - |   2,429 B |
|                                 InvokeIntEvent |    401.9 ns |   6.77 ns |     6.95 ns |    401.9 ns | 0.0005 |      - |      24 B |
|                  InvokeIntEventWithSenderCheck |    411.9 ns |   6.09 ns |     5.40 ns |    410.6 ns | 0.0005 |      - |      24 B |
|           AddAndRemoveIntEventOnNewEventSource | 29,435.9 ns | 587.51 ns |   784.31 ns | 29,202.4 ns | 0.0610 | 0.0305 |   1,998 B |
|                         NativeIntEventOverhead |  5,776.4 ns |  89.41 ns |    91.81 ns |  5,742.7 ns | 0.0076 |      - |     272 B |
|              AddNativeIntEventToNewEventSource | 13,203.4 ns | 257.25 ns |   285.93 ns | 13,208.1 ns | 0.0305 | 0.0153 |     880 B |
|        AddMultipleNativeEventsToNewEventSource | 19,166.6 ns | 413.49 ns | 1,219.20 ns | 19,594.0 ns | 0.0458 | 0.0153 |   1,468 B |
|     AddAndInvokeNativeIntEventOnNewEventSource | 15,532.8 ns | 288.20 ns |   255.48 ns | 15,513.5 ns | 0.0458 | 0.0153 |   1,256 B |
|                           InvokeNativeIntEvent |  1,432.3 ns |  24.42 ns |    22.84 ns |  1,435.5 ns | 0.0134 |      - |     376 B |
|     AddAndRemoveNativeIntEventOnNewEventSource | 13,053.9 ns | 259.23 ns |   288.13 ns | 13,008.2 ns | 0.0305 | 0.0153 |     880 B |
