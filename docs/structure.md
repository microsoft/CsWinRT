# Repository Structure

This document describes the CsWinRT repository organization. Documentation and specs are located in the [`/docs`](.) folder. All source code for CsWinRT is located in the [`/src`](../src) folder, and files for generating the NuGet package are located in [`/nuget`](../nuget).

## [`build`](../build)

Contains source files for Azure DevOps pipeline that handles official builds and testing for C#/WinRT. Uses Maestro to publish builds conveniently for dependent projects; Maestro is a dependency manager 
developed by dotnet as part of the [Arcade Build System](https://github.com/dotnet/arcade).

## [`eng`](../eng)

Contains files that assist with publishing to Maestro.

## [`nuget`](../nuget)

Contains source files for producing the C#/WinRT NuGet package, which is regularly built, signed, and published to nuget.org by Microsoft. The package contains the **cswinrt.exe** projection compiler, the post-build tools (**cswinrtprojectiongen.exe**, **cswinrtimplgen.exe**, **cswinrtinteropgen.exe**), the runtime assembly (`WinRT.Runtime.dll`), precompiled SDK projection assemblies, MSBuild `.props`/`.targets` files, and the Roslyn source generator.

## [`src/Authoring`](../src/Authoring)

Contains projects for implementing authoring and hosting support, including the source generators, the WinRT host, and the cswinmd tool.

## [`src/Benchmarks`](../src/Benchmarks)

Contains benchmarks written using BenchmarkDotNet to track the performance of scenarios in the generated projection.  To run the benchmarks using the CsWinRT projection, run `benchmark.cmd`.

## [`src/cswinrt`](../src/cswinrt) 

Contains the sources and `cswinrt.vcxproj` project file for building the C#/WinRT compiler, **cswinrt.exe**.

The compiler uses the [WinMD NuGet package](http://aka.ms/winmd/nuget) for parsing [ECMA-335 metadata](http://www.ecma-international.org/publications/standards/Ecma-335.htm) files.  The WinMD github repo includes a [winmd.natvis](https://github.com/microsoft/winmd/blob/master/vs/winmd.natvis) script for debugging metadata parsing.  A symlink can be used to install the script:
  > for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\winmd.natvis" "c:\git\winmd\vs\winmd.natvis" 
  
The C#/WinRT project also contains a cswinrt.natvis script for debugging the C# projection writing, which can also be installed with a symlink:
> for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\cswinrt.natvis" "c:\git\cswinrt\cswinrt\cswinrt.natvis"

See also [Deploying .natvis files](https://docs.microsoft.com/en-us/visualstudio/debugger/create-custom-views-of-native-objects?view=vs-2015#BKMK_natvis_location).

## [`src/Perf`](../src/Perf)

Contains performance-related tools, including a benchmark baseline and the IID optimizer.

## [`src/Projections`](../src/Projections) 

Contains several projects for generating and building projections from the Windows SDK, WinUI, Benchmark (produced by the BenchmarkComponent project), and Test metadata (produced by the TestWinRT and TestComponentCSharp projects).

## [`src/Samples`](../src/Samples) 

- [`NetProjectionSample`](../src/Samples/NetProjectionSample): Contains an end-to-end sample for component authors, showing how to generate a projection from a C++/WinRT component and consume it using a NuGet package.

- [`WinUIDesktopSample`](../src/Samples/WinUIDesktopSample): Contains an end-to-end sample app that uses the Windows SDK and WinUI projections generated above.

## [`src/Tests`](../src/Tests)

Contains various testing-related projects:

- [`TestComponentCSharp`](../src/Tests/TestComponentCSharp): This is an implementation of a WinRT test component, defined in `class.idl` and used by the UnitTest project.  To complement the general TestComponent above, the TestComponentCSharp tests scenarios specific to the C#/WinRT language projection.

- [`UnitTest`](../src/Tests/UnitTest): Unit tests for validating the Windows SDK, WinUI, and Test projections generated above.  All pull requests should ensure that this project executes without errors.

- [`HostTest`](../src/Tests/HostTest): Unit tests for WinRT.Host.dll, which provides hosting for runtime components written in C#.

## [`src/TestWinRT`](https://github.com/microsoft/TestWinRT/)

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo is cloned into the root of the C#/WinRT repo, via `get_testwinrt.cmd`, so that `cswinrt.sln` can resolve its reference to `TestComponent.vcxproj`.  The resulting `TestComponent` and `BenchmarkComponent` files are consumed by the UnitTest and Benchmarks projects above.

## [`src/WinRT.Generator.Tasks`](../src/WinRT.Generator.Tasks)

Contains MSBuild task wrappers that invoke the CsWinRT code generators during the build. These tasks orchestrate the three post-build tools — the projection generator, the impl/forwarder generator, and the interop generator — and are called from the MSBuild targets in the `nuget/` directory.

## [`src/WinRT.Impl.Generator`](../src/WinRT.Impl.Generator)

Contains the **forwarder assembly generator** (`cswinrtimplgen.exe`). This tool takes a reference projection assembly and produces a forwarder DLL that type-forwards all public types to the appropriate runtime assembly — either `WinRT.Sdk.Projection` (for `Windows.*` and `WinRT.Interop.*` namespaces) or `WinRT.Projection` (for all other namespaces). The forwarder assembly is distributed in NuGet packages alongside the reference projection so that consumers can compile against the reference assembly while the forwarder routes types to the actual implementations generated at app build time.

## [`src/WinRT.Interop.Generator`](../src/WinRT.Interop.Generator)

Contains the **interop assembly generator** (`cswinrtinteropgen.exe`). This tool runs at **app build time** after all assemblies are compiled, analyzing the entire application to generate `WinRT.Interop.dll`. This assembly contains deduplicated native COM interface entries, vtable implementations, and marshalling infrastructure for all WinRT types used across the app. Because it sees the whole application, it avoids the code duplication and type map conflicts that would occur if marshalling code were generated per-project.

## [`src/WinRT.Projection.Generator`](../src/WinRT.Projection.Generator)

Contains the **projection assembly generator** (`cswinrtprojectiongen.exe`). This tool runs at **app build time** and produces `WinRT.Projection.dll`, which contains the actual projection implementations for all WinRT types used by the application. The forwarder assemblies from component NuGet packages route their types into this assembly. For Windows SDK types, the CsWinRT NuGet package includes precompiled `WinRT.Sdk.Projection.dll` binaries, so this tool only needs to generate projections for third-party components.

## [`src/WinRT.Runtime2`](../src/WinRT.Runtime2) 

Contains the WinRT.Runtime project for building the C#/WinRT runtime assembly, `WinRT.Runtime.dll`. The runtime assembly targets .NET 10 and provides Xaml reference tracking support which is necessary for WinUI 3 applications to manage memory correctly. The runtime assembly implements the following features for all projected C#/WinRT types:

- WinRT activation and marshaling logic
- Custom type mappings, primarily for WinUI
- [ComWrappers](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers) management
- IDynamicInterfaceCastable casting support
- Extension methods common to projected types

## [`src/WinRT.Sdk.Projection`](../src/WinRT.Sdk.Projection)

Contains the project that produces the `WinRT.Sdk.Projection` assembly, which includes projected types from the `Windows.*` and `WinRT.Interop.*` namespaces.

