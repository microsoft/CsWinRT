using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;

// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
SetTypeProperties setTypeProperties = new();
Console.WriteLine(setTypeProperties.GetPropertyInfo());/*.Equals("TestComponentCSharp.TestType1 Metadata");*/

SetTypeProperties customSetTypeProperties = new();
Console.WriteLine(customSetTypeProperties.GetPropertyInfoFromCustomType(typeof(CustomTestType)));

sealed class CustomTestType : Composable
{
}
