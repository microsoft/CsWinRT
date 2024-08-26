# .NET trimming and AOT support in C#/WinRT

## Overview

As of CsWinRT **2.1.1** and Windows SDK projection 10.0.<sdk_ver>.**38**, generated projections are AOT compatible and various trimming issues have been addressed as part of it.  This Windows SDK projection version is included in the .NET SDK starting from the following versions: 6.0.134, 6.0.426, 8.0.109, 8.0.305 or 8.0.402.

Until these versions are available, you can use the updated Windows SDK projection by specifying the [WindowsSdkPackageVersion property](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#windowssdkpackageversion) and by adding the `Microsoft.Windows.CsWinRT` package reference to enable the source generator used to make WinRT scenarios trimming and AOT compatible. Once you moved to one of the .NET SDK versions that come with it by default, you can remove the `WindowsSdkPackageVersion` property and also the `Microsoft.Windows.CsWinRT` package reference if you added it for the source generator.

Projections can choose to mark their projects as [IsAotCompatible](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#aot-compatibility-analyzers) to run the relevant .NET AOT analyzers on them.  Similarly, C# libraries using C#/WinRT authoring support with the new version are also AOT compatible and can be published for AOT using the [PublishAOT property and running publish](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#publish-native-aot-using-the-cli).

## Consuming projected types and being trimming and AOT compatible

If your app or library has non-WinRT classes that implement C#/WinRT projected interfaces or implement WinRT mapped built-in .NET interfaces and are passed across the ABI to other WinRT functions, the class needs to be marked `partial`.  Marking it `partial` allows the source generator distributed with C#/WinRT to add an attribute to the class which has the necessary logic to produce the WinRT vtable for it in a way that is both trimming and AOT compatible.

To help with this, there is a `code fixer` that will produce diagnostics when such types are not marked `partial`. The severity and scope of the diagnostics are determined by `CsWinRTAotWarningLevel`:

| Level | Meaning |
| ----- | ------- |
| 0 | Info diagnostics |
| 1 | Warnings for types not marked partial that implement C#/WinRT projected interfaces. |
| 2 | Warnings from level 1 plus warnings for types not marked partial that implement WinRT mapped built-in .NET interfaces. |

It is recommended to set the `CsWinRTAotWarningLevel` to 2 and go through all the warnings and mark such types `partial` or suppress the warning if those types are not passed across the WinRT ABI. By default, if you reference the CsWinRT package, the `CsWinRTAotWarningLevel` is set to 1. If you don't have a CsWinRT package reference, then the .NET default is to set `CsWinRTAotWarningLevel` to 1 if you have marked your binary as AOT compatible either by using `IsAotCompatible` or `PublishAot`. Even though it is only by default enabled as warnings for AOT scenarios, you should manually set it to 1 or 2 if you support trimming and address them.

The source generator also detects other scenarios such as boxing of arrays and instantiations of generic WinRT types for which it generates code that will allow the WinRT vtable for it to be looked up when passed across the WinRT ABI.

In general, this also means that if your app or library has a dependency on another library that uses or returns types falling into one of those above scenarios, that library needs to have also been ran with the updated CsWinRT version for your app or library to be AOT compatible.  This is similar to the general .NET requirement where all your dependencies need to be AOT compatible for your app or library to be AOT compatible.

## ICustomPropertyProvider type support on AOT (WinUI binding)

Non source generated WinUI binding scenarios (i.e not `x:bind`) such as `DisplayMemberPath` make use of `ICustomPropertyProvider`. The implementation for this was reflection based and therby not AOT safe. CsWinRT provides an AOT safe implementation for this support. To make use of this, any classes which are provided as the source for such scenarios should be made `partial` and marked with the `GeneratedBindableCustomProperty` attribute to make the CsWinRT source generator aware that this type is used in such scenarios and it should generate the AOT safe implementation for it. By default, with the empty constructor, the generated implementation will support all public properties. You are able to scope down the supported public properties by manually specfying the property names and indexer types in the constructor.  For example: `[GeneratedBindableCustomProperty([nameof(Name), nameof(Value)], [typeof(int)])]`. To help determine the impacted scenarios, we added an exception that gets thrown when you rely on the non AOT safe version of this feature while `PublishAot` is set.

## Known issues when publishing for AOT

There are a couple issues related to our AOT support that are known with the current release which will be addressed in future updates or be fixed in .NET:

1. If any built-in .NET `private` types implementing mapped WinRT interfaces are passed across the WinRT ABI, they will not be detected by the source generator and thereby not be AOT compatible.  You can workaround this by either avoiding directly passing them across the ABI by wrapping them or by registering your own WinRT vtable lookup function using `WinRT.ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup` and `WinRT.ComWrappersSupport.RegisterTypeRuntimeClassNameLookup`.  An example of this can be seen [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L57-L75) and [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L87-L90).

2. In WinUI apps, you might run into a race condition which triggers a hang during .NET GC needing to restart the app.  This is the result of an exception being thrown unexpectedly during GC due to an invalid handle.

## Known issues when publishing for JIT

1. When publishing for ARM64 with ReadyToRun (R2R) enabled while targeting .NET 6, you may see a failed to optimize error.  You can workaround this by moving your project to target .NET 8 or by using [PublishReadyToRunExclude](https://learn.microsoft.com/dotnet/core/deploying/ready-to-run#how-is-the-set-of-precompiled-assemblies-chosen) to exclude `WinRT.Runtime.dll` from R2R when building for ARM64.

## Known issues in general

1. When the source generator runs, it may produce code that makes use of `unsafe` depending on scenario. If you do not already have the property `AllowUnsafeBlocks` set to `true`, you will see `error CS0227: Unsafe code may only appear if compiling with /unsafe`. To address this, you should set the property `AllowUnsafeBlocks` to `true`. In future builds, we will instead look at producing a diagnostic for this rather than resulting in a compiler error. 

2. In WPF scenarios where you are not building a projection, you may see duplicate type errors where the same generated types are getting generated twice. This is due to a WPF targets issue where the same source generator is ran twice due to the file conflict of it being in multiple packages not being resolved. We are investigating addressing this, but in the mean time, you can workaround it by adding a target similar to the below:

```
<Target Name="RemoveCsWinRTPackageAnalyzer" BeforeTargets="CoreCompile">
    <ItemGroup>
        <Analyzer Remove="@(Analyzer)" Condition="%(Analyzer.NuGetPackageId) == 'Microsoft.Windows.CsWinRT'" />
    </ItemGroup>
</Target>
```