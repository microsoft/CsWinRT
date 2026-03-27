---
name: interop-generator
description: Work with, answer questions about, edit, debug, or extend the CsWinRT interop generator (cswinrtinteropgen.exe) that produces WinRT.Interop.dll. Also use whenever the user asks anything related to the interop generator.
---

# CsWinRT interop generator (`cswinrtinteropgen.exe`)

This skill provides comprehensive knowledge of the interop sidecar generator — the post-build CLI tool that produces `WinRT.Interop.dll`. Use this as the primary reference when working on, debugging, or extending any part of the interop generator.

<investigate_before_answering>
Before making changes, always read the specific source files involved. This document provides an architectural map, but the code is the source of truth. Use this skill to know *where* to look and *why* things are structured the way they are.
</investigate_before_answering>

## Overview

The interop generator (`src/WinRT.Interop.Generator/`) is a command-line tool that analyzes all assemblies in a published application and emits `WinRT.Interop.dll` — a sidecar assembly containing all marshalling code needed for Windows Runtime interop. It runs at the very end of the build pipeline (after all user assemblies and projection assemblies are compiled), giving it a **whole-program view** of every type that crosses the Windows Runtime interop boundary. This enables performance optimizations (all vtables can be pre-initialized), security features (all vtables are in readonly data segments in the PE file), and usability improvements (no need to mark types as being marshalled — things "just work").

**Why it exists:** See the "Why the interop generator?" section in `.github/copilot-instructions.md` for the architectural motivation. In short: it deduplicates marshalling code across assemblies, avoids type map conflicts, and enables fully pre-initialized vtables for AOT.

