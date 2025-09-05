# Runtime class projection specification

## Overview

This document describes the runtime class projection specification for CsWinRT 3.0, with a particular focus on the constructor patterns generated for Windows Runtime classes. In CsWinRT 3.0, the architecture has been significantly refactored to centralize all activation logic in the base `WindowsRuntimeObject` class. This is a major departure from CsWinRT 2.x, where each projected type handled its own activation logic.

The new design provides several benefits:
- **Binary size improvements**: all activation code can now be shared across projected types.
- **Better versioning**: the activation code can be updated without needing new projections.
- **Better performance**: no need to use interface stub dispatch to access common `IInspectable` functionality.

Additionally, it is worth noting that the new design is structured this way due to several constraints. One of the main problems it solves is that the initialization logic for the derived cases needs to run before the body of any of the constructors. Without the initialization logic, the object is completely unusable (i.e. setting a property in the body of the constructor would crash the program). Because of this, all derived constructors must always pass all parameters required to perform the full initialization to the base constructor, which will fully initialize the object. They can then perform any additional initialization logic for each derived type, if necessary.

Note that while slightly odd, this design with multiple base constructor overloads is only ever meant to be used by generated projection code. User authored code would never see nor have to directly use any of these constructors, in any scenario. This pattern is also required because other possible solutions (e.g. static abstracts) are not viable here. For static abstracts specifically, they can't be used because there is no generic context available (the constructors are not generic), and also because users expect the constructors to be just like all other normal constructors. The CsWinRT design shouldn't force everyone to use static abstracts instead, as that would be too breaking.

## Constructor patterns

For sealed runtime classes like `WinRTClass`, the generated constructors are relatively straightforward:

```csharp
public sealed class WinRTClass : WindowsRuntimeObject
{
    // Default constructor - uses activation factory directly
    public WinRTClass()
        : base(_objRef_global__MyLib_WinRTClass, in IID_MyLib_WinRTClass)
    {
    }

    // Parameterized constructor - uses custom activation callback
    public unsafe WinRTClass(string name)
        : base(
              ActivationCallback,
              in IID_MyLib_WinRTClass,
              [name])
    {
    }

    // Custom activation callback for parameterized constructors
    private static unsafe void ActivationCallback(
        ReadOnlySpan<object?> additionalParameters,
        out void* defaultInterface)
    {
        // Custom activation logic here
    }

    // Other members...
}
```

In this example, we have a parameterless constructor and one with a `string` parameter. Any additional constructors taking parameters would use the same callback pattern (see details below), where the parameters are passed via a `ReadOnlySpan<object?>`.

> [!NOTE]
> This can be further optimized in the future without a breaking change, if/when C# gets support for `ReadOnlySpan<TypedReference>`, to avoid boxing any value type parameters. For now, this approach provides a good enough compromise between code size, simplicity, and performance.

### Unsealed (composable) runtime classes

Unsealed runtime classes like `UnsealedWinRTClass` require a more complex constructor pattern to support all possible derivation scenarios. These classes need to handle four different activation cases, which are detailed below. Note that all these additional constructors will just be trimmed if unused, and they contain no actual code other than forwarding parameters, so they are not a binary size concern.

Here are all the generated constructors for `UnsealedWinRTClass`:

```csharp
public class UnsealedWinRTClass : WindowsRuntimeObject
{
    // 1. Public default constructor for direct instantiation
    public UnsealedWinRTClass()
        : base(
              default(WindowsRuntimeActivationTypes.DerivedComposed),
              _objRef_global__MyLib_UnsealedWinRTClass,
              in IID_MyLib_UnsealedWinRTClass)
    {
    }

    // 2. Protected constructor for derived composable types
    protected UnsealedWinRTClass(
        WindowsRuntimeActivationTypes.DerivedComposed _,
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        in Guid iid)
        : base(_, activationFactoryObjectReference, in iid)
    {
    }

    // 3. Protected constructor for derived sealed types
    protected UnsealedWinRTClass(
        WindowsRuntimeActivationTypes.DerivedSealed _,
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        in Guid iid)
        : base(_, activationFactoryObjectReference, in iid)
    {
    }

    // 4. Protected constructor for derived composable types with custom parameters
    protected UnsealedWinRTClass(
        WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
        : base(activationFactoryCallback, in iid, additionalParameters)
    {
    }

    // 5. Protected constructor for derived sealed types with custom parameters
    protected UnsealedWinRTClass(
        WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
        : base(activationFactoryCallback, in iid, additionalParameters)
    {
    }
}
```

