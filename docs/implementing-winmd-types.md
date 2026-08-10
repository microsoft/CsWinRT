# Implementing Windows Runtime types defined in an existing .winmd

Normally, authoring a Windows Runtime component in C# means writing the C# type first and letting CsWinRT
produce a `.winmd` for it. CsWinRT can also do the reverse: implement, in C#, a Windows Runtime type whose
shape is *already* defined in an existing `.winmd`.

## Enabling it on a projection

Set `CsWinRTImplementWinMDTypes` on the projection project for that metadata:

```xml
<PropertyGroup>
  <CsWinRTGenerateReferenceProjection>true</CsWinRTGenerateReferenceProjection>
  <CsWinRTImplementWinMDTypes>true</CsWinRTImplementWinMDTypes>
</PropertyGroup>
```

The projection then carries, in addition to its usual projected types, an abstract `ABI.<Namespace>.<Class>`
base class per runtime class, plus an `ABI.<Namespace>.<Class>ActivationFactory` base for its statics, factory
methods and activation. Every member the Windows Runtime type requires is declared `abstract`, so the compiler
guarantees none is missed. The Windows Runtime interfaces behind them stay `internal`, so none of the
marshalling infrastructure is exposed.

This is purely additive: consumers that only *use* the projection are unaffected, and there is no second
package to produce or reference.

## Implementing the types

Reference the projection and extend the generated base:

```csharp
public sealed class MyWidget : ABI.Contoso.Widgets.Widget
{
    public override void DoStuff() { }
}

[WindowsRuntimeActivationFactory(typeof(MyWidget))]
public sealed class MyWidgetFactory : ABI.Contoso.Widgets.WidgetActivationFactory, IWidgetInterop
{
    public override object ActivateInstance() => new MyWidget();
}
```

The generated base is separate from the projected class (which is often `sealed`) and provides an implicit
conversion to it, so an instance can be passed anywhere the projected type is expected. Any additional
(non-exclusive) Windows Runtime interfaces declared on the factory are added to its vtable as well.

### When the factory can be omitted

Declaring the factory is only necessary when it has something to say. If the class can only be activated
through the parameterless `IActivationFactory.ActivateInstance` — no factory methods, statics or composable
interfaces — a factory can do nothing but construct the implementation, so CsWinRT generates one:

```csharp
// No factory needed: 'Widget' has default activation only
public sealed class MyWidget : ABI.Contoso.Widgets.Widget
{
    public override void DoStuff() { }
}
```

This requires an accessible parameterless constructor, and only one implementation of that runtime class in
the project. Declaring the factory anyway always takes precedence, which is what to do when it needs to carry
extra interop interfaces on its vtable.

## At application build time

In a reference projection the abstract bases carry no implementation, exactly like the rest of it. CsWinRT
supplies their bodies, and the marshalling code for the implemented types, into `WinRT.Projection.dll` when
the application is built. Two consequences follow:

- The projection is independent of the CsWinRT version an application uses, so it does not need to be rebuilt
  when CsWinRT updates.
- Several components can implement types from the same projection. They share one set of definitions, so
  there is a single definition of each Windows Runtime interface and no duplicate IIDs in the interop type map.
