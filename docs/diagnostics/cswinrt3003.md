# CsWinRT warning CSWINRT3003

The Windows Runtime component assembly attributes (`WindowsRuntimeComponentAssemblyAttribute`, `WindowsRuntimeComponentAssemblyExportsTypeAttribute` and `WindowsRuntimeActivationFactoryAssemblyAttribute`, all in the `WindowsRuntime.InteropServices` namespace) are a private implementation detail of `WinRT.Runtime.dll`. They are only meant to be applied by CsWinRT, to mark assemblies taking part in Windows Runtime activation and to identify the generated type that contains their activation factory entry point. They are exposed in the reference assembly for `WinRT.Runtime.dll` solely so that this generated code can reference them, and they are not intended for direct use in user code.

For instance, the following sample generates CSWINRT3003:

```csharp
using WindowsRuntime.InteropServices;

// CSWINRT3003: the component assembly attributes are a private implementation detail
[assembly: WindowsRuntimeComponentAssembly]
```

## Additional resources

`CSWINRT3003` is emitted when user code references one of the Windows Runtime component assembly attributes directly. These attributes identify an assembly taking part in Windows Runtime activation and the generated type that exposes its managed `GetActivationFactory` method: the CsWinRT source generator emits them automatically when building a Windows Runtime component (when `CsWinRTComponent` is set to `true`), or when a project declares activation factories for Windows Runtime classes declared in existing metadata. Other CsWinRT tooling (the source generator, the projection generator, and the interop generator) reads them to merge activation factories across referenced assemblies. All of that generated code suppresses this diagnostic, so it never affects normal builds.

The component assembly attributes are not considered part of the versioned API surface of `WinRT.Runtime.dll`, and they may be modified or removed across any version change. Using them in user code is undefined behavior and not supported.

## Recommended action

- Do not reference the component assembly attributes in user code, and let CsWinRT emit them for you.
- If you are authoring a Windows Runtime component, set `CsWinRTComponent` to `true` and let CsWinRT generate the activation factory exports (and the attributes that identify them) automatically; no manual annotation is needed.

Keeping the component assembly attributes exclusive to generated code is what allows CsWinRT to evolve the authoring infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
