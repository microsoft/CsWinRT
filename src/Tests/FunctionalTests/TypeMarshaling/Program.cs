using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;
using Windows.Foundation;

SetTypeProperties setTypeProperties = new();
TestType TestTypeClass = new();

// Convert to Managed Trimmed Metadata NoMetadataTypeInfo Test Case
// Goes into NoMetadataTypeInfo codepath for Type.cs ConvertToManaged
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
String expectedMetadataPropertyInfoTrimmed = "TestComponentCSharp.TestTypeTrimmed Metadata";
if (setTypeProperties.GetPropertyInfoTestTypeTrimmed(TestTypeClass) != expectedMetadataPropertyInfoTrimmed)
{
    return 101;
}

// Projected WinRT Types
String TestComponentCSharpClassExpected = "TestComponentCSharp.Class Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponentCSharp.Class)) != TestComponentCSharpClassExpected)
{
    return 102;
}

// Custom Type (your existing TestType case)
String CustomTestTypeExpected = "TestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestType)) != CustomTestTypeExpected)
{
    return 103;
}


// Primitive Types
String expectedPrimitiveInt64 = "Int64 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(long)) != expectedPrimitiveInt64)
{
    return 104;
}

String expectedPrimitiveInt32 = "Int32 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(int)) != expectedPrimitiveInt32)
{
    return 105;
}

String expectedPrimitiveInt16 = "Int16 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(short)) != expectedPrimitiveInt16)
{
    return 106;
}

String expectedPrimitiveUInt64 = "UInt64 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(ulong)) != expectedPrimitiveUInt64)
{
    return 107;
}

String expectedPrimitiveUInt32 = "UInt32 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(uint)) != expectedPrimitiveUInt32)
{
    return 108;
}

String expectedPrimitiveUInt16 = "UInt16 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(ushort)) != expectedPrimitiveUInt16)
{
    return 109;
}

String expectedPrimitiveUInt8 = "UInt8 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(byte)) != expectedPrimitiveUInt8)
{
    return 110;
}

String expectedPrimitiveChar16 = "Char16 Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(char)) != expectedPrimitiveChar16)
{
    return 111;
}

String expectedPrimitiveSingle = "Single Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(float)) != expectedPrimitiveSingle)
{
    return 112;
}

String expectedPrimitiveDouble = "Double Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(double)) != expectedPrimitiveDouble)
{
    return 113;
}

String expectedPrimitiveBoolean = "Boolean Primitive";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(bool)) != expectedPrimitiveBoolean)
{
    return 114;
}

// Projected WinRT Types (unit test set)
String expectedProjected1 = "TestComponent.Class Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponent.Class)) != expectedProjected1)
{
    return 115;
}

String expectedProjected2 = "TestComponent.Nested Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponent.Nested)) != expectedProjected2)
{
    return 116;
}

String expectedProjected3 = "TestComponent.IRequiredOne Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponent.IRequiredOne)) != expectedProjected3)
{
    return 117;
}

String expectedProjected4 = "TestComponent.Param6Handler Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponent.Param6Handler)) != expectedProjected4)
{
    return 118;
}

String expectedProjected5 = "TestComponentCSharp.ComposedBlittableStruct Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponentCSharp.ComposedBlittableStruct)) != expectedProjected5)
{
    return 119;
}

String expectedProjected6 = "TestComponentCSharp.IArtist Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponentCSharp.IArtist)) != expectedProjected6)
{
    return 120;
}

String expectedProjected7 = "TestComponentCSharp.EnumValue Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponentCSharp.EnumValue)) != expectedProjected7)
{
    return 121;
}

String expectedProjected8 = "TestComponentCSharp.EventHandler0 Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestComponentCSharp.EventHandler0)) != expectedProjected8)
{
    return 122;
}

// Mapped WinRT Types
String expectedMapped1 = "Windows.UI.Xaml.Interop.TypeName Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Type)) != expectedMapped1)
{
    return 123;
}

String expectedMapped2 = "Guid Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Guid)) != expectedMapped2)
{
    return 124;
}

String expectedMapped3 = "Object Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Object)) != expectedMapped3)
{
    return 125;
}

String expectedMapped4 = "String Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(String)) != expectedMapped4)
{
    return 126;
}

String expectedMapped5 = "Windows.Foundation.TimeSpan Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TimeSpan)) != expectedMapped5)
{
    return 127;
}

String expectedMapped6 = "Windows.Foundation.Point Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Point)) != expectedMapped6)
{
    return 128;
}

