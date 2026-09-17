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

internal static unsafe partial class XamlTypeBridgeTests
{
    private static readonly Guid ProviderIid = new("7C925755-3E48-42B4-8677-76372267033F");
    private static readonly Guid InspectableIid = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
    private const int E_NOINTERFACE = unchecked((int)0x80004002);

    public static int Run()
    {
        try
        {
#if TEST_XAML_PROVIDER_DISABLED
            bool automaticProvider = false;
#else
            bool automaticProvider = true;
#endif

            // Exercise the managed CCWs without activating XAML or requiring a UI thread/package.
            // No native Control members are used; real style/layout coverage lives in ObjectLifetimeTests.
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(UnannotatedControl)), typeof(UnannotatedControl), automaticProvider);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DerivedControl)), typeof(DerivedControl), automaticProvider);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(GenericControl<int>)), typeof(GenericControl<int>), automaticProvider);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(GenericControl<string>)), typeof(GenericControl<string>), automaticProvider);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(DependencyObjectProbe)), typeof(DependencyObjectProbe), automaticProvider);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(StringableDependencyObject)), typeof(StringableDependencyObject), automaticProvider);
            CheckProvider(new NonXamlObject(), typeof(NonXamlObject), hasProvider: false);

            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(EmptyProviderControl)), typeof(EmptyProviderControl), hasProvider: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ExplicitProviderControl)), typeof(ExplicitProviderControl), hasProvider: true, hasProperties: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(InheritedProviderControl)), typeof(ExplicitProviderControl), hasProvider: true, hasProperties: true);
            CheckProvider(RuntimeHelpers.GetUninitializedObject(typeof(ManualProviderControl)), typeof(ReportedType), hasProvider: true, expectedString: "manual");

            if (automaticProvider)
            {
                CheckStringRepresentationFailure();
            }

            Console.WriteLine("XAML type bridge checks passed.");

            return 100;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return 120;
        }
    }

    private static void CheckProvider(object value, Type expectedType, bool hasProvider, bool hasProperties = false, string expectedString = "control")
    {
        void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
        nint inspectable = 0;
        nint provider = 0;
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

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, void**, int>)inspectableVtable[4])(inspectable, &runtimeClassName));

            string className = HStringMarshaller.ConvertToManaged(runtimeClassName);

#if TEST_WINUI
            const string ControlRuntimeClassName = "Microsoft.UI.Composition.IAnimationObject";
#else
            const string ControlRuntimeClassName = "Windows.UI.Composition.IAnimationObject";
#endif
            string expectedClassName = value is UnannotatedControl
                ? ControlRuntimeClassName
                : value is StringableDependencyObject or NonXamlObject ? "Windows.Foundation.IStringable" : "Object";

            Check(className == expectedClassName, $"The type bridge changed the runtime class name of {value.GetType()}: {className}.");

            int hr = Marshal.QueryInterface((nint)unknown, ProviderIid, out provider);

            if (!hasProvider)
            {
                Check(hr == E_NOINTERFACE && provider == 0, $"Unexpected automatic provider for {value.GetType()}.");

                return;
            }

            Marshal.ThrowExceptionForHR(hr);
            Check(WindowsRuntimeMarshal.TryGetManagedObject((void*)provider, out object roundTrip) && ReferenceEquals(value, roundTrip), "Provider COM identity changed.");

            void** providerVtable = *(void***)provider;

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, ABI.System.Type*, int>)providerVtable[9])(provider, &typeName));

            Check((int)typeName.Kind == 2 && HStringMarshaller.ConvertToManaged(typeName.Name) == expectedType.AssemblyQualifiedName,
                $"Incorrect managed Type for {value.GetType()}.");

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, void**, int>)providerVtable[8])(provider, &stringRepresentation));
            Check(HStringMarshaller.ConvertToManaged(stringRepresentation) == expectedString, "Incorrect string representation.");

            CheckProperty(provider, "Included", indexed: false, expected: hasProperties);
            CheckProperty(provider, "Excluded", indexed: false, expected: false);
            CheckProperty(provider, "Missing", indexed: false, expected: false);
            CheckProperty(provider, "Item", indexed: true, expected: hasProperties);
        }
        finally
        {
            ABI.System.TypeMarshaller.Dispose(typeName);
            HStringMarshaller.Free(stringRepresentation);
            HStringMarshaller.Free(runtimeClassName);
            Marshal.FreeCoTaskMem((nint)iids);
            WindowsRuntimeMarshal.Free((void*)provider);
            WindowsRuntimeMarshal.Free((void*)inspectable);
            WindowsRuntimeMarshal.Free(unknown);
        }
    }

    private static void CheckProperty(nint provider, string name, bool indexed, bool expected)
    {
        void* propertyName = HStringMarshaller.ConvertToUnmanaged(name);
        void* property = null;
        ABI.System.Type indexType = ABI.System.TypeMarshaller.ConvertToUnmanaged(typeof(int));

        try
        {
            void** vtable = *(void***)provider;
            int hr = indexed
                ? ((delegate* unmanaged[MemberFunction]<nint, void*, ABI.System.Type, void**, int>)vtable[7])(provider, propertyName, indexType, &property)
                : ((delegate* unmanaged[MemberFunction]<nint, void*, void**, int>)vtable[6])(provider, propertyName, &property);

            Check(hr == 0 && (property is not null) == expected, $"Incorrect property lookup for {name}.");

            if (expected)
            {
                ICustomProperty descriptor = (ICustomProperty)WindowsRuntimeMarshal.ConvertToManaged(property);

                Check(descriptor.Type == typeof(int) && descriptor.CanRead, "Explicit property descriptor changed.");
            }
        }
        finally
        {
            ABI.System.TypeMarshaller.Dispose(indexType);
            WindowsRuntimeMarshal.Free(property);
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

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

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

    private sealed class NonXamlObject : IStringable
    {
        public override string ToString() => "non-XAML";
    }

    private class DependencyObjectProbe : DependencyObject
    {
        public override string ToString() => "control";
    }

    // This has the same explicit interface set as NonXamlObject, but must not share its bridge-less table.
    private sealed class StringableDependencyObject : DependencyObjectProbe, IStringable;

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
