using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;

bool success = true;

// Metadata TypeKind test case
// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
String expectedPropertyInfo = "TestComponentCSharp.TestType1 Metadata";
SetTypeProperties setTypeProperties = new();
if (setTypeProperties.GetPropertyInfo() != expectedPropertyInfo)
{
    success = false;
}

// Custom TypeKind test case
String expectedCustomTypePropertyInfo = "CustomTestType, TypeMarshaling, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null Custom";
SetTypeProperties customSetTypeProperties = new();
if (customSetTypeProperties.GetPropertyInfoFromCustomType(typeof(CustomTestType)) != expectedCustomTypePropertyInfo)
{
    success = false;
}

return success ? 100 : 101;

sealed class CustomTestType : Composable
{
}