String expectedMapped7 = "Windows.Foundation.Rect Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Rect)) != expectedMapped7)
{
    return 129;
}

String expectedMapped8 = "Windows.Foundation.Numerics.Vector2 Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Vector2)) != expectedMapped8)
{
    return 130;
}

String expectedMapped10 = "Windows.Foundation.IClosable Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IDisposable)) != expectedMapped10)
{
    return 132;
}

// Special Cases
String expectedSpecial1 = "Windows.Foundation.HResult Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Exception)) != expectedSpecial1)
{
    return 133;
}

// Nullable types
String expectedNullable1 = "Windows.Foundation.IReference`1<Int64> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(Nullable<long>)) != expectedNullable1)
{
    return 134;
}

// Generic Interfaces
String expectedGenInterface1 = "Windows.Foundation.Collections.IVector`1<Int64> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IList<long>)) != expectedGenInterface1)
{
    return 135;
}

String expectedGenInterface2 = "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IList<TestComponentCSharp.EventWithGuid>)) != expectedGenInterface2)
{
    return 136;
}

String expectedGenInterface3 = "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IList<TestComponentCSharp.Class>)) != expectedGenInterface3)
{
    return 137;
}

String expectedGenInterface4 = "Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IEnumerator<TestComponentCSharp.Class>)) != expectedGenInterface4)
{
    return 138;
}

String expectedGenInterface5 = "Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IEnumerable<TestComponentCSharp.Class>)) != expectedGenInterface5)
{
    return 139;
}

// Generic Delegates
String expectedGenDelegate1 = "Windows.Foundation.EventHandler`1<Guid> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<Guid>)) != expectedGenDelegate1)
{
    return 140;
}

String expectedGenDelegate2 = "Windows.Foundation.EventHandler`1<TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<TestComponentCSharp.Class>)) != expectedGenDelegate2)
{
    return 141;
}

// KeyValuePair
String expectedKvp1 = "Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(KeyValuePair<Object, TestComponentCSharp.Class>)) != expectedKvp1)
{
    return 142;
}

String expectedKvp2 = "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(KeyValuePair<Object, Object>)) != expectedKvp2)
{
    return 143;
}

String expectedKvp3 = "Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(KeyValuePair<String, TestComponentCSharp.Class>)) != expectedKvp3)
{
    return 144;
}

// Nested Generics
String expectedNested1 = "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<IList<long>>)) != expectedNested1)
{
    return 145;
}

String expectedNested2 = "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<KeyValuePair<Object, Object>>)) != expectedNested2)
{
    return 146;
}

String expectedNested3 = "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<KeyValuePair<String, TestComponentCSharp.Class>>)) != expectedNested3)
{
    return 147;
}

String expectedNested4 = "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IList<KeyValuePair<Object, Object>>)) != expectedNested4)
{
    return 148;
}

String expectedNested5 = "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IEnumerator<IEnumerable<Object>>)) != expectedNested5)
{
    return 149;
}

String expectedNested6 = "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>> Metadata";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>)) != expectedNested6)
{
    return 150;
}

// Custom Types (unit test set)
// NOTE: Use AssemblyQualifiedName to avoid hardcoding assembly identity in functional tests.
String expectedCustom1 = $"{typeof(TestCSharp).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(TestCSharp)) != expectedCustom1)
{
    return 151;
}

String expectedCustom2 = $"{typeof(IList<TestCSharp>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(IList<TestCSharp>)) != expectedCustom2)
{
    return 152;
}

String expectedCustom3 = $"{typeof(ITestCSharp<double>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(ITestCSharp<double>)) != expectedCustom3)
{
    return 153;
}

String expectedCustom4 = $"{typeof(KeyValuePair<String, TestCSharp>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(KeyValuePair<String, TestCSharp>)) != expectedCustom4)
{
    return 154;
}

String expectedCustom5 = $"{typeof(EventHandler<IList<TestCSharp>>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<IList<TestCSharp>>)) != expectedCustom5)
{
    return 155;
}

String expectedCustom6 = $"{typeof(EventHandler<TestCSharp>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(EventHandler<TestCSharp>)) != expectedCustom6)
{
    return 156;
}

String expectedCustom7 = $"{typeof(DelegateTestCSharp<Guid>).AssemblyQualifiedName} Custom";
if (setTypeProperties.GetPropertyInfoTestType(TestTypeClass, typeof(DelegateTestCSharp<Guid>)) != expectedCustom7)
{
    return 157;
}

return 100;

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