**Key technology:** The generator uses [AsmResolver](https://github.com/Washi1337/AsmResolver) for reading and writing .NET assemblies and IL. It does **not** use `System.Reflection.Emit` or Roslyn — it directly constructs CIL metadata and instructions via AsmResolver's API.

**Accessing interop APIs:** `WinRT.Interop.dll` cannot be directly referenced by any other assembly, as it is produced at the very end of the build process. All upstream assemblies that need to invoke APIs from it must do so by using [`[UnsafeAccessor]`](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute) and `[UnsafeAccessorType]`. All generated projections and code within `WinRT.Runtime.dll` already use this technique. User code should never try to or need to do this manually: all of this code exists solely to support the Windows Runtime marshalling infrastructure behind the scenes.

## Build infrastructure

The `WinRT.Interop.dll` assembly is produced by `cswinrtinteropgen`, which is a native binary (compiled with Native AOT) invoked during build. It is invoked by the [`RunCsWinRTGenerator`](https://github.com/dotnet/sdk/blob/2ab975ef4c560f9383e897d9af4e9784798b7576/src/Tasks/Microsoft.NET.Build.Tasks/RunCsWinRTGenerator.cs) MSBuild task in the .NET SDK. This task is invoked by the [`_RunCsWinRTGenerator`](https://github.com/dotnet/sdk/blob/2ab975ef4c560f9383e897d9af4e9784798b7576/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Windows.targets#L275) target, defined in `Microsoft.NET.Windows.targets`, also in the .NET SDK.

### Version compatibility

`cswinrtinteropgen` must be versioned with the `WinRT.Runtime.dll` assembly it was compiled for. Not doing so will result in undefined behavior (e.g. failures to run the tool, or runtime crashes). To ensure these versions always match, the .NET SDK selects the right version of `cswinrtinteropgen` after all reference assemblies are resolved:

- If a project **does not** reference CsWinRT, then `cswinrtinteropgen` is loaded from the Windows SDK projections targeting pack.
- If CsWinRT **is** referenced (directly or transitively), then `cswinrtinteropgen` is loaded from that package, but only if the `WinRT.Runtime.dll` binary from that package has a higher version than the one in the Windows SDK projections package being referenced. This correctly handles cases where a dependent project might have a reference to an outdated CsWinRT package.

This version matching is critical because `cswinrtinteropgen` relies on "implementation details only" APIs in `WinRT.Runtime.dll` — APIs which are public, hidden, and marked as `[Obsolete]`, and which are exclusively meant to be consumed by generated code produced by `cswinrtinteropgen`. These APIs might change at any time without following semantic versioning for CsWinRT. For instance, they are crucial to support marshalling generic Windows Runtime collection interfaces, and the code in `WinRT.Interop.dll` makes heavy use of them in this scenario.

## Project settings

- **Target**: `net10.0`, C# 14, `AllowUnsafeBlocks`
- **Root namespace**: `WindowsRuntime.InteropGenerator`
- **Assembly name**: `WinRT.Interop.Generator`
- **Output type**: Exe (console application)
- **Key dependency**: `AsmResolver.DotNet` (assembly reading/writing), `ConsoleAppFramework` (CLI)
- **Warnings as errors**: release only. `EnforceCodeStyleInBuild` enabled, `AnalysisLevelStyle` = `latest-all`.

## Project structure

```
WinRT.Interop.Generator/
├── Program.cs                              # Entry point (delegates to InteropGenerator.Run)
├── Attributes/                             # CLI argument attribute
│   └── CommandLineArgumentNameAttribute.cs # Maps properties to CLI arg names
├── Builders/                               # IL type/method definition builders
│   ├── InteropTypeDefinitionBuilder.cs     # Core builder (IID, NativeObject, ComWrappersCallback)
│   ├── InteropTypeDefinitionBuilder.*.cs   # Per-interface builders (20+ partials)
│   ├── DynamicCustomMappedTypeMapEntriesBuilder.cs        # ICommand, INotifyPropertyChanged, etc.
│   ├── IgnoresAccessChecksToBuilder.cs     # [IgnoresAccessChecksTo] attribute emission
│   ├── MetadataAssemblyAttributesBuilder.cs # Assembly-level metadata attributes
│   └── WindowsRuntimeTypeHierarchyBuilder.cs # Type hierarchy lookup table
├── Discovery/                              # Type discovery logic
│   ├── InteropTypeDiscovery.cs             # Main discovery: type hierarchy, user types, arrays
│   └── InteropTypeDiscovery.Generics.cs    # Generic instantiation discovery + cascade logic
├── Errors/                                 # Error/warning infrastructure
│   ├── UnhandledInteropException.cs        # Wraps unexpected errors with phase context
│   ├── WellKnownInteropException.cs        # Structured errors with CSWINRTINTEROPGEN### codes
│   ├── WellKnownInteropExceptions.cs       # Factory for all error/warning codes (80+ codes)
│   └── WellKnownInteropWarning.cs          # Non-fatal warnings (can be promoted to errors)
├── Extensions/                             # Extension methods (26 files)
│   ├── WindowsRuntimeExtensions.cs         # Core: IsProjectedWindowsRuntimeType, IsBlittable, etc.
│   ├── TypeSignatureExtensions.cs          # GetAbiType, EnumerateAllInterfaces, etc.
│   ├── ModuleDefinitionExtensions.cs       # GetType, TryGetType, ReferencesAssembly
│   ├── CilInstructionCollectionExtensions.cs # IL instruction manipulation
│   └── ... (22 more)                       # Various AsmResolver type extensions
├── Factories/                              # Type/member/attribute creation factories
│   ├── InteropCustomAttributeFactory.cs    # [Guid], [UnmanagedCallersOnly], [TypeMap], etc.
│   ├── InteropMemberDefinitionFactory.cs   # Properties, methods, lazy-init patterns
│   ├── InteropMethodDefinitionFactory.*.cs # Per-interface method body factories (20+ partials)
│   ├── InteropTypeDefinitionFactory.SzArrayMarshaller.cs         # SZ array marshaller type emission
│   ├── InteropTypeDefinitionFactory.SzArrayElementMarshaller.cs  # SZ array element marshaller emission
│   ├── InteropTypeDefinitionFactory.IEnumeratorElementMarshaller.cs # Collection element marshaller emission
│   ├── InteropTypeDefinitionFactory.IReadOnlyCollectionKeyValuePair2.cs # IReadOnlyCollection<KeyValuePair<K,V>> emission
│   ├── InteropUtf8NameFactory.cs           # ABI-prefixed namespace/type name generation
│   ├── WellKnownTypeDefinitionFactory.cs   # IUnknownVftbl, IInspectableVftbl, DelegateVftbl
│   ├── WellKnownMemberDefinitionFactory.cs # IID/Vtable property definitions
│   └── ... (8 more)                        # Various well-known and utility factories
├── Fixups/                                 # Post-emit IL cleanup
│   ├── InteropMethodFixup.cs               # Abstract base with label redirect helpers
│   ├── InteropMethodFixup.RemoveLeftoverNopAfterLeave.cs  # ECMA-335 compliance
│   └── InteropMethodFixup.RemoveUnnecessaryTryStartNop.cs # IL size optimization
├── Generation/                             # Core pipeline orchestration
│   ├── InteropGenerator.cs                 # Main entry: Run() → Discover() → Emit()
│   ├── InteropGenerator.DebugRepro.cs      # Debug repro pack/unpack
│   ├── InteropGenerator.Discover.cs        # Discovery phase orchestration
│   ├── InteropGenerator.Emit.cs            # Emit phase orchestration (~131 KB, largest file)
│   ├── InteropGeneratorArgs.cs             # CLI argument definitions
│   ├── InteropGeneratorArgs.Parsing.cs     # .rsp file parsing
│   ├── InteropGeneratorArgs.Formatting.cs  # .rsp file serialization (for debug repros)
│   ├── InteropGeneratorDiscoveryState.cs   # Thread-safe discovery phase state
│   └── InteropGeneratorEmitState.cs        # Thread-safe emit phase state
├── Helpers/                                # Utility classes
│   ├── GuidGenerator.cs                    # IID computation (SHA1-based, RFC 4122 v5)
│   ├── SignatureGenerator.cs               # Windows Runtime type signature strings
│   ├── SignatureGenerator.Primitives.cs    # Primitive type signatures (i4, u4, f8, etc.)
│   ├── SignatureGenerator.Projections.cs   # Projected type signatures (pinterface, struct, etc.)
│   ├── TypeMapping.cs                      # Managed ↔ Windows Runtime type mapping registry (~70 types)
│   ├── TypeExclusions.cs                   # Types excluded from processing
│   ├── MvidGenerator.cs                    # Deterministic MVID from input assemblies
│   ├── RuntimeClassNameGenerator.cs        # Windows Runtime class name generation
│   ├── MetadataTypeNameGenerator.cs        # Metadata type name formatting
│   ├── WindowsRuntimeTypeAnalyzer.cs       # Type hierarchy and covariance analysis
│   ├── InteropGeneratorJsonSerializerContext.cs # JSON serializer for debug repros
│   └── Comparers/                          # IComparer implementations for sorting
├── Models/                                 # Data models
│   ├── TypeSignatureEquatableSet.cs        # Immutable, equatable set of type signatures
│   ├── TypeSignatureEquatableSet.Builder.cs # Mutable builder (uses object pooling)
│   └── MethodRewriteInfo/                  # Method rewrite descriptors (8 files)
│       ├── MethodRewriteInfo.cs            # Abstract base with comparison logic
│       ├── MethodRewriteInfo.ReturnValue.cs
│       ├── MethodRewriteInfo.RetVal.cs
│       ├── MethodRewriteInfo.RawRetVal.cs
│       ├── MethodRewriteInfo.ManagedParameter.cs
│       ├── MethodRewriteInfo.ManagedValue.cs
│       ├── MethodRewriteInfo.NativeParameter.cs
│       └── MethodRewriteInfo.Dispose.cs
├── References/                             # Assembly/type reference registries
│   ├── InteropReferences.cs                # 100+ cached type/method references from core libs
│   ├── InteropDefinitions.cs               # Generated type definitions in output assembly
│   ├── InteropNames.cs                     # Well-known assembly/DLL name constants
│   ├── InteropValues.cs                    # CsWinRT strong-name public key
│   ├── WellKnownInterfaceIIDs.cs           # Pre-computed IIDs for well-known interfaces
│   └── WellKnownPublicKeyTokens.cs         # System assembly public key tokens
├── Resolvers/                              # Type/method resolution
│   ├── InteropImplTypeResolver.cs          # Locates Impl types (IID + Vtable) across assemblies
│   ├── InteropInterfaceEntriesResolver.cs  # Builds interface entry lists for CCW
│   ├── InteropInterfaceEntryInfo.cs        # Abstract base for IID/Vtable loading
│   ├── InteropMarshallerType.cs            # Accessor for marshaller methods (ref struct)
│   ├── InteropMarshallerTypeResolver.cs    # Locates marshaller type for a type signature
│   └── PathAssemblyResolver.cs             # Custom assembly resolver from file paths
├── Rewriters/                              # Two-pass IL rewriting
│   ├── InteropMethodRewriter.ReturnValue.cs   # ABI → managed return value marshalling
│   ├── InteropMethodRewriter.RetVal.cs        # Indirect return value (out param) marshalling
│   ├── InteropMethodRewriter.RawRetVal.cs     # Raw return value marshalling
│   ├── InteropMethodRewriter.ManagedParameter.cs # ABI → managed parameter conversion
│   ├── InteropMethodRewriter.ManagedValue.cs  # Stack value ABI → managed conversion
│   ├── InteropMethodRewriter.NativeParameter.cs # Managed → ABI with try/finally cleanup
│   └── InteropMethodRewriter.Dispose.cs       # Resource cleanup IL generation
└── Visitors/                               # AsmResolver type signature visitors
    ├── AllGenericTypesVisitor.cs            # Recursively extracts all generic instantiations
    ├── AllSzArrayTypesVisitor.cs            # Recursively extracts all SZ array types
    └── IsConstructedGenericTypeVisitor.cs   # Validates no open generic parameters remain
```

## Pipeline architecture

The generator runs in three sequential phases:

```
Program.cs → ConsoleApp.Run(args, InteropGenerator.Run)
                │
                ├── 1. Debug repro handling (optional)
                │   ├── UnpackDebugRepro() — if input is .zip
                │   └── SaveDebugRepro() — if --debug-repro-directory is set
                │
                ├── 2. Discovery phase (InteropGenerator.Discover)
                │   ├── Load all reference + implementation assemblies (parallel)
                │   ├── Load special WinRT modules (Sdk.Projection, Projection, Component)
                │   ├── For each module (parallel):
                │   │   ├── DiscoverTypeHierarchyTypes()
                │   │   ├── DiscoverGenericTypeInstantiations()
                │   │   ├── DiscoverExposedUserDefinedTypes()
                │   │   └── DiscoverSzArrayTypes()
                │   ├── ValidateWinRTRuntimeDllVersion2References()
                │   └── Return frozen InteropGeneratorDiscoveryState
                │
                └── 3. Emit phase (InteropGenerator.Emit)
                    ├── DefineInteropModule() — create output assembly
                    ├── Type hierarchy emission
                    ├── Generic type definition (26 Define*() methods)
                    ├── SZ array type definition
                    ├── RewriteMethodDefinitions() — two-pass IL
                    ├── FixupMethodDefinitions() — IL cleanup
                    ├── DefineUserDefinedTypes() — CCW code
                    ├── Dynamic custom-mapped type map entries
                    ├── Assembly attributes
                    └── WriteInteropModuleToDisk()
```

## Response (.rsp) files

The generator is invoked via a response file: `cswinrtinteropgen.exe @path/to/response.rsp`

**Format:** Plain text, one argument per line: `--argument-name value`

**Parsing** (`InteropGeneratorArgs.Parsing.cs`):
- Each property in `InteropGeneratorArgs` is decorated with `[CommandLineArgumentName("--name")]`
- String arrays use comma-separated values
- Validates no duplicate arguments
- Maps property names to CLI argument names via reflection over the attribute

**Serialization** (`InteropGeneratorArgs.Formatting.cs`):
- Reverses the parsing process for debug repro generation
- Maintains exact format for reproducibility

**Parameters:**

| CLI argument | Type | Description |
|-------------|------|-------------|
| `--reference-assembly-paths` | `string[]` | Windows SDK and framework reference assemblies |
| `--implementation-assembly-paths` | `string[]` | User app + library assemblies |
| `--output-assembly-path` | `string` | Main app .dll being published |
| `--winrt-sdk-projection-assembly-path` | `string` | `WinRT.Sdk.Projection.dll` path (required) |
| `--winrt-sdk-xaml-projection-assembly-path` | `string?` | `WinRT.Sdk.Xaml.Projection.dll` path (optional) |
| `--winrt-projection-assembly-path` | `string?` | `WinRT.Projection.dll` path (for 3rd-party components) |
| `--winrt-component-assembly-path` | `string?` | `WinRT.Component.dll` path (for authored components) |
| `--generated-assembly-directory` | `string` | Output folder for `WinRT.Interop.dll` |
| `--use-windows-ui-xaml-projections` | `bool` | Use UWP XAML (`Windows.UI.Xaml`) instead of WinUI |
| `--validate-winrt-runtime-assembly-version` | `bool` | Check version compatibility |
| `--validate-winrt-runtime-dll-version-2-references` | `bool` | Reject CsWinRT 2.x references |
| `--enable-incremental-generation` | `bool` | Enable incremental generation (caching) |
| `--treat-warnings-as-errors` | `bool` | Promote warnings to errors |
| `--max-degrees-of-parallelism` | `int` | Parallel task limit |
| `--debug-repro-directory` | `string?` | Directory to save debug repro .zip |

## Debug repros

Debug repros are self-contained .zip files that capture the exact generator invocation state, enabling reproduction of issues without needing the original build environment or file paths.

**Structure of a debug repro .zip:**
```
cswinrtinteropgen.rsp              — Response file with normalized paths
original-reference-paths.json      — Map: normalized name → original path
original-implementation-paths.json — Map: normalized name → original path
reference/                         — All reference .dll-s (renamed to avoid conflicts)
implementation/                    — All implementation .dll-s (renamed to avoid conflicts)
```

**DLL naming:** To avoid filename conflicts, DLLs are renamed using a SHAKE128 hash of their full original path: `<originalName>_<SHAKE128(filePath)>.dll` (16 bytes of hash).

**Generation** (`SaveDebugRepro` in `InteropGenerator.DebugRepro.cs`):
1. Creates temporary directory structure
2. Copies all input DLLs with hash-based names
3. Records original paths in JSON maps
4. Serializes args to response file with updated paths
5. Zips everything to `--debug-repro-directory`

**Consumption** (`UnpackDebugRepro`):
1. If the input path ends with `.zip`, extracts to a temp directory
2. Loads JSON maps to restore original→normalized path mappings
3. Returns path to extracted response file
4. Processing proceeds as normal using the extracted assemblies

**Use case:** Share issues without disclosing actual file paths; enables deterministic reproduction. Attach the .zip to a GitHub issue and anyone can run `cswinrtinteropgen.exe @path/to/extracted.rsp` to reproduce.

## Input filtering

The generator processes two categories of assemblies:

1. **Reference assemblies** — Read-only metadata (Windows SDK projections, framework assemblies). These are analyzed for type hierarchy discovery but are not modified.
2. **Implementation assemblies** — User app assemblies analyzed for user-defined types, generic instantiations, and array types.

**Special modules** are loaded separately and treated specially:
- `WinRT.Sdk.Projection.dll` — Precompiled Windows SDK projection (always required)
- `WinRT.Sdk.Xaml.Projection.dll` — Precompiled UWP XAML projection (optional)
- `WinRT.Projection.dll` — 3rd-party component projections (optional)
- `WinRT.Component.dll` — Authored component projections (optional)

**Type exclusions** (`Helpers/TypeExclusions.cs`):
- `System.Threading.Tasks.Task<T>` — Cannot be marshalled across Windows Runtime boundary
- `System.Collections.Concurrent.ConditionalWeakTable<,>` — Memory semantics conflict

**Type inclusion criteria:**
- Must be a projected Windows Runtime type (marked with `[WindowsRuntimeMetadata]` or similar)
- Generic types must be fully constructed (no open generic parameters)
- Type hierarchy must be fully resolvable (no missing dependencies)
- Must not be a managed-only type (types that never cross the Windows Runtime boundary)

## Discovery phase

The discovery phase (`InteropGenerator.Discover.cs` + `Discovery/InteropTypeDiscovery.cs`) scans all input assemblies to find every type requiring marshalling code. It uses `Parallel.ForEach` over all input modules for performance.

### Discovery state (`InteropGeneratorDiscoveryState`)

A thread-safe state container using `ConcurrentDictionary` collections. Key collections:

| Collection | Tracks |
|------------|--------|
| `Modules` | All loaded `ModuleDefinition` objects (keyed by path) |
| `TypeHierarchyEntries` | Projected class → base class mappings |
| `IEnumerator1Types`, `IEnumerable1Types`, `IList1Types`, etc. | Generic instantiations per interface |
| `GenericDelegateTypes` | `EventHandler<T>`, `TypedEventHandler<TSender, TArgs>` instantiations |
| `KeyValuePairTypes` | `KeyValuePair<K,V>` instantiations |
| `UserDefinedTypes` | User classes/structs + their vtable interface sets |
| `SzArrayTypes` | SZ array types needing marshallers |

After discovery completes, the state is frozen via `MakeReadOnly()` to prevent accidental mutation during emit.

### Type hierarchy discovery (`TryTrackTypeHierarchyType`)

Processes reference assemblies to build a map of projected Windows Runtime class inheritance chains. Only `IsProjectedWindowsRuntimeClassType` types are tracked. This map is used at emit time to generate the type hierarchy lookup table (`WindowsRuntimeTypeHierarchyBuilder`), which enables runtime class name → type resolution.

### Generic instantiation discovery (`TryTrackGenericTypeInstance`)

The core of discovery — finds all constructed generic types used anywhere in the application. Located in `InteropTypeDiscovery.Generics.cs`.

**Algorithm:**
1. Validate the type is fully constructed (via `IsConstructedGenericTypeVisitor`)
2. Check exclusion list
3. Validate the type is resolvable
4. Branch based on whether the type is a Windows Runtime type or a managed type

**For Windows Runtime generic types** (`TryTrackWindowsRuntimeGenericTypeInstance`), each interface type has specific tracking and **automatic cascading** — discovering one type automatically discovers all its transitive dependencies:

| Interface | Cascades to |
|-----------|-------------|
| `IEnumerable<T>` | `IEnumerator<T>` |
| `IList<T>` | `IReadOnlyList<T>`, `IEnumerable<T>`, and if `T` is `KeyValuePair<K,V>`: `IDictionary<K,V>` |
| `IReadOnlyList<T>` | `IEnumerable<T>`, and if `T` is `KeyValuePair<K,V>`: `IReadOnlyDictionary<K,V>` |
| `IDictionary<K,V>` | `IReadOnlyDictionary<K,V>`, `KeyValuePair<K,V>`, `IList<KeyValuePair<K,V>>`, `IEnumerable<KeyValuePair<K,V>>`, helper collection types |
| `IReadOnlyDictionary<K,V>` | Similar cascades as `IDictionary` plus split adapter types |
| `IObservableVector<T>` | `VectorChangedEventHandler<T>` |
| `IObservableMap<K,V>` | `MapChangedEventHandler<K,V>`, `IMapChangedEventArgs<K>` |
| `IAsyncOperation<T>` | `AsyncOperationCompletedHandler<T>` |
| `IAsyncActionWithProgress<P>` | `AsyncActionProgressHandler<P>`, `AsyncActionWithProgressCompletedHandler<P>` |
| `IAsyncOperationWithProgress<T,P>` | Both progress and completion handlers |

**For managed generic types** (`TryTrackManagedGenericTypeInstance`):
- `Span<T>` and `ReadOnlySpan<T>` where `T` is a Windows Runtime type → construct `SzArrayType` and route to array discovery
- All other types → route to `TryTrackExposedUserDefinedType`

### User-defined type discovery (`TryTrackExposedUserDefinedType`)

Discovers user-authored types that need CCW (COM Callable Wrapper) code. These are managed classes/structs that implement Windows Runtime interfaces and thus need to be callable from native code.

**Algorithm:**
1. Filter out excluded types, array types, unconstructed generics, and projected types
2. Validate the entire type hierarchy is resolvable (with recursion guard)
3. Check the type is possibly Windows Runtime-exposed and not managed-only
4. Use `TryMarkUserDefinedType()` for deduplication (prevents infinite recursion from circular interface dependencies)
5. Gather all implemented Windows Runtime interfaces (including `[ExclusiveTo]` interfaces from authored components)
6. Perform **covariance expansion** — e.g., if type implements `IEnumerable<DerivedClass>`, it also needs `IEnumerable<BaseClass>` entries
7. For each generic interface found, trigger tracking of that generic instantiation
8. Only track if the type implements ≥1 projected Windows Runtime interface (ignore types with only `[GeneratedComInterface]` interfaces)

**Interface limit:** Maximum 128 interfaces per type; emits a warning if exceeded.

### SZ array discovery (`TryTrackSzArrayType` / `TryTrackExposedSzArrayType`)

Discovers `T[]` array types that need marshalling code. Arrays auto-implement several collection interfaces (`IEnumerable<T>`, `IList<T>`, etc.), so array discovery triggers generic type tracking for all those interfaces.

If the element type is a Windows Runtime type, the array is tracked via `TrackSzArrayType()` (specialized marshalling). Otherwise, it's treated as a user-defined type.

### Visitors (`Visitors/`)

Three singleton visitors implementing AsmResolver's `ITypeSignatureVisitor<T>` for recursive type traversal:

- **`AllGenericTypesVisitor`** — Extracts every `GenericInstanceTypeSignature` in a signature tree. Example: `List<(int, string, List<bool>)>` yields `List<(int, string, List<bool>)>`, `ValueTuple<int, string, List<bool>>`, and `List<bool>`.
- **`AllSzArrayTypesVisitor`** — Extracts every `SzArrayTypeSignature` in a signature tree.
- **`IsConstructedGenericTypeVisitor`** — Returns `false` if any `GenericParameter` node exists (open generic), `true` otherwise.

## Emit phase

The emit phase (`InteropGenerator.Emit.cs`, ~131 KB) is the largest file in the project. It generates the complete `WinRT.Interop.dll` assembly using AsmResolver.

### Emit pipeline

The emit method runs these steps sequentially:

1. **Module setup** — `DefineInteropModule()` creates the output module with a deterministic MVID (computed from input assembly content via `MvidGenerator`)
2. **Type hierarchy** — `WindowsRuntimeTypeHierarchyBuilder.Lookup()` emits a frozen hash table mapping runtime class names to types
3. **Default implementation details** — Built-in base types (vtable structures, etc.)
4. **Generic type processing** — 26 separate `Define*Types()` methods, one per interface family:
   - `DefineGenericDelegateTypes()` — `EventHandler<T>`, `TypedEventHandler<TSender, TArgs>`
   - `DefineIEnumeratorTypes()` through `DefineIAsyncOperationWithProgressTypes()` — all collection/async interfaces
   - `DefineSzArrayTypes()` — `T[]` array marshallers
5. **IL rewriting** — `RewriteMethodDefinitions()` (two-pass, see below)
6. **IL fixups** — `FixupMethodDefinitions()` (cleanup, see below)
7. **User-defined types** — `DefineUserDefinedTypes()` emits CCW code
8. **Dynamic custom-mapped types** — `DefineDynamicCustomMappedTypeMapEntries()` for `ICommand`, `INotifyPropertyChanged`, `INotifyCollectionChanged`
9. **Assembly attributes** — `[IgnoresAccessChecksTo]`, metadata attributes
10. **Write to disk** — `WriteInteropModuleToDisk()`

### What gets generated

For each discovered type, the generator emits some or all of these components:

**Per generic interface instantiation (e.g., `IList<string>`):**
- **IID property** — Static property returning the computed interface GUID
- **Vtable struct** — Sequential layout struct with unmanaged function pointer fields
- **Vtable static constructor** — Initializes vtable from `IInspectableImpl.Vtable` base, sets per-method function pointers
- **CCW method implementations** — `[UnmanagedCallersOnly]` methods for each interface method
- **Native object (RCW) type** — Sealed class deriving from the appropriate `WindowsRuntime*<T, TMethods>` base
- **Methods impl type** — Static abstract interface implementation with marshalling for type-specific methods
- **ComWrappers callback** — `IWindowsRuntimeUnsealedObjectComWrappersCallback` or `IWindowsRuntimeObjectComWrappersCallback` implementation
- **Marshaller type** — `ConvertToManaged`/`ConvertToUnmanaged` methods
- **ComWrappersMarshallerAttribute** — For opaque object marshalling
- **Proxy type** — `[WindowsRuntimeClassName]` + marshaller attribute for type map registration
- **Type map attributes** — `[TypeMap<*TypeMapGroup>]` attributes for all three type map groups
- **Element marshaller type** (when applicable) — Implements a `IWindowsRuntime*ElementMarshaller<T>` interface from the runtime, providing per-element conversion logic for collection `GetMany` CCW methods. Emitted by the `IEnumerator<T>` builder and reused by `IList<T>` and `IReadOnlyList<T>` builders. Not needed for blittable, `object`, `string`, `Type`, or `Exception` element types (these use specialized direct paths).

For additional details on generic interface code generation, see `references/marshalling-generic-interfaces.md`.

**Per SZ array type (e.g., `string[]`):**
- **Array element marshaller type** (when applicable) — Implements a `IWindowsRuntime*ArrayElementMarshaller<T>` interface from the runtime, providing bidirectional per-element conversion. Emitted for non-trivial element types: `KeyValuePair<K,V>`, `Nullable<T>`, managed value types, unmanaged value types, and reference types. Not needed for blittable value types, `object`, `string`, `Type`, or `Exception` (these use specialized direct marshallers).
- **Array marshaller** — `ConvertToManaged`/`ConvertToUnmanaged`/`CopyToManaged`/`CopyToUnmanaged`/`Free`. Delegates to the runtime's generic array marshaller classes, passing the generated element marshaller type as a generic type argument when applicable.
- **Array ComWrappers callback** — `IWindowsRuntimeArrayComWrappersCallback` implementation
- **Array impl type** — `IReferenceArray` vtable with `get_Value` CCW method
- **Interface entries impl type** — CCW interface entries for the array type
- **Proxy + marshaller attribute** — For opaque object marshalling

For additional details on array code generation, see `references/marshalling-arrays.md`.

**Per user-defined type:**
- **Interface entries type** — Struct listing all COM interface vtable entries
- **Interface entries implementation** — `ComputeVtables()` method returning vtable pointers
- **ComWrappersMarshallerAttribute** — Declares marshalling strategy

**Per generic delegate (e.g., `TypedEventHandler<object, string>`):**
- IID properties, vtable, native delegate type, CCW implementation, marshaller, impl type, reference impl type, proxy, type map attributes
- **Event source types** (when applicable) — `EventHandler1EventSource`, `EventHandler2EventSource`, `VectorChangedEventHandler1EventSource`, `MapChangedEventHandler2EventSource`

### Emit state (`InteropGeneratorEmitState`)

Thread-safe state container for the emit phase:

| Collection | Purpose |
|------------|---------|
| `_typeDefinitionLookup` | Generated types keyed by `(string key, TypeSignature)` |
| `_methodDefinitionLookup` | Generated methods keyed by `(string key, TypeSignature)` |
| `_methodRewriteInfos` | Queue of methods needing IL rewriting |
| `_mapViewVftblTypes`, `_mapVftblTypes`, `_delegateVftblTypes` | Vtable type caching for deduplication |

Frozen via `MakeReadOnly()` after emit completes.

### Builders (`Builders/`)

Builders construct complete type definitions with IL method bodies. They use a **static partial class pattern** for modularity.

**`InteropTypeDefinitionBuilder`** is the central builder, split into 20+ partial files:

| Partial file | Generates code for |
|--------------|-------------------|
| `InteropTypeDefinitionBuilder.cs` | Core: IID, NativeObject, ComWrappersCallback |
| `.Delegate.cs` | Generic delegate marshalling (vtable, native type, marshaller, impl, event source) |
| `.EventSource.cs` | Event source types for EventHandler and collection changed events |
| `.IEnumerator1.cs` | `IEnumerator<T>` methods impl, marshaller, CCW, element marshaller |
| `.IEnumerable1.cs` | `IEnumerable<T>` methods impl, marshaller, CCW |
| `.IList1.cs` | `IList<T>` full interface marshalling |
| `.IReadOnlyList1.cs` | `IReadOnlyList<T>` full interface marshalling |
| `.IDictionary2.cs` | `IDictionary<K,V>` full interface marshalling |
| `.IReadOnlyDictionary2.cs` | `IReadOnlyDictionary<K,V>` full interface marshalling |
| `.IObservableVector1.cs` | `IObservableVector<T>` marshalling |
| `.IObservableMap2.cs` | `IObservableMap<K,V>` marshalling |
| `.IMapChangedEventArgs1.cs` | `IMapChangedEventArgs<K>` marshalling |
| `.IAsyncOperation1.cs` | `IAsyncOperation<T>` marshalling |
| `.IAsyncActionWithProgress1.cs` | `IAsyncActionWithProgress<P>` marshalling |
| `.IAsyncOperationWithProgress2.cs` | `IAsyncOperationWithProgress<T,P>` marshalling |
| `.KeyValuePair.cs` | `KeyValuePair<K,V>` struct marshalling |
| `.ICollectionKeyValuePair2.cs` | `ICollection<KeyValuePair<K,V>>` marshalling |
| `.IReadOnlyCollectionKeyValuePair2.cs` | `IReadOnlyCollection<KeyValuePair<K,V>>` marshalling |
| `.SzArray.cs` | `T[]` array marshallers (element-type-specific strategies) |
| `.UserDefinedType.cs` | CCW interface entries + vtable |

**Other builders:**
- **`WindowsRuntimeTypeHierarchyBuilder`** — Emits a frozen hash table (keys/values/buckets as RVA static data) for O(1) runtime class name → type lookup
- **`DynamicCustomMappedTypeMapEntriesBuilder`** — Emits type map entries for dynamically custom-mapped interfaces (`ICommand`, `INotifyPropertyChanged`, `INotifyCollectionChanged`), with partial files per interface
- **`IgnoresAccessChecksToBuilder`** — Emits `[IgnoresAccessChecksTo("AssemblyName")]` attributes so the generated assembly can access internal members of referenced assemblies
- **`MetadataAssemblyAttributesBuilder`** — Copies metadata assembly attributes from the runtime assembly

### Factories (`Factories/`)

Factories create individual metadata elements (types, methods, properties, attributes):

- **`InteropCustomAttributeFactory`** — Creates `CustomAttribute` instances: `[Guid]`, `[UnmanagedCallersOnly]`, `[DisableRuntimeMarshalling]`, `[TypeMap<*>]`, `[AttributeUsage]`, `[IgnoresAccessChecksTo]`, `[AssemblyMetadata]`
- **`InteropMemberDefinitionFactory`** — Creates property/method definitions. Key pattern: `LazyVolatileReferenceDefaultConstructorReadOnlyProperty()` — a lazy-initialized static property using `Interlocked.CompareExchange` for thread-safe initialization
- **`InteropMethodDefinitionFactory`** — Per-interface method body factories (20+ partial files, e.g., `.IEnumerator1Impl.cs`, `.IList1Impl.cs`, `.IReadOnlyList1Impl.cs`). Generate IL method bodies for CCW and RCW methods. Key methods like `GetMany()` consume element marshaller types via `emitState.LookupTypeDefinition(elementType, "ElementMarshaller")`.
- **`InteropTypeDefinitionFactory`** — Per-category type definition factories (partial files):
  - **`.SzArrayElementMarshaller.cs`** — Emits concrete element marshaller types for SZ arrays (one per element type category: `UnmanagedValueType`, `ManagedValueType`, `KeyValuePair`, `NullableValueType`, `ReferenceType`). Generated types implement `IWindowsRuntime*ArrayElementMarshaller<T>` interfaces from the runtime.
  - **`.IEnumeratorElementMarshaller.cs`** — Emits concrete element marshaller types for collection interfaces (same 5 categories). Generated types implement `IWindowsRuntime*ElementMarshaller<T>` interfaces. Tracked in emit state for reuse by `IList<T>` and `IReadOnlyList<T>` method factories.
  - **`.SzArrayMarshaller.cs`** — Emits array marshaller types that forward to runtime array marshaller classes, passing the element marshaller type as a generic argument.
  - **`.IReadOnlyCollectionKeyValuePair2.cs`** — Emits `IReadOnlyCollection<KeyValuePair<K,V>>` types.
- **`WellKnownTypeDefinitionFactory`** — Creates fundamental vtable struct types (`IUnknownVftbl`, `IInspectableVftbl`, `DelegateVftbl`) with sequential layout and unmanaged function pointer fields
- **`WellKnownMemberDefinitionFactory`** — Creates IID properties (backed by RVA static data containing GUID bytes) and Vtable properties
- **`InteropUtf8NameFactory`** — Generates ABI-prefixed type/namespace names in `Utf8String` format

## Two-pass IL emit and fixups

### Why two passes?

IL generation requires forward references — a method body may need to call another method that hasn't been fully defined yet, or the marshalling code for a return value depends on knowing the exact ABI type of a parameter type that is resolved later. Two-pass generation solves this:

1. **Pass 1 (definition):** Create method bodies with **marker instructions** (`nop` or similar) at points where marshalling code will be inserted later. Track each marker in a `MethodRewriteInfo` object.
2. **Pass 2 (rewriting):** Process all collected `MethodRewriteInfo` objects, replacing markers with actual IL sequences.

### Method rewrite info (`Models/MethodRewriteInfo/`)

Each `MethodRewriteInfo` subclass describes a specific rewrite to apply:

| Variant | Purpose | Key fields |
|---------|---------|------------|
| `ReturnValue` | Marshal ABI return value → managed | `Source` (local variable with ABI value) |
| `RetVal` | Marshal indirect return value (out parameter) | `Source` local, parameter index |
| `RawRetVal` | Raw return value before processing | `Source` local |
| `ManagedParameter` | Convert ABI parameter → managed | `ParameterIndex` |
| `ManagedValue` | Convert value already on stack | (no extra fields) |
| `NativeParameter` | Convert managed → ABI with try/finally | `TryMarker`, `LoadMarker`, `FinallyMarker`, `ParameterIndex` |
| `Dispose` | Generate cleanup/dispose code | (no extra fields) |

All variants implement `IComparable` for deterministic ordering: by target method, then instruction offset, then type signature.

### Rewriters (`Rewriters/`)

Each rewriter is a nested static class in `InteropMethodRewriter` (partial class), one per `MethodRewriteInfo` variant:

- **`ReturnValue`** — Generates try/finally blocks around ABI → managed conversion. Handles blittable types (direct load), `KeyValuePair<K,V>` (struct marshalling), `Nullable<T>` (unboxing), strings (`HStringMarshaller`), exceptions (`ExceptionMarshaller`), reference types (marshaller + `WindowsRuntimeUnknownMarshaller.Free` cleanup).
- **`RetVal`** — Similar to ReturnValue but for indirect returns via out parameters.
- **`RawRetVal`** — Simplest: just loads the raw ABI value.
- **`ManagedParameter`** — Converts incoming ABI parameters to managed types. Blittable types: `ldarg` directly. Strings: `HStringMarshaller.ConvertToManaged`. Reference types: marshaller `ConvertToManaged`.
- **`ManagedValue`** — Like ManagedParameter but value is already on the evaluation stack.
- **`NativeParameter`** — Most complex: generates try/finally with three markers (try start, load point, finally start). Handles marshalling managed → ABI and cleanup on exception.
- **`Dispose`** — Generates cleanup IL: `HStringMarshaller.Free` for strings, marshaller `Dispose` for managed value types, `WindowsRuntimeUnknownMarshaller.Free` for reference types.

### IL fixups (`Fixups/`)

After rewriting, fixups clean up IL artifacts:

- **`RemoveLeftoverNopAfterLeave`** — Removes `nop` instructions that follow non-fall-through instructions (`leave`, `endfinally`, `throw`, etc.) inside protected regions. These are invalid per ECMA-335 and would cause verification errors.
- **`RemoveUnnecessaryTryStartNop`** — Removes `nop` instructions at the start of try regions that were used as markers during pass 1 but are no longer needed.

Both fixups use the base class helpers `ValidateExceptionHandlerLabels()`, `ValidateBranchInstructionLabels()`, and `RedirectLabels()` to safely update all instruction references when removing instructions.

## Signature and IID generation

### Type signatures (`Helpers/SignatureGenerator.cs`)

Windows Runtime type signatures are string representations of types used for IID computation. The generator builds them recursively:

**Primitive signatures** (from `SignatureGenerator.Primitives.cs`):
`i1` (sbyte), `u1` (byte), `i2` (short), `u2` (ushort), `i4` (int), `u4` (uint), `i8` (long), `u8` (ulong), `f4` (float), `f8` (double), `b1` (bool), `c2` (char), `string`, `g16` (Guid), `cinterface(IInspectable)` (object)

**Compound signatures** (from `SignatureGenerator.Projections.cs`):
- **Value types:** `struct(Windows.Foundation.Point;f4;f4)` — recursive field enumeration
- **Enums:** `enum(Windows.Foundation.AsyncStatus;i4)` — with underlying type
- **Delegates:** `delegate({iid})` — wraps the delegate's interface IID
- **Classes:** `rc(Windows.Foundation.Uri;{default-interface-iid})` — reference class with default interface
- **Interfaces:** `{iid}` — direct IID
- **Generic instances:** `pinterface({generic-iid};arg1-sig;arg2-sig;...)` — parameterized interface
- **Arrays:** Handled via element type signature

**Custom-mapped types** use hardcoded signatures from `TypeMapping.cs` (e.g., `DateTimeOffset` → `struct(Windows.Foundation.DateTime;i8)`).

### IID computation (`Helpers/GuidGenerator.cs`)

IIDs for generic instantiations are computed as RFC 4122 v5 (SHA1-based) GUIDs:

1. Start with the Windows Runtime PIID namespace GUID: `{D57AF411-737B-C042-ABAE-878B1E16ADEE}`
2. Encode the namespace GUID as 16 bytes (big-endian for the first three fields per RFC 4122)
3. Append the UTF-8 encoded type signature string
4. Compute SHA1 hash of the combined data
5. Take first 16 bytes of the hash
6. Set version bits to 5 (SHA1) and variant bits to RFC 4122
7. Adjust byte order for little-endian systems (swap bytes in the first three GUID fields)

This produces a deterministic GUID that is identical across all implementations that follow the same algorithm (the Windows Runtime standard).

### MVID generation (`Helpers/MvidGenerator.cs`)

The generated assembly's Module Version ID (MVID) is deterministic: it's computed by streaming all input assembly files (sorted alphabetically by path) through an incremental SHA1 hasher, then taking the first 16 bytes as a GUID. Same inputs = same MVID = reproducible builds.

## Resolvers (`Resolvers/`)

Resolvers locate types and methods across assemblies:

### `InteropImplTypeResolver`

Locates "Impl" types (containing IID properties and Vtable getters) for different type categories:

| Method | Looks in | Pattern |
|--------|----------|---------|
| `GetGenericInstanceTypeImpl()` | Generated `WinRT.Interop.dll` | `ABI.InterfaceIIDs.get_IID_*` + impl type's `get_Vtable` |
| `GetCustomMappedOrManuallyProjectedTypeImpl()` | `WinRT.Runtime.dll` | `ABI.{Namespace}.{Name}Impl` |
| `GetProjectedTypeImpl()` | Appropriate projection assembly | `ABI.{Namespace}.{Name}Impl` in Sdk.Projection / Projection |
| `GetComponentTypeImpl()` | `WinRT.Component.dll` | `ABI.{Namespace}.{Name}Impl` |
| `GetSzArrayTypeImpl()` | `WinRT.Runtime.dll` | Maps element type to specific `get_*ArrayVtable` getter |

### `InteropInterfaceEntriesResolver`

Builds the interface entries list for CCW (COM Callable Wrapper) types. Each user-defined type gets:

**Metadata entries** (variable, from discovered interfaces):
- Generic types → `GetGenericInstanceTypeImpl()`
- Custom-mapped/manually projected → `GetCustomMappedOrManuallyProjectedTypeImpl()`
- Projected types → `GetProjectedTypeImpl()`
- `[GeneratedComInterface]` types → `ComInterfaceEntryInfo` (uses `IIUnknownInterfaceType`)
- `[ExclusiveTo]` interfaces → `GetComponentTypeImpl()`

**Native entries** (fixed set of 6, always present):
1. `IStringable` (or user override)
2. `IWeakReferenceSource`
3. `IMarshal` (or user override if `[GeneratedComInterface]`)
4. `IAgileObject`
5. `IInspectable`
6. `IUnknown` (always last)

### `InteropMarshallerTypeResolver`

Locates the marshaller type for a given type signature. Resolution order:
1. `Nullable<T>` → get marshaller for underlying `T`
2. Generic types → look up `{TypeName}Marshaller` in `WinRT.Interop.dll`
3. `object` → use `WindowsRuntimeObjectMarshaller` from runtime
4. Fundamental/custom-mapped types → look up `ABI.{Namespace}.{Name}Marshaller` in `WinRT.Runtime.dll`
5. Projected types → look up marshaller in appropriate projection assembly

The returned `InteropMarshallerType` (a `readonly ref struct`) provides access to `ConvertToManaged()`, `ConvertToUnmanaged()`, `BoxToUnmanaged()`, `UnboxToManaged()`, and `Dispose()` method references.

## Type mapping (`Helpers/TypeMapping.cs`)

Central registry of managed ↔ Windows Runtime type mappings. Contains three `FrozenDictionary` instances:

- **`ProjectionTypeMapping`** (~70+ entries) — Maps managed type full names to `(Windows Runtime namespace, Windows Runtime name, optional signature)`. Includes all custom-mapped types from `.github/copilot-instructions.md` plus identical-name mappings for primitives.
- **`FundamentalTypeMapping`** — Maps primitive type names (Boolean→Boolean, Byte→UInt8, Int32→Int32, etc.)
- **`WindowsUIXamlTypeMapping`** — Alternative mappings when `CsWinRTUseWindowsUIXamlProjections = true` (e.g., `ICommand` → `Windows.UI.Xaml.Input.ICommand` instead of `Microsoft.UI.Xaml.Input.ICommand`)

Key methods:
- `TryFindMappedTypeName()` — Returns the Windows Runtime type name for a managed type
- `TryFindMappedTypeSignature()` — Returns the hardcoded Windows Runtime signature for a type (used by `SignatureGenerator`)

## References (`References/`)

- **`InteropReferences`** — Central registry of 100+ cached type/method references from core libraries (`System.Runtime`, `System.Memory`, etc.) and the `WinRT.Runtime` assembly. Properties are lazy-initialized (`??=` pattern).
- **`InteropDefinitions`** — Tracks generated type definitions in the output assembly. Includes `RvaFields` (for IID data), `InterfaceIIDs` (holder type for IID properties), and per-interface generated types.
- **`InteropNames`** — String constants for well-known assembly names (`WinRT.Runtime.dll`, `WinRT.Interop.dll`, etc.), including UTF-8 versions for zero-copy comparison.
- **`InteropValues`** — CsWinRT strong-name public key data.
- **`WellKnownInterfaceIIDs`** — Pre-computed GUIDs for ~60+ well-known interfaces plus a `ReservedIIDsMap` for the 6 fixed native interface entries.
- **`WellKnownPublicKeyTokens`** — Public key tokens for system assemblies.

## Diagnostics and error handling

### Error types

| Type | Purpose |
|------|---------|
| `WellKnownInteropException` | Structured errors with `CSWINRTINTEROPGEN###` codes (1–81+) |
| `WellKnownInteropWarning` | Non-fatal warnings; `LogOrThrow(treatWarningsAsErrors)` |
| `UnhandledInteropException` | Wraps unexpected errors with phase context; formats as `CSWINRTINTEROPGEN9999` |

### Error code categories

All factory methods are in `WellKnownInteropExceptions.cs` (38+ KB):

| Range | Category | Examples |
|-------|----------|---------|
| 001–002 | Runtime class names | Name too long, lookup size limit exceeded |
| 003–005 | Module loading | Assembly not found, WinRT runtime not found |
| 006–010 | Type hierarchy | RVA errors, implementation errors |
| 011–040 | Code generation | Per-interface-type errors (delegate, KVP, IEnumerator, IList, etc.) |
| 041–050 | Response file | Read error, argument parsing, malformed format |
| 051–055 | Debug repros | Directory not found |
| 056–060 | Version validation | Assembly version mismatch |
| 061–075 | Method rewriting | Missing body, marker not found, type mismatch, parameter index invalid |
| 076–081 | Tracked definitions | Duplicate type/method, lookup errors |
| 9999 | Unhandled | Unexpected errors (with GitHub issue link) |

### Error flow

1. Each phase wraps operations in try/catch
2. Known errors throw `WellKnownInteropException` with a descriptive message
3. Unknown errors are caught and wrapped in `UnhandledInteropException` with phase context
4. The main `Run()` method catches everything and formats the error for MSBuild consumption
5. Warnings use `LogOrThrow()` which either logs via `ConsoleApp.Log()` or throws based on `--treat-warnings-as-errors`

## Control flow between generated code and WinRT.Runtime

The generated `WinRT.Interop.dll` does not operate in isolation — it works in concert with `WinRT.Runtime.dll`. Understanding this relationship is critical:

**RCW (Runtime Callable Wrapper) flow:**
1. `WinRT.Interop.dll` generates a sealed native object class (e.g., `<string>NativeEnumerator`) that extends a base class from `WinRT.Runtime.dll` (e.g., `WindowsRuntimeEnumerator<T, TMethods>`)
2. The base class in the runtime handles state management, interface dispatch (`IDynamicInterfaceCastable`), and COM pointer lifetime
3. The generated `TMethods` type provides the type-specific marshalling (e.g., `Current` property marshalling `HSTRING*` → `string`)
4. The generated `ComWrappersCallback` type implements `IWindowsRuntimeUnsealedObjectComWrappersCallback` (from runtime) to create the appropriate native object instance

**CCW (COM Callable Wrapper) flow:**
1. `WinRT.Interop.dll` generates vtable structs and `[UnmanagedCallersOnly]` methods for each interface
2. Vtable initialization copies base entries from `IInspectableImpl.Vtable` (in `WinRT.Runtime.dll`) and sets interface-specific function pointers
3. The `ComputeVtables()` method (generated) returns an array of `ComInterfaceEntry` structs for `WindowsRuntimeComWrappers` (in runtime) to use
4. Interface entries reference both generated IIDs and vtables, plus the 6 fixed native entries from the runtime

**Type map registration:**
1. Generated code emits `[TypeMap<WindowsRuntimeComWrappersTypeMapGroup>]`, `[TypeMap<WindowsRuntimeMetadataTypeMapGroup>]`, and `[TypeMap<DynamicInterfaceCastableImplementationTypeMapGroup>]` attributes
2. These are consumed at runtime by `WindowsRuntimeComWrappers` for marshalling dispatch

For detailed control flow of generic interface marshalling, see `references/marshalling-generic-interfaces.md`. For array marshalling, see `references/marshalling-arrays.md`. For the generated type name mangling scheme, see `references/name-mangling-scheme.md`.

## Key patterns and conventions

### Naming conventions for generated types

Generated types use angle-bracket mangling to avoid conflicts with user types:
- Type names: `<AssemblyName>TypeName` (e.g., `<WinRT.Interop>IListImpl`)
- ABI namespace: `ABI.{original.namespace}` (e.g., `ABI.System.Collections.Generic`)
- `InteropUtf8NameFactory` handles all name generation

For the full specification of the name mangling scheme (including rules for primitives, generics, arrays, nested types, character substitutions, and a formal ANTLR4 grammar), see `references/name-mangling-scheme.md`.

### Thread safety

Both discovery and emit states use `ConcurrentDictionary` for thread-safe access during parallel processing. Object pooling (`ConcurrentBag<Builder>`) is used for `TypeSignatureEquatableSet.Builder` instances to reduce allocations.

### Determinism

- MVID is computed deterministically from input assembly content
- IIDs are computed deterministically from type signatures
- Type processing order is sorted for reproducible output
- Debug repros capture exact state for reproducible runs

### AsmResolver patterns

- Types are created as `TypeDefinition` with explicit `TypeAttributes`, `ClassLayout`, etc.
- Methods use `CilMethodBody` with explicit `CilInstruction` sequences
- RVA static data (IID bytes, type hierarchy data) is embedded via `DataSegment` + `FieldRva`
- References across assemblies use `MemberReference` with `ImportWith()` for resolution scope
