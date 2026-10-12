using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TestComponent;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

var instance = new Class();
TestComponentCSharp.Class instance2 = new TestComponentCSharp.Class();

unsafe
{
    Guid IID_IAgileObject = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");
    void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(instance);
    if (ptr == null ||
        Marshal.QueryInterface((nint)ptr, IID_IAgileObject, out nint ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 101;
    }

    var list = new List<int> { 0, 1, 2 };
    var retVal = SetAndGetBoxedValue(instance2, list);
    if (list != retVal)
    {
        return 102;
    }

    var IID_IListInt = new Guid("b939af5b-b45d-5489-9149-61442c1905fe");
    var IID_IEnumeratorInt = new Guid("81a643fb-f51c-5565-83c4-f96425777b66");
    ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(retVal);
    if (ptr == null ||
        Marshal.QueryInterface((nint)ptr, IID_IListInt, out nint iListCCW) != 0 ||
        iListCCW == IntPtr.Zero)
    {
        return 103;
    }

    if (Marshal.QueryInterface((nint)ptr, IID_IEnumeratorInt, out nint iEnumerableCCW) != 0 ||
        iEnumerableCCW == IntPtr.Zero)
    {
        return 104;
    }
}

IList<List<Point>> list2 = new List<List<Point>>();
instance2.IterableOfPointIterablesProperty = list2;

// Ensure that these don't crash but return null
if ((object)instance as IList<Point> != null)
{
    return 105;
}

if ((object)instance as IList<ManagedClass> != null)
{
    return 106;
}

// Use different element types so the three discovery paths cannot supply each other's marshalling
if (!IsList<InputStreamOptions>(instance2.GetInputStreamOptionsVector()))
{
    return 107;
}

object genericCastVector = instance2.GetInputStreamOptionsVector();

if (!ReferenceEquals(genericCastVector, AsList<InputStreamOptions>(genericCastVector)))
{
    return 108;
}

if (ReadCount<InputStreamOptions>(instance2.GetInputStreamOptionsVector()) != 3)
{
    return 109;
}

if (!Is<IList<FileAccessMode>>(instance2.GetFileAccessModeVector()))
{
    return 110;
}

object methodArgumentVector = instance2.GetFileAccessModeVector();

if (!ReferenceEquals(methodArgumentVector, As<IList<FileAccessMode>>(methodArgumentVector)))
{
    return 111;
}

if (!OuterIsList<CreationCollisionOption>(instance2.GetCreationCollisionOptionVector()))
{
    return 112;
}

object nestedCastVector = instance2.GetCreationCollisionOptionVector();

if (!ReferenceEquals(nestedCastVector, OuterAsList<CreationCollisionOption>(nestedCastVector)))
{
    return 113;
}

if (OuterReadCount<CreationCollisionOption>(instance2.GetCreationCollisionOptionVector()) != 3)
{
    return 114;
}

// Each operation gets a fresh opaque wrapper, so a successful type test cannot prime the cast
int dispatchResult = RunGenericDispatchChecks(instance2, cast: false);

if (dispatchResult != 100)
{
    return dispatchResult;
}

return RunGenericDispatchChecks(instance2, cast: true);

static int RunGenericDispatchChecks(TestComponentCSharp.Class component, bool cast)
{
    GenericDispatch.IListChecker interfaceChecker = new GenericDispatch.ListChecker();

    if (!interfaceChecker.Check<TestComponentCSharp.InterfaceDispatchElement>(
        GenericDispatch.Wrap(component.GetInterfaceDispatchVector()), cast))
    {
        return cast ? 116 : 115;
    }

    GenericDispatch.IDirectChecker directChecker = new GenericDispatch.DirectChecker();

    if (!directChecker.Check<TestComponentCSharp.DirectDispatchElement>(
        GenericDispatch.Wrap(component.GetDirectDispatchVector()), cast))
    {
        return cast ? 118 : 117;
    }

    GenericDispatch.AbstractChecker abstractChecker = new GenericDispatch.AbstractCheckerImpl();

    if (!abstractChecker.Check<TestComponentCSharp.AbstractDispatchElement>(
        GenericDispatch.Wrap(component.GetAbstractDispatchVector()), cast))
    {
        return cast ? 120 : 119;
    }

    GenericDispatch.VirtualChecker virtualChecker = new GenericDispatch.VirtualCheckerImpl();

    if (!virtualChecker.Check<TestComponentCSharp.VirtualDispatchElement>(
        GenericDispatch.Wrap(component.GetVirtualDispatchVector()), cast))
    {
        return cast ? 122 : 121;
    }

    GenericDispatch.VirtualCheckerImpl sealedChecker = new GenericDispatch.VirtualCheckerImpl();

    if (!sealedChecker.Check<TestComponentCSharp.SealedDispatchElement>(
        GenericDispatch.Wrap(component.GetSealedDispatchVector()), cast))
    {
        return cast ? 124 : 123;
    }

    // Control: IList<E> is already an explicit call-site argument, independent of dispatch discovery
    GenericDispatch.ITypeChecker typeChecker = new GenericDispatch.TypeChecker();

    if (!typeChecker.Check<IList<TestComponentCSharp.ClosedArgumentElement>>(
        GenericDispatch.Wrap(component.GetClosedArgumentVector()), cast))
    {
        return cast ? 126 : 125;
    }

    // Control: ordinary generic calls propagate the two-hop Cast<IList<T>> context
    if (!GenericDispatch.Forward<TestComponentCSharp.StaticChainElement>(
        GenericDispatch.Wrap(component.GetStaticChainVector()), cast))
    {
        return cast ? 128 : 127;
    }

    return 100;
}

static bool IsList<T>(object value) => value is IList<T>;

static object AsList<T>(object value) => (IList<T>)value;

static int ReadCount<T>(object value) => ((IList<T>)value).Count;

static bool Is<T>(object value) => value is T;

static object As<T>(object value) => (T)value;

static bool OuterIsList<T>(object value) => IsList<T>(value);

static object OuterAsList<T>(object value) => AsList<T>(value);

static int OuterReadCount<T>(object value) => ReadCount<T>(value);

object SetAndGetBoxedValue(TestComponentCSharp.Class instance, object val)
{
    instance.ObjectProperty = val;
    return instance.ObjectProperty;
}

class ManagedClass
{
    public int Number { get; set; }
}