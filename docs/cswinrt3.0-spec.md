# CsWinRT 3.0 overview

## Background context

CsWinRT provides the WinRT interop stack for C# applications. It was built to replace the built-in WinRT interop support that .NET dropped starting from .NET 5. The initial version of CsWinRT was built with specific limitations and constraints (many of which no longer applies), and as a result:
- It also had to support .NET Standard 2.0 code
- It also had to support an "embedded", source-only mode
- Trimming wasn't a consideration at all (it didn't exist yet)
- AOT wasn't a consideration at all (it didn't exist yet)
- No projection changes were allowed (decision made at that time)
- The initial release was also under a lot of time pressure to ship (to align with .NET 5 and WindowsAppSDK 1.0)
  - The CsWinRT 3.0 effort is driven by internal partner teams adopting C# for Windows components
  - Unlike CsWinRT 1.0, which had to be published for developers to be able to use WinRT APIs at all on modern .NET, CsWinRT 3.0 doesn't have an urgent need to ship to unblock consumers. Existing C# developers can continue using CsWinRT 2.x for the time being, which allows CsWinRT 3.0 to take the time to "get things right".

All of these original constraints resulted in CsWinRT accumulating technical debt over time, and being structured in a way that fundamentally makes it impossible for it to leverage all the advantages of trimming and AOT in the modern .NET world. Support for trimming and AOT did get added over time, but with it being an afterthought and not something designed into the product from the start, the support for it is not perfect, some things are just such that they cannot be fixed (eg. making runtime class casts trim-compatible and trim-friendly), and a lot of performance is left on the table.

Additionally, over the past few years, the adoption of CsWinRT allowed us to learn a lot, and get a better understanding of what decisions were made that turned out to be not ideal for performance or other kinds of optimizations (eg. reducing binary size). Furthermore, the original mandate not to change the API surface for projections made it such that some modern C# language features can't properly be leveraged with the WinRT API surface (eg. passing stack-allocated buffers to APIs with array parameters).

CsWinRT also incurred some regressions in developer UX: it doesn't support marshalling private and synthesized types in many scenarios, requires explicit actions from users to make types compatible with WinRT marshalling when using AOT (eg. making all types partial), and it fundamentally affects the performance of IntelliSense, as it relies on very expensive source generators that ultimately just cannot be made fast enough.

## CsWinRT 3.0 in a nutshell

CsWinRT 3.0 is an effort to address all these existing issues with CsWinRT mentioned above, and rebuild a new version of CsWinRT that is designed specifically for modern .NET, with trimming and AOT as core aspects of its architecture, and with a focus on performance. This new version is targeting .NET 10 and taking advantage of many new APIs for interop that we've collaborated with the .NET team to design and iterate upon (see [tracking issue](https://github.com/dotnet/runtime/issues/114179)). It will also resolve many of the regressions in the developer UX, such as IntelliSense being slow (and being almost unusable when working on large projects targeting Windows), and not being able to marshal private types.

CsWinRT 3.0 is designed around these core principles:
- Security:
  - Addressing all lifetime/concurrency issues with native object references in CsWinRT 2.x
  - All vtables and COM interface entries in the entire application are in readonly data sections
  - No need for consumers to set `<AllowUnsafeBlocks>` in projects that use WinRT generics
- Performance and binary size optimizations:
  - AOT-first: all core features must (1) work, and (2) be fast, on Native AOT.
    - All optimizations will prioritize AOT (eg. all vtables and CCW entries should be foldable)
    - All `IReference<T>`, `IReferenceArray<T>`, generic IDIC uses should work on AOT too
    - Interop code should match or exceed the capabilities of .NET Native, and beat it in perf
  - Trim-safe and trim-friendly: code should be trim-safe without user action, and fully trimmable.
    - That is, all generated code (types, methods, etc.) can be removed when publishing
- Modernization:
  - Targeting .NET 10 as a baseline (dropping .NET Standard 2.0 support as well)
  - Leveraging the latest .NET and C# features
    - To make this possible, we'll also no longer be supporting an embedded mode
  - Updating the WinRT projections API surface to use new .NET types, where applicable
- Usability and developer UX:
  - Making WinRT interop more seamless (like it used to be before .NET 5)
  - Making IntelliSense much more responsive (no expensive source generators needed anymore)
  - No need to manually make every single type partial anymore
  - Private and synthesized types (eg. generated collection types) can now be marshalled too
- Source compatibility:
  - The projected WinRT APIs will keep working just like with CsWinRT 2.x
  - We can apply several source-compatible changes to improve usability and performance
  - We'll take this opportunity to make small, targeted breaking changes (more details below)
