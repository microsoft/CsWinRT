# Migrating from CsWinRT 2.x to 3.0

CsWinRT 3.0 requires .NET 10 or later and is not binary compatible with CsWinRT 2.x projections. Updating `Microsoft.Windows.CsWinRT` alone does not migrate an application. Audit its projection dependencies, select the appropriate Windows target-framework revision, repair affected source, and validate the resulting application. Ordinary managed libraries do not necessarily need rebuilding; libraries shipping legacy projections or depending on removed runtime infrastructure do.

## Choose an entry point

**Preflight-first (recommended):** preserve a working baseline, inspect dependencies and the rules below, choose in-place migration or multi-targeting, then retarget and restore. This avoids changing an application to 3.0 before discovering that a required projection has no compatible package.

**TFM-first:** preserve a baseline, change or add the `.1` target, restore, and repair the resulting compilation. The supported SDK selects the 3.0 Windows reference projections. When its package dependency graph brings in `Microsoft.Windows.CsWinRT`, the package also supplies the migration code fixes. Check the actual restored assets: explicit 2.x package pins, incompatible third-party projections, older SDKs, and excluded analyzer assets can prevent this path from working. No old semantic snapshot or separate migration package is required by the typed-event fix.

```xml
<!-- Keep the same supported Windows SDK build when switching interop stacks. -->
<TargetFramework>net10.0-windows10.0.22621.1</TargetFramework>
```

The corresponding `.0` revision selects CsWinRT 2.x. Do not change the application's minimum supported Windows version just to select CsWinRT 3.0. Verify the installed .NET SDK, Windows SDK reference package, WindowsAppSDK packages, and CsWinRT package together; use compatible versions from the intended release channel rather than blindly selecting the latest preview. An explicit `LangVersion` lower than C# 14 can prevent span-based source compatibility.

