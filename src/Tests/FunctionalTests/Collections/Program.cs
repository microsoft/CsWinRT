using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using test_component_derived.Nested;
using TestComponentCSharp;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

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

string[] stringArr = new string[] { "apples", "oranges", "pears" };
string[] stringArr2 = new string[stringArr.Length];
string[] outStringArr;
string[] retStringArr = instance2.Array12(stringArr, stringArr2, out outStringArr);
if (!AllEqual(stringArr, stringArr2, outStringArr, retStringArr))
{
    return 101;
}

TestComponent.Blittable[] blittableArr = new TestComponent.Blittable[] {
                new TestComponent.Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(TestComponent.ITests).GUID),
                new TestComponent.Blittable(10, 20, 30, 40, -50, -60, -70, 80.0f, 90.0, typeof(IStringable).GUID)
            };
TestComponent.Blittable[] blittableArr2 = new TestComponent.Blittable[blittableArr.Length];
TestComponent.Blittable[] outBlittableArr;
TestComponent.Blittable[] retBlittableArr = instance2.Array13(blittableArr, blittableArr2, out outBlittableArr);
if (!AllEqual(blittableArr, blittableArr2, outBlittableArr, retBlittableArr))
{
    return 101;
}

#if NET9_0_OR_GREATER

TestComponent.NonBlittable[] nonBlittableArr = new TestComponent.NonBlittable[] {
                new TestComponent.NonBlittable(false, 'X', "First", (long?)PropertyValue.CreateInt64(123)),
                new TestComponent.NonBlittable(true, 'Y', "Second", (long?)PropertyValue.CreateInt64(456)),
                new TestComponent.NonBlittable(false, 'Z', "Third", (long?)PropertyValue.CreateInt64(789))
            };
TestComponent.NonBlittable[] nonBlittableArr2 = new TestComponent.NonBlittable[nonBlittableArr.Length];
TestComponent.NonBlittable[] outNonBlittableArr;
TestComponent.NonBlittable[] retNonBlittableArr = instance2.Array14(nonBlittableArr, nonBlittableArr2, out outNonBlittableArr);
if (!AllEqual(nonBlittableArr, nonBlittableArr2, outNonBlittableArr, retNonBlittableArr))
{
    return 101;
}

#pragma warning disable CSWINRT3001 // Type or member is obsolete
TestComponent.Nested[] nestedArr = new TestComponent.Nested[]{
                new TestComponent.Nested(
                    new TestComponent.Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(TestComponent.ITests).GUID),
                    new TestComponent.NonBlittable(false, 'X', "First", (long?)PropertyValue.CreateInt64(123))),
                new TestComponent.Nested(
                    new TestComponent.Blittable(10, 20, 30, 40, -50, -60, -70, 80.0f, 90.0, typeof(IStringable).GUID),
                    new TestComponent.NonBlittable(true, 'Y', "Second", (long?)PropertyValue.CreateInt64(456))),
                new TestComponent.Nested(
                    new TestComponent.Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, WellKnownInterfaceIIDs.IID_IInspectable),
                    new TestComponent.NonBlittable(false, 'Z', "Third", (long?)PropertyValue.CreateInt64(789)))
            };
#pragma warning restore CSWINRT3001 // Type or member is obsolete
TestComponent.Nested[] nestedArr2 = new TestComponent.Nested[nestedArr.Length];
TestComponent.Nested[] outNestedArr;
TestComponent.Nested[] retNestedArr = instance2.Array15(nestedArr, nestedArr2, out outNestedArr);
if (!AllEqual(nestedArr, nestedArr2, outNestedArr, retNestedArr))
{
    return 101;
}

EnumValue[] enumArr = new EnumValue[] { EnumValue.One, EnumValue.Two };
instance.EnumsProperty = enumArr;
EnumValue[] retEnumArr = instance.EnumsProperty;
if (!AllEqual(enumArr, retEnumArr))
{
    return 101;
}

#endif

IStringable[] stringableArr = new IStringable[] {
                Windows.Data.Json.JsonValue.CreateNumberValue(3),
                Windows.Data.Json.JsonValue.CreateNumberValue(4),
                Windows.Data.Json.JsonValue.CreateNumberValue(5.0)
            };
IStringable[] stringableArr2 = new IStringable[stringableArr.Length];
IStringable[] outStringableArr;
IStringable[] retStringableArr = instance2.Array16(stringableArr, stringableArr2, out outStringableArr);
if (!AllEqual(stringableArr, stringableArr2, outStringableArr, retStringableArr))
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

propertySet.Add("test", new short[] { 1, 2, 3, 4 });
if (propertySet["test"] is not short[] shortArray || shortArray.Length != 4)
{
    return 101;
}

