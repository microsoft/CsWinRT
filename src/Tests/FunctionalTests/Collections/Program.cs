using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        // This is set in a failure state as we are testing the TestLibrary module having
        // an entry on the vtable lookup table without it actually being loaded or used yet.
        // So our intention is to not actually set it.  This is just to ensure when in this
        // scenario we don't run into other crashes from other lookup tables being registered.
        instance.BindableIterableProperty = new List<TestLibrary.TestClass>();
        return 101;
    }
}

var propertySet = Class.PropertySet;
if (propertySet["beta"] is not string str || str != "second")
{
    return 101;
}

var types = Class.ListOfTypes;
if (types.Count != 2 || types[0] != typeof(Class))
{
    return 101;
}

var cancellationDictionary = new Dictionary<string, CancellationTokenSource>();
instance.BindableIterableProperty = cancellationDictionary;
if (cancellationDictionary != instance.BindableIterableProperty)
{
    return 101;
}

var observableCollection = new System.Collections.ObjectModel.ObservableCollection<string>();
instance.BindableIterableProperty = observableCollection;
if (observableCollection != instance.BindableIterableProperty)
{
    return 101;
}

var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
var names = profile.GetNetworkNames();

List<string> networkNames = new();
if (names?.Count > 0)
{
    networkNames.AddRange(names);
}

var exceptionList = new List<Exception>();
instance.BindableIterableProperty = exceptionList;
if (exceptionList != instance.BindableIterableProperty)
{
    return 101;
}

// Test for collection expression
// instance.SetCharIterable(['c']);

return 100;

static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) => list.All((y) => x.SequenceEqual(y));

static bool AllEqual<T>(T[] x, params T[][] list) => list.All((y) => x.SequenceEqual(y));