using System;
using System.Collections.Generic;
using System.ComponentModel;
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

if (names is IList<double> networkNamesList)
{
    return 101;
}

var exceptionList = new List<Exception>();
instance.BindableIterableProperty = exceptionList;
if (exceptionList != instance.BindableIterableProperty)
{
    return 101;
}

var exceptionList2 = new List<ArgumentException>();
instance.BindableIterableProperty = exceptionList2;
if (exceptionList2 != instance.BindableIterableProperty)
{
    return 101;
}

instance.BindableIterableProperty = CustomClass.Instances;
if (CustomClass.Instances != instance.BindableIterableProperty)
{
    return 101;
}

var customObservableCollection = new CustomObservableCollection();
instance.BindableIterableProperty = customObservableCollection;
if (customObservableCollection != instance.BindableIterableProperty)
{
    return 101;
}

var customIntObservableCollection = new CustomGenericObservableCollection<int>();
instance.BindableIterableProperty = customIntObservableCollection;
if (customIntObservableCollection != instance.BindableIterableProperty)
{
    return 101;
}

var uriList = new List<Uri>();
instance.BindableIterableProperty = uriList;
if (uriList != instance.BindableIterableProperty)
{
    return 101;
}

var dateTimeOffsetList = new List<System.DateTimeOffset>();
instance.BindableIterableProperty = dateTimeOffsetList;
if (dateTimeOffsetList != instance.BindableIterableProperty)
{
    return 101;
}

// Test sccenarios where the actual implementation type of the result or its RCW factory
// hasn't been initialized and the statically declared type is a derived generic interface.
var enums = instance.GetEnumIterable();
int count = 0;
foreach (var curEnum in enums)
{
    count++;
}

if (count != 2)
{
    return 101;
}

count = 0;
var disposableClasses = instance.GetClassIterable();
foreach (var curr in disposableClasses)
{
    count++;
}

if (count != 2)
{
    return 101;
}

int sum = 0;
var enumerator = instance.GetIteratorForCollection(expected);
while (enumerator.MoveNext())
{
    sum += enumerator.Current;
}

if (sum != 3)
{
    return 101;
}

sum = 0;

CustomIterableTest customIterableTest = new CustomIterableTest();
foreach (var i in customIterableTest)
{
    sum += i;
}

if (sum != 6)
{
    return 101;
}

sum = 0;

var arr = new int[] { 2, 4, 6 };
CustomIterableTest customIterableTest2 = new CustomIterableTest(arr);
foreach (var i in customIterableTest2)
{
    sum += i;
}

if (sum != 12)
{
    return 101;
}

CustomIteratorTest iterator = new CustomIteratorTest();
iterator.MoveNext();
if (iterator.Current != 2)
{
    return 101;
}

sum = 0;

var customIterableTest3 = CustomIterableTest.CreateWithCustomIterator();
foreach (var i in customIterableTest3)
{
    sum += i;
}

if (sum != 6)
{
    return 101;
}

return 100;

static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) => list.All((y) => x.SequenceEqual(y));

static bool AllEqual<T>(T[] x, params T[][] list) => list.All((y) => x.SequenceEqual(y));

sealed partial class CustomClass : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public static IReadOnlyList<CustomClass> Instances { get; } = new CustomClass[] { };
}

sealed partial class CustomObservableCollection : System.Collections.ObjectModel.ObservableCollection<CustomClass>
{
    public int CustomCount => Items.Count;
}

sealed partial class CustomGenericObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
{
    public int CustomCount => Items.Count;
}
