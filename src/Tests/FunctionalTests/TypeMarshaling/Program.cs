using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;

SetTypeProperties setTypeProperties = new();

// Convert to Managed Metadata Marshalling Info Test Case
String expectedMetadataPropertyInfo = "TestComponentCSharp.TestType Metadata";
CustomTestType TestType = new();
if (setTypeProperties.GetPropertyInfoTestType(TestType) != expectedMetadataPropertyInfo)
{
    return 101;
}

// Convert to Managed Trimmed Metadata NoMetadataTypeInfo Test Case
// Goes into NoMetadataTypeInfo codepath for Type.cs ConvertToManaged
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
String expectedMetadataPropertyInfoTrimmed = "TestComponentCSharp.TestTypeTrimmed Metadata";
CustomTestType TestTypeTrimmed = new();
if (setTypeProperties.GetPropertyInfoTestTypeTrimmed(TestTypeTrimmed) != expectedMetadataPropertyInfoTrimmed)
{
    return 102;
}

// Custom TypeKind test case
String expectedCustomTypePropertyInfo = "CustomTestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom";
SetTypeProperties customSetTypeProperties = new();
if (setTypeProperties.GetPropertyInfoWithType(typeof(CustomTestType)) != expectedCustomTypePropertyInfo)
{
    return 103;
}

// Primitive TypeKind test case
String expectedPrimitiveTypePropertyInfo = "Int32 Primitive";
SetTypeProperties primitiveSetTypeProperties = new();
if (setTypeProperties.GetPropertyInfoWithType(typeof(int)) != expectedPrimitiveTypePropertyInfo)
{
    return 104;
}

// Primitive TypeKind test case 2
String expectedPrimitiveTypePropertyInfo2 = "Int64 Primitive";
SetTypeProperties primitiveSetTypeProperties2 = new();
if (setTypeProperties.GetPropertyInfoWithType(typeof(System.Int64)) != expectedPrimitiveTypePropertyInfo2)
{
    return 105;
}

return 100;

sealed class CustomTestType : IType
{
    public System.Type TypeProperty { get; set; }
}