The constructors provide support for the following scenarios:
- **Public default constructor (#1)**: used when instantiating `UnsealedWinRTClass` directly. Uses `DerivedComposed` marker type to indicate this is a composable activation scenario. This will select the constructor overload in `WindowsRuntimeObject` using the right activation factory signature and logic.
- **Protected `DerivedComposed` constructor (#2)**: used by any derived classes that are themselves composable (unsealed). The derived class will pass its own activation factory and IID. The `DerivedComposed` marker tells the base class this is a composition scenario where aggregation is needed.
- **Protected `DerivedSealed` constructor (#3)**: used by derived classes that are sealed. The `DerivedSealed` marker tells the base class this is a non-composition scenario where no aggregation is needed.
- **Protected `DerivedComposed` callback constructor (#4)**: Used by derived composable classes that need custom activation parameters. Same as the parameterless overload for `DerivedComposed` scenarios.
- **Protected `DerivedSealed` callback constructor (#5)**: Used by derived sealed classes that need custom activation parameters. Same as the parameterless overload for `DerivedSealed` scenarios.

## Derived Class Examples

### Sealed Derived Class

```csharp
public sealed class DerivedWinRTClass : UnsealedWinRTClass
{
    public DerivedWinRTClass()
        : base(
              default(WindowsRuntimeActivationTypes.DerivedSealed),  // Sealed marker
              _objRef_global__MyLib_DerivedWinRTClass,
              in IID_MyLib_DerivedWinRTClass)
    {
    }
}
```

### Composable Derived Class

```csharp
public class AnotherUnsealedWinRTClass : UnsealedWinRTClass
{
    public AnotherUnsealedWinRTClass()
        : base(
              default(WindowsRuntimeActivationTypes.DerivedComposed),  // Composable marker
              _objRef_global__MyLib_AnotherUnsealedWinRTClass,
              in IID_MyLib_AnotherUnsealedWinRTClass)
    {
    }
}
```

## Activation Factory Callbacks

For complex activation scenarios requiring custom parameters, CsWinRT 3.0 uses activation factory callbacks:

### Callback Types

```csharp
public static class WindowsRuntimeActivationFactoryCallback
{
    // For composable (unsealed) derived types
    public unsafe delegate void DerivedComposed(
        ReadOnlySpan<object?> additionalParameters,
        WindowsRuntimeObject? baseInterface,
        out void* innerInterface,
        out void* defaultInterface);

    // For sealed derived types
    public unsafe delegate void DerivedSealed(
        ReadOnlySpan<object?> additionalParameters,
        out void* defaultInterface);
}
```

### Usage Example

```csharp
private static unsafe void ActivationCallback(
    ReadOnlySpan<object?> additionalParameters,
    WindowsRuntimeObject? baseInterface,
    out void* innerInterface,
    out void* defaultInterface)
{
    using WindowsRuntimeObjectReferenceValue activationFactoryValue =
        _objRef_global__Windows_Data_Json_JsonArray.AsValue();

    string? name = (string?)additionalParameters[0];
    fixed (char* namePtr = name)
    {
        HStringMarshaller.ConvertToUnmanagedUnsafe(namePtr, name?.Length, out HStringReference nameReference);

        fixed (void** innerInterfacePtr = &innerInterface)
        fixed (void** defaultInterfacePtr = &defaultInterface)
        {
            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                baseInterface: baseInterface,
                innerInterface: innerInterfacePtr,
                instance: defaultInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }
}
```
