# CsWinRT warning CSWINRT3004

The `WindowsRuntimeReferenceAssemblyAttribute` and `WindowsRuntimeReferenceAssemblyMetadataAttribute`
types (in the `WindowsRuntime.InteropServices` namespace) are private implementation details of
`WinRT.Runtime.dll`. CsWinRT applies them to generated reference projections to identify their Windows
Runtime APIs and preserve build-time projection requirements. Unlike most implementation details, they
remain in the `WinRT.Runtime.dll` reference assembly: generated projection packages carry these attributes,
so they must remain resolvable when those packages are compiled and consumed. Neither is intended for
direct use in user code.

For instance, the following sample generates CSWINRT3004:

```csharp
using WindowsRuntime.InteropServices;

// CSWINRT3004: the reference assembly attribute is a private implementation detail
[assembly: WindowsRuntimeReferenceAssembly]

// CSWINRT3004: reference-projection metadata is also a private implementation detail
[assembly: WindowsRuntimeReferenceAssemblyMetadata("Example", "Value")]
```

## Additional resources

`CSWINRT3004` is emitted when user code references either reference assembly attribute directly.
`WindowsRuntimeReferenceAssemblyAttribute` is the parameterless marker identifying a projection.
`WindowsRuntimeReferenceAssemblyMetadataAttribute` carries generator-owned key/value pairs, such as
the exact exclusive-interface IDIC selection under the `CsWinRT.IdicExclusiveTo.v1` key.
CsWinRT emits these annotations automatically and its tooling reads them at build time. All generated
code suppresses this diagnostic, so it does not affect normal builds.

The reference assembly attributes are not considered part of the versioned API surface of
`WinRT.Runtime.dll`, and they may be modified or removed across any version change. Using them in user
code is undefined behavior and not supported.

## Recommended action

- Do not reference either attribute in user code; let CsWinRT emit them for you.
- If you are authoring a Windows Runtime projection to ship in a NuGet package, set `CsWinRTGenerateReferenceProjection` to `true` and let CsWinRT generate the reference projection (and the attribute that identifies it) automatically; no manual annotation is needed.

Keeping these attributes exclusive to generated code is what allows CsWinRT to evolve the projection
infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
