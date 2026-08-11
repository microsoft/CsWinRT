# Attribute projections

## Overview

Most Windows Runtime attributes are projected like any other Windows Runtime type: the attribute type itself is projected into C#, and every application of it is carried over onto the projected member. A handful are **custom-projected** instead: CsWinRT replaces them with a .NET attribute that models the same concept, or consumes them without ever emitting them.

There are two reasons to do that:

- **A .NET counterpart already exists.** `[Windows.Foundation.Metadata.Deprecated]` and `[Obsolete]`, or `[Windows.Foundation.Metadata.Experimental]` and `[Experimental]`, mean the same thing. Projecting the Windows Runtime one as-is would leave the .NET tooling (compilers, analyzers, IDEs) unable to act on it.
- **The attribute is metadata for CsWinRT itself.** `[Guid]` becomes the interface IID, `[AllowMultiple]` becomes part of the projected `[AttributeUsage]`, and `[Default]`, `[ExclusiveTo]`, `[Activatable]` and friends shape the generated code. Carrying them over would add permanent metadata that nothing reads.

This document is the reference for those cases, in both directions: `.winmd` → C# (**projection**, what a consumer sees) and C# → `.winmd` (**authoring**, what a component author writes). Everything not listed explicitly is covered by the catch-all row at the end of each table.

## Projection (`.winmd` → C#)

What CsWinRT emits for a Windows Runtime attribute application when projecting Windows Runtime metadata into C#.

The **Modes** column says which projection assemblies the result lands in. Reference projections are what user code, compilers, analyzers and metadata tooling compile against; implementation projections are only ever loaded at runtime. Attribute blobs cannot be trimmed by ILLink or ILC, so anything emitted into an implementation projection is permanent, unremovable metadata in the shipped application. Anything whose only consumer is a compiler or analyzer is therefore reference-projection-only.

| Windows Runtime metadata | Projected as | Modes |
| --- | --- | --- |
| `[Windows.Foundation.Metadata.Deprecated(message, DeprecationType.Deprecate, ...)]` | `[System.Obsolete(message)]` | both |
| `[Windows.Foundation.Metadata.Deprecated(message, DeprecationType.Remove, ...)]` | *nothing* — a removed member is omitted from the projection, and its ABI vtable slot is preserved but stubbed to return `E_NOTIMPL`; a removed type is omitted from the projection and the ABI alike | both |
| `[Windows.Foundation.Metadata.Experimental]` | `[System.Diagnostics.CodeAnalysis.Experimental("CSWINRT3005", UrlFormat = ..., Message = ...)]`, see [CSWINRT3005](diagnostics/cswinrt3005.md) | reference only |
| `[Windows.Foundation.Metadata.AttributeUsage(targets)]` | `[System.AttributeUsage(targets)]`, with `Windows.Foundation.Metadata.AttributeTargets` mapped to `System.AttributeTargets` | both |
| `[Windows.Foundation.Metadata.AllowMultiple]` | *nothing on its own* — it is folded into the `AllowMultiple` argument of the projected `[AttributeUsage]` (which is synthesized if the metadata declares none) | both |
| `[Windows.Foundation.Metadata.Guid(...)]` | `[System.Runtime.InteropServices.Guid("...")]`, as the IID of the projected interface or delegate | both |
| `[Windows.Foundation.Metadata.ContractVersion(contract, version)]` | itself, plus a synthesized `[System.Runtime.Versioning.SupportedOSPlatform("WindowsX.Y.Z.0")]` for the first contract that maps to a Windows release | reference only |
| `[Windows.Foundation.Metadata.GCPressure]`, `[Windows.Foundation.Metadata.Version]`, and every other `Windows.Foundation.Metadata` attribute not listed above | *nothing* — they are either consumed to shape the generated code (`[Default]`, `[ExclusiveTo]`, `[Activatable]`, `[Static]`, `[Composable]`, `[Overridable]`, `[FastAbi]`, ...) or have no meaning in a .NET projection | — |
| `[Windows.Foundation.Metadata.Overload(name)]`, `[Windows.Foundation.Metadata.DefaultOverload]`, and attributes from any other namespace (e.g. `[Microsoft.UI.Xaml.TemplatePart]`) | themselves — those attribute types are projected too, so their applications are carried over unchanged | reference only |

Two consequences of the custom-mapped rows are worth calling out, because they are the ones that show up as "missing" types:

- `Windows.Foundation.Metadata.ExperimentalAttribute`, `AttributeUsageAttribute` and `AttributeTargets` are **not** projected as types, since their .NET counterparts take their place. `ApiContractAttribute` and `ContractVersionAttribute` are not projected either: they are shipped by `WinRT.Runtime.dll` instead, so that projections can apply them without redefining them.
- `DeprecatedAttribute`, `DeprecationType`, `OverloadAttribute`, `DefaultOverloadAttribute`, `VersionAttribute` and the rest of `Windows.Foundation.Metadata` **are** projected as ordinary types, so component authors can apply them (see the next section).

Generated projection code suppresses `CS0612`/`CS0618` and `CSWINRT3005`: a projection has to name a deprecated or experimental type in order to project it at all, so those markers are guidance for the consumers of a projection, not for the projection itself. The suppressions are per file, so user code calling such an API still gets the diagnostic.

