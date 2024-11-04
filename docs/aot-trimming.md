# .NET trimming and AOT support in C#/WinRT

## Overview

As of CsWinRT **2.1.1** and Windows SDK projection 10.0.<sdk_ver>.**38**, generated projections are AOT compatible and various trimming issues have been addressed as part of it.  This Windows SDK projection version is included in the .NET SDK starting from the following versions: 6.0.134, 6.0.426, 8.0.109, 8.0.305 or 8.0.403. The `OptIn` mode of this support discussed in this document is added in CsWinRT **2.2.0** and is added for .NET 8 and later consumers.

Projections can choose to mark their projects as [IsAotCompatible](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#aot-compatibility-analyzers) to run the relevant .NET AOT analyzers on them.  Similarly, C# libraries using C#/WinRT authoring support with the new version can also be AOT compatible and can be published for AOT using the [PublishAOT property and running publish](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#publish-native-aot-using-the-cli).

## Source generator modes

CsWinRT makes use of a source generator as part of making generated projections and uses of it trimming and AOT compatible. The source generator is primarily used to generate vtables for classes defined by the project and for generic types used such that if they were passed across the WinRT ABI, the interfaces that they implement will be projected into WinRT. CsWinRT still has its existing reflection based approach which still works for non AOT and non trimming scenarios but also comes at a runtime cost. There are four different modes which the source generator can run in which is determined by `CsWinRTAotOptimizerEnabled`:

| Mode | Meaning |
| ----- | ------- |
| OptIn | Only generates code for attributed types. |
| Auto | Attempts to detect scenarios that implement interfaces that can be passed across the WinRT ABI boundary and generates code for them. |
| True* | If running in a project that references WinUI / UWP, then runs in `Auto`. Otherwise runs in `OptIn`. |
| False | Disables any generation of code and the analyzer. |

In the default mode of `True`, the source generator looks at the type of project to determine whether it runs in `Auto` or `OptIn` mode. For WinUI / UWP projects, it runs in `Auto` mode given they frequently pass things across the WinRT boundary. This is to avoid having to mark every class with the attribute and to avoid listing every generic type used. Eventually the plan is to have the source generator in these scenarios to run as a build task. For projects which have explictly set `CsWinRTAotWarningLevel` to `2`, it also runs in `Auto` mode given the project had explicitly opted in to warnings for scenarios involving built-in interfaces and we want to avoid the changing the current behavior for such projects. These projects can manually still set to `OptIn` if that is what they desire. For all other projects, the source generator runs in `OptIn` mode where when it sees one of the generator attributes used, it will generate code for it to make it trimming and AOT compatible.

The `OptIn` mode attributes still does work in `Auto` mode to allow to use in scenarios where the source generator didn't detect a certain scenario.

## Consuming projected types and being trimming and AOT compatible

If your app or library has non-WinRT classes that implement C#/WinRT projected interfaces, extend C#/WinRT projected classes, or implement WinRT mapped built-in .NET interfaces and are passed across the ABI to other WinRT functions, the class needs to be marked `partial` and if using `OptIn` mode also needs to be have the `WinRT.GeneratedWinRTExposedType` attribute. This enables the source generator distributed with C#/WinRT to add an attribute to the class with the WinRT vtable for it such that it can be looked up later in a way that is both trimming and AOT compatible.

To help with this, there is a `code fixer` that will produce diagnostics when such types are not marked `partial`. This is helpful in `Auto` mode but will also produce diagnostics for types in `OptIn` mode that have the `WinRT.GeneratedWinRTExposedType` attribute but aren't marked `partial`. The severity and scope of the diagnostics are determined by `CsWinRTAotWarningLevel`:

| Level | Meaning |
| ----- | ------- |
| 0 | Info diagnostics |
| 1 | Warnings for types not marked partial that implement C#/WinRT projected interfaces. |
| 2 | Warnings from level 1 plus warnings for types not marked partial that implement WinRT mapped built-in .NET interfaces. |

In `Auto` mode, it is recommended to set the `CsWinRTAotWarningLevel` to 2 and go through all the warnings and mark such types `partial` or suppress the warning if those types are not passed across the WinRT ABI. By default, if you reference the CsWinRT package, the `CsWinRTAotWarningLevel` is set to 1. If you don't have a CsWinRT package reference, then the .NET default is to set `CsWinRTAotWarningLevel` to 1 if you have marked your binary as AOT compatible either by using `IsAotCompatible` or `PublishAot`. Even though it is only by default enabled as warnings for AOT scenarios, you should manually set it to 1 or 2 if you support trimming and address them.

In `Auto` mode, the source generator also detects other scenarios such as boxing of arrays and instantiations of generic WinRT types for which it generates code that will allow the WinRT vtable for it to be looked up when passed across the WinRT ABI. In `OptIn` mode, for such types (boxed arrays, generic types) that are passed across the WinRT boundary, an assembly attribute `WinRT.GeneratedWinRTExposedExternalType`, can be placed in the project for each type to have the same code generated for it. This will also typically require `AllowUnsafeBlocks` to be enabled for your project as the generated code does make use of unsafe blocks.

In general, this also means that if your app or library has a dependency on another library that uses or returns types falling into one of those above scenarios, that library needs to have also been ran with the updated CsWinRT version for your app or library to be AOT compatible.  This is similar to the general .NET requirement where all your dependencies need to be AOT compatible for your app or library to be AOT compatible.

## ICustomPropertyProvider type support on AOT (WinUI binding)

Non source generated WinUI binding scenarios (i.e not `x:bind`) such as `DisplayMemberPath` make use of `ICustomPropertyProvider`. The implementation for this was reflection based and therby not AOT safe. CsWinRT provides an AOT safe implementation for this support. To make use of this, any classes which are provided as the source for such scenarios should be made `partial` and marked with the `WinRT.GeneratedBindableCustomProperty` attribute to make the CsWinRT source generator aware that this type is used in such scenarios and it should generate the AOT safe implementation for it. By default, with the empty constructor, the generated implementation will support all public properties. You are able to scope down the supported public properties by manually specfying the property names and indexer types in the constructor.  For example: `[GeneratedBindableCustomProperty([nameof(Name), nameof(Value)], [typeof(int)])]`. To help determine the impacted scenarios, we added an exception that gets thrown when you rely on the non AOT safe version of this feature while `PublishAot` is set.

## Known issues when publishing for AOT

There are a couple issues related to our AOT support that are known with the current release which will be addressed in future updates or be fixed in .NET:

1. Collection expressions can not be passed across the WinRT ABI boundary.

2. In WinUI apps, you might run into a race condition which triggers a hang during .NET GC needing to restart the app.  This is the result of an exception being thrown unexpectedly during GC due to an invalid handle.  This is fixed as of .NET 9 RC2.

## Known issues when publishing for JIT

1. When publishing for ARM64 with ReadyToRun (R2R) enabled while targeting .NET 6, you may see a failed to optimize error.  You can workaround this by moving your project to target .NET 8 or by using [PublishReadyToRunExclude](https://learn.microsoft.com/dotnet/core/deploying/ready-to-run#how-is-the-set-of-precompiled-assemblies-chosen) to exclude `WinRT.Runtime.dll` from R2R when building for ARM64.

## Known issues in general

