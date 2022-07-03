using System;
using TestComponentCSharp;
using WinRT.Interop;
using WinRT;

var managedProperties = new ManagedProperties(42);
var instance = new Class();

// Ensure we can use the IProperties interface from the native side.
instance.CopyProperties(managedProperties);
if (managedProperties.ReadWriteProperty != instance.ReadWriteProperty)
{
    return 101;
}

// Check for the default interfaces provided by WinRT.Runtime
Guid IID_IMarshal = new Guid("00000003-0000-0000-c000-000000000046");
IObjectReference ccw = MarshalInterface<IProperties1>.CreateMarshaler(managedProperties);
ccw.TryAs<IUnknownVftbl>(IID_IMarshal, out var marshalCCW);
if (marshalCCW == null)
{
    return 102;
}

// Check for managed implemented interface to ensure not trimmed.
Guid IID_IUriHandler = new Guid("FF4B4334-2104-537D-812E-67E3856AC7A2");
ccw.TryAs<IUnknownVftbl>(IID_IUriHandler, out var uriHandlerCCW);
if (uriHandlerCCW == null)
{
    return 103;
}

// Ensure that interfaces on the vtable / object don't get trimmed even if unused.
Guid IID_IWarning1 = new Guid("4DB3FA26-4BB1-50EA-8362-98F49651E516");
Guid IID_IWarningClassOverrides = new Guid("E5635CE4-D483-55AA-86D5-080DC07F0A09");

var managedWarningClass = new ManagedWarningClass();
ccw = MarshalInterface<IUriHandler>.CreateMarshaler(managedWarningClass);
ccw.TryAs<IUnknownVftbl>(IID_IWarning1, out var warningCCW);
if (warningCCW == null)
{
    return 104;
}

ccw.TryAs<IUnknownVftbl>(IID_IWarningClassOverrides, out var warningOverrideCCW);
if (warningOverrideCCW == null)
{
    return 105;
}

return 100;

sealed class ManagedProperties : IProperties1, IUriHandler
{
    private readonly int _value;

    public ManagedProperties(int value)
    {
        _value = value;
    }

    public int ReadWriteProperty => _value;

    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}

sealed class ManagedWarningClass : WarningClass, IUriHandler
{
    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}