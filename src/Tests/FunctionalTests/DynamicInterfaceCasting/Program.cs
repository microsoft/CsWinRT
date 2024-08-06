using System;
using System.Collections.Generic;
using TestComponent;
using Windows.Foundation;
using WinRT;
using WinRT.Interop;

var instance = new Class();
var agileObject = (IAgileObject)(IWinRTObject)instance;
if (agileObject == null)
{
    return 101;
}

TestComponentCSharp.Class instance2 = new TestComponentCSharp.Class();
var list = new List<int> { 0, 1, 2 };
var retVal = SetAndGetBoxedValue(instance2, list);
if (list != retVal)
{
    return 102;
}

var IID_IListInt = new Guid("b939af5b-b45d-5489-9149-61442c1905fe");
var IID_IEnumeratorInt = new Guid("81a643fb-f51c-5565-83c4-f96425777b66");
var ccw = MarshalInspectable<object>.CreateMarshaler(retVal);
ccw.TryAs<IUnknownVftbl>(IID_IListInt, out var iListCCW);
if (iListCCW == null)
{
    return 103;
}

ccw.TryAs<IUnknownVftbl>(IID_IEnumeratorInt, out var iEnumerableCCW);
if (iEnumerableCCW == null)
{
    return 104;
}

IList<List<Point>> list2 = new List<List<Point>>();
instance2.IterableOfPointIterablesProperty = list2;

// Ensure that these don't crash but return null
if ((IWinRTObject)instance as IList<Point> != null)
{
    return 105;
}

if ((IWinRTObject)instance as IList<ManagedClass> != null)
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