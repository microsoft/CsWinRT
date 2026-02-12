using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows.Input;
using TestComponent;
using TestComponentCSharp;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

int failure;

TypeCase[] TestCases =
[
    // --------------------
    // Projected WinRT Types (existing e2e cases)
    // --------------------
    new(typeof(TestComponentCSharp.Class), "TestComponentCSharp.Class", "Metadata", 101),

    // --------------------
    // Custom Type (existing e2e case)
    // --------------------
    new(typeof(TestType), "TestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom", 150),

    // --------------------
    // Mapped System.* Types Primitive Types
    // --------------------
    new(typeof(bool),   "Boolean", "Primitive", 200),
    new(typeof(byte),   "UInt8",   "Primitive", 201),
    new(typeof(char),   "Char16",  "Primitive", 202),
    new(typeof(double), "Double",  "Primitive", 203),
    new(typeof(short),  "Int16",   "Primitive", 204),
    new(typeof(int),    "Int32",   "Primitive", 205),
    new(typeof(long),   "Int64",   "Primitive", 206),
    new(typeof(float),  "Single",  "Primitive", 207),
    new(typeof(ushort), "UInt16",  "Primitive", 208),
    new(typeof(uint),   "UInt32",  "Primitive", 209),
    new(typeof(ulong),  "UInt64",  "Primitive", 210),

    // --------------------
    // Mapped System.* Types Metadata Types
    // --------------------
    new(typeof(DateTimeOffset),      "Windows.Foundation.DateTime",              "Metadata", 250),
    new(typeof(EventHandler<Guid>),  "Windows.Foundation.EventHandler`1<Guid>",  "Metadata", 251),
    new(typeof(Exception),           "Windows.Foundation.HResult",               "Metadata", 252),
    new(typeof(Guid),                "Guid",                                      "Metadata", 253),
    new(typeof(IDisposable),         "Windows.Foundation.IClosable",             "Metadata", 254),
    new(typeof(IServiceProvider),    "Microsoft.UI.Xaml.IXamlServiceProvider",   "Metadata", 255, true),
    new(typeof(object),              "Object",                                    "Metadata", 256),
    new(typeof(string),              "String",                                    "Metadata", 257),
    new(typeof(TimeSpan),            "Windows.Foundation.TimeSpan",              "Metadata", 258),
    new(typeof(Type),                "Windows.UI.Xaml.Interop.TypeName",         "Metadata", 259),
    new(typeof(Uri),                 "Windows.Foundation.Uri",                   "Metadata", 260),
    new(typeof(ICommand),            "Microsoft.UI.Xaml.Input.ICommand",         "Metadata", 261),

    // --------------------
    // Mapped System.Collections.* Types
    // --------------------
    new(typeof(IEnumerable),          "Microsoft.UI.Xaml.Interop.IBindableIterable",                 "Metadata", 300),
    new(typeof(IEnumerator),          "Microsoft.UI.Xaml.Interop.IBindableIterator",                 "Metadata", 301),
    new(typeof(IList),                "Microsoft.UI.Xaml.Interop.IBindableVector",                   "Metadata", 302),
    new(typeof(IReadOnlyList<long>),  "Windows.Foundation.Collections.IVectorView`1<Int64>",         "Metadata", 303),

    // --------------------
    // Mapped System.Collections.Specialized* Types
    // --------------------
    new(typeof(INotifyCollectionChanged),          "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged",          "Metadata", 350, true),
    new(typeof(NotifyCollectionChangedEventArgs),  "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",  "Metadata", 351),
    new(typeof(NotifyCollectionChangedEventHandler),"Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler","Metadata", 352),

    // --------------------
    // Mapped System.ComponentModel.* Types
    // --------------------
    new(typeof(DataErrorsChangedEventArgs), "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", "Metadata", 400),
    new(typeof(INotifyDataErrorInfo),       "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo",       "Metadata", 401),
    new(typeof(INotifyPropertyChanged),          "Microsoft.UI.Xaml.Data.INotifyPropertyChanged",     "Metadata", 402),
    new(typeof(PropertyChangedEventArgs),   "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",   "Metadata", 403),
    new(typeof(PropertyChangedEventHandler),"Microsoft.UI.Xaml.Data.PropertyChangedEventHandler","Metadata", 404),

    // --------------------
    // Mapped System.Numerics.* Types
    // --------------------
    new(typeof(Matrix3x2),  "Windows.Foundation.Numerics.Matrix3x2",  "Metadata", 450),
    new(typeof(Matrix4x4),  "Windows.Foundation.Numerics.Matrix4x4",  "Metadata", 451),
    new(typeof(Plane),      "Windows.Foundation.Numerics.Plane",      "Metadata", 452),
    new(typeof(Quaternion), "Windows.Foundation.Numerics.Quaternion", "Metadata", 453),
    new(typeof(Vector2),    "Windows.Foundation.Numerics.Vector2",    "Metadata", 454),
    new(typeof(Vector3),    "Windows.Foundation.Numerics.Vector3",    "Metadata", 455),
    new(typeof(Vector4),    "Windows.Foundation.Numerics.Vector4",    "Metadata", 456),

    // --------------------
    // Mapped Windows.Foundation.* Types
    // --------------------
    new(typeof(AsyncActionCompletedHandler), "Windows.Foundation.AsyncActionCompletedHandler", "Metadata", 500),
    new(typeof(IAsyncAction),                "Windows.Foundation.IAsyncAction",                "Metadata", 501),
    new(typeof(IAsyncInfo),                  "Windows.Foundation.IAsyncInfo",                  "Metadata", 502),
    new(typeof(Point),                       "Windows.Foundation.Point",                       "Metadata", 503),
    new(typeof(Rect),                        "Windows.Foundation.Rect",                        "Metadata", 504),
    new(typeof(Size),                        "Windows.Foundation.Size",                        "Metadata", 505),
    new(typeof(IStringable),                 "Windows.Foundation.IStringable",                 "Metadata", 506),

    // --------------------
    // Mapped Windows.Foundation.Collections.* Types
    // --------------------
    new(typeof(Windows.Foundation.Collections.CollectionChange),       "Windows.Foundation.Collections.CollectionChange",        "Metadata", 550),
    new(typeof(Windows.Foundation.Collections.IVectorChangedEventArgs),"Windows.Foundation.Collections.IVectorChangedEventArgs", "Metadata", 551),

    // --------------------
    // Mapped WindowsRuntime.InteropServices.* Types
    // --------------------
    new(typeof(EventRegistrationToken), "WindowsRuntime.InteropServices.EventRegistrationToken", "Metadata", 600),

    // --------------------
    // Projected WinRT Types
    // --------------------
    new(typeof(TestComponent.Class),                      "TestComponent.Class",                      "Metadata", 650),
    new(typeof(TestComponent.Nested),                     "TestComponent.Nested",                     "Metadata", 651),
    new(typeof(TestComponent.IRequiredOne),               "TestComponent.IRequiredOne",               "Metadata", 652),
    new(typeof(TestComponent.Param6Handler),              "TestComponent.Param6Handler",              "Metadata", 653),
    new(typeof(TestComponentCSharp.Class),                "TestComponentCSharp.Class",                "Metadata", 654),
    new(typeof(TestComponentCSharp.ComposedBlittableStruct),"TestComponentCSharp.ComposedBlittableStruct","Metadata", 655),
    new(typeof(TestComponentCSharp.IArtist),              "TestComponentCSharp.IArtist",              "Metadata", 656),
    new(typeof(TestComponentCSharp.EnumValue),            "TestComponentCSharp.EnumValue",            "Metadata", 657),
    new(typeof(TestComponentCSharp.EventHandler0),        "TestComponentCSharp.EventHandler0",        "Metadata", 658),

    // --------------------
    // Nullable Types
    // --------------------
    new(typeof(long?),                      "Windows.Foundation.IReference`1<Int64>",                                               "Metadata", 700),
    new(typeof(Point?),                     "Windows.Foundation.IReference`1<Windows.Foundation.Point>",                            "Metadata", 701),
    new(typeof(Vector3?),                   "Windows.Foundation.IReference`1<Windows.Foundation.Numerics.Vector3>",                 "Metadata", 702),
    new(typeof(Guid?),                      "Windows.Foundation.IReference`1<Guid>",                                                "Metadata", 703),
    new(typeof(IList<int?>),                "Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int32>>",     "Metadata", 704),
    new(typeof(TestComponentCSharp.EnumValue?),       "Windows.Foundation.IReference`1<TestComponentCSharp.EnumValue>",             "Metadata", 705),
    new(typeof(TestComponentCSharp.BlittableStruct?), "Windows.Foundation.IReference`1<TestComponentCSharp.BlittableStruct>",       "Metadata", 706),
    new(typeof(int?[]),                     "Windows.Foundation.IReferenceArray`1<Windows.Foundation.IReference`1<Int32>>",         "Metadata", 707),

    // --------------------
    // Generic Types
    // --------------------
    new(typeof(IList<long>),                         "Windows.Foundation.Collections.IVector`1<Int64>",                                            "Metadata", 750),
    new(typeof(IList<TestComponentCSharp.EventWithGuid>),"Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid>",           "Metadata", 751),
    new(typeof(IList<TestComponentCSharp.Class>),     "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class>",                        "Metadata", 752),
    new(typeof(IEnumerator<TestComponentCSharp.Class>),"Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class>",                     "Metadata", 753),
    new(typeof(IEnumerable<TestComponentCSharp.Class>),"Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class>",                     "Metadata", 754),
    new(typeof(EventHandler<TestComponentCSharp.Class>),"Windows.Foundation.EventHandler`1<TestComponentCSharp.Class>",                            "Metadata", 755),
    new(typeof(KeyValuePair<object, TestComponentCSharp.Class>),"Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class>", "Metadata", 756),
    new(typeof(KeyValuePair<object, object>),         "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>",                             "Metadata", 757),
    new(typeof(KeyValuePair<string, TestComponentCSharp.Class>),"Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>", "Metadata", 758),

    // --------------------
    // Nested Generics
    // --------------------
    new(typeof(EventHandler<IList<long>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>>",                                         "Metadata", 800),
    new(typeof(EventHandler<KeyValuePair<object, object>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>",        "Metadata", 801),
    new(typeof(EventHandler<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata", 802),
    new(typeof(IList<KeyValuePair<object, object>>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>",         "Metadata", 803),
    new(typeof(IEnumerator<IEnumerable<object>>), "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>",                       "Metadata", 804),
    new(typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata", 805),

    // --------------------
    // Custom Types
    // --------------------
    new(typeof(TestCSharp), "TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom", 850),

    new(typeof(IList<TestCSharp>),
        "System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        851),

    new(typeof(ITestCSharp<double>),
        "ITestCSharp`1[[System.Double, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "Custom",
        852),

    new(typeof(KeyValuePair<string, TestCSharp>),
        "System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        853),

    new(typeof(EventHandler<IList<TestCSharp>>),
        "System.EventHandler`1[[System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        854),

    new(typeof(EventHandler<TestCSharp>),
        "System.EventHandler`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        855),

    new(typeof(DelegateTestCSharp<Guid>),
        "DelegateTestCSharp`1[[System.Guid, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "Custom",
        856),

    new(typeof(WeakReference<TestComponent.Class>),
        "System.WeakReference`1[[TestComponent.Class, WinRT.Projection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        857),

    new(typeof(KeyValuePair<int, WeakReference<object>>),
        "System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.WeakReference`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        858),

    new(typeof(ICollection<object>),
        "System.Collections.Generic.ICollection`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        859),

    new(typeof(KeyValuePair<object, object>?),
        "System.Nullable`1[[System.Collections.Generic.KeyValuePair`2[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        860),

    // --------------------
    // Arrays
    // --------------------
    new(typeof(object[]),  "Windows.Foundation.IReferenceArray`1<Object>",                                       "Metadata", 950),
    new(typeof(long[]),    "Windows.Foundation.IReferenceArray`1<Int64>",                                        "Metadata", 951),
    new(typeof(TestComponentCSharp.Class[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.Class>",  "Metadata", 952),
    new(typeof(TestComponentCSharp.ComposedBlittableStruct[]),
        "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.ComposedBlittableStruct>",
        "Metadata",
        953),

    new(typeof(TestComponentCSharp.IArtist[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.IArtist>", "Metadata", 954),
    new(typeof(TestComponentCSharp.EnumValue[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.EnumValue>", "Metadata", 955),
    new(typeof(TestComponentCSharp.EventHandler0[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.EventHandler0>", "Metadata", 956),
    new(typeof(Point[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Point>", "Metadata", 957),
    new(typeof(IList<long>[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IVector`1<Int64>>", "Metadata", 958),
    new(typeof(IList<long?>[]),
        "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int64>>>",
        "Metadata",
        959),

    new(typeof(KeyValuePair<object, object>[]),
        "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>",
        "Metadata",
        960),

    // --------------------
    // Others
    // --------------------
    new(typeof(NotifyCollectionChangedAction),  "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction",                                 "Metadata", 1000),
    new(typeof(NotifyCollectionChangedAction?), "Windows.Foundation.IReference`1<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction>", "Metadata", 1001),
];

UntrimmedTypes();

// Convert to Managed Trimmed Metadata NoMetadataTypeInfo Test Case
// Goes into NoMetadataTypeInfo codepath for Type.cs ConvertToManaged
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
if ((failure = CheckTrimmed("TestComponentCSharp.TestTypeTrimmed Metadata", 101)) != 0)
{
    return failure;
}

if ((failure = RunCases(TestCases)) != 0)
{
    return failure;
}

return 100;

static int RunCases(ReadOnlySpan<TypeCase> cases)
{
    for (int i = 0; i < cases.Length; i++)
    {
        TypeCase c = cases[i];

        if (isAOT() && cases[i].ignoreAOT)
        {
            Console.WriteLine($"Skipping {c.Name}");

            continue;
        }

        string expected = $"{c.Name} {c.Kind}";

        int failure = CheckType(c.Type, expected, c.ErrorCode);

        if (failure != 0)
        {
            return failure;
        }
    }

    return 0;
}

static int CheckTrimmed(string expected, int errorCode)
{
    TestType testTypeClass = new();
    return FailIfNotEqual(new SetTypeProperties().GetPropertyInfoTestTypeTrimmed(testTypeClass), expected, errorCode);
}

static int CheckType(Type type, string expected, int errorCode)
{
    TestType testTypeClass = new();
    return FailIfNotEqual(new SetTypeProperties().GetPropertyInfoTestType(testTypeClass, type), expected, errorCode);
}

static int FailIfNotEqual(string actual, string expected, int errorCode)
{
    if (actual != expected)
    {
        return errorCode;
    }

    return 0;
}

void UntrimmedTypes()
{
    // Untrimming INotifyPropertyChanged
    var person = new Person();

    person.PropertyChanged += (sender, args) =>
    {
        Console.WriteLine($"Untrim INotifyPropertyChanged");
    };

    person.Name = "TestName";
}

static bool isAOT()
{
    return !RuntimeFeature.IsDynamicCodeSupported;
}

readonly record struct TypeCase(Type Type, string Name, string Kind, int ErrorCode, bool ignoreAOT = false);

sealed class TestType : IType
{
    public Type TypeProperty { get; set; }
}

sealed class TestCSharp
{
}

interface ITestCSharp<T>
{
}

delegate void DelegateTestCSharp<T>(object sender, T value);

public sealed class Person : INotifyPropertyChanged
{
    private string _name;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }
}
