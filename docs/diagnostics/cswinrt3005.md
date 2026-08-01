# CsWinRT warning CSWINRT3005

The `WindowsRuntimeImplementableClassAttribute` and `WindowsRuntimeImplementableClassFactoryAttribute` types (in the `WindowsRuntime` namespace) are private implementation details of `WinRT.Runtime.dll`. They are applied by CsWinRT to the abstract base classes it generates for Windows Runtime types that can be implemented (authored) in C#, to identify the Windows Runtime class each one stands for. Unlike most other CsWinRT implementation details, they are not stripped from the reference assembly, because the reference projections carrying those base classes are compiled against them. They are not intended for direct use in user code.

For instance, the following sample generates CSWINRT3005:

```csharp
using WindowsRuntime;

// CSWINRT3005: the implementable class attribute is a private implementation detail
[WindowsRuntimeImplementableClass(typeof(Contoso.Widgets.Widget))]
public abstract class MyBase;
```

## Additional resources

`CSWINRT3005` is emitted when user code references the `WindowsRuntimeImplementableClassAttribute` or `WindowsRuntimeImplementableClassFactoryAttribute` types directly. CsWinRT emits them automatically onto the abstract `ABI.<Namespace>.<Class>` and `ABI.<Namespace>.<Class>ActivationFactory` base classes produced when a projection is built with `CsWinRTImplementWinMDTypes`, and CsWinRT tooling reads them to recognize those bases and to determine the runtime class name a derived implementation reports to the Windows Runtime. All of that generated code suppresses this diagnostic, so it never affects normal builds.

The attributes are not considered part of the versioned API surface of `WinRT.Runtime.dll`, and they may be modified or removed across any version change. Using them in user code is undefined behavior and not supported.

## Recommended action

- Do not reference the `WindowsRuntimeImplementableClassAttribute` or `WindowsRuntimeImplementableClassFactoryAttribute` types in user code, and let CsWinRT emit them for you.
- To implement a Windows Runtime type declared in an existing `.winmd`, extend the generated `ABI.<Namespace>.<Class>` abstract base class from a projection built with `CsWinRTImplementWinMDTypes`, rather than annotating a base class of your own.
- To customize the runtime class name that an unrelated type reports to the Windows Runtime, use `WindowsRuntimeClassNameAttribute` instead, which is supported for that purpose.

Keeping the attribute exclusive to generated code is what allows CsWinRT to evolve the projection infrastructure rapidly. Respecting the diagnostic ensures your applications remain stable across updates.
