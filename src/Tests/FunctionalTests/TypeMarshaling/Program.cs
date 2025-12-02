using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;

bool success = true;

SetTypeProperties setTypeProperties = new();

// Metadata TypeKind test case
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
String expectedMetadataPropertyInfo = "TestComponentCSharp.TestType1 Metadata";
if (setTypeProperties.GetPropertyInfo() != expectedMetadataPropertyInfo)
{
    success = false;
}

// Custom TypeKind test case
String expectedCustomTypePropertyInfo = "CustomTestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom";
SetTypeProperties customSetTypeProperties = new();
if (setTypeProperties.GetPropertyInfoFromCustomType(typeof(CustomTestType)) != expectedCustomTypePropertyInfo)
{
    success = false;
}

// Primitive TypeKind test case
String expectedPrimitiveTypePropertyInfo = "Int32 Primitive";
SetTypeProperties primitiveSetTypeProperties = new();
if (setTypeProperties.GetPropertyInfoFromCustomType(typeof(int)) != expectedPrimitiveTypePropertyInfo)
{
    success = false;
}

// Primitive TypeKind test case 2
String expectedPrimitiveTypePropertyInfo2 = "Int64 Primitive";
SetTypeProperties primitiveSetTypeProperties2 = new();
if (setTypeProperties.GetPropertyInfoFromCustomType(typeof(System.Int64)) != expectedPrimitiveTypePropertyInfo2)
{
    success = false;
}

return success ? 100 : 101;

sealed class CustomTestType : Composable
{
}

