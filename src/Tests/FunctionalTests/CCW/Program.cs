using System;
using TestComponentCSharp;
using WinRT.Interop;
using WinRT;
using System.Collections.Generic;
using System.Collections;

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
Guid IID_IArtist = new Guid("B7233F79-63CF-5AFA-A026-E4F1924F17A1");

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

ccw.TryAs<IUnknownVftbl>(IID_IArtist, out var artistCCW);
if (artistCCW == null)
{
    return 106;
}

var managedWarningClass2 = new ManagedWarningClass2();
ccw = MarshalInspectable<object>.CreateMarshaler(managedWarningClass2);
ccw.TryAs<IUnknownVftbl>(IID_IWarning1, out var warningCCW2);
if (warningCCW2 == null)
{
    return 107;
}

ccw.TryAs<IUnknownVftbl>(IID_IWarningClassOverrides, out var warningOverrideCCW2);
if (warningOverrideCCW2 == null)
{
    return 108;
}

Guid IID_IProperties1 = new Guid("4BB22177-718B-57C4-8977-CDF2621C781A");
Guid IID_IProperties2 = new Guid("6090AE4B-83A1-5474-A8D9-AF9B8C8DBD09");
var managedInterfaceInheritance = new ManagedInterfaceInheritance();
ccw = MarshalInspectable<object>.CreateMarshaler(managedInterfaceInheritance);
ccw.TryAs<IUnknownVftbl>(IID_IProperties1, out var propertiesCCW);
if (propertiesCCW == null)
{
    return 109;
}

ccw.TryAs<IUnknownVftbl>(IID_IProperties2, out var properties2CCW);
if (properties2CCW == null)
{
    return 110;
}

return 100;

sealed partial class ManagedProperties : IProperties1, IUriHandler
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

sealed partial class ManagedWarningClass : WarningClass, IUriHandler, IArtist
{
    public int Test => 4;

    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    public void Draw()
    {
    }

    public void Draw(int _)
    {
    }

    public int DrawTo()
    {
        return 0;
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}

// Used to test interfaces on base class where
// the child class has no WinRT interfaces.
sealed partial class ManagedWarningClass2 : WarningClass 
{
}

sealed partial class ManagedInterfaceInheritance : IProperties2
{
    private int _value;

    public int ReadWriteProperty { get => _value; set => _value = value; }
}

sealed partial class ManagedIntList : IList<int>
{
    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(int item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(int item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(int[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(int item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, int item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(int item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

sealed partial class ManagedDerivedList : IList<TestComponent.Derived>
{
    public TestComponent.Derived this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(TestComponent.Derived[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<TestComponent.Derived> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
