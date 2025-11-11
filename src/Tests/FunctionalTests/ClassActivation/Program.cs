using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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

TestMixedComClass testMixedComClass = new();

unsafe
{
    void* testMixedComClassUnknownPtr = WindowsRuntimeMarshal.ConvertToUnmanaged(testMixedComClass);
    void* classicComActionPtr = null;
    void* closablePtr = null;
    void* inspectablePtr = null;

    try
    {
        // We should be able to get an 'IClassicComAction' interface pointer
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(
            pUnk: (nint)testMixedComClassUnknownPtr,
            iid: new Guid("3C832AA5-5F7E-46EE-B1BF-7FE03AE866AF"),
            ppv: out *(nint*)&classicComActionPtr));

        // Verify that we can correctly call 'Invoke'
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, int>)(*(void***)classicComActionPtr)[3])(classicComActionPtr));

        // Sanity check: we should still also be able to 'QueryInterface' for other interfaces
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(
            pUnk: (nint)testMixedComClassUnknownPtr,
            iid: new Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E"),
            ppv: out *(nint*)&closablePtr));
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(
            pUnk: (nint)testMixedComClassUnknownPtr,
            iid: new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"),
            ppv: out *(nint*)&inspectablePtr));
    }
    finally
    {
        WindowsRuntimeMarshal.Free(testMixedComClassUnknownPtr);
        WindowsRuntimeMarshal.Free(classicComActionPtr);
        WindowsRuntimeMarshal.Free(closablePtr);
        WindowsRuntimeMarshal.Free(inspectablePtr);
    }
}

sealed class TestComposable : Composable
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

sealed class TestMixedComClass : IClassicComAction, IDisposable
{
    public void Invoke()
    {
    }

    public void Dispose()
    {
    }
}

[Guid("3C832AA5-5F7E-46EE-B1BF-7FE03AE866AF")]
[GeneratedComInterface]
partial interface IClassicComAction
{
    void Invoke();
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