## Authoring (C# → `.winmd`)

What the WinMD generator emits into an authored component's `.winmd` for an attribute applied in C#.

| C# attribute | Emitted into the `.winmd` as | Notes |
| --- | --- | --- |
| `[System.Diagnostics.CodeAnalysis.Experimental(id, ...)]` | `[Windows.Foundation.Metadata.Experimental]` | The diagnostic id, `UrlFormat` and `Message` are dropped: Windows Runtime metadata has nowhere to carry them, and CsWinRT synthesizes its own when the component is projected back. On a property or event the attribute is emitted on the accessor method, matching MIDL. On an assembly, a module or a constructor it is dropped, and reported as [CSWINRT2021](#cswinrt2021-unsupported-experimental-targets). |
| `[Windows.Foundation.Metadata.Deprecated(...)]` | itself | On a property or event it is emitted on the accessor method, matching MIDL. |
| `[System.Runtime.InteropServices.Guid("...")]` | `[Windows.Foundation.Metadata.Guid(...)]` | Without one, the IID is derived from the type name (UUID v5, as MIDL does). |
| `[System.AttributeUsage(targets)]` | `[Windows.Foundation.Metadata.AttributeUsage(targets)]`, with `System.AttributeTargets` mapped to `Windows.Foundation.Metadata.AttributeTargets` | |
| `[System.Flags]` | itself | Windows Runtime metadata uses the same `System.FlagsAttribute` for flag enums. |
| `[Windows.Foundation.Metadata.Version(v)]` | itself | Emitted from the value the author specified, or the component's assembly major version when absent. |
| `[Windows.Foundation.Metadata.Overload(name)]` | itself | Emitted with the author-specified name, or a generated one for overloads that need disambiguating. |
| `[WindowsRuntime.Xaml.GeneratedCustomPropertyProvider]`, `[System.Reflection.DefaultMember]`, and anything under `System.Runtime.CompilerServices` | *nothing* | Either handled by CsWinRT itself or meaningless in Windows Runtime metadata. |
| Any other public attribute type | itself | Non-public attribute types, and attributes whose signature cannot be read, are skipped. |

> **Note**: `[System.Obsolete]` is **not** translated into `[Windows.Foundation.Metadata.Deprecated]`. It is copied as-is, so it is invisible to every other language projection. Use `[Windows.Foundation.Metadata.Deprecated]` to deprecate an API of an authored component; applying `[Obsolete]` without it is reported as [CSWINRT2022](#cswinrt2022-obsolete-without-deprecated). `[Experimental]` is the exception to that rule only because the Windows Runtime attribute has no projected form to apply.

### CSWINRT2021: unsupported `[Experimental]` targets

The .NET `[Experimental]` attribute supports more targets than the Windows Runtime one it is translated into. Types (runtime classes, interfaces, structs, enums and delegates), methods, properties, events and fields (individual enum members and struct fields) all translate; **assemblies**, **modules** and **constructors** have no Windows Runtime metadata target that can carry the marker:

- An assembly or a module has no counterpart at all: the generator produces a fresh `.winmd` containing only the authored types.
- A constructor is exposed through an activation factory method (`IFooFactory.CreateFoo`), and the `.ctor` row on the runtime class is not where markers live. MIDL never emits one there, and no `.ctor` row in the Windows SDK carries a `[Deprecated]` or `[Experimental]` attribute.

Rather than emit the marker where nothing would read it, which would silently make the API look stable to every other language projection, those applications are dropped and `CSWINRT2021` reports them at the source. Mark the whole runtime class as experimental to cover its constructors.

### CSWINRT2022: `[Obsolete]` without `[Deprecated]`

`[Obsolete]` is *the* way to deprecate an API in C#, so it is the natural thing to reach for in an authored component. It is not translated into `[Windows.Foundation.Metadata.Deprecated]` though: it is copied verbatim, so the `.winmd` ends up carrying a `System.ObsoleteAttribute` reference that no other language projection understands. The component still builds and works, and the deprecation simply never reaches any consumer.

`CSWINRT2022` reports a publicly exposed API that has `[Obsolete]` but no `[Deprecated]`. Applying both is the supported way to deprecate an API for .NET and Windows Runtime consumers alike, and silences the diagnostic:

```csharp
[Obsolete("Use NewMethod instead")]
[Deprecated("Use NewMethod instead", DeprecationType.Deprecate, 1)]
public void OldMethod()
{
}
```

Only APIs that actually reach the `.winmd` are reported, so that every report has an action available. That includes individual **enum members** and **struct fields**, which Windows Runtime metadata carries member markers on (the Windows SDK uses this to deprecate a single member of an existing enum). Constructors are excluded for the same reason as in `CSWINRT2021`: `[Deprecated]` has no `AttributeTargets.Constructor` in its usage, so it cannot be applied to one at all. Deprecate the whole runtime class to cover its constructors.

## Related documentation

- [CSWINRT3005](diagnostics/cswinrt3005.md): using an experimental Windows Runtime API
- [Authoring C#/WinRT components](authoring.md)
- [CsWinRT 3.0 overview](cswinrt3.0-spec.md)
