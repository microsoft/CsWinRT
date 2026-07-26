# CsWinRT warning CSWINRT3005

The `WindowsRuntimeTypeAttribute` type (in the `WindowsRuntime` namespace) is a private implementation detail of `WinRT.Runtime.dll`. It is applied by CsWinRT to projected Windows Runtime types to mark them as participating in Windows Runtime marshalling. Unlike most other CsWinRT implementation details, it is not stripped from the reference assembly for `WinRT.Runtime.dll`, because CsWinRT generates interface declarations carrying it directly into a component's own assembly when implementing Windows Runtime types defined in an existing `.winmd` (see the `CsWinRTImplementWinMDType` build item). It is not intended for direct use in user code.

For instance, the following sample generates CSWINRT3005:

```csharp
using WindowsRuntime;

// CSWINRT3005: the Windows Runtime type attribute is a private implementation detail
[WindowsRuntimeType]
public interface IMyInterface;
```

## Additional resources

`CSWINRT3005` is emitted when user code references the `WindowsRuntimeTypeAttribute` type directly. CsWinRT emits it automatically onto every projected type and proxy type it generates, and CsWinRT tooling reads it to recognize those types. All of that generated code suppresses this diagnostic, so it never affects normal builds.

The attribute is not considered part of the versioned API surface of `WinRT.Runtime.dll`, and it may be modified or removed across any version change. Using it in user code is undefined behavior and not supported.

## Recommended action

- Do not reference the `WindowsRuntimeTypeAttribute` type in user code, and let CsWinRT emit it for you.
- To implement a Windows Runtime type defined in an existing `.winmd`, add a `CsWinRTImplementWinMDType` item for it and extend the generated `ABI.<Namespace>.<Class>` abstract base class, rather than declaring the Windows Runtime interfaces yourself.

Keeping the attribute exclusive to generated code is what allows CsWinRT to evolve the projection infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
