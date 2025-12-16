using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;
using TestComponent;
using TestComponentCSharp;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Tasks;
using WindowsRuntime.InteropServices;

CustomDisposableTest customDisposableTest = new();
customDisposableTest.Dispose();

Composable composable = new();
_ = composable.Value;
composable.Value = 5;
_ = composable.Value;

_ = Composable.ExpectComposable(composable);
_ = Composable.ExpectRequiredOne(composable);

Composable composable2 = new(42);
_ = composable2.Value;

_ = Composable.ExpectComposable(composable2);
_ = Composable.ExpectRequiredOne(composable2);

_ = ComImports.NumObjects;
_ = ComImports.MakeObject();

TestComposable testComposable = new();

_ = testComposable.Value;

_ = Composable.ExpectComposable(testComposable);
_ = Composable.ExpectRequiredOne(testComposable);

TestMixedComClass testMixedComClass = new();

unsafe
{
    void* testMixedComClassUnknownPtr = WindowsRuntimeMarshal.ConvertToUnmanaged(testMixedComClass);
    void* classicComActionPtr = null;

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
        ComHelpers.EnsureQueryInterface(
            unknownPtr: testMixedComClassUnknownPtr,
            iids: [
                new Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E"),
                new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90")]);
    }
    finally
    {
        WindowsRuntimeMarshal.Free(testMixedComClassUnknownPtr);
        WindowsRuntimeMarshal.Free(classicComActionPtr);
    }
}

ConstructedDerivedType constructedDerivedType = new();

unsafe
{
    void* constructedDerivedTypePtr = WindowsRuntimeMarshal.ConvertToUnmanaged(constructedDerivedType);

    try
    {
        ComHelpers.EnsureQueryInterface(
            unknownPtr: constructedDerivedTypePtr,
            iids: [
                new Guid("E2FCC7C1-3BFC-5A0B-B2B0-72E769D1CB7E"),   // 'IEnumerable<string>'
                new Guid("07F69483-8097-5F92-BB53-068DC81F281A"),   // 'IEnumerable'
                new Guid("E8A8353A-767E-5C47-97CF-E0336C354311"),   // 'IClosable'
                new Guid("8311ED02-4F46-5CAF-BF01-2AE354C04BF5")]); // 'IMapChangedEventArgs<IEnumerable>'
    }
    finally
    {
        WindowsRuntimeMarshal.Free(constructedDerivedTypePtr);
    }
}

object genericType = GenericFactory.Make();

unsafe
{
    void* genericTypePtr = WindowsRuntimeMarshal.ConvertToUnmanaged(genericType);

    try
    {
        ComHelpers.EnsureQueryInterface(
            unknownPtr: genericTypePtr,
            iids: [
                new Guid("30160817-1D7D-54E9-99DB-D7636266A476"),   // 'IEnumerable<bool>'
                new Guid("07F69483-8097-5F92-BB53-068DC81F281A"),   // 'IEnumerable'
                new Guid("907661AB-C065-5A14-9AC3-2FEB0D164DDA"),   // 'IReadOnlyDictionary<bool, float>'
                new Guid("3631E370-2F65-5F4A-8364-1619C536DB12"),   // 'IEnumerable<KeyValuePair<bool, float>>'
                new Guid("F61E8483-D7A0-5840-9DCF-40423CCC97D0")]); // 'IMapChangedEventArgs<float>'
    }
    finally
    {
        WindowsRuntimeMarshal.Free(genericTypePtr);
    }
}

IAsyncActionWithProgress<int> asyncActionWithProgress = GenericFactory.MakeAsyncActionWithProgress();

unsafe
{
    void* asyncActionWithProgressPtr = WindowsRuntimeMarshal.ConvertToUnmanaged(asyncActionWithProgress);

    try
    {
        ComHelpers.EnsureQueryInterface(
            unknownPtr: asyncActionWithProgressPtr,
            iids: [
                new Guid("0EDE398F-0090-574E-AD30-E152B433BF6A"),   // 'IAsyncActionWithProgress<int>'
                new Guid("0DB2462F-B6D6-5A6C-8834-B530BAAA45FD")]); // 'IAsyncInfo'
    }
    finally
    {
        WindowsRuntimeMarshal.Free(asyncActionWithProgressPtr);
    }
}

IAsyncOperation<TimeSpan> asyncOperation = GenericFactory.MakeAsyncOperation();

unsafe
{
    void* asyncOperationPtr = WindowsRuntimeMarshal.ConvertToUnmanaged(asyncOperation);

    try
    {
        ComHelpers.EnsureQueryInterface(
            unknownPtr: asyncOperationPtr,
            iids: [
                new Guid("1AE01209-1ACA-51D3-A080-8B1214E0A39E"),   // 'IAsyncOperation<TimeSpan>'
                new Guid("0DB2462F-B6D6-5A6C-8834-B530BAAA45FD")]); // 'IAsyncInfo'
    }
    finally
    {
        WindowsRuntimeMarshal.Free(asyncOperationPtr);
    }
}

sealed class TestComposable : Composable
{
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

class GenericBaseType<T> : IEnumerable<T>, IDisposable
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

// This type is extending a generic type. The base type is a 'TypeSpec' with a constructed
// generic type. We use this to validate that the base constructed interfaces are seen.
class ConstructedDerivedType : GenericBaseType<string>, IMapChangedEventArgs<IEnumerable>
{
    public CollectionChange CollectionChange => throw new NotImplementedException();

    public IEnumerable Key => throw new NotImplementedException();
}

class GenericType<T1, T2> : IEnumerable<T1>, IReadOnlyDictionary<T1, T2>, IMapChangedEventArgs<T2>
{
    public T2 this[T1 key] => throw new NotImplementedException();

    public IEnumerable<T1> Keys => throw new NotImplementedException();

    public IEnumerable<T2> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public CollectionChange CollectionChange => throw new NotImplementedException();

    public T2 Key => throw new NotImplementedException();

    public bool ContainsKey(T1 key)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T1> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(T1 key, [MaybeNullWhen(false)] out T2 value)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<KeyValuePair<T1, T2>> IEnumerable<KeyValuePair<T1, T2>>.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class GenericFactory
{
    // This method is caling a generic one, which then constructs a generic type.
    // The 'GenericType<T, int>' instantiation doesn't appear anywhere in the .dll,
    // but we should be able to still find it, because:
    //   - We can see 'Make<bool>' in the 'MethodSpec' table.
    //   - We can inspect all instructions in that constructed method
    //   - We can see a 'newobj' instruction with a 'MemberRef' to 'GenericType<T, int>.ctor'
    // From there, we should be able to gather info on that constructed generic type.
    public static object Make() => Make<bool>();

    private static object Make<T>() => new GenericType<T, float>();

    // Specific test for 'AsyncInfo.Run' with explicit type arguments
    [SupportedOSPlatform("windows10.0.10240.0")]
    public static IAsyncActionWithProgress<int> MakeAsyncActionWithProgress()
    {
        return AsyncInfo.Run<int>((token, progress) => Task.CompletedTask);
    }

    // Specific test for 'AsyncInfo.Run' with transitive type arguments
    [SupportedOSPlatform("windows10.0.10240.0")]
    public static IAsyncOperation<TimeSpan> MakeAsyncOperation()
    {
        return MakeAsyncActionOperation<TimeSpan>();
    }

    [SupportedOSPlatform("windows10.0.10240.0")]
    private static IAsyncOperation<T> MakeAsyncActionOperation<T>()
    {
        return AsyncInfo.Run(token => Task.FromResult(default(T)));
    }
}

[Guid("3C832AA5-5F7E-46EE-B1BF-7FE03AE866AF")]
[GeneratedComInterface]
partial interface IClassicComAction
{
    void Invoke();
}

file static class ComHelpers
{
    [SupportedOSPlatform("windows6.3")]
    public static unsafe void EnsureQueryInterface(void* unknownPtr, params ReadOnlySpan<Guid> iids)
    {
        foreach (Guid iid in iids)
        {
            void* interfacePtr = null;

            try
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(
                    pUnk: (nint)unknownPtr,
                    iid: iid,
                    ppv: out *(nint*)&interfacePtr));
            }
            finally
            {
                WindowsRuntimeMarshal.Free(interfacePtr);
            }
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