# CsWinRT warning CSWINRT3004

The `WindowsRuntimeReferenceAssemblyAttribute` type (in the `WindowsRuntime.InteropServices` namespace) is a private implementation detail of `WinRT.Runtime.dll`. It is only meant to be applied (via `[assembly: WindowsRuntimeReferenceAssembly]`) to generated Windows Runtime projection assemblies by CsWinRT, to identify them as containing projected Windows Runtime APIs. Unlike most other CsWinRT implementation details, it is not stripped from the reference assembly for `WinRT.Runtime.dll`, because the reference projection assemblies that ship in Windows Runtime projection NuGet packages carry it and it must remain resolvable when those assemblies are consumed. It is not intended for direct use in user code.

For instance, the following sample generates CSWINRT3004:

```csharp
using WindowsRuntime.InteropServices;

// CSWINRT3004: the reference assembly attribute is a private implementation detail
[assembly: WindowsRuntimeReferenceAssembly]
```

## Additional resources

`CSWINRT3004` is emitted when user code references the `WindowsRuntimeReferenceAssemblyAttribute` type directly. This attribute marks an assembly as containing generated Windows Runtime APIs from a given Windows Runtime metadata file (`.winmd`): CsWinRT emits it automatically into the reference projection assemblies it produces (via `cswinrtprojectionrefgen.exe` and `cswinrtprojectiongen.exe`), and CsWinRT tooling reads it to recognize those assemblies. All of that generated code suppresses this diagnostic, so it never affects normal builds.

The reference assembly attribute is not considered part of the versioned API surface of `WinRT.Runtime.dll`, and it may be modified or removed across any version change. Using it in user code is undefined behavior and not supported.

## Recommended action

- Do not reference the `WindowsRuntimeReferenceAssemblyAttribute` type in user code, and let CsWinRT emit it for you.
- If you are authoring a Windows Runtime projection to ship in a NuGet package, set `CsWinRTGenerateReferenceProjection` to `true` and let CsWinRT generate the reference projection (and the attribute that identifies it) automatically; no manual annotation is needed.

Keeping the reference assembly attribute exclusive to generated code is what allows CsWinRT to evolve the projection infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
