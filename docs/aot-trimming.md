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

### Requirements

- The type must be a non-abstract, non-static `class` or `struct`
- The type (and all containing types) must be marked `partial`
- The type must not already implement `ICustomPropertyProvider` members

The source generator produces diagnostics (`CSWINRT2000`–`CSWINRT2008`) for invalid usage. See the [diagnostics reference](diagnostics/) for details.


