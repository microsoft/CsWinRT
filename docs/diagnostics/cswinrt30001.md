# CsWinRT warning CSWINRT3001

This type or method is a private implementation detail, and it's only meant to be consumed by generated projections (produced by 'cswinrt.exe') and by generated interop code (produced by 'cswinrtgen.exe'). Private implementation detail types are not considered part of the versioned API surface, and they are ignored when determining the assembly version following semantic versioning. Types might be modified or removed across any version change for 'WinRT.Runtime.dll', and using them in user code is undefined behavior and not supported.

For instance, the following sample generates CSWINRT3001:

```csharp
using Microsoft.UI.Xaml;
using WindowsRuntime.InteropServices;

namespace MyProgram;

Window window = Window.Current;

// CSWINRT3001: 'GetObjectReferenceForInterface' is a private implementation detail API
WindowsRuntimeObjectReference objectReference = window.GetObjectReferenceForInterface(typeof(object).TypeHandle);
```

Using any private implementation detail API is not supported, and should be considered undefined behavior.

## Additional resources

`CSWINRT30001` is emitted when user code tries to reference a type that is marked as a **private implementation detail** within `WinRT.Runtime.dll` or the generated `WinRT.Interop.dll`. These private implementation detail types exist solely to support the marshalling pipeline that CsWinRT and the .NET SDK generate at build time. They are not part of the public, versioned API surface, and consuming them from application code is unsupported.

While all of these types are public (as they are used across assembglies), they are intentionally hidden from IntelliSense and decorated with `[Obsolete]` (with `CSWINRT3001` as the diagnostic id) to warn when they are referenced. Their names often include `Impl`, `Helpers`, or other internal wording, and their diagnostic message explicitly states that they are private implementation details. During a build, `cswinrt.exe` produces projections and `cswinrtgen.exe` produces `WinRT.Interop.dll`. The generated code inside these tools uses private implementation detail types to perform marshalling work. See `docs/winrt-interop-dll-spec.md` for a description of the generated interop assembly. Because the tooling controls all references to these types, their shape can change whenever needed without breaking consumers. This flexibility is what allows performance and reliability improvements across releases.

## Recommended action

- Remove all references to any private implementation detail types.
- Look for supported alternatives in `WinRT.Runtime.dll`, the Windows SDK projections, or your own code.
- If you are authoring source generators or tooling, never take a dependency on private implementation detail types.
- When in doubt, file an issue describing the scenario so the CsWinRT team can help identify a stable API or consider exposing a supported helper.

Keeping private implementation detail types exclusive to generated code is what allows CsWinRT to deliver fast, safe interop while evolving rapidly. Respecting the diagnostic ensures your applications remain stable across updates (and also avoids accidentally breaking builds when updating the CsWinRT version).
