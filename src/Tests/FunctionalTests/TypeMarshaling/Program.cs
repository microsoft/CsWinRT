using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
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
    new(typeof(TestComponentCSharp.Class), "TestComponentCSharp.Class", "Metadata", 102),

    // --------------------
    // Custom Type (existing e2e case)
    // --------------------
    new(typeof(TestType), "TestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom", 103),

    // --------------------
    // Mapped System.* Types Primitive Types
    // --------------------
    new(typeof(bool),  "Boolean", "Primitive", 104),
    new(typeof(byte),  "UInt8",   "Primitive", 105),
    new(typeof(char),  "Char16",  "Primitive", 106),
    new(typeof(double),"Double",  "Primitive", 107),
    new(typeof(short), "Int16",   "Primitive", 108),
    new(typeof(int),   "Int32",   "Primitive", 109),
    new(typeof(long),  "Int64",   "Primitive", 110),
    new(typeof(float), "Single",  "Primitive", 111),
    new(typeof(ushort),"UInt16",  "Primitive", 112),
    new(typeof(uint),  "UInt32",  "Primitive", 113),
    new(typeof(ulong), "UInt64",  "Primitive", 114),

    // --------------------
    // Mapped System.* Types Metadata Types
    // --------------------
    new(typeof(DateTimeOffset),     "Windows.Foundation.DateTime",             "Metadata", 115),
    new(typeof(EventHandler<Guid>), "Windows.Foundation.EventHandler`1<Guid>", "Metadata", 116),
    new(typeof(Exception),          "Windows.Foundation.HResult",              "Metadata", 117),
    new(typeof(Guid),               "Guid",                                     "Metadata", 118),
    new(typeof(IDisposable),        "Windows.Foundation.IClosable",            "Metadata", 119),
    new(typeof(IServiceProvider),   "Microsoft.UI.Xaml.IXamlServiceProvider",  "Metadata", 120),
    new(typeof(object),             "Object",                                   "Metadata", 121),
    new(typeof(string),             "String",                                   "Metadata", 122),
    new(typeof(TimeSpan),           "Windows.Foundation.TimeSpan",             "Metadata", 123),
    new(typeof(Type),               "Windows.UI.Xaml.Interop.TypeName",        "Metadata", 124),
    new(typeof(Uri),                "Windows.Foundation.Uri",                  "Metadata", 125),
    new(typeof(ICommand),           "Microsoft.UI.Xaml.Input.ICommand",        "Metadata", 126),

    // --------------------
    // Mapped System.Collections.* Types
    // --------------------
    new(typeof(IEnumerable),         "Microsoft.UI.Xaml.Interop.IBindableIterable",          "Metadata", 127),
    new(typeof(IEnumerator),         "Microsoft.UI.Xaml.Interop.IBindableIterator",          "Metadata", 128),
    new(typeof(IList),               "Microsoft.UI.Xaml.Interop.IBindableVector",            "Metadata", 129),
    new(typeof(IReadOnlyList<long>), "Windows.Foundation.Collections.IVectorView`1<Int64>",  "Metadata", 130),

    // --------------------
    // Mapped System.Collections.Specialized* Types
    // --------------------
    new(typeof(INotifyCollectionChanged),         "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged",         "Metadata", 131),
    new(typeof(NotifyCollectionChangedEventArgs), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", "Metadata", 132),
    new(typeof(NotifyCollectionChangedEventHandler),"Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler","Metadata", 133),

    // --------------------
    // Mapped System.ComponentModel.* Types
    // --------------------
    new(typeof(DataErrorsChangedEventArgs), "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", "Metadata", 134),
    new(typeof(INotifyDataErrorInfo),       "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo",       "Metadata", 135),
    new(typeof(INotifyPropertyChanged),     "Microsoft.UI.Xaml.Data.INotifyPropertyChanged",     "Metadata", 136),
    new(typeof(PropertyChangedEventArgs),   "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",   "Metadata", 137),
    new(typeof(PropertyChangedEventHandler),"Microsoft.UI.Xaml.Data.PropertyChangedEventHandler","Metadata", 138),

    // --------------------
    // Mapped System.Numerics.* Types
    // --------------------
    new(typeof(Matrix3x2),  "Windows.Foundation.Numerics.Matrix3x2",  "Metadata", 139),
    new(typeof(Matrix4x4),  "Windows.Foundation.Numerics.Matrix4x4",  "Metadata", 140),
    new(typeof(Plane),      "Windows.Foundation.Numerics.Plane",      "Metadata", 141),
    new(typeof(Quaternion), "Windows.Foundation.Numerics.Quaternion", "Metadata", 142),
    new(typeof(Vector2),    "Windows.Foundation.Numerics.Vector2",    "Metadata", 143),
    new(typeof(Vector3),    "Windows.Foundation.Numerics.Vector3",    "Metadata", 144),
    new(typeof(Vector4),    "Windows.Foundation.Numerics.Vector4",    "Metadata", 145),

    // --------------------
    // Mapped Windows.Foundation.* Types
    // --------------------
    new(typeof(AsyncActionCompletedHandler), "Windows.Foundation.AsyncActionCompletedHandler", "Metadata", 146),
    new(typeof(IAsyncAction),                "Windows.Foundation.IAsyncAction",                "Metadata", 147),
    new(typeof(IAsyncInfo),                  "Windows.Foundation.IAsyncInfo",                  "Metadata", 148),
    new(typeof(Point),                       "Windows.Foundation.Point",                       "Metadata", 149),
    new(typeof(Rect),                        "Windows.Foundation.Rect",                        "Metadata", 150),
    new(typeof(Size),                        "Windows.Foundation.Size",                        "Metadata", 151),

    // --------------------
    // Mapped Windows.Foundation.Collections.* Types
    // --------------------
    new(typeof(Windows.Foundation.Collections.CollectionChange),      "Windows.Foundation.Collections.CollectionChange",      "Metadata", 152),
    new(typeof(Windows.Foundation.Collections.IVectorChangedEventArgs),"Windows.Foundation.Collections.IVectorChangedEventArgs","Metadata", 153),

    // --------------------
    // Mapped WindowsRuntime.InteropServices.* Types
    // --------------------
    new(typeof(EventRegistrationToken), "WindowsRuntime.InteropServices.EventRegistrationToken", "Metadata", 154),

    // --------------------
    // Projected WinRT Types
    // --------------------
    new(typeof(TestComponent.Class),             "TestComponent.Class",             "Metadata", 155),
    new(typeof(TestComponent.Nested),            "TestComponent.Nested",            "Metadata", 156),
    new(typeof(TestComponent.IRequiredOne),      "TestComponent.IRequiredOne",      "Metadata", 157),
    new(typeof(TestComponent.Param6Handler),     "TestComponent.Param6Handler",     "Metadata", 158),
    new(typeof(TestComponentCSharp.Class),       "TestComponentCSharp.Class",       "Metadata", 159),
    new(typeof(TestComponentCSharp.ComposedBlittableStruct), "TestComponentCSharp.ComposedBlittableStruct", "Metadata", 160),
    new(typeof(TestComponentCSharp.IArtist),     "TestComponentCSharp.IArtist",     "Metadata", 161),
    new(typeof(TestComponentCSharp.EnumValue),   "TestComponentCSharp.EnumValue",   "Metadata", 162),
    new(typeof(TestComponentCSharp.EventHandler0),"TestComponentCSharp.EventHandler0","Metadata", 163),

    // --------------------
    // Nullable Types
    // --------------------
    new(typeof(long?),    "Windows.Foundation.IReference`1<Int64>",                                  "Metadata", 164),
    new(typeof(Point?),   "Windows.Foundation.IReference`1<Windows.Foundation.Point>",               "Metadata", 165),
    new(typeof(Vector3?), "Windows.Foundation.IReference`1<Windows.Foundation.Numerics.Vector3>",    "Metadata", 166),
    new(typeof(Guid?),    "Windows.Foundation.IReference`1<Guid>",                                   "Metadata", 167),
    new(typeof(IList<int?>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int32>>", "Metadata", 168),
    new(typeof(TestComponentCSharp.EnumValue?),       "Windows.Foundation.IReference`1<TestComponentCSharp.EnumValue>",       "Metadata", 195),
    new(typeof(TestComponentCSharp.BlittableStruct?), "Windows.Foundation.IReference`1<TestComponentCSharp.BlittableStruct>", "Metadata", 196),

    // --------------------
    // Generic Types
    // --------------------
    new(typeof(IList<long>), "Windows.Foundation.Collections.IVector`1<Int64>", "Metadata", 169),
    new(typeof(IList<TestComponentCSharp.EventWithGuid>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid>", "Metadata", 170),
    new(typeof(IList<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class>", "Metadata", 171),
    new(typeof(IEnumerator<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class>", "Metadata", 172),
    new(typeof(IEnumerable<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class>", "Metadata", 173),
    new(typeof(EventHandler<TestComponentCSharp.Class>), "Windows.Foundation.EventHandler`1<TestComponentCSharp.Class>", "Metadata", 174),
    new(typeof(KeyValuePair<object, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class>", "Metadata", 175),
    new(typeof(KeyValuePair<object, object>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>", "Metadata", 176),
    new(typeof(KeyValuePair<string, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>", "Metadata", 177),

    // --------------------
    // Nested Generics
    // --------------------
    new(typeof(EventHandler<IList<long>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>>", "Metadata", 178),
    new(typeof(EventHandler<KeyValuePair<object, object>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>", "Metadata", 179),
    new(typeof(EventHandler<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata", 180),
    new(typeof(IList<KeyValuePair<object, object>>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>", "Metadata", 181),
    new(typeof(IEnumerator<IEnumerable<object>>), "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>", "Metadata", 182),
    new(typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata", 183),

    // --------------------
    // Custom Types
    // NOTE: these strings assume the e2e test assembly identity matches "TypeMarshaling" (like your existing TestType expectation)
    // --------------------
    new(typeof(TestCSharp), "TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom", 184),

    new(typeof(IList<TestCSharp>),
        "System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        185),

    new(typeof(ITestCSharp<double>),
        "ITestCSharp`1[[System.Double, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "Custom",
        186),

    new(typeof(KeyValuePair<string, TestCSharp>),
        "System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        187),

    new(typeof(EventHandler<IList<TestCSharp>>),
        "System.EventHandler`1[[System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        188),

    new(typeof(EventHandler<TestCSharp>),
        "System.EventHandler`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        189),

    new(typeof(DelegateTestCSharp<Guid>),
        "DelegateTestCSharp`1[[System.Guid, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "Custom",
        190),

    new(typeof(WeakReference<TestComponent.Class>),
        "System.WeakReference`1[[TestComponent.Class, WinRT.Projection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        191),

    new(typeof(KeyValuePair<int, WeakReference<object>>),
        "System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.WeakReference`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        192),

    new(typeof(ICollection<object>),
        "System.Collections.Generic.ICollection`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        193),

    new(typeof(KeyValuePair<object, object>?),
        "System.Nullable`1[[System.Collections.Generic.KeyValuePair`2[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
        "Custom",
        194),
];

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

readonly record struct TypeCase(Type Type, string Name, string Kind, int ErrorCode);

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