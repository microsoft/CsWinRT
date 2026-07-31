# CsWinRT warning CSWINRT3005

The `WindowsRuntimeImplementableClassAttribute` type (in the `WindowsRuntime` namespace) is a private implementation detail of `WinRT.Runtime.dll`. It is applied by CsWinRT to the abstract base classes it generates for Windows Runtime types that can be implemented (authored) in C#, to identify the Windows Runtime class each one stands for. Unlike most other CsWinRT implementation details, it is not stripped from the reference assembly, because the reference projections carrying those base classes are compiled against it. It is not intended for direct use in user code.

For instance, the following sample generates CSWINRT3005:

```csharp
using WindowsRuntime;

// CSWINRT3005: the implementable class attribute is a private implementation detail
[WindowsRuntimeImplementableClass(typeof(Contoso.Widgets.Widget))]
public abstract class MyBase;
```

## Additional resources

`CSWINRT3005` is emitted when user code references the `WindowsRuntimeImplementableClassAttribute` type directly. CsWinRT emits it automatically onto the abstract `ABI.<Namespace>.<Class>` and `ABI.<Namespace>.<Class>Factory` base classes produced when a projection is built with `CsWinRTImplementWinMDTypes`, and CsWinRT tooling reads it to recognize those bases and to determine the runtime class name a derived implementation reports to the Windows Runtime. All of that generated code suppresses this diagnostic, so it never affects normal builds.

The attribute is not considered part of the versioned API surface of `WinRT.Runtime.dll`, and it may be modified or removed across any version change. Using it in user code is undefined behavior and not supported.

## Recommended action

- Do not reference the `WindowsRuntimeImplementableClassAttribute` type in user code, and let CsWinRT emit it for you.
- To implement a Windows Runtime type declared in an existing `.winmd`, extend the generated `ABI.<Namespace>.<Class>` abstract base class from a projection built with `CsWinRTImplementWinMDTypes`, rather than annotating a base class of your own.
- To customize the runtime class name that an unrelated type reports to the Windows Runtime, use `WindowsRuntimeClassNameAttribute` instead, which is supported for that purpose.

Keeping the attribute exclusive to generated code is what allows CsWinRT to evolve the projection infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
