using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;
using Windows.Foundation;


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


// Primitive Types
if ((failure = CheckType(typeof(long), "Int64 Primitive", 104)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(int), "Int32 Primitive", 105)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(short), "Int16 Primitive", 106)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ulong), "UInt64 Primitive", 107)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(uint), "UInt32 Primitive", 108)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ushort), "UInt16 Primitive", 109)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(byte), "UInt8 Primitive", 110)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(char), "Char16 Primitive", 111)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(float), "Single Primitive", 112)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(double), "Double Primitive", 113)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(bool), "Boolean Primitive", 114)) != 0)
{
    return failure;
}


// Projected WinRT Types (unit test set)
if ((failure = CheckType(typeof(TestComponent.Class), "TestComponent.Class Metadata", 115)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.Nested), "TestComponent.Nested Metadata", 116)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.IRequiredOne), "TestComponent.IRequiredOne Metadata", 117)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponent.Param6Handler), "TestComponent.Param6Handler Metadata", 118)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.ComposedBlittableStruct), "TestComponentCSharp.ComposedBlittableStruct Metadata", 119)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.IArtist), "TestComponentCSharp.IArtist Metadata", 120)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.EnumValue), "TestComponentCSharp.EnumValue Metadata", 121)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TestComponentCSharp.EventHandler0), "TestComponentCSharp.EventHandler0 Metadata", 122)) != 0)
{
    return failure;
}


// Mapped WinRT Types
if ((failure = CheckType(typeof(Type), "Windows.UI.Xaml.Interop.TypeName Metadata", 123)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Guid), "Guid Metadata", 124)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(object), "Object Metadata", 125)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(string), "String Metadata", 126)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(TimeSpan), "Windows.Foundation.TimeSpan Metadata", 127)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Point), "Windows.Foundation.Point Metadata", 128)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Rect), "Windows.Foundation.Rect Metadata", 129)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(Vector2), "Windows.Foundation.Numerics.Vector2 Metadata", 130)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IDisposable), "Windows.Foundation.IClosable Metadata", 132)) != 0)
{
    return failure;
}


// Special Cases
if ((failure = CheckType(typeof(Exception), "Windows.Foundation.HResult Metadata", 133)) != 0)
{
    return failure;
}


// Nullable types
if ((failure = CheckType(typeof(long?), "Windows.Foundation.IReference`1<Int64> Metadata", 134)) != 0)
{
    return failure;
}


// Generic Interfaces
if ((failure = CheckType(typeof(IList<long>), "Windows.Foundation.Collections.IVector`1<Int64> Metadata", 135)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestComponentCSharp.EventWithGuid>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid> Metadata", 136)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class> Metadata", 137)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerator<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class> Metadata", 138)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerable<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class> Metadata", 139)) != 0)
{
    return failure;
}


// Generic Delegates
if ((failure = CheckType(typeof(EventHandler<Guid>), "Windows.Foundation.EventHandler`1<Guid> Metadata", 140)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<TestComponentCSharp.Class>), "Windows.Foundation.EventHandler`1<TestComponentCSharp.Class> Metadata", 141)) != 0)
{
    return failure;
}


// KeyValuePair
if ((failure = CheckType(typeof(KeyValuePair<object, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class> Metadata", 142)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<object, object>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object> Metadata", 143)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<string, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class> Metadata", 144)) != 0)
{
    return failure;
}


// Nested Generics
if ((failure = CheckType(typeof(EventHandler<IList<long>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>> Metadata", 145)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<KeyValuePair<object, object>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata", 146)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata", 147)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<KeyValuePair<object, object>>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata", 148)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerator<IEnumerable<object>>), "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>> Metadata", 149)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata", 150)) != 0)
{
    return failure;
}


// Custom Types (unit test set)
if ((failure = CheckType(typeof(TestCSharp), $"{typeof(TestCSharp).AssemblyQualifiedName} Custom", 151)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(IList<TestCSharp>), $"{typeof(IList<TestCSharp>).AssemblyQualifiedName} Custom", 152)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(ITestCSharp<double>), $"{typeof(ITestCSharp<double>).AssemblyQualifiedName} Custom", 153)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(KeyValuePair<string, TestCSharp>), $"{typeof(KeyValuePair<string, TestCSharp>).AssemblyQualifiedName} Custom", 154)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<IList<TestCSharp>>), $"{typeof(EventHandler<IList<TestCSharp>>).AssemblyQualifiedName} Custom", 155)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(EventHandler<TestCSharp>), $"{typeof(EventHandler<TestCSharp>).AssemblyQualifiedName} Custom", 156)) != 0)
{
    return failure;
}

if ((failure = CheckType(typeof(DelegateTestCSharp<Guid>), $"{typeof(DelegateTestCSharp<Guid>).AssemblyQualifiedName} Custom", 157)) != 0)
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

