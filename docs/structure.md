# Repository Structure

This document describes the CsWinRT repository organization. Documentation and specs are located in the [`/docs`](.) folder. All source code for CsWinRT is located in the [`/src`](../src) folder, and files for generating the NuGet package are located in [`/nuget`](../nuget).

## [`build`](../build)

Contains source files for Azure DevOps pipeline that handles official builds and testing for C#/WinRT. Uses Maestro to publish builds conveniently for dependent projects. Maestro is a dependency manager 
developed by dotnet as part of the [Arcade Build System](https://github.com/dotnet/arcade).

## [`eng`](../eng)

Contains files that assist with publishing to Maestro.

## [`nuget`](../nuget)

Contains source files for producing the C#/WinRT NuGet package, which is regularly built, signed, and published to nuget.org by Microsoft. The package contains the post-build tools (**cswinrtprojectionrefgen.exe**, **cswinrtprojectiongen.exe**, **cswinrtimplgen.exe**, **cswinrtinteropgen.exe**, **cswinrtwinmdgen.exe**), the runtime assembly (`WinRT.Runtime.dll`), precompiled SDK projection assemblies, MSBuild `.props`/`.targets` files, and the Roslyn source generator.

## [`src/Authoring`](../src/Authoring)

Contains projects for implementing authoring and hosting support, including the source generators and the WinRT host.

## [`src/Benchmarks`](../src/Benchmarks)

Contains benchmarks written using BenchmarkDotNet to track the performance of scenarios in the generated projection.  To run the benchmarks using the CsWinRT projection, run `benchmark.cmd`.

## [`src/Perf`](../src/Perf)

Contains performance-related tools, including a benchmark baseline and the IID optimizer.

## [`src/Projections`](../src/Projections) 

Contains several projects for generating and building projections from the Windows SDK, WinUI, Benchmark (produced by the BenchmarkComponent project), and Test metadata (produced by the TestWinRT and TestComponentCSharp projects).

## [`src/Samples`](../src/Samples) 

- [`NetProjectionSample`](../src/Samples/NetProjectionSample): Contains an end-to-end sample for component authors, showing how to generate a projection from a C++/WinRT component and consume it using a NuGet package.

- [`WinUIDesktopSample`](../src/Samples/WinUIDesktopSample): Contains an end-to-end sample app that uses the Windows SDK and WinUI projections generated above.

## [`src/Tests`](../src/Tests)

Contains various testing-related projects:

- [`TestComponentCSharp`](../src/Tests/TestComponentCSharp): An implementation of a WinRT test component, defined in `TestComponentCSharp.idl` and used by the UnitTest and functional test projects.  To complement the general TestComponent above, the TestComponentCSharp tests scenarios specific to the C#/WinRT language projection.

- [`UnitTest`](../src/Tests/UnitTest): MSTest unit tests for validating the Windows SDK, WinUI, and Test projections generated above, plus core marshalling, COM interop, exceptions, and source-generator integration.  All pull requests should ensure that this project executes without errors.

- [`FunctionalTests`](../src/Tests/FunctionalTests): A collection of standalone console applications, each validating a specific interop scenario (async, collections, events, CCW, dynamic casting, structs, and more) under real publishing conditions such as trimming and Native AOT.  Each test reports success with exit code `100`.

- [`SourceGenerator2Test`](../src/Tests/SourceGenerator2Test): MSTest unit tests for the source generators and diagnostic analyzers in `WinRT.SourceGenerator2`, built on the Roslyn testing libraries.

- [`ObjectLifetimeTests`](../src/Tests/ObjectLifetimeTests): A WinUI application-style MSTest project validating reference tracking, garbage collection behavior, and XAML element lifetime.

- [`SmokeTests`](../src/Tests/SmokeTests): Minimal, isolated end-to-end smoke tests that consume the real `Microsoft.Windows.CsWinRT` NuGet package — a consumption app (`Consumption`), an authoring component (`Authoring`), a reference projection for a third-party `.winmd` (`Projection`), and reference projections for the Windows SDK (`WindowsSdkProjection`) and its `Windows.UI.Xaml` surface (`WindowsSdkXamlProjection`) — to verify the produced package works correctly outside the repository build infrastructure.

- [`AuthoringTest`](../src/Tests/AuthoringTest): A C#-authored WinRT component (`CsWinRTComponent=true`) covering a broad set of authoring type patterns.  Companion projects exercise consuming authored components — `AuthoringTest2`/`AuthoringTest3`, the `AuthoringConsumptionTest*` C++ consumers, and the WUX (`Windows.UI.Xaml`) and WinUI variants — several of which are still work in progress.

- [`HostTest`](../src/Tests/HostTest): C++ (gtest) tests for `WinRT.Host.dll`, which provides hosting for runtime components written in C#.

- [`DiagnosticTests`](../src/Tests/DiagnosticTests): Tests for the CsWinRT diagnostic and analyzer rules, driven by positive and negative source snippets.

- [`BuildDeterminismTest`](../src/Tests/BuildDeterminismTest): Compares whole-file SHA256 hashes of freshly generated `WinRT.Interop.dll` files across maximum parallelism values `-1`, `1`, and `2`, including repeated runs. By default it performs clean component builds with all-assembly marshalling discovery. To isolate the generator from MSBuild, run `BuildDeterminismTest --interop <generator.exe|generator.dll> <response.rsp>` with fixed inputs. Each invocation uses a fresh process and output directory with incremental generation disabled; failed replay outputs are retained for inspection. Relative paths in the response file are resolved from the current working directory.

  Persistent cache regression coverage is available separately:

  ```text
  BuildDeterminismTest --interop-incremental <generator.exe|generator.dll> <response.rsp>
  BuildDeterminismTest --interop-incremental <generator.exe|generator.dll> <response.rsp> --scenario cold,unchanged,method-body
  BuildDeterminismTest --interop-incremental <generator.exe|generator.dll> <response.rsp> --probe-only
  BuildDeterminismTest --interop-incremental --list-scenarios
  ```

  This mode **copies every input binary** into an owned `interop-incremental-<guid>` directory beneath the current working directory, preserving original DLL names and shared paths for reserved projection assemblies. It rewrites all input/output paths, removes `--debug-repro-directory`, and preserves the response file's marshalling and validation settings. Neither caller inputs nor caller outputs are modified. Relative input paths are resolved from the current working directory, just as when invoking the generator directly.

  An AsmResolver-generated managed probe references the input `System.Runtime.dll` and `WinRT.Runtime.dll`, roots `IList<string>`, and explicitly implements `IStringable`. Two tiny support assemblies define identically named managed types; the probe roots exposed generic carriers whose arguments differ only by these assembly scopes, including nested generic and array arguments. This makes mutations and scope-ordering coverage independent of application-specific types. The two runtime implementation paths must be present and unambiguous. Every scenario checks the expected hit/miss log and the **entire output DLL's SHA256**, including its MVID, against a cache-disabled emission of the **same mutated inputs**. Successful enabled misses are followed by a separate-process hit to verify that a usable sidecar was published. Opt-out runs check that sidecars are neither created nor changed.

  For inexpensive exhaustive coverage using a large application's response file, add **`--probe-only`**. This selects inputs **before copying**: only the `Microsoft.Windows.SDK.NET` reference projection (plus `Microsoft.Windows.UI.Xaml` when its private SDK projection is present), framework implementations identified by name and public key token, `WinRT.Runtime`, and the private SDK projections are retained. The generated .NET 10 probe becomes `--output-assembly-path`; application assemblies and custom `WinRT.Projection`/`WinRT.Component` assemblies are excluded. This reduced mode explicitly uses **`Minimal` discovery** and discards assembly opt-ins, so framework binaries remain resolution dependencies rather than introducing the entire BCL's marshalling workload. Other validation/code-generation options remain unchanged. All scenarios and exact hash assertions still run, and `--scenario` can further restrict them. Full application replay remains the default, preserves the original marshalling settings, and should still be used for representative application-scale comparisons.

  | Scenarios | Coverage |
  |-----------|----------|
  | `cold`, `unchanged`, `reordered-paths` | Cold generation; five unchanged hits with parallelism `-1, 1, 2, -1, 1`; reversed reference/implementation path lists |
  | `generic-argument-scopes` | Five forced-fresh emissions and cache hits with parallelism `1, 2, -1, 1, 2` and alternating input order, comparing whole DLLs to catch scope-insensitive sorting of nested generic/array arguments |
  | `missing-output`, `missing-cache`, `malformed-cache`, `truncated-cache`, `corrupt-dll`, `truncated-dll` | Recovery from absent or damaged cache artifacts, including same-length DLL corruption outside the MVID |
  | `opt-out-cold`, `opt-out`, `re-enable` | Opt-out with absent/existing caches, and stale-cache rejection after disabled generation replaces the DLL with changed interop code |
  | `method-body`, `unrelated-type` | Hits after verified actual IL-byte changes or a plain managed type addition, with new input and output MVIDs |
  | `type-identity`, `assembly-identity`, `assembly-version`, `runtime-class-name`, `interop-usage` | Misses after exposed type/assembly changes, adding a runtime class name marker, or adding another exposed type |
  | `runtime-bytes`, `framework-bytes`, `projection-bytes`, `reference-projection-bytes` | Full-byte invalidation of runtime, framework, private SDK projection, and reference projection inputs while keeping their MVIDs unchanged |

  All 24 scenarios run by default, with a fixed repetition count; use the comma-separated `--scenario` selector for small batches on expensive repros. `reference-projection-bytes` deterministically selects an assembly carrying `[WindowsRuntimeReferenceAssembly]` and fails clearly if none is available. Optionally append `--baseline-generator <previous-generator.exe|previous-generator.dll>` to compare the unmodified seed output with a cache-disabled emission from a pre-change generator as well. Both managed and Native AOT tools are supported.

  On failure the runner stops, preserving the mutated inputs, response files, complete stdout/stderr logs, output hashes/MVIDs, and DLL/cache files under the named scenario directory. On success it removes only its owned replay directory.

- [`OOPExe`](../src/Tests/OOPExe): An out-of-process executable harness used by the authoring test scenarios.

## [`src/TestWinRT`](https://github.com/microsoft/TestWinRT/)

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo is cloned into the root of the C#/WinRT repo, via `get_testwinrt.cmd`, so that `cswinrt.slnx` can resolve its reference to `TestComponent.vcxproj`.  The resulting `TestComponent` and `BenchmarkComponent` files are consumed by the UnitTest and Benchmarks projects above.

## [`src/WinRT.Generator.Tasks`](../src/WinRT.Generator.Tasks)

Contains MSBuild task wrappers that invoke the CsWinRT code generators during the build. These tasks orchestrate the post-build tools — the reference projection source generator, the projection generator, the impl/forwarder generator, the interop generator, and the WinMD generator — and are called from the MSBuild targets in the `nuget/` directory.

## [`src/WinRT.Impl.Generator`](../src/WinRT.Impl.Generator)

Contains the **forwarder assembly generator** (`cswinrtimplgen.exe`). This tool takes a reference projection assembly and produces a forwarder DLL that type-forwards all public types to the appropriate runtime assembly — either `WinRT.Sdk.Projection` (for `Windows.*` and `WinRT.Interop.*` namespaces) or `WinRT.Projection` (for all other namespaces). The forwarder assembly is distributed in NuGet packages alongside the reference projection so that consumers can compile against the reference assembly while the forwarder routes types to the actual implementations generated at app build time.

## [`src/WinRT.Interop.Generator`](../src/WinRT.Interop.Generator)

Contains the **interop assembly generator** (`cswinrtinteropgen.exe`). This tool runs at **app build time** after all assemblies are compiled, analyzing the entire application to generate `WinRT.Interop.dll`. This assembly contains deduplicated native COM interface entries, vtable implementations, and marshalling infrastructure for all WinRT types used across the app. Because it sees the whole application, it avoids the code duplication and type map conflicts that would occur if marshalling code were generated per-project.

To keep output independent of parallel discovery, interface entries and fallback runtime class names use stable type-name ordering. Type comparisons use assembly identities for both definitions and references, and non-binding assembly processor-architecture flags are normalized in memory before emission.

## [`src/WinRT.Projection.Generator`](../src/WinRT.Projection.Generator)

Contains the **projection assembly generator** (`cswinrtprojectiongen.exe`). This tool runs at **app build time** and produces `WinRT.Projection.dll`, which contains the actual projection implementations for all WinRT types used by the application. The forwarder assemblies from component NuGet packages route their types into this assembly. For Windows SDK types, the CsWinRT NuGet package includes precompiled `WinRT.Sdk.Projection.dll` binaries, so this tool only needs to generate projections for third-party components. The generator drives the [`WinRT.Projection.Writer`](../src/WinRT.Projection.Writer) library in-process to produce its C# sources, then compiles them with Roslyn.

## [`src/WinRT.Projection.Ref.Generator`](../src/WinRT.Projection.Ref.Generator)

Contains the **reference projection source generator** (`cswinrtprojectionrefgen.exe`). This Native AOT CLI tool runs at component-library build time, driving the projection writer in-process to produce the `.cs` files that `csc.exe` then compiles into the user library/component `.dll`. It is invoked from the `CsWinRTGenerateProjection` MSBuild target.

## [`src/WinRT.Projection.Writer`](../src/WinRT.Projection.Writer)

Contains the **projection writer**, a C# library that reads `.winmd` metadata and generates C# projection source code for Windows Runtime types. The writer ships as a library and is consumed by both `cswinrtprojectionrefgen.exe` (component-library build time) and `cswinrtprojectiongen.exe` (app publish time) via a single `ProjectionWriter.Run(ProjectionWriterOptions)` entry point.

## [`src/WinRT.WinMD.Generator`](../src/WinRT.WinMD.Generator)

Contains the **WinMD generator** (`cswinrtwinmdgen.exe`). This tool, distributed as a Native AOT binary alongside the other CsWinRT post-build tools, generates a `.winmd` metadata file from a compiled C# component assembly so developers can author Windows Runtime components in C#. It is a port and restructuring of the previous WinMD generator from CsWinRT 2.x, which was implemented as a Roslyn source generator. In addition to consistency with the other CsWinRT 3.0 build tools, moving it out of a source generator addresses a fundamental design issue: the 2.x generator emitted a `.winmd` file **on disk**, but arbitrary file I/O is explicitly unsupported in Roslyn source generators (which may only contribute additional source to the compilation). The new tool runs as a normal MSBuild step — invoked by the `RunCsWinRTWinMDGenerator` MSBuild task wired up through `nuget/Microsoft.Windows.CsWinRT.Authoring.WinMD.targets`, after `CoreCompile` when `CsWinRTComponent == true` — where file I/O is the expected output mechanism.

## [`src/WinRT.Runtime2`](../src/WinRT.Runtime2) 

Contains the WinRT.Runtime project for building the C#/WinRT runtime assembly, `WinRT.Runtime.dll`. The runtime assembly targets .NET 10 and provides Xaml reference tracking support which is necessary for WinUI 3 applications to manage memory correctly. The runtime assembly implements the following features for all projected C#/WinRT types:

- WinRT activation and marshaling logic
- Custom type mappings, primarily for WinUI
- [ComWrappers](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers) management
- IDynamicInterfaceCastable casting support
- Extension methods common to projected types

## [`src/WinRT.Sdk.Projection`](../src/WinRT.Sdk.Projection)

Contains the project that produces the `WinRT.Sdk.Projection` assembly, which includes projected types from the `Windows.*` and `WinRT.Interop.*` namespaces.