The current tested combination of .NET SDK `10.0.403` and `Microsoft.Windows.SDK.NET.Ref` `10.0.26100.85-preview` selects the 3.0 Windows reference assembly without implicitly restoring the main CsWinRT package. For such SDK/package combinations, add an explicit package reference with the intended 3.0 version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Windows.CsWinRT" Version="$(CsWinRTVersion)" />
</ItemGroup>
```

Set `CsWinRTVersion` to the compatible version being adopted. This reference installs the runtime/build support and the bundled fixer; it is not a separate migration-tool package. Do not assume a future implicit dependency is present until the restored assets establish it.

## Applications: preflight and verification

1. Save a baseline commit or diff and run the existing build/tests. Record failures already present.
2. Inspect each target framework, imported `Directory.Build.props`/`.targets`, central package versions, project references, transitive packages, and raw DLL references. Identify which dependencies contain 2.x projections or runtime-specific code.
3. Find supported 3.0 versions of those dependencies, or rebuild projections you own. Report unavailable dependencies as blockers; do not hide them with binding redirects or copy old projection implementations into the new build.
4. Select in-place migration or retained 2.x targets. Retarget and restore. Verify `CSWINRT3_0` on the 3.0 compilation, the selected reference assets, and `WinRT.Runtime` package identity.
5. Apply reviewed source changes. Inspect public signatures, interface casts, native pointer ownership, equality, and precision independently of compiler errors.
6. Build and run the existing tests. Publish and launch representative JIT/trimmed/Native AOT configurations that the project supports. Confirm required generated projection/interop assemblies or their native compiled equivalents are present.
7. Rebuild every retained target and check the diff for unrelated changes. A successful C# compilation is not sufficient evidence of migration.

## Migration rules

The rule names below are stable references for assessment and review, not compiler diagnostic IDs. Separate mechanical fixes from changes requiring explicit semantic approval.

| Rule | Detection and action | Boundary / validation |
|---|---|---|
| `MIG-TFM` | Select .NET 10 or later and the supported Windows `.1` revision; update conflicting SDK/package pins at their source declarations. | Preserve the Windows minimum and all intended targets. Restore and inspect selected assets per TFM. |
| `MIG-DEPENDENCIES` | Identify 2.x projections and runtime-bound libraries in the resolved dependency graph. Upgrade, regenerate owned projections, or report a blocker. | Do not reject all older managed libraries. No binary compatibility shim exists between the projection models. |
| `MIG-TYPED-EVENT` | Map `Windows.Foundation.TypedEventHandler<TSender,TArgs>` to `System.EventHandler<TSender,TArgs>` on 3.0. | Preserve event invocation and public API intent; maintain conditional source for 2.x. The bundled fix handles only proven missing-type sites. |
| `MIG-ARRAYS` | Review implemented/overridden array-parameter signatures: input arrays become `ReadOnlySpan<T>`, caller-allocated output arrays become `Span<T>`, and callee-allocated output remains `out T[]`. | Ordinary array arguments often still compile with C# 14. Do not rewrite array properties/return values or retained/captured arrays to spans indiscriminately. Inspect method groups, overloads, null handling, and async capture. |
| `MIG-STRUCTS` | Review projected struct members becoming fields, and `Point`/`Rect`/`Size` exposing float fields. | Use verified constructor and coordinate narrowing fixes where they reproduce the 2.x boundary. Review reflection, binding assumptions, dimension validation, public API, and changed arithmetic; do not convert every property automatically. |
| `MIG-CASTS` | Classify legacy `As<T>()` calls as instance interface casts versus activation-factory access. | A supported projected-interface cast is not equivalent to arbitrary ComImport or restricted exclusive-interface access. Preserve exceptions and native support requirements. |
| `MIG-ABI` | Review removed `FromAbi`, `FromManaged`, `IWinRTObject`, and ABI/runtime helpers. Use public `WindowsRuntimeMarshal` APIs only where contracts match. | Confirm pointer type, IID, null behavior, ownership, thread/context use, and cleanup on failure. Do not replace calls by spelling alone. |
| `MIG-EQUALITY` | Identify legacy native-identity equality, `IEquatable<T>`, operators, and dictionary/set use. | 3.0 runtime classes use reference equality. `WindowsRuntimeMarshal.NativeReferenceEquals` provides opt-in native identity comparison, not a replacement hash-code contract. |
| `MIG-ANNOTATIONS` | Use the bundled attribute migration fixes to remove redundant 2.x markers or replace verified equivalents. | Preserve legacy annotations conditionally for retained targets. Custom vtables, projection metadata, and unmatched array directions require review; genuine partial declarations remain. |
| `MIG-PROJECTION-PACK` | Convert owned projection libraries to reference projections and distribute matching ref/forwarder assets plus owned metadata. | Never make component implementation assemblies reference-only. Test clean external consumers and multi-target package selection. |
| `MIG-AUTHORING` | Review component fields, array direction, exported signatures, versioning, activation, and hosting. | Compare WinMD API shape and exercise native consumption/activation. Component build success alone is insufficient. |
| `MIG-UNSUPPORTED` | Identify embedded mode or targets below .NET 10. | Retain 2.x or restructure the project into a supported configuration; do not promise an in-place 3.0 equivalent. |

### Typed events and bundled IDE fixes

```csharp
// CsWinRT 2.x
Windows.Foundation.TypedEventHandler<object, int> handler = (sender, value) => Handle(value);

