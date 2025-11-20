// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class TypeMapping
{
    /// <summary>
    /// Represents a mapping from a CLR/public type name to its Windows Runtime (ABI) counterpart,
    /// optionally including the WinRT type signature used for GUID generation and marshalling.
    /// </summary>
    /// <variable name="PublicName">The CLR/public type name.</variable>
    /// <variable name="WindowsRuntimeNamespace">The Windows Runtime namespace.</variable>
    /// <variable name="WindowsRuntimeName">The Windows Runtime type name.</variable>
    /// <variable name="Signature">Optional hardcoded WinRT type signature</variable>
    internal readonly record struct MappedType(
         string PublicName,
         string WindowsRuntimeNamespace,
         string WindowsRuntimeName,
         string? Signature = null
    );

    /// <summary>
    /// Represents a mapping from a projected Microsoft.UI.Xaml type to the Windows.UI.Xaml counterpart.
    /// </summary>
    /// <variable name="WindowsRuntimeNamespace">The Windows.UI.Xaml namespace.</variable>
    /// <variable name="WindowsRuntimeName">The Windows.UI.Xaml type name.</variable>
    /// <variable name="Signature">Optional hardcoded WinRT type signature</variable>
    internal readonly record struct WindowsUIXamlMappedType(
         string WindowsRuntimeNamespace,
         string WindowsRuntimeName,
         string? Signature = null
    );

    /// <summary>
    /// Immutable map from CLR namespaces to the set of CLR→Windows Runtime type mappings.
    /// Keys are CLR namespaces (e.g., "System"), and values are the materialized mappings for types
    /// within that namespace.
    /// 
    /// This should be in sync with the mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
    /// 
    /// </summary>
    private static readonly FrozenDictionary<string, ImmutableArray<MappedType>> WinRTToABITypeMapping = FrozenDictionary.Create<string, ImmutableArray<MappedType>>(comparer: null,
        new("System", [
            new("IServiceProvider", "Microsoft.UI.Xaml", "IXamlServiceProvider"),
            new("IntPtr", "WinRT.Interop", "HWND"),
            new("DateTimeOffset", "Windows.Foundation", "DateTime", "struct(Windows.Foundation.DateTime;i8)"),
            new("EventHandler`1", "Windows.Foundation", "EventHandler`1"),
            new("Exception", "Windows.Foundation", "HResult", "struct(Windows.Foundation.HResult;i4)"),
            new("IDisposable", "Windows.Foundation", "IClosable"),
            new("Nullable`1", "Windows.Foundation", "IReference`1"),
            new("TimeSpan", "Windows.Foundation", "TimeSpan", "struct(Windows.Foundation.TimeSpan;i8)"),
            new("EventHandler`2", "Windows.Foundation", "TypedEventHandler`2"),
            new("Uri", "Windows.Foundation", "Uri", "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})"),

            new("AttributeTargets", "Windows.Foundation.Metadata", "AttributeTargets"),
            new("AttributeUsageAttribute", "Windows.Foundation.Metadata", "AttributeUsageAttribute"),

            new("IServiceProvider", "Windows.UI.Xaml", "IXamlServiceProvider"),

            new("Type", "Windows.UI.Xaml.Interop", "TypeName", "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))"),
       ]),

       new("System.ComponentModel", [
            new("DataErrorsChangedEventArgs", "Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})"),
            new("INotifyDataErrorInfo", "Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo"),
            new("INotifyPropertyChanged", "Microsoft.UI.Xaml.Data", "INotifyPropertyChanged"),
            new("PropertyChangedEventArgs", "Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.PropertyChangedEventArgs;{63d0c952-396b-54f4-af8c-ba8724a427bf})"),
            new("PropertyChangedEventHandler", "Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler")
       ]),

        new("System.Windows.Input", [
            new("ICommand", "Microsoft.UI.Xaml.Input", "ICommand")
        ]),

        new("System.Collections",
        [
            new("IEnumerable", "Microsoft.UI.Xaml.Interop", "IBindableIterable"),
            new("IList", "Microsoft.UI.Xaml.Interop", "IBindableVector")
,
        ]),

        new("System.Collections.Specialized",
        [
            new("INotifyCollectionChanged", "Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged"),
            new("NotifyCollectionChangedAction", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)"),
            new("NotifyCollectionChangedEventArgs", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{da049ff2-d2e0-5fe8-8c7b-f87f26060b6f})"),
            new("NotifyCollectionChangedEventHandler", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")
        ]),

        new("Microsoft.UI.Xaml", [
            new("CornerRadius", "Microsoft.UI.Xaml", "CornerRadius"),
            new("Duration", "Microsoft.UI.Xaml", "Duration"),
            new("GridLength", "Microsoft.UI.Xaml", "GridLength")
        ]),

        new("Microsoft.UI.Xaml.Media.Animation", [
            new("KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime"),
            new("RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior")
        ]),

        new("Microsoft.UI.Xaml.Media.Media3D", [
            new("Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D")
        ]),

        new("WindowsRuntime.InteropServices", [
            new("EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken", "struct(Windows.Foundation.EventRegistrationToken;i8)")
        ]),

        new("WindowsRuntime.InteropServices", [
            new("EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken", "struct(Windows.Foundation.EventRegistrationToken;i8)")
        ]),

        new("Windows.Foundation", [
            new("IReferenceArray", "Windows.Foundation", "IReferenceArray`1")
        ]),

        new("System.Collections.Generic", [
            new("IEnumerable`1", "Windows.Foundation.Collections", "IIterable`1"),
            new("IEnumerator`1", "Windows.Foundation.Collections", "IIterator`1"),
            new("KeyValuePair`2", "Windows.Foundation.Collections", "IKeyValuePair`2"),
            new("IReadOnlyDictionary`2", "Windows.Foundation.Collections", "IMapView`2"),
            new("IDictionary`2", "Windows.Foundation.Collections", "IMap`2"),
            new("IReadOnlyList`1", "Windows.Foundation.Collections", "IVectorView`1"),
            new("IList`1", "Windows.Foundation.Collections", "IVector`1")
        ]),

        new("System.Numerics", [
            new("Matrix3x2", "Windows.Foundation.Numerics", "Matrix3x2", "struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4)"),
            new("Matrix4x4", "Windows.Foundation.Numerics", "Matrix4x4", "struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4)"),
            new("Plane", "Windows.Foundation.Numerics", "Plane", "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)"),
            new("Quaternion", "Windows.Foundation.Numerics", "Quaternion", "struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4)"),
            new("Vector2", "Windows.Foundation.Numerics", "Vector2", "struct(Windows.Foundation.Numerics.Vector2;f4;f4)"),
            new("Vector3", "Windows.Foundation.Numerics", "Vector3", "struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4)"),
            new("Vector4", "Windows.Foundation.Numerics", "Vector4", "struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4)")
        ]),

        new("Windows.Foundation", [
            new("Point", "Windows.Foundation", "Point", "struct(Windows.Foundation.Point;f4;f4)"),
            new("Size", "Windows.Foundation", "Size", "struct(Windows.Foundation.Size;f4;f4)"),
            new("Rect", "Windows.Foundation", "Rect", "struct(Windows.Foundation.Rect;f4;f4;f4;f4)")
        ])
    );

    /// <summary>
    /// Immutable map from projected Microsoft.UI.Xaml type full names (keys) to their corresponding
    /// Windows.UI.Xaml mapping descriptor (values).
    /// </summary>
    private static readonly FrozenDictionary<string, WindowsUIXamlMappedType> WindowsUIXamlProjectionTypeMapping = FrozenDictionary.Create<string, WindowsUIXamlMappedType>(comparer: null,
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)")),
        new("Microsoft.UI.Xaml.Interop.INotifyCollectionChanged", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "INotifyCollectionChanged")),
        new("Microsoft.UI.Xaml.Interop.IBindableIterable", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "IBindableIterable")),
        new("Microsoft.UI.Xaml.Interop.IBindableVector", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "IBindableVector")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),

        new("Microsoft.UI.Xaml.Input.ICommand", new WindowsUIXamlMappedType("Windows.UI.Xaml.Input", "ICommand")),

        new("Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Windows.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})")),
        new("Microsoft.UI.Xaml.Data.INotifyDataErrorInfo", new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "INotifyDataErrorInfo")),
        new("Microsoft.UI.Xaml.Data.INotifyPropertyChanged", new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "INotifyPropertyChanged")),
        new("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Windows.UI.Xaml.Data.PropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})")),
        new("Microsoft.UI.Xaml.Data.PropertyChangedEventHandler", new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "PropertyChangedEventHandler"))
    );

    /// <summary>
    /// Returns the mapped WinRT full type name for a given CLR namespace and type name.
    /// </summary>
    /// <param name="Namespace">The CLR namespace of the type.</param>
    /// <param name="Name">The CLR type name.</param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <returns>
    /// The WinRT full type name if a mapping exists; otherwise, the original CLR namespace and type name.
    /// </returns>
    internal static string FindMappedWinRTFullName(string? Namespace, string? Name, bool useWindowsUIXamlProjections)
    {
        if (Namespace is null || Name is null)
        {
            throw new ArgumentNullException("Namespace or Name is null");
        }

        if (!WinRTToABITypeMapping.TryGetValue(Namespace, out ImmutableArray<MappedType> results))
        {
            return Namespace + "." + Name;
        }

        // Match the default struct case by checking PublicName for non-null
        string? result = results.FirstOrDefault(t => t.PublicName == Name) is { PublicName: not null } found ? found.WindowsRuntimeNamespace + "." + found.WindowsRuntimeName : null;
        return result is null
            ? Namespace + "." + Name
            : (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(result))
                ? WindowsUIXamlProjectionTypeMapping[result].WindowsRuntimeNamespace + "." + WindowsUIXamlProjectionTypeMapping[result].WindowsRuntimeName : result;
    }


    /// <summary>
    /// Retrieves the WinRT signature string for a mapped CLR type.
    /// </summary>
    /// <param name="Namespace">The CLR namespace of the type.</param>
    /// <param name="Name">The CLR type name.</param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <returns>
    /// The signature string if found; otherwise, null.
    /// </returns>
    internal static string? FindGuidSignatureForMappedType(string? Namespace, string? Name, bool useWindowsUIXamlProjections)
    {
        if (Namespace is null || Name is null)
        {
            return null;
        }

        if (!WinRTToABITypeMapping.TryGetValue(Namespace, out ImmutableArray<MappedType> results))
        {
            return null;
        }

        MappedType? result = results.FirstOrDefault(t => t.PublicName == Name);

        if (result is null)
        {
            return null;
        }

        string? resultSignature = result.Value.Signature;

        if (resultSignature is null)
        {
            return null;
        }

        string? resultFullName = result.Value.WindowsRuntimeNamespace + "." + result.Value.WindowsRuntimeName;

        if (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(resultFullName))
        {
            resultSignature = WindowsUIXamlProjectionTypeMapping[resultFullName].Signature;
        }

        return resultSignature;
    }
}
