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
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

int failure;

// Convert to Managed Trimmed Metadata NoMetadataTypeInfo Test Case
// Goes into NoMetadataTypeInfo codepath for Type.cs ConvertToManaged
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
if ((failure = CheckTrimmed("TestComponentCSharp.TestTypeTrimmed Metadata", 101)) != 0)
{
    return failure;
}

// Projected WinRT Types
if ((failure = CheckType(typeof(TestComponentCSharp.Class), "TestComponentCSharp.Class Metadata", 102)) != 0)
{
    return failure;
}

// Custom Type (your existing TestType case)
if ((failure = CheckType(typeof(TestType), "TestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom", 103)) != 0)
{
    return failure;
}

// Mapped System.* Types Primitive Types
if ((failure = CheckType(typeof(bool), "Boolean Primitive", 104)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(byte), "UInt8 Primitive", 105)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(char), "Char16 Primitive", 106)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(double), "Double Primitive", 107)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(short), "Int16 Primitive", 108)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(int), "Int32 Primitive", 109)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(long), "Int64 Primitive", 110)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(float), "Single Primitive", 111)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ushort), "UInt16 Primitive", 112)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(uint), "UInt32 Primitive", 113)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ulong), "UInt64 Primitive", 114)) != 0)
{
    return failure;
}

// Mapped System.* Types Metadata Types
if ((failure = CheckType(typeof(DateTimeOffset), "Windows.Foundation.DateTime Metadata", 115)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<Guid>), "Windows.Foundation.EventHandler`1<Guid> Metadata", 116)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Exception), "Windows.Foundation.HResult Metadata", 117)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Guid), "Guid Metadata", 118)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IDisposable), "Windows.Foundation.IClosable Metadata", 119)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IServiceProvider), "Microsoft.UI.Xaml.IXamlServiceProvider Metadata", 120)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(object), "Object Metadata", 121)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(string), "String Metadata", 122)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TimeSpan), "Windows.Foundation.TimeSpan Metadata", 123)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Type), "Windows.UI.Xaml.Interop.TypeName Metadata", 124)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Uri), "Windows.Foundation.Uri Metadata", 125)) != 0)
{
    return failure;
}

//if ((failure = CheckType(typeof(ICommand), "Microsoft.UI.Xaml.Input.ICommand Metadata", 126)) != 0)
//{
//    return failure;
//}

// Mapped System.Collections.* Types
if ((failure = CheckType(typeof(IEnumerable), "Microsoft.UI.Xaml.Interop.IBindableIterable Metadata", 127)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerator), "Microsoft.UI.Xaml.Interop.IBindableIterator Metadata", 128)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList), "Microsoft.UI.Xaml.Interop.IBindableVector Metadata", 129)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IReadOnlyList<long>), "Windows.Foundation.Collections.IVectorView`1<Int64> Metadata", 130)) != 0)
{
    return failure;
}

// Mapped System.Collections.Specialized* Types
if ((failure = CheckType(typeof(INotifyCollectionChanged), "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged Metadata", 131)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(NotifyCollectionChangedEventArgs), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs Metadata", 132)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(NotifyCollectionChangedEventHandler), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler Metadata", 133)) != 0)
{
    return failure;
}

// Mapped System.ComponentModel.* Types
if ((failure = CheckType(typeof(DataErrorsChangedEventArgs), "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs Metadata", 134)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(INotifyDataErrorInfo), "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo Metadata", 135)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(INotifyPropertyChanged), "Microsoft.UI.Xaml.Data.INotifyPropertyChanged Metadata", 136)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(PropertyChangedEventArgs), "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs Metadata", 137)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(PropertyChangedEventHandler), "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler Metadata", 138)) != 0)
{
    return failure;
}

// Mapped System.Numerics.* Types
if ((failure = CheckType(typeof(Matrix3x2), "Windows.Foundation.Numerics.Matrix3x2 Metadata", 139)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Matrix4x4), "Windows.Foundation.Numerics.Matrix4x4 Metadata", 140)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Plane), "Windows.Foundation.Numerics.Plane Metadata", 141)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Quaternion), "Windows.Foundation.Numerics.Quaternion Metadata", 142)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Vector2), "Windows.Foundation.Numerics.Vector2 Metadata", 143)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Vector3), "Windows.Foundation.Numerics.Vector3 Metadata", 144)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Vector4), "Windows.Foundation.Numerics.Vector4 Metadata", 145)) != 0)
{
    return failure;
}

// Mapped Windows.Foundation.* Types
if ((failure = CheckType(typeof(AsyncActionCompletedHandler), "Windows.Foundation.AsyncActionCompletedHandler Metadata", 146)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IAsyncAction), "Windows.Foundation.IAsyncAction Metadata", 147)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IAsyncInfo), "Windows.Foundation.IAsyncInfo Metadata", 148)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Point), "Windows.Foundation.Point Metadata", 149)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Rect), "Windows.Foundation.Rect Metadata", 150)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Size), "Windows.Foundation.Size Metadata", 151)) != 0)
{
    return failure;
}