// CsWinRT 3.0
global::System.EventHandler<object, int> handler = (sender, value) => Handle(value);
```

The package's `WinRT.Migration.CodeFixes` assembly offers **Migrate to System.EventHandler<TSender, TEventArgs>** for missing-type compiler diagnostics (`CS0234`/`CS0246`) on a compilation defining `CSWINRT3_0`. It handles explicit qualification, resolved namespace aliases, and unqualified names imported from `Windows.Foundation`. It requires the two-argument delegate in the target .NET framework, declines bound legacy or competing types, and excludes conventionally generated files. Fixes never run merely because the package was restored.

Review the light-bulb preview before applying a fix or Fix All. Shared source must also compile for the other targets; the current document's compilation cannot prove the compatibility of projects that are not loaded. Missing names without a proven Foundation namespace/import are left for review. A still-bound legacy delegate usually indicates an incompatible projection dependency rather than a missing 3.0 name.

The existing SDK formatter can also discover this packaged provider. After reviewing the affected files, run `dotnet format analyzers MyProject.csproj --diagnostics CS0234 CS0246 --no-restore --include ApprovedFile.cs` on the 3.0 project. Inspect the diff and build every target afterward. These broad compiler IDs can also invoke other installed providers; the CsWinRT fixer still filters each site conservatively. There is no need for a separate migration CLI for this first supported transformation.

### Legacy attribute fixes

The source generator reports `CSWINRT2051` (an informational migration suggestion) for supported legacy attribute applications on the 3.0 target. It recognizes both bound 2.x attributes and proven missing names after retargeting, including aliases and the generated dynamic-cast polyfill. Unrelated same-named source attributes are not migration candidates.

| Legacy attribute | 3.0 action |
|---|---|
| `WinRT.DynamicWindowsRuntimeCast` | Remove; discovery is handled by the 3.0 interop pipeline. |
| `WinRT.GeneratedWinRTExposedType` | Remove; managed CCW discovery is automatic. |
| `WinRT.WinRTExposedType` with no arguments | Remove; default CCW support does not need the marker. |
| `WinRT.GeneratedWinRTExposedExternalType(typeof(T))` | Remove for a resolved managed class/struct; replace with `WindowsRuntime.InteropServices.WindowsRuntimeNativeExposedType` for an eligible projected class. |
| `WinRT.WinRTRuntimeClassName` | Replace with `WindowsRuntime.WindowsRuntimeClassName`, retaining arguments. |
| `WinRT.GeneratedBindableCustomProperty` | Replace with `WindowsRuntime.Xaml.GeneratedCustomPropertyProvider`, retaining constructor arguments and required partial declarations. |
| `WinRT.ProjectedRuntimeClass` / `WinRT.ObjectReferenceWrapper` | Review only. Regenerate legacy projections with 3.0 tooling; handwritten wrappers require contract review. Inheriting from `WindowsRuntimeObject` does not establish a supported wrapper or make attribute deletion safe. |
| `ReadOnlyArray` / `WriteOnlyArray` in `System.Runtime.InteropServices.WindowsRuntime` | Convert supported array parameters to `ReadOnlySpan<T>` / `Span<T>` using `CSWINRT2053`; remove the old attribute. If the matching span signature is already present, `CSWINRT2051` removes just the attribute. |

For known 3.0-only projects the action removes or replaces the annotation directly. When evaluated `TargetFrameworks` contains a retained legacy target, the action emits a balanced conditional branch preserving the original attribute and its arguments. The package normalizes whitespace (including multiline target lists) and comma-encodes the complete evaluated list for editorconfig; single-target projects use their evaluated `TargetFramework`. If target metadata is unavailable or empty, the light bulb offers explicit preservation and 3.0-only actions instead of guessing. Fix All edits each selected attribute list once, including mixed lists, and preserves unrelated attributes and surrounding directives. Dangling legacy imports are removed or guarded only when the remaining compilation errors establish that this is safe.

```csharp
#if CSWINRT3_0
[global::WindowsRuntime.WindowsRuntimeClassNameAttribute("Contoso.Widget")]
#else
[WinRT.WinRTRuntimeClassName("Contoso.Widget")]
#endif
public partial class Widget { }
```

Batch application uses `dotnet format analyzers MyProject.csproj --diagnostics CSWINRT2051 --severity info --no-restore --include ApprovedFile.cs`. For a multi-target project, ensure the formatter loads the 3.0 compilation, then build every retained target. The `--severity info` option includes the migration suggestions. Run this precise attribute rule family before broad compiler-error fixes, in separate formatter passes; mixing providers over the same broken document can leave incomplete fixes.

`CSWINRT2052` reports review-required cases without a speculative fix: custom `WinRTExposedType` details (including `WinRTManagedOnlyTypeDetails`, whose throwing behavior differs from 3.0's opaque managed-only marshalling), `WindowsRuntimeType`, `WindowsRuntimeHelperType`, RCW factories, assembly export metadata, WUX/MUX projection markers, unresolved external types, and array-direction mismatches. Regenerate owned projections or review the corresponding marshalling/authoring contract; never replace these with implementation-only attributes simply to make the source compile.

### Attributed array-to-span conversions

`CSWINRT2053` names the specific 3.0 mapping for a legacy single-dimensional array parameter: `[ReadOnlyArray] T[]` becomes `ReadOnlySpan<T>`, and `[WriteOnlyArray] T[]` becomes `Span<T>`. Supported sites offer **Convert array parameter to span**, with the same direct-versus-retained-target choice as the attribute fixes. The edit changes the parameter type and removes only its direction attribute; it does not convert array properties, returns, `out T[]`, or arbitrary array variables.

```csharp
// Before
private int Count([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] values) => values.Length;

