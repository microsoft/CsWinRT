// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Foundation;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using WindowsRuntime.Xaml;
#if TEST_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
#endif

[assembly: SupportedOSPlatform("windows10.0.17763.0")]

internal static unsafe partial class DefaultCustomPropertyProviderTests
{
    private static readonly Guid ProviderIid = new("7C925755-3E48-42B4-8677-76372267033F");
    private static readonly Guid InspectableIid = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
    private static readonly Guid UnknownIid = new("00000000-0000-0000-C000-000000000046");
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_NOTSUPPORTED = unchecked((int)0x80131515);

    public static int Run()
    {
        try
        {
#if TEST_DEFAULT_PROVIDER_DISABLED
            bool automaticProvider = false;
#else
            bool automaticProvider = true;
#endif

            // Exercise the managed CCWs without activating XAML or requiring a UI thread/package.
            // No native Control members are used; real style/layout coverage lives in ObjectLifetimeTests.
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(UnannotatedControl)), typeof(UnannotatedControl), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DerivedControl)), typeof(DerivedControl), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(GenericControl<int>)), typeof(GenericControl<int>), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(GenericControl<string>)), typeof(GenericControl<string>), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(FrameworkElementProbe)), typeof(FrameworkElementProbe), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DerivedFrameworkElementProbe)), typeof(DerivedFrameworkElementProbe), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DependencyObjectProbe)), typeof(DependencyObjectProbe), automaticProvider, supportsPropertyLookup: false, hasGeneratedCcw: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DerivedDependencyObjectProbe)), typeof(DerivedDependencyObjectProbe), automaticProvider, supportsPropertyLookup: false, hasGeneratedCcw: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(StringableDependencyObject)), typeof(StringableDependencyObject), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ApplicationProbe)), typeof(ApplicationProbe), automaticProvider, supportsPropertyLookup: false);
            CheckProvider(new NonXamlObject(), typeof(NonXamlObject), automaticProvider, expectedString: "non-XAML", supportsPropertyLookup: false);
            CheckProvider(new GenericNonXamlObject<int>(), typeof(GenericNonXamlObject<int>), automaticProvider, expectedString: "generic", supportsPropertyLookup: false);
            CheckProvider(new GenericNonXamlObject<string>(), typeof(GenericNonXamlObject<string>), automaticProvider, expectedString: "generic", supportsPropertyLookup: false);
            CheckProvider(new NonXamlValue(), typeof(NonXamlValue), automaticProvider, expectedString: "value", supportsPropertyLookup: false);
            CheckProvider(new PlainObject(), typeof(PlainObject), automaticProvider, expectedString: "plain", supportsPropertyLookup: false, hasGeneratedCcw: false);
            CheckProvider(new DerivedPlainObject(), typeof(DerivedPlainObject), automaticProvider, expectedString: "plain", supportsPropertyLookup: false, hasGeneratedCcw: false);
            CheckProvider(new PlainValue(), typeof(PlainValue), automaticProvider, expectedString: "plain value", supportsPropertyLookup: false, hasGeneratedCcw: false);
            CheckProvider(new object(), typeof(object), automaticProvider, expectedString: "System.Object", supportsPropertyLookup: false, hasGeneratedCcw: false,
                expectedMetadataTypeName: "Object");

            List<string> list = ["value"];
            CheckProvider(list, typeof(List<string>), automaticProvider, expectedString: list.ToString(), supportsPropertyLookup: false,
                expectedRuntimeClassName: "Windows.Foundation.Collections.IVectorView`1<Windows.Foundation.Collections.IIterable`1<Char>>");

            string[] array = ["value"];
            CheckProvider(array, typeof(string[]), automaticProvider, expectedString: array.ToString(), supportsPropertyLookup: false,
                expectedRuntimeClassName: "Windows.Foundation.IReferenceArray`1<String>",
                expectedMetadataTypeName: "Windows.Foundation.IReferenceArray`1<String>");

            KeyValuePair<int, string> pair = new(7, "value");
            CheckProvider(pair, typeof(KeyValuePair<int, string>), automaticProvider, expectedString: pair.ToString(), supportsPropertyLookup: false,
                expectedRuntimeClassName: "Windows.Foundation.Collections.IKeyValuePair`2<Int32, String>",
                expectedMetadataTypeName: "Windows.Foundation.Collections.IKeyValuePair`2<Int32, String>");

            EventHandler<string> handler = static (_, _) => { };
            CheckProvider(handler, typeof(EventHandler<string>), automaticProvider, expectedString: handler.ToString(), supportsPropertyLookup: false,
                expectedRuntimeClassName: "Windows.Foundation.IReference`1<Windows.Foundation.EventHandler`1<String>>",
                expectedMetadataTypeName: "Windows.Foundation.EventHandler`1<String>");

            CheckProvider(new ExplicitNonXamlProvider(), typeof(ExplicitNonXamlProvider), hasProvider: true, hasProperties: true);
            CheckProvider(new InheritedNonXamlProvider(), typeof(ExplicitNonXamlProvider), hasProvider: true, hasProperties: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ExplicitProviderDependencyObject)), typeof(ExplicitProviderDependencyObject), hasProvider: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(InheritedExplicitProviderDependencyObject)), typeof(ExplicitProviderDependencyObject), hasProvider: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(EmptyProviderControl)), typeof(EmptyProviderControl), hasProvider: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ExplicitProviderControl)), typeof(ExplicitProviderControl), hasProvider: true, hasProperties: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(InheritedProviderControl)), typeof(ExplicitProviderControl), hasProvider: true, hasProperties: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ManualProviderControl)), typeof(ReportedType), hasProvider: true, expectedString: "manual");

            if (automaticProvider)
            {
                CheckStringRepresentationFailure();
            }

            CheckOpaqueObjectLifetime(automaticProvider);

            Console.WriteLine("Default custom property provider checks passed.");

            return 100;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return 120;
        }
    }

    private static void CheckProvider(
        object value,
        Type expectedType,
        bool hasProvider,
        bool hasProperties = false,
        string expectedString = "control",
        bool supportsPropertyLookup = true,
        bool hasGeneratedCcw = true,
        string expectedRuntimeClassName = null,
        string expectedMetadataTypeName = null)
    {
        void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
        nint inspectable = 0;
        nint provider = 0;
        nint identity = 0;
        Guid* iids = null;
        void* runtimeClassName = null;
        void* stringRepresentation = null;
        ABI.System.Type typeName = default;

        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, InspectableIid, out inspectable));

            void** inspectableVtable = *(void***)inspectable;
            uint count = 0;

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, uint*, Guid**, int>)inspectableVtable[3])(inspectable, &count, &iids));

            HashSet<Guid> distinctIids = [];

            for (uint i = 0; i < count; i++)
            {
                Check(iids[i] != Guid.Empty && distinctIids.Add(iids[i]), "Empty or duplicate interface entry.");
            }

            Check(distinctIids.Contains(ProviderIid) == hasProvider, $"Incorrect GetIids result for {value.GetType()}.");
            Check(distinctIids.Contains(InspectableIid) && distinctIids.Contains(typeof(IStringable).GUID), "Native interface slots were lost.");

            if (!hasGeneratedCcw)
            {
                Check(count == (hasProvider ? 7 : 6), "The opaque table must preserve all six existing entries.");
                Check(distinctIids.IsSupersetOf([
                    UnknownIid,
                    InspectableIid,
                    typeof(IStringable).GUID,
                    new Guid("00000038-0000-0000-C000-000000000046"),
                    new Guid("00000003-0000-0000-C000-000000000046"),
                    new Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90")]) && iids[count - 1] == UnknownIid,
                    "The opaque table must retain its native interfaces with IUnknown last.");
            }

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, void**, int>)inspectableVtable[4])(inspectable, &runtimeClassName));

            string className = HStringMarshaller.ConvertToManaged(runtimeClassName);