- Working closely with other teams:
  - Validating the design every step of the way with the .NET and Roslyn teams
  - Requesting specific new .NET/AOT features to make WinRT interop great for C# devs

All this is meant to offer a new, modern, efficient WinRT interop stack for C# for the future. There are no new limitations introduced with CsWinRT 3.0, and the new version will in fact remove some of the existing limitations in CsWinRT 2.x (such as not being able to marshal private types). As for existing limitations that will carry forward to CsWinRT 3.0, we'll continue prioritizing them based on feedback, and addressing them. **There is no way to achieve these goals without making breaking changes with CsWinRT 2.x.**

## Technical details

CsWinRT 3.0 is centered around .NET 10, and in particular it relies on several key new APIs being added in this release (tracked by [dotnet/runtime#114179](https://github.com/dotnet/runtime/issues/114179)). The whole design and architecture for the ABI layer of CsWinRT 3.0 is structured around these key points in particular:
- All CCW/RCW marshalling is built on top of the new interop type map
  - This makes all marshalling code for CCWs and RCWs fully trimmable
  - This easily allows us to support `IReference<T>`  and `IReferenceArray<T>` too
  - The same applies to all generic type instantiations: they all go through the type map
  - The proxy type map in particular means no user action is needed to enable marshalling
- A new post-build tool will analyze input .dll-s and produce the `WinRT.Interop.dll` "sidecar":
  - This relies on `[UnsafeAccessorType]` to allow all upstream code to refer to it
  - We can generate all necessary marshalling code here with a global program view:
    - All marshalling code (including CCW support) for generic type instantiations
    - Marshalling code for all WinRT-exposed types (user-defined and not)
    - IDIC implementations for generic interface instantiations too
    - This can support internal/synthesized types too (just like .NET Native could)
  - This will run from an MSBuild target (see [dotnet/sdk#49417](https://github.com/dotnet/sdk/pull/49417)): no impact on IntelliSense at all anymore
    - Using a build task doing IL analysis was a strong recommendation from the Roslyn team
  - This makes all this code fully versionable: it's generated by the final application code, so it gets all improvements whenever CsWinRT is updated (it just needs a stable and documented ABI), instead of needing updates in every single referenced .dll like with CsWinRT 2.x
- All CCW vtables and COM interface entries should be fully pre-initialized by ILC:
  - This relies on ILC improvements done in [dotnet/runtime#114024](https://github.com/dotnet/runtime/issues/114024), [dotnet/runtime#114355](https://github.com/dotnet/runtime/issues/114355), [dotnet/runtime#114455](https://github.com/dotnet/runtime/issues/114455)
  - This effectively gets rid of thousands of static constructors
  - Provides 0 overhead, 0 allocation for all vtables in the entire application domain
  - Improved security: all pre-initialized data is in the `.rdata` section and immutable ([dotnet/runtime#114455](https://github.com/dotnet/runtime/issues/114477))
  - This is only made possible by the new sidecar .dll, providing hooks for all this code
- We can also fully pre-initialize the entire type hierarchy lookup for the whole application domain
- Similarly, we can fully pre-initialize the entire interface mapping for dynamic casts for all WinRT types

## Projection updates

Like mentioned above, the overall projected WinRT API surface will remain source compatible with previous CsWinRT versions. However, we can take this opportunity to make some targeted improvements and fixes. To clarify, the new projections will **not** be binary compatible with previous ones. CsWinRT 2.x and 3.0 are by design fundamentally incompatible.

### First class `Span<T>` projections for `T[]` parameters

CsWinRT 3.0 will update the projection of `T[]` parameters to use first-class spans in C# and match what C++/WinRT is also doing (it uses `array_view<T>`). Meaning we'll be changing `T[]` parameters as follows:
- `[In] T[]` -> `ReadOnlySpan<T>` (instead of `T[]`)
- `[Out] T[]` -> `Span<T>` (instead of `T[]`)
- `T[]` -> `Span<T>` (instead of `T[]`)

This means you no longer need to allocate and copy stuff into arrays all over the place (because you also need the size to be exactly right, so you can't even use the array pool). Instead, you'll be able to just use spans normally. Which also means you can now even make it 0-alloc and pass stack-allocated params entirely. This is source compatible thanks to the [first class span](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/first-class-span-types) types feature of C# 14.

### Fixing `Point`/`Rect`/`Size` properties

These foundational types have historically been projecting their fields as `double`, instead of `float`. This is not ideal for several reasons: it introduces implicit casts when assigning to or reading from them, it doesn't match the WinRT ABI (the backing data is still just floats), and it unnecessary impacts performance when doing lots of heavy calculations with them. In CsWinRT 3.0, we want to try fixing this design aspect and correctly projecting these members as `float`, and monitor what the real impact is on popular projects using WinRT from C#.

### Unifying foundational event handers with .NET

WinRT has the `Windows.Foundation.TypedEventHandler<TSender, TResult>` delegate type, which has always been explicitly projected in C# as a new delegate type. On the other hand, the `Windows.Foundation.EventHandler<T>` type has been custom mapped to the built-in .NET System.`EventHandler<TEventArgs>` type. This distinction and inconsistency has always been confusing for C# developers using WinRT API, and it also frequently causes friction when working with multiple projects that target different TFMs (as the WinRT event handler is only available when targeting Windows, and it's not a type you'd want to expose in types meant to be "platform agnostic", such as backend services or viewmodels).

In CsWinRT 3.0, we worked with the .NET team to complete the proposal for adding a `System.EventHandler<TSender, TEventArgs>` type to .NET 10 ([dotnet/runtime#28289](https://github.com/dotnet/runtime/issues/28289#issuecomment-2877604945)). This will allow us to update all WinRT projections to C# to project the WinRT typed event handler as the .NET type instead, finally making it consistent with events using `EventHandler<T>`.

### Other breaking changes

The API surface of projected runtime classes will be significantly streamlined compared to CsWinRT 2.x. In particular:
- **`IWinRTObject` and all related infrastructure in each projected runtime class will be removed.** All the shared functionality will be provided by the base `WindowsRuntimeObject` class, which will also handle `IDynamicInterfaceCastable` caching. This also reduces the object size for each runtime class type, as well as improving binary size.
- **`As<I>()` will be removed.** This functionality is available via `WindowsRuntimeActivationFactory`.
- **`FromAbi(nint)` will be removed.** This functionality is available via the marshaller type for each runtime class type.
- **All `IEquatable<T>` support will be removed.** Projected runtime class instances will compare via reference equality, like any other object. Equality support for the underlying native object will be provided as opt-in via the `WindowsRuntimeMarshal` type.

## Adoption plan

While in general the projected WinRT API surface will be source compatible, CsWinRT 3.0 will not be binary compatible with previous versions. Meaning that to switch to CsWinRT 3.0 in a given project, all WinRT projections will need to be updated to reference CsWinRT 3.0 as well (which will update the generated projections in that project). This will be somewhat similar (although more drastic) to the change to CsWinRT 2.x, which while more backwards compatible, still required projections to be updated to avoid .dll conflicts. We will continue servicing CsWinRT 2.x after releasing CsWinRT 3.0, for people that can't migrate to it yet, or for those that rely on features that will no longer be supported (such as embedded mode). We will then work on a deprecation plan for 2.x, to apply after this transition period where CsWinRT 2.x will continue receiving critical bug fixes.

### Multi-targeting support

Because CsWinRT 3.0 is fundamentally incompatible with CsWinRT 2.x, we need a way for NuGet package authors to keep supporting CsWinRT 2.x in parallel with CsWinRT 3.0, so that developers that can't upgrade their apps and libraries to CsWinRT 3.0 (e.g. because they're blocked by one or more dependencies not having updated just yet) will be able to still get new updates for those packages. The solution for this is to leverage the multi-targeting feature from NuGet. Specifically, the .NET SDK will add support for multi-targeting the CsWinRT version based on the revision number of the Windows SDK version indicated in the TFM (see [dotnet/sdk#49721](https://github.com/dotnet/sdk/pull/49721)).

Using a revision value of `.1` will include CsWinRT 3.0 and enable all its tooling. A revision of `.0` will use CsWinRT 2.x, e.g.:
- `net10.0-windows10.0.22621.0` ---> CsWinRT 2.x
- `net10.0-windows10.0.22621.1` ---> CsWinRT 3.0

This allows NuGet package authors to provide a version of their .dll-s for both flavors of CsWinRT, if needed. Similarly, consuming projects can also leverage this to multi-target, so that e.g. they can continue shipping a CsWinRT 2.x version of their apps while working on getting the CsWinRT 3.0 version ready to publish.

The .NET SDK will also define a new `CSWINRT3_0` constant to help in multi-targeting scenarios. This will be defined in the same way as other implicit constants (e.g. `NET10_0_OR_GREATER`), whenever the TFM in use implies that CsWinRT 3.0 should be used.