if (propertySet["delta"] is not byte[] byteArray || byteArray.Length != 4 || byteArray[1] != 2)
{
    return 101;
}

if (propertySet["echo"] is not Point[] pointArray || pointArray.Length != 3 || pointArray[1].X != 2 || pointArray[1].Y != 2)
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
var names = profile?.GetNetworkNames();

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

instance.BindableIterableProperty = CustomClass.DictionaryInstance;
instance.BindableIterableProperty = CustomClass.DictionaryInstance2;

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

if (sum != 7)
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

if (sum != 7)
{
    return 101;
}

var nullableDoubleList = new List<double?>() { 1, 2, null, 3, 4, null};
var result = instance.Calculate(nullableDoubleList);
if (result != 10)
{
    return 101;
}

sum = 0;

var nullableIntList = instance.GetNullableIntList();
foreach (var num in nullableIntList)
{
    if (num.HasValue)
    {
        sum += num.Value;
    }
}

if (sum != 3)
{
    return 101;
}

// Testing to ensure no exceptions from any of the analyzers while building.
Action<int, int> s = (a, b) => { _ = a + b; };
ActionToFunction(s)(2, 3);

var intToListDict = instance.GetIntToListDictionary();
intToListDict.Add(2, new List<EnumValue> { EnumValue.One, EnumValue.Two });
intToListDict[4] = new Collection<EnumValue> { EnumValue.Two };
if (intToListDict.Count != 3 ||
    intToListDict[1].First() != EnumValue.One)
{
    return 101;
}

if (intToListDict[2].Count != 2 || intToListDict[2].First() != EnumValue.One)
{
    return 101;
}

if (intToListDict[4].Count != 1 || intToListDict[4].First() != EnumValue.Two)
{
    return 101;
}

// Make sure for collections of value types that they don't project IEnumerable<object>
// as it isn't a covariant interface.
if (instance.CheckForBindableObjectInterface(new List<int>()) ||
    instance.CheckForBindableObjectInterface(new List<EnumValue>()) ||
    instance.CheckForBindableObjectInterface(new List<System.DateTimeOffset>()) ||
    instance.CheckForBindableObjectInterface(new Dictionary<string, System.DateTimeOffset>()))
{
    return 102;
}

// Make sure for collections of object types that they do project IEnumerable<object>
// as it is an covariant interface.
if (!instance.CheckForBindableObjectInterface(new List<object>()) ||
    !instance.CheckForBindableObjectInterface(new List<CustomClass>()) ||
    !instance.CheckForBindableObjectInterface(new List<Class>()) ||
    !instance.CheckForBindableObjectInterface(new List<IProperties1>()))
{
    return 103;
}

var stringArr3 = (string[])Class.BoxedStringArray;
if (stringArr3.Length != 3 || stringArr3[0] != "one" || stringArr3[1] != "two" || stringArr3[2] != "three")
{
    return 104;
}

var intArr = (int[])Class.BoxedInt32Array;
if (intArr.Length != 3 || intArr[0] != 1 || intArr[1] != 2 || intArr[2] != 3)
{
    return 104;
}

#if NET9_0_OR_GREATER

var timeSpanArr = (TimeSpan[])Class.BoxedTimeSpanArray;
if (timeSpanArr.Length != 2 || timeSpanArr[0] != TimeSpan.FromSeconds(10) || timeSpanArr[1] != TimeSpan.FromSeconds(20))
{
    return 105;
}

#endif

var objectArr = (object[])instance.BoxedObjectArray;
if (objectArr.Length != 2 || objectArr[0] is not Class c || c != instance || objectArr[1] is not Class c2 || c2 != instance)
{
    return 105;
}

return 100;

static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) => list.All((y) => x.SequenceEqual(y));

static bool AllEqual<T>(T[] x, params T[][] list) => list.All((y) => x.SequenceEqual(y));

static Func<TA1, TA2, TA1> ActionToFunction<TA1, TA2>(Action<TA1, TA2> action) =>
    (a1, a2) =>
    {
        action(a1, a2);
        return a1;
    };

sealed partial class CustomClass : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public static IReadOnlyList<CustomClass> Instances { get; } = new CustomClass[] { };

    public static IReadOnlyDictionary<string, CustomClass> DictionaryInstance => new Dictionary<string, CustomClass>();

    public static IReadOnlyDictionary<int, CustomClass> DictionaryInstance2
    {
        get
        {
            return new Dictionary<int, CustomClass>();
        }
    }
}

sealed partial class CustomObservableCollection : System.Collections.ObjectModel.ObservableCollection<CustomClass>
{
    public int CustomCount => Items.Count;
}

sealed partial class CustomGenericObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
{
    public int CustomCount => Items.Count;
}
