using System;
using System.Runtime.InteropServices;
using TestComponent;
using TestComponentCSharp;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // TODO: remove this once the attributes below are removed

// TODO: This shouldn't be needed if transitive references are detected correctly.
[assembly: WindowsRuntime.WindowsRuntimeReferenceAssembly]

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("Test")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]

//CustomDisposableTest customDisposableTest = new();
//customDisposableTest.Dispose();


ThrowingManagedProperties throwingManagedProperties = new();
throwingManagedProperties.ThrowWithIProperty1(new ThrowingManagedProperty(new NullReferenceException()));

//Composable composable = new();
//_ = composable.Value;
//composable.Value = 5;
//_ = composable.Value;

//_ = Composable.ExpectComposable(composable);
//_ = Composable.ExpectRequiredOne(composable);

//Composable composable2 = new(42);
//_ = composable2.Value;

//_ = Composable.ExpectComposable(composable2);
//_ = Composable.ExpectRequiredOne(composable2);

//_ = ComImports.NumObjects;
//_ = ComImports.MakeObject();

//TestComposable testComposable = new();

//_ = testComposable.Value;

//_ = Composable.ExpectComposable(testComposable);
//_ = Composable.ExpectRequiredOne(testComposable);

//sealed class TestComposable : Composable
//{
//}

//sealed class TestComposable2 : Composable
//{
//}

sealed class ThrowingManagedProperty : IProperties1
{
    public ThrowingManagedProperty(Exception exceptionToThrow)
    {
        ExceptionToThrow = exceptionToThrow;
    }

    public Exception ExceptionToThrow { get; }

    public int ReadWriteProperty
    {
        get
        {
            throw ExceptionToThrow;
        }
    }
}

/*
// new RCW / Factory activation
var instance = new Class();

var expectedEnum = EnumValue.Two;
instance.EnumProperty = expectedEnum;

// Custom type marshaling
var expectedUri = new Uri("http://expected");
instance.UriProperty = expectedUri;

var instance2 = new Class(32);

return instance.EnumProperty == expectedEnum && 
       instance.UriProperty == expectedUri &&
       instance2.IntProperty == 32 ? 100 : 101;
*/