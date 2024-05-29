# .NET AOT support in C#/WinRT

## Overview

As of CsWinRT **2.1.0-preview** and Windows SDK projection 10.0.<sdk_ver>.**35-preview**, generated projections are AOT compatible.  You can use the preview version of the Windows SDK projection by using the [WindowsSdkPackageVersion property](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#windowssdkpackageversion).  Projections can choose to mark their projects as [IsAotCompatible](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#aot-compatibility-analyzers) to run the relevant .NET AOT analyzers on them.  Similarly, C# libraries using C#/WinRT authoring support with the new versions are also AOT compatible and can be published for AOT using the [PublishAOT property and running publish](https://learn.microsoft.com/dotnet/core/deploying/native-aot/?tabs=net8plus%2Cwindows#publish-native-aot-using-the-cli).

## Consuming projected types and being AOT compatible

If your app or library has non-WinRT classes that implement C#/WinRT projected interfaces or built-in .NET interfaces that are mapped to WinRT types and are passed across the ABI to other WinRT functions, the class needs to be marked `partial` in order to be AOT compatible.  Marking it `partial` allows the source generator distributed with C#/WinRT to add an attribute to the class which has the necessary logic to produce the WinRT vtable for it in a way that is both trimming and AOT compatible.

The source generator also detects other scenarios such as boxing of arrays and instantiations of generic WinRT types for which it generates code that will allow the WinRT vtable for it to be looked up when passed across the WinRT ABI.

In general, this also means that if your app or library has a dependency on another library that uses or returns types falling into one of those above scenarios, that library needs to have also been ran with the updated CsWinRT version for your app or library to be AOT compatible.  This is similar to the general .NET requirement where all your dependencies need to be AOT compatible for your app or library to be AOT compatible.

## Known issues when publishing for AOT

There are a couple issues related to our AOT support that are known with the current release which will be addressed in future updates or be fixed in .NET:

1. If any built-in .NET private types implementing mapped WinRT interfaces are passed across the WinRT ABI, they will not be detected by the source generator and thereby not be AOT compatible.  You can workaround this by either avoiding passing them across the ABI or by registering your own WinRT vtable lookup function using `WinRT.ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup` and `WinRT.ComWrappersSupport.RegisterTypeRuntimeClassNameLookup`.  An example of this can be seen [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L57-L75) and [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L87-L90).

2. If you have any nested C# types that you use as part of .NET collections that are passed across the WinRT ABI, the source generator generates the incorrect type name to represent it in the generated lookup function.  You can workaround this issue until it is addressed by registering your own WinRT vtable lookup function using `WinRT.ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup` and `WinRT.ComWrappersSupport.RegisterTypeRuntimeClassNameLookup`. An example of this can be seen [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L21-L53) and [here](https://github.com/manodasanW/WinUI-Gallery/blob/16ed717700b929dcb6591d32a4f10cd8b102aa07/WinUIGallery/VtableInitialization.cs#L83-L86).

3. In WinUI apps, you might run into a race condition which triggers what looks to be a hang during .NET GC.  This is the result of an exception being thrown unexpectedly during GC due to an invalid handle.