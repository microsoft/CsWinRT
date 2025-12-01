using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponent;
using TestComponentCSharp;

// Do not reference TestComponentCSharp::TestType1 in managed because it needs to be trimmed to test the Metadata TypeKind scenario
SetTypeProperties setTypeProperties = new();
return setTypeProperties.SetProperty().Equals("TestComponentCSharp.TestType1 Metadata") ? 100 : 101;

//TestComponentCSharp.Class TestObject = new();
//TestObject.TypeProperty = typeof(System.Type);
//Console.WriteLine(TestObject.GetTypePropertyAbiName());