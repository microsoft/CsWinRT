using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TestComponent;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete
// TODO: This shouldn't be needed if transitive references are detected correctly.
[assembly: WindowsRuntime.WindowsRuntimeReferenceAssembly]

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("Test")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
#pragma warning restore CSWINRT3001 // Type or member is obsolete

var instance = new Class();
TestComponentCSharp.Class instance2 = new TestComponentCSharp.Class();

unsafe
{
    Guid IID_IAgileObject = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");
    void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(instance);
    if (ptr == null ||
        Marshal.QueryInterface((nint) ptr, IID_IAgileObject, out nint ptr2) != 0 ||
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

return 100;

object SetAndGetBoxedValue(TestComponentCSharp.Class instance, object val)
{
    instance.ObjectProperty = val;
    return instance.ObjectProperty;
}

class ManagedClass
{
    public int Number { get; set; }
}