#if TEST_WINUI
            const string ControlRuntimeClassName = "Microsoft.UI.Composition.IAnimationObject";
            const string ProviderRuntimeClassName = "Microsoft.UI.Xaml.Data.ICustomPropertyProvider";
            const string ApplicationRuntimeClassName = "Microsoft.UI.Xaml.IApplicationOverrides";
#else
            const string ControlRuntimeClassName = "Windows.UI.Composition.IAnimationObject";
            const string ProviderRuntimeClassName = "Windows.UI.Xaml.Data.ICustomPropertyProvider";
            const string ApplicationRuntimeClassName = "Windows.UI.Xaml.IApplicationOverrides";
#endif
            string expectedClassName = expectedRuntimeClassName ?? (value switch
            {
                UIElement => ControlRuntimeClassName,
                StringableDependencyObject or NonXamlObject or NonXamlValue => "Windows.Foundation.IStringable",
                ExplicitProviderDependencyObject or ExplicitNonXamlProvider => ProviderRuntimeClassName,
                ApplicationProbe => ApplicationRuntimeClassName,
                _ => "Object"
            });

            Check(className == expectedClassName, $"The type bridge changed the runtime class name of {value.GetType()}: {className}.");

            int hr = Marshal.QueryInterface((nint)unknown, ProviderIid, out provider);

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                object info = hasGeneratedCcw
                    ? GetMarshallingInfo(null, value.GetType())
                    : GetOpaqueMarshallingInfo(null, value);
                CheckReadOnlyImageMemory(GetVtableEntries(GetVtableInfo(info)), "COM interface entries");
            }

            if (!hasProvider)
            {
                Check(hr == E_NOINTERFACE && provider == 0, $"Unexpected automatic provider for {value.GetType()}.");

                return;
            }

            Marshal.ThrowExceptionForHR(hr);
            Check(WindowsRuntimeMarshal.TryGetManagedObject((void*)provider, out object roundTrip) && ReferenceEquals(value, roundTrip), "Provider COM identity changed.");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(provider, UnknownIid, out identity));
            Check(identity == (nint)unknown, "The provider must share the object's canonical IUnknown.");

            void** providerVtable = *(void***)provider;

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                CheckReadOnlyImageMemory(providerVtable, "ICustomPropertyProvider vtable");
            }

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, ABI.System.Type*, int>)providerVtable[9])(provider, &typeName));

            Check((int)typeName.Kind == (expectedMetadataTypeName is null ? 2 : 1) &&
                HStringMarshaller.ConvertToManaged(typeName.Name) == (expectedMetadataTypeName ?? expectedType.AssemblyQualifiedName),
                $"Incorrect managed Type for {value.GetType()}.");

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, void**, int>)providerVtable[8])(provider, &stringRepresentation));
            Check(HStringMarshaller.ConvertToManaged(stringRepresentation) == expectedString, "Incorrect string representation.");

            CheckProperty(provider, "Included", indexed: false, expected: hasProperties, supported: supportsPropertyLookup);
            CheckProperty(provider, "Excluded", indexed: false, expected: false, supported: supportsPropertyLookup);
            CheckProperty(provider, "Missing", indexed: false, expected: false, supported: supportsPropertyLookup);
            CheckProperty(provider, "Item", indexed: true, expected: hasProperties, supported: supportsPropertyLookup);
        }
        finally
        {
            ABI.System.TypeMarshaller.Dispose(typeName);
            HStringMarshaller.Free(stringRepresentation);
            HStringMarshaller.Free(runtimeClassName);
            Marshal.FreeCoTaskMem((nint)iids);
            WindowsRuntimeMarshal.Free((void*)identity);
            WindowsRuntimeMarshal.Free((void*)provider);
            WindowsRuntimeMarshal.Free((void*)inspectable);
            WindowsRuntimeMarshal.Free(unknown);
        }
    }

    private static void CheckProperty(nint provider, string name, bool indexed, bool expected, bool supported)
    {
        void* propertyName = HStringMarshaller.ConvertToUnmanaged(name);
        void* property = (void*)1;
        ABI.System.Type indexType = ABI.System.TypeMarshaller.ConvertToUnmanaged(typeof(int));

        try
        {
            void** vtable = *(void***)provider;
            int hr = indexed
                ? ((delegate* unmanaged[MemberFunction]<nint, void*, ABI.System.Type, void**, int>)vtable[7])(provider, propertyName, indexType, &property)
                : ((delegate* unmanaged[MemberFunction]<nint, void*, void**, int>)vtable[6])(provider, propertyName, &property);

            if (!supported)
            {
                Check(hr == E_NOTSUPPORTED && property is null, $"Unsupported property lookup for {name} must fail and clear its output.");
                Exception error = RestrictedErrorInfoExceptionMarshaller.ConvertToManaged(hr);

                Check(error is NotSupportedException && error.Message.Contains("GeneratedCustomPropertyProviderAttribute", StringComparison.Ordinal),
                    "Unsupported property binding must provide actionable restricted error information.");

                return;
            }

            Check(hr == 0 && property != (void*)1 && (property is not null) == expected, $"Incorrect property lookup for {name}.");

            if (expected)
            {
                ICustomProperty descriptor = (ICustomProperty)WindowsRuntimeMarshal.ConvertToManaged(property);

                Check(descriptor.Type == typeof(int) && descriptor.CanRead, "Explicit property descriptor changed.");
            }
        }
        finally
        {
            ABI.System.TypeMarshaller.Dispose(indexType);
            if (property != (void*)1)
            {
                WindowsRuntimeMarshal.Free(property);
            }
            HStringMarshaller.Free(propertyName);
        }
    }

    private static void CheckStringRepresentationFailure()
    {
        void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(RuntimeHelpers.GetUninitializedObject(typeof(ThrowingControl)));
        nint provider = 0;
        void* result = null;

        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, ProviderIid, out provider));

            int hr = ((delegate* unmanaged[MemberFunction]<nint, void**, int>)(*(void***)provider)[8])(provider, &result);

            Check(hr == unchecked((int)0x80070057) && result is null, "ToString exceptions must be marshalled, with the output cleared.");
        }
        finally
        {
            HStringMarshaller.Free(result);
            WindowsRuntimeMarshal.Free((void*)provider);
            WindowsRuntimeMarshal.Free(unknown);
        }
    }

    private static void CheckOpaqueObjectLifetime(bool hasProvider)
    {
        WeakReference<PlainObject> reference = CreateOpaqueObjectReference(hasProvider, out nint native);

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Check(IsOpaqueObjectAlive(reference, native), "The opaque CCW must retain its managed object while native references exist.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free((void*)native);
        }

        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!IsOpaqueObjectAlive(reference, 0))
            {
                return;
            }
        }

        throw new InvalidOperationException("The opaque CCW retained its managed object after its last native reference was released.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<PlainObject> CreateOpaqueObjectReference(bool hasProvider, out nint native)
    {
        PlainObject value = new();
        WeakReference<PlainObject> reference = new(value);
        void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);

        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, hasProvider ? ProviderIid : InspectableIid, out native));
        }
        finally
        {
            WindowsRuntimeMarshal.Free(unknown);
        }

        return reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsOpaqueObjectAlive(WeakReference<PlainObject> reference, nint native)
    {
        return reference.TryGetTarget(out PlainObject target) &&
            (native == 0 || (WindowsRuntimeMarshal.TryGetManagedObject((void*)native, out object roundTrip) && ReferenceEquals(target, roundTrip)));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void CheckReadOnlyImageMemory(void* address, string description)
    {
        MemoryBasicInformation memory = default;

        Check(VirtualQuery(address, &memory, (nuint)sizeof(MemoryBasicInformation)) != 0,
            $"VirtualQuery failed for {description}.");
        Check(memory.Protect == 0x02 && memory.Type == 0x1000000,
            $"{description} must be preinitialized in read-only image memory (protection 0x{memory.Protect:X}, type 0x{memory.Type:X}).");
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern nuint VirtualQuery(void* address, MemoryBasicInformation* information, nuint length);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public void* BaseAddress;
        public void* AllocationBase;
        public uint AllocationProtect;
        public nuint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetInfo")]
    [return: UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeMarshallingInfo, WinRT.Runtime")]
    private static extern object GetMarshallingInfo(
        [UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeMarshallingInfo, WinRT.Runtime")] object unused,
        Type type);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetOpaqueInfo")]
    [return: UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeMarshallingInfo, WinRT.Runtime")]
    private static extern object GetOpaqueMarshallingInfo(
        [UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeMarshallingInfo, WinRT.Runtime")] object unused,
        object value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetVtableInfo")]
    [return: UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeVtableInfo, WinRT.Runtime")]
    private static extern object GetVtableInfo(
        [UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeMarshallingInfo, WinRT.Runtime")] object info);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_VtableEntries")]
    private static extern ComWrappers.ComInterfaceEntry* GetVtableEntries(
        [UnsafeAccessorType("WindowsRuntime.InteropServices.WindowsRuntimeVtableInfo, WinRT.Runtime")] object info);

    private class UnannotatedControl : Control
    {
        public int Included => 42;
        public int Excluded => 43;
        public int this[int index] => index;
        public override string ToString() => "control";
    }

    private sealed class DerivedControl : UnannotatedControl;
    private sealed class GenericControl<T> : UnannotatedControl;
    private sealed class ThrowingControl : UnannotatedControl
    {
        public override string ToString() => throw new ArgumentException("Expected test exception.");
    }

    private class NonXamlObject : IStringable
    {
        public override string ToString() => "non-XAML";
    }

    private sealed class GenericNonXamlObject<T> : NonXamlObject
    {
        public override string ToString() => "generic";
    }

    private readonly struct NonXamlValue : IStringable
    {
        public override string ToString() => "value";
    }

    private class PlainObject
    {
        public int Included => 42;
        public int this[int index] => index;
        public override string ToString() => "plain";
    }

    private sealed class DerivedPlainObject : PlainObject;

    private readonly struct PlainValue
    {
        public override string ToString() => "plain value";
    }

    [GeneratedCustomPropertyProvider(["Included"], [typeof(int)])]
    private partial class ExplicitNonXamlProvider
    {
        public int Included => 42;
        public int Excluded => 43;
        public int this[int index] => index;
        public override string ToString() => "control";
    }

    private sealed class InheritedNonXamlProvider : ExplicitNonXamlProvider;

    private class FrameworkElementProbe : FrameworkElement
    {
        public override string ToString() => "control";
    }

    private sealed class DerivedFrameworkElementProbe : FrameworkElementProbe;

    private class DependencyObjectProbe : DependencyObject
    {
        public override string ToString() => "control";
    }

    private sealed class DerivedDependencyObjectProbe : DependencyObjectProbe;

    private sealed class StringableDependencyObject : DependencyObjectProbe, IStringable;

    private sealed class ApplicationProbe : Application
    {
        public override string ToString() => "control";
    }

    [GeneratedCustomPropertyProvider([], [])]
    private partial class ExplicitProviderDependencyObject : DependencyObjectProbe;

    private sealed class InheritedExplicitProviderDependencyObject : ExplicitProviderDependencyObject;

    [GeneratedCustomPropertyProvider([], [])]
    private sealed partial class EmptyProviderControl : UnannotatedControl;

    [GeneratedCustomPropertyProvider(["Included"], [typeof(int)])]
    private partial class ExplicitProviderControl : UnannotatedControl;

    private sealed class InheritedProviderControl : ExplicitProviderControl;

    private sealed class ManualProviderControl : UnannotatedControl, ICustomPropertyProvider
    {
        public Type Type => typeof(ReportedType);
        public ICustomProperty GetCustomProperty(string name) => null;
        public ICustomProperty GetIndexedProperty(string name, Type type) => null;
        public string GetStringRepresentation() => "manual";
    }

    private sealed class ReportedType;
}