// After, for a 3.0-only project
private int Count(global::System.ReadOnlySpan<int> values) => values.Length;
```

For retained 2.x targets, the parameter itself gets a conditional branch: the 3.0 branch has the span type, while the legacy branch keeps the original attribute and array type. The method body is shared. Supported automatic conversions are synchronous nonvirtual methods and ordinary constructors with independent signatures. The body may use length, read input elements/iterate readonly input, write output elements, or pass through the corresponding existing span API. Mutating receivers on input struct elements require review: readonly span indexing could otherwise invoke a method or getter on a defensive copy, silently changing behavior. Reference-type receivers and verified readonly struct/member calls retain their existing semantics. The fixer trial-compiles the change and checks call operations and overload binding in the project and loaded dependent projects, including constructor chains, primary-constructor base calls, and implicit collection-initializer `Add` calls.

The concrete mapping suggestion remains available when no safe local fix exists. Loaded callers must pass a directly constructed array or an unmodified local initialized with an array construction; nullable annotations alone do not prove non-nullness. Writable `Span<T>` callers additionally need an exact match between the constructed array's element type and the instantiated `T`: `object[] items = new string[2]` is not safe for `Span<object>`, even though the local is declared `object[]`, because span construction would throw `ArrayTypeMismatchException`. Readonly spans retain their supported covariance. Method-group references (including inferred delegates such as `var function = Process`) require review because changing the inferred delegate signature would bypass validation of its eventual callers. Dynamic member calls with a matching name and dynamically dispatched construction of the affected type are conservatively review-only: the runtime binder cannot establish non-nullness or array element identity at compile time. Method/property results, parameters, reassigned locals, reference escapes, and captures require review because converting a null array to a span would silently turn it into an empty span. Collection expressions populating a custom collection through the changed `Add` method remain review-only because their implicit population calls are not exposed as invocations by Roslyn's operation model. Async/iterator methods, partial/virtual/interface/override contracts, `ref`/`out`/`in`/`params` or optional parameters, nullable/null-sensitive uses, escaping or captured arrays, output-buffer reads, conflicting direction attributes, changed overload selection, and introduced compilation errors require review. External consumers and projects not loaded into the workspace cannot be validated automatically; review public API changes and rebuild every supported target.

Run `dotnet format analyzers MyProject.csproj --diagnostics CSWINRT2053 --severity info --no-restore --include ApprovedFile.cs` after the `CSWINRT2051` attribute pass and before the broad compiler-error pass. The separate rule passes avoid competing document rewrites, and an unsupported suggestion is not permission to suppress its underlying errors.

### Geometry narrowing fixes

CsWinRT 2.x already stored `Point`, `Size`, and `Rect` components as floats. Its double-taking constructors cast their arguments to float before delegating to the float constructors, including dimension validation. The 3.0 constructors accept floats directly, and components are exposed as float fields rather than double properties.

The bundled **Preserve CsWinRT 2.x geometry narrowing** action handles constructor conversion errors (`CS1503`) and simple `Point.X`/`Point.Y`/`Rect.X`/`Rect.Y` assignments or object initializers (`CS0266`/`CS0664`). It targets exact metadata-defined `Windows.Foundation` structs from `WinRT.Runtime`, not WPF or unrelated geometry types. It casts the complete double expression, preserving evaluation order and double calculations within that expression:

```csharp
// Before
new Windows.Foundation.Point(element.ActualWidth / 3, element.ActualHeight / 3);
point.X = element.ActualWidth / 3;