// Mapped Windows.Foundation.Collections.* Types
if ((failure = CheckType(typeof(Windows.Foundation.Collections.CollectionChange), "Windows.Foundation.Collections.CollectionChange Metadata", 152)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Windows.Foundation.Collections.IVectorChangedEventArgs), "Windows.Foundation.Collections.IVectorChangedEventArgs Metadata", 153)) != 0)
{
    return failure;
}

// Mapped WindowsRuntime.InteropServices.* Types
if ((failure = CheckType(typeof(EventRegistrationToken), "WindowsRuntime.InteropServices.EventRegistrationToken Metadata", 154)) != 0)
{
    return failure;
}

// Projected WinRT Types
if ((failure = CheckType(typeof(TestComponent.Class), "TestComponent.Class Metadata", 155)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.Nested), "TestComponent.Nested Metadata", 156)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.IRequiredOne), "TestComponent.IRequiredOne Metadata", 157)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.Param6Handler), "TestComponent.Param6Handler Metadata", 158)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.Class), "TestComponentCSharp.Class Metadata", 159)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.ComposedBlittableStruct), "TestComponentCSharp.ComposedBlittableStruct Metadata", 160)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.IArtist), "TestComponentCSharp.IArtist Metadata", 161)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.EnumValue), "TestComponentCSharp.EnumValue Metadata", 162)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.EventHandler0), "TestComponentCSharp.EventHandler0 Metadata", 163)) != 0)
{
    return failure;
}

// Nullable Types
if ((failure = CheckType(typeof(long?), "Windows.Foundation.IReference`1<Int64> Metadata", 164)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Point?), "Windows.Foundation.IReference`1<Windows.Foundation.Point> Metadata", 165)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Vector3?), "Windows.Foundation.IReference`1<Windows.Foundation.Numerics.Vector3> Metadata", 166)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Guid?), "Windows.Foundation.IReference`1<Guid> Metadata", 167)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<int?>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int32>> Metadata", 168)) != 0)
{
    return failure;
}

// Generic Types
if ((failure = CheckType(typeof(IList<long>), "Windows.Foundation.Collections.IVector`1<Int64> Metadata", 169)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestComponentCSharp.EventWithGuid>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid> Metadata", 170)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class> Metadata", 171)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerator<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class> Metadata", 172)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerable<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class> Metadata", 173)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<TestComponentCSharp.Class>), "Windows.Foundation.EventHandler`1<TestComponentCSharp.Class> Metadata", 174)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<object, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class> Metadata", 175)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<object, object>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object> Metadata", 176)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<string, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class> Metadata", 177)) != 0)
{
    return failure;
}

// Nested Generics
if ((failure = CheckType(typeof(EventHandler<IList<long>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>> Metadata", 178)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<KeyValuePair<object, object>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata", 179)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata", 180)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<KeyValuePair<object, object>>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata", 181)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerator<IEnumerable<object>>), "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>> Metadata", 182)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata", 183)) != 0)
{
    return failure;
}

// Custom Types
if ((failure = CheckType(typeof(TestCSharp), "TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom", 184)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestCSharp>), "System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 185)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ITestCSharp<double>), "ITestCSharp`1[[System.Double, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom", 186)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<string, TestCSharp>), "System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 187)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<IList<TestCSharp>>), "System.EventHandler`1[[System.Collections.Generic.IList`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 188)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<TestCSharp>), "System.EventHandler`1[[TestCSharp, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 189)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(DelegateTestCSharp<Guid>), "DelegateTestCSharp`1[[System.Guid, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom", 190)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(WeakReference<TestComponent.Class>), "System.WeakReference`1[[TestComponent.Class, WinRT.Projection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 191)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<int, WeakReference<object>>), "System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.WeakReference`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 192)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ICollection<object>), "System.Collections.Generic.ICollection`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 193)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<object, object>?), "System.Nullable`1[[System.Collections.Generic.KeyValuePair`2[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e Custom", 194)) != 0)
{
    return failure;
}


return 100;

static int CheckTrimmed(String expected, int errorCode)
{
    TestType testTypeClass = new();
    return FailIfNotEqual(new SetTypeProperties().GetPropertyInfoTestTypeTrimmed(testTypeClass), expected, errorCode);
}

static int CheckType(System.Type type, String expected, int errorCode)
{
    TestType testTypeClass = new();
    return FailIfNotEqual(new SetTypeProperties().GetPropertyInfoTestType(testTypeClass, type), expected, errorCode);
}

static int FailIfNotEqual(String actual, String expected, int errorCode)
{
    if (actual != expected)
    {
        return errorCode;
    }

    return 0;
}

sealed class TestType : IType
{
    public System.Type TypeProperty { get; set; }
}

sealed class TestCSharp
{
}

interface ITestCSharp<T>
{
}

delegate void DelegateTestCSharp<T>(object sender, T value);

