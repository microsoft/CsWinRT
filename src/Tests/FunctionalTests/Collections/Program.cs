using System.Collections.Generic;
using System.Linq;
using test_component_derived.Nested;
using TestComponentCSharp;

var instance = new Class();

var expected = new int[] { 0, 1, 2 };

instance.BindableIterableProperty = expected;
if (expected != instance.BindableIterableProperty)
{
    return 101;
}

var instance2 = TestComponent.TestRunner.MakeTests();
var dict = new Dictionary<string, string>()
{
    ["apples"] = "1",
    ["oranges"] = "2",
    ["pears"] = "3"
};
IDictionary<string, string> outDict = null;
var retDict = instance2.Collection3(dict, out outDict);
if (!SequencesEqual(dict, outDict, retDict))
{
    return 101;
}

float[] floatArr = new float[] { 1.0f, 2.0f, 3.0f };
float[] floatArr2 = new float[floatArr.Length];
float[] outFloatArr;
float[] retFloatArr = instance2.Array9(floatArr, floatArr2, out outFloatArr);
if (!AllEqual(floatArr, floatArr2, outFloatArr, retFloatArr))
{
    return 101;
}

var hierarchyDAsObjectList = HierarchyC.CreateDerivedHierarchyDAsObjectList();
foreach (var hierarchyDAsObject in hierarchyDAsObjectList)
{
    var hierarchyDAsHierarchyCCast = (HierarchyC)hierarchyDAsObject;
    if (hierarchyDAsHierarchyCCast.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
           hierarchyDAsHierarchyCCast.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
    {
        return 101;
    }
}

var hierarchyDAsHierarchyCList = HierarchyC.CreateDerivedHierarchyDList();
foreach (var hierarchyDAsHierarchyC in hierarchyDAsHierarchyCList)
{
    if (hierarchyDAsHierarchyC.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
           hierarchyDAsHierarchyC.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
    {
        return 101;
    }
}

return 100;

static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) => list.All((y) => x.SequenceEqual(y));

static bool AllEqual<T>(T[] x, params T[][] list) => list.All((y) => x.SequenceEqual(y));