// After
new Windows.Foundation.Point((float)(element.ActualWidth / 3), (float)(element.ActualHeight / 3));
point.X = (float)(element.ActualWidth / 3);
```

When a constructor has mixed numeric arguments, the fix also preserves the old double overload's widening before narrowing: a `long` argument can become `(float)((double)(value))`, rather than `(float)(value)`, because the two rounding paths can differ. All arguments of the selected constructor are handled together; named arguments, aliases, target-typed `new`, comments, and supported nested geometry constructors are retained. The fixer verifies the new compilation and surrounding call bindings before offering the action. Fix All reevaluates diagnostics after each edit, and generated source is excluded.

These casts can remain shared with retained 2.x targets because they reproduce the old constructor/coordinate-setter conversions; build both targets afterward. They do not restore arithmetic that already changed because geometry getters now expose floats. Constructor casts cannot recover lost precision, preserve double-valued state, or substitute for review of floating-point calculations.

`Size.Width`/`Size.Height` and `Rect.Width`/`Rect.Height` assignments remain outside automatic fixes: their old setters validated the double before casting, whereas assignment to the new field has no validation. The fixer also declines compound or value-producing assignments, arbitrary numeric APIs, nullable/dynamic/decimal or user-defined constructor conversions, direct `Deconstruct(out double ...)` calls, and unsupported binding/error contexts. No blanket arithmetic rewrite or runtime double-overload addition is included.

After the attribute, array, and typed-event passes, run `dotnet format analyzers MyProject.csproj --diagnostics CS1503 CS0266 CS0664 --no-restore --include ApprovedFile.cs` separately on the 3.0 compilation. Inspect the diff, since other installed providers can handle these compiler IDs. Verify numerical boundary behavior, all retained targets, and formatter idempotence.

### Native conversions require ownership review

`WindowsRuntimeMarshal.ConvertToManaged(void*)` does not take ownership of its input pointer. A native object must implement `IInspectable`, unless the pointer is a recognized managed CCW. The conversion can return the projected object rather than the original authored implementation. Use `TryGetManagedObject` when that is the required contract.

`WindowsRuntimeMarshal.ConvertToUnmanaged(object, in Guid)` returns the requested interface pointer with an owned reference; release it with `WindowsRuntimeMarshal.Free` when finished. The overload without an IID returns an unknown-interface pointer, not necessarily the ABI interface the old caller expected. Never add an extra release or omit a required one as part of a mechanical rename.

These are review rules, not currently bundled code fixes. See [interop](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/interop.md) and the public API documentation for the complete contracts.

## Preserve CsWinRT 2.x through multi-targeting

```xml
<TargetFrameworks>net10.0-windows10.0.22621.0;net10.0-windows10.0.22621.1</TargetFrameworks>
```

Condition package versions, projection-generation settings, and incompatible source on the actual target. The SDK defines `CSWINRT3_0` for the 3.0 target:

```csharp
#if CSWINRT3_0
using Handler = System.EventHandler<object, int>;
#else
using Handler = Windows.Foundation.TypedEventHandler<object, int>;
#endif
```

Closed aliases can keep a shared declaration readable. Open-generic uses and public signatures may require conditional declarations. Do not unconditionally introduce the two-argument .NET delegate into a retained target that does not provide it. Do not automatically surround every edit with preprocessor directives; choose the smallest deliberate compatibility shape and compile both branches.

The existing `CsWinRTGenerateVersion2PolyfillAttributes` option provides a narrow `DynamicWindowsRuntimeCastAttribute` multi-targeting aid. It is not a runtime/projection compatibility layer. Keep 2.x annotations where they remain required.

## Projection-library publishers

Generate a reference projection with `CsWinRTGenerateReferenceProjection=true` on the projection library, not on a component implementation. Preserve include/exclude intent and strong-name/public identity requirements. The package must contain matching `ref\<tfm>` reference assemblies, `lib\<tfm>` forwarders, and the metadata the consumer's app-time generation needs.

List metadata owned by the package explicitly in `CsWinRTPackedWinMD`. `CsWinRTInputs` also includes dependency metadata; packing it all can redistribute metadata belonging to other packages. Preserve access to dependent metadata without duplicating ownership. Windows SDK metadata is handled by the supported SDK projection path.

Pack the library, restore it into a clean application with no producer project reference, publish, and exercise a projected API. If publishing both interop stacks, check that NuGet selects the right assets for `.0` and `.1`. See [usage](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/usage.md) for reference projection generation and distribution.

## Windows Runtime component authors

Keep the component's implementation code and retain `CsWinRTComponent=true`. Do not enable reference-projection mode on it. A separate library can distribute a reference projection of its WinMD.

Review exported struct fields, namespace/type eligibility, versioning, overload metadata, array directions, and authoring diagnostics. `Span<T>` fill-array parameters are write-only: the implementation must not read the caller's existing contents. Validate managed consumption as well as WinMD API compatibility, activation, native host architecture, and JIT/Native AOT hosting where applicable. See [authoring](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/authoring.md), [hosting](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/hosting.md), and [implementing existing WinMD types](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/implementing-winmd-types.md).

For Native AOT shared libraries, follow the authoring deployment settings and verify the type-map root in the evaluated build:

```xml
<PropertyGroup Condition="'$(PublishAot)' == 'true'">
  <NativeLib>Shared</NativeLib>
  <SelfContained>true</SelfContained>
  <TypeMapEntryAssembly>$(AssemblyName)</TypeMapEntryAssembly>
