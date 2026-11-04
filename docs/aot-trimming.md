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
| `CsWinRTEnableDefaultCustomPropertyProviderSupport` | `true` | Adds a default, type-only `ICustomPropertyProvider` to generated managed CCWs and the runtime's shared opaque-object CCW, without replacing explicit providers. |
| `CsWinRTSuppressCustomPropertyNotSupportedException` | `false` | Returns null instead of reporting `NotSupportedException` when the default provider cannot supply a property or indexer. |
| `CsWinRTEnableOpaqueRcwMarshalling` | `false` | Allows projected runtime class instances (RCWs) that are not native-exposed to be marshalled to native code as opaque CCWs, instead of reporting `NotSupportedException`. |

## Selecting dynamic casting for exclusive interfaces

`CsWinRTPublicExclusiveToInterfaces` controls visibility, not dynamic casting. A projection producer must set `CsWinRTDynamicallyInterfaceCastableExclusiveTo=true` to opt eligible `[ExclusiveTo]` interfaces into IDIC. The optional `CsWinRTDynamicallyInterfaceCastableExclusiveToIncludes` and `CsWinRTDynamicallyInterfaceCastableExclusiveToExcludes` prefix lists narrow that opt-in; exclusions always win, and filters alone never enable it. The same selection applies to CoreCLR and Native AOT. See the [usage guide](usage.md#projecting-standalone-exclusive-interfaces) for syntax and precedence.

Reference projection packages preserve the exact effective selection as generator-owned `WindowsRuntimeReferenceAssemblyMetadataAttribute` key/value entries, independently of the public API surface. These entries remain in the reference assembly and are not copied to the forwarder. Applications consume that policy automatically. Missing metadata does not imply IDIC, so older preview projection packages that need dynamic casting must be regenerated. The runtime switch `CsWinRTEnableIDynamicInterfaceCastableSupport` remains the separate, application-wide switch for all dynamic interface casting.

Opting out avoids the selected interface's IDIC shim and type-map association, but does not remove ABI helpers or CCW implementations still needed by public/default/overridable interfaces. In the single-method exclusive-interface regression fixture, opting in adds exactly one shim type, one DIM method, and one assembly type-map association. Final binary size depends on trimming and reachability.

## Marshalling projected runtime classes as opaque CCWs

Projected runtime classes are backed by native objects, so passing one to native code unwraps the native object and no CCW is involved. Native code can still ask the runtime for a CCW around the managed wrapper (RCW) through `ComWrappers`. For example, on some Windows versions XAML asks the reference tracker host to wrap a projected object it was handed, such as a `Frame` or a `Grid` set as `Window.Content`. A projected class has no CCW vtables unless it is [native-exposed](interop.md#create-ccw), so by default this request is reported as a `NotSupportedException`, and the native caller can fail fast.

To expose these objects to native code as opaque CCWs instead, opt in by setting the following property in the project file:

```xml
<PropertyGroup>
  <CsWinRTEnableOpaqueRcwMarshalling>true</CsWinRTEnableOpaqueRcwMarshalling>
</PropertyGroup>
```

This defaults to `false` and sets the `CSWINRT_ENABLE_OPAQUE_RCW_MARSHALLING` runtime feature switch. The CCW is then the runtime's shared opaque-object CCW, the same one used for plain managed objects, so the generated marshalling code does not change and no type needs to be annotated. The behavior is as follows:

- The CCW implements `IInspectable`, `IStringable`, `IWeakReferenceSource`, `IMarshal` and `IAgileObject`, plus `ICustomPropertyProvider` when `CsWinRTEnableDefaultCustomPropertyProviderSupport` is enabled. `GetIids` reports only these interfaces and `IUnknown`.
- Queries for any other interface, including the interfaces of the projected type, are forwarded by the RCW to the native object, so native code can still reach the interfaces that the native object implements. They fail if the native object does not implement them.
- `GetRuntimeClassName` reports the full name of the projected type, such as `Windows.Data.Json.JsonObject`. A plain opaque object reports `Object`.
- Native-exposed projected types are not affected. They keep the CCW vtables from their own `[WindowsRuntimeNativeExposedType]` annotation.
- Only `ComWrappers` requests are affected. Ordinary marshalling, such as passing the RCW to a projected method, still unwraps the native object and keeps the same object identity.
- Only projected runtime classes are covered, not interfaces, delegates or other types.

The opaque CCW does not implement the interfaces of the projected type, and only forwards queries for them to the native object. Scenarios that need the CCW itself to implement additional interfaces still require `[assembly: WindowsRuntimeNativeExposedType(typeof(...))]`. This is the case for the covariant collection interfaces that XAML queries when a `DependencyObjectCollection` is used as an items source, as described in [Create CCW](interop.md#create-ccw).

The switch is pay-for-play: when it is disabled, the trimmer and Native AOT remove the code behind it.

## ICustomPropertyProvider support for XAML binding

Non source-generated WinUI and UWP XAML binding scenarios (i.e., not `x:Bind`) such as `DisplayMemberPath` make use of `ICustomPropertyProvider`. CsWinRT 3.0 provides an AOT-safe source-generated implementation via the `[GeneratedCustomPropertyProvider]` attribute.

Every managed type for which the interop generator emits a COM callable wrapper (CCW) automatically exposes a default, **type-only** native `ICustomPropertyProvider` implementation. This includes non-XAML classes and structs, collections and arrays, generic delegates, and key/value pairs; it is not limited to controls or `FrameworkElement` subclasses. The provider lets native callers identify the managed type, including when XAML applies typed styles and templates, without requiring an attribute or `partial` modifier. It does not change `IInspectable.GetRuntimeClassName`, which can still report an implemented interface for non-authored types.

The runtime's shared opaque-object CCW also supplies this provider. Plain managed objects and `DependencyObject` subclasses without exposed WinRT interfaces can use this fallback even though they have no generated type-map entry. They continue to report `"Object"` from `GetRuntimeClassName`, while `ICustomPropertyProvider.Type` reports their actual managed type. For composed XAML objects, native base interfaces still come from the inner object; the provider supplies the managed outer's type identity.

The automatic provider supplies managed type identity and uses the object's `ToString()` for its string representation. Its `GetCustomProperty` and `GetIndexedProperty` methods report `NotSupportedException` through the ABI (`0x80131515`, with restricted error information) because property lookup through this interface requires an explicit provider, such as one generated by `[GeneratedCustomPropertyProvider]`. This is separate from member access supplied by generated XAML metadata (`IXamlMetadataProvider`). The failure matches the default CsWinRT 2.x Native AOT behavior when generated binding support is absent. CsWinRT 3.0 uses the same failure behavior on CoreCLR because this type-only fallback does not perform reflection-based property discovery.

To suppress these diagnostics, set `<CsWinRTSuppressCustomPropertyNotSupportedException>true</CsWinRTSuppressCustomPropertyNotSupportedException>` in the project file. The default provider then returns `S_OK` with a null property or indexer result, matching the CsWinRT 2.x opt-in behavior under Native AOT. This applies on both CoreCLR and Native AOT in CsWinRT 3.0; it does not enable reflection-based binding or change `ICustomPropertyProvider.Type` and `GetStringRepresentation()`. Explicit providers are unaffected.

Existing providers, including inherited implementations and their property lists, take precedence. Their lookup behavior is unchanged: for example, a source-generated provider returns null for a property it does not expose. This option does not expand type discovery or change other specialized CCW tables supplied by the runtime or projection assemblies.

To opt out of only the automatic type bridge, set:

```xml
<PropertyGroup>
  <CsWinRTEnableDefaultCustomPropertyProviderSupport>false</CsWinRTEnableDefaultCustomPropertyProviderSupport>
</PropertyGroup>
```

This defaults to `true` and is consumed by the interop generator at build time. Set the MSBuild property and rebuild to change the emitted interface entries. The generator emits exactly the required table for each type, with straight-line static initializers and no runtime switch checks or alternate tables. When disabled, the fallback entry is not emitted and its unreferenced implementation can be trimmed. Explicit and source-generated providers are unaffected.

The shared opaque-object CCW is part of the prebuilt runtime, so its marshaller uses the matching `CSWINRT_ENABLE_DEFAULT_CUSTOM_PROPERTY_PROVIDER_SUPPORT` feature switch to select a complete table with or without the default provider. This selection happens outside the straight-line table initializers, preserving Native AOT preinitialization. The MSBuild property sets both generation-time and runtime configuration; trimming and Native AOT can remove the unselected table and unused provider implementation.

Set the MSBuild property and rebuild for a consistent opt-out across both paths. Changing `AppContext` configuration alone does not change generated entries, and marshalling tables are cached once used. Disabling the default provider can break native scenarios that rely on it, including XAML binding-source identity and typed styles.

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

Generic providers can also be created through generic factories or extension methods in referenced libraries. Their properties and indexers support native XAML binding without adding direct constructions or `typeof` references to the closed provider types.

### Requirements

- The type must be a non-abstract, non-static `class` or `struct`
- The type (and all containing types) must be marked `partial`
- The type must not already implement `ICustomPropertyProvider` members

The source generator produces diagnostics (`CSWINRT2000`–`CSWINRT2008`) for invalid usage. See the [diagnostics reference](diagnostics/) for details.
