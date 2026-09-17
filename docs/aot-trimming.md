# .NET trimming and AOT support in C#/WinRT

## Overview

CsWinRT 3.0 is designed **AOT-first**. All generated projections are fully compatible with [Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) and [IL trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained) without requiring any additional configuration from the developer.

With CsWinRT 2.x, classes needed to be made `partial` class annotations, and certain opt-in attributes were needed to be AOT-compatible such as with casts. CsWinRT 3.0 handles all of this automatically at app build or library publish time:

- **Vtables, COM interface entries, and CCW data** are pre-initialized at app build time and foldable into readonly data sections by the Native AOT compiler.
- **All marshalling code** is generated into `WinRT.Interop.dll` by the interop generator (`cswinrtinteropgen`), which analyzes the entire application to produce deduplicated, optimized marshalling stubs and can see source generated code.
- **No `partial` annotations or generator attributes are needed** on your types as the interop generator discovers all WinRT compatible types automatically.

## Runtime feature switches

CsWinRT provides runtime feature switches that allow opt-in/opt-out of specific functionality. When a feature is disabled, all code behind that switch is trimmed by the trimmer, making features fully pay-for-play.

Feature switches are set as MSBuild properties in your project file:

```xml
<PropertyGroup>
  <CsWinRTEnableManifestFreeActivation>false</CsWinRTEnableManifestFreeActivation>
</PropertyGroup>
```

| Property | Default | Description |
|----------|---------|-------------|
| `CsWinRTEnableIDynamicInterfaceCastableSupport` | `true` | Enables `IDynamicInterfaceCastable` for casting to interfaces not listed as implemented. |
| `CsWinRTEnableManifestFreeActivation` | `true` | Enables activation without manifest registration. |
| `CsWinRTEnableXamlTypeMarshalling` | `true` | Enables type marshalling support. |
| `CsWinRTEnableMarshalingTypeMetadataSupport` | `true` | Enables using `MarshalingType` as part of marshaling optimizations. |
| `CsWinRTEnableMarshalingTypeValidation` | `false` | Validates the `MarshalingType` to determine whether the metadata is the same as the implementation. |

## Selecting dynamic casting for exclusive interfaces

`CsWinRTPublicExclusiveToInterfaces` controls visibility, not dynamic casting. A projection producer
must set `CsWinRTDynamicallyInterfaceCastableExclusiveTo=true` to opt eligible `[ExclusiveTo]`
interfaces into IDIC. The optional `CsWinRTDynamicallyInterfaceCastableExclusiveToIncludes` and
`CsWinRTDynamicallyInterfaceCastableExclusiveToExcludes` prefix lists narrow that opt-in; exclusions
always win, and filters alone never enable it. The same selection applies to CoreCLR and Native AOT.
See the [usage guide](usage.md#projecting-standalone-exclusive-interfaces) for syntax and precedence.

Reference projection packages preserve the exact effective selection as generator-owned
`WindowsRuntimeReferenceAssemblyMetadataAttribute` key/value entries, independently of the public API
surface. These entries remain in the reference assembly and are not copied to the forwarder.
Applications consume that policy automatically.
Missing metadata does not imply IDIC, so older preview projection packages that need dynamic casting
must be regenerated. The runtime switch `CsWinRTEnableIDynamicInterfaceCastableSupport` remains the
separate, application-wide switch for all dynamic interface casting.

Opting out avoids the selected interface's IDIC shim and type-map association, but does not remove
ABI helpers or CCW implementations still needed by public/default/overridable interfaces. In the
single-method exclusive-interface regression fixture, opting in adds exactly one shim type, one DIM
method, and one assembly type-map association. Final binary size depends on trimming and reachability;
this output-shape measurement is not a published-size estimate.

## ICustomPropertyProvider support for XAML binding

Non source-generated WinUI and UWP XAML binding scenarios (i.e., not `x:Bind`) such as `DisplayMemberPath` make use of `ICustomPropertyProvider`. CsWinRT 3.0 provides an AOT-safe source-generated implementation via the `[GeneratedCustomPropertyProvider]` attribute.

### Usage

Mark your class as `partial` and apply the attribute:

```csharp
using WindowsRuntime.Xaml;

[GeneratedCustomPropertyProvider]
public partial class MyViewModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

By default, the generated implementation supports all public properties. You can scope down to specific properties and indexer types:

```csharp
[GeneratedCustomPropertyProvider(
    propertyNames: ["Name", "Age"],
    indexerPropertyTypes: [typeof(int)]
)]
public partial class MyViewModel
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string City { get; set; }  // excluded from binding
    public int this[int index] { get; set; }
}
```

Generic types and types nested in generic containing types are supported, including their generic constraints:

```csharp
[GeneratedCustomPropertyProvider]
public partial class Box<T>
{
    public T Value { get; set; } = default!;
}
```

Property descriptors are cached separately for each closed owner type. For example, `Box<int>` and `Box<string>` expose `Value` as `int` and `string`, respectively, and use separate descriptors. Generic property types, indexer parameter types, and static properties retain the owner's generic context.

### Requirements

- The type must be a non-abstract, non-static `class` or `struct`
- The type (and all containing types) must be marked `partial`
- The type must not already implement `ICustomPropertyProvider` members

The source generator produces diagnostics (`CSWINRT2000`–`CSWINRT2008`) for invalid usage. See the [diagnostics reference](diagnostics/) for details.