</PropertyGroup>
```

CsWinRT's targets already default `TypeMapEntryAssembly` to `$(AssemblyName)` for Native AOT libraries; an explicit setting is normally unnecessary. The SDK/compiler must support this property and use the intended assembly as the type-map root. Verify activation and event subscription/removal from a clean native consumer, not merely publish success. Do not silently change the framework or release channel, set `OutputType=Exe` for a shared library, or try to repair build-time type-map configuration with a runtime entry-assembly initializer.

## Optional Copilot Upgrade assistance

GitHub Copilot Upgrade supports [custom upgrade instructions](https://learn.microsoft.com/en-us/dotnet/core/porting/github-copilot-upgrade/dotnet-how-to-custom-upgrade-instructions) and [custom skills](https://learn.microsoft.com/en-us/dotnet/core/porting/github-copilot-upgrade/dotnet-scenarios-and-skills). Use the guide without Copilot, or install this repository's self-contained migration skill into the consuming repository:

```powershell
# Run from the CsWinRT checkout. Existing skill files are not overwritten by default.
& .\.github\skills\cswinrt-migration\scripts\Install-MigrationSkill.ps1 -RepositoryPath 'D:\src\MyApplication'
```

Then ask Upgrade: **Migrate this solution from CsWinRT 2.x to 3.0 using the cswinrt-migration skill; assess dependencies before changing target frameworks.** If already retargeted, say so; do not revert solely to run an assessment. Verify that the skill was retrieved and that the assessment records its rules, dependency blockers, target choice, and per-category semantic approvals.

The installer copies `SKILL.md` and this guide into the consumer's `.github\skills\cswinrt-migration` folder. A NuGet dependency does not automatically install a Copilot skill. The skill guides assessment and reviewed execution; it does not register a first-class scenario with Upgrade's discovery service or guarantee automatic invocation of IDE code fixes. Those integration contracts must be validated with the Upgrade maintainers.

If Upgrade's execution service is unavailable, distinguish connection failures from migration/build errors and retry through supported controls. Ordinary Copilot with the same skill and the SDK formatter can carry out already-approved edits. Record fallback validation separately; never edit Upgrade's private state to mark failed tasks complete or weaken workspace trust to bypass a connection failure.

The old .NET Upgrade Assistant is [deprecated](https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview); do not build a new extension against it. A separate migration CLI is intentionally deferred until batch/offline needs justify it.
