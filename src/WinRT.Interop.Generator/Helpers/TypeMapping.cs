// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class TypeMapping
{
    internal readonly record struct MappedType(
         string PublicName,
         string? WinRTNamespace = null, // WinRT (ABI) namespace
         string? WinRTName = null       // WinRT (ABI) type name (same as winrt_name)
    );

    private static readonly Dictionary<string, MappedType[]> WinRTToABITypeMapping = new(StringComparer.Ordinal)
    {
        ["System"] =
        [
            new MappedType("IServiceProvider", "Microsoft.UI.Xaml", "IXamlServiceProvider"),

            new MappedType("IntPtr", "WinRT.Interop", "HWND"),

            new MappedType("DateTimeOffset", "Windows.Foundation", "DateTime"),
            new MappedType("EventHandler`1", "Windows.Foundation", "EventHandler`1"),
            new MappedType("Exception", "Windows.Foundation", "HResult"),
            new MappedType("IDisposable", "Windows.Foundation", "IClosable"),
            new MappedType("Nullable`1", "Windows.Foundation", "IReference`1"),
            new MappedType("TimeSpan", "Windows.Foundation", "TimeSpan"),
            new MappedType("EventHandler`2", "Windows.Foundation", "TypedEventHandler`2"),
            new MappedType("Uri", "Windows.Foundation", "Uri"),

            new MappedType("AttributeTargets", "Windows.Foundation.Metadata", "AttributeTargets"),
            new MappedType("AttributeUsageAttribute", "Windows.Foundation.Metadata", "AttributeUsageAttribute"),

            new MappedType("IServiceProvider", "Windows.UI.Xaml", "IXamlServiceProvider"),

            new MappedType("Type", "Windows.UI.Xaml.Interop", "TypeName"),
        ],


        ["System.ComponentModel"] =
        [
            new MappedType("DataErrorsChangedEventArgs", "Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs"),
            new MappedType("INotifyDataErrorInfo", "Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo"),
            new MappedType("INotifyPropertyChanged", "Microsoft.UI.Xaml.Data", "INotifyPropertyChanged"),
            new MappedType("PropertyChangedEventArgs", "Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs"),
            new MappedType("PropertyChangedEventHandler", "Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler"),

            //new MappedType("DataErrorsChangedEventArgs", "Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs"),
            //new MappedType("INotifyDataErrorInfo", "Windows.UI.Xaml.Data", "INotifyDataErrorInfo"),
            //new MappedType("INotifyPropertyChanged", "Windows.UI.Xaml.Data", "INotifyPropertyChanged"),
            //new MappedType("PropertyChangedEventArgs", "Windows.UI.Xaml.Data", "PropertyChangedEventArgs"),
            //new MappedType("PropertyChangedEventHandler", "Windows.UI.Xaml.Data", "PropertyChangedEventHandler"),
        ],

        ["System.Windows.Input"] =
        [
            new MappedType("ICommand", "Microsoft.UI.Xaml.Input", "ICommand"),

            //new MappedType("ICommand", "Windows.UI.Xaml.Input", "ICommand"),
        ],


        ["System.Collections"] =
        [
            new MappedType("IEnumerable", "Microsoft.UI.Xaml.Interop", "IBindableIterable"),
            new MappedType("IList", "Microsoft.UI.Xaml.Interop", "IBindableVector"),

            //new MappedType("IBindableIterable", "Windows.UI.Xaml.Interop", "IBindableIterable"),
            //new MappedType("IBindableVector", "Windows.UI.Xaml.Interop", "IBindableVector"),
        ],

        ["System.Collections.Specialized"] =
        [
            new MappedType("INotifyCollectionChanged", "Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged"),
            new MappedType("NotifyCollectionChangedAction", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction"),
            new MappedType("NotifyCollectionChangedEventArgs", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs"),
            new MappedType("NotifyCollectionChangedEventHandler", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler"),

            //new MappedType("INotifyCollectionChanged", "Windows.UI.Xaml.Interop", "INotifyCollectionChanged"),
            //new MappedType("NotifyCollectionChangedAction", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction"),
            //new MappedType("NotifyCollectionChangedEventArgs", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs"),
            //new MappedType("NotifyCollectionChangedEventHandler", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler"),
        ],

        ["Microsoft.UI.Xaml"] =
        [
            new MappedType("CornerRadius", "Microsoft.UI.Xaml", "CornerRadius"),
            new MappedType("Duration", "Microsoft.UI.Xaml", "Duration"),
            new MappedType("GridLength", "Microsoft.UI.Xaml", "GridLength"),
        ],


        ["Microsoft.UI.Xaml.Media.Animation"] =
        [
            new MappedType("KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime"),
            new MappedType("RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior"),
        ],

        ["Microsoft.UI.Xaml.Media.Media3D"] =
        [
            new MappedType("Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D"),
        ],

        ["WindowsRuntime.InteropServices"] =
        [
            new MappedType("EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken"),
        ],

        ["Windows.Foundation"] =
        [
            new MappedType("IReferenceArray", "Windows.Foundation", "IReferenceArray`1"),
        ],

        // ---------------------------
        // System.Collections.Generic
        // ---------------------------
        ["System.Collections.Generic"] =
        [
            // From Windows.Foundation.Collections
            new MappedType("IEnumerable`1", "Windows.Foundation.Collections", "IIterable`1"),
            new MappedType("IEnumerator`1", "Windows.Foundation.Collections", "IIterator`1"),
            new MappedType("KeyValuePair`2", "Windows.Foundation.Collections", "IKeyValuePair`2"),
            new MappedType("IReadOnlyDictionary`2", "Windows.Foundation.Collections", "IMapView`2"),
            new MappedType("IDictionary`2", "Windows.Foundation.Collections", "IMap`2"),
            new MappedType("IReadOnlyList`1", "Windows.Foundation.Collections", "IVectorView`1"),
            new MappedType("IList`1", "Windows.Foundation.Collections", "IVector`1"),
        ],

        // ---------------------------
        // Windows.Foundation.Collections
        // ---------------------------
        ["Windows.Foundation.Collections"] =
        [
            // From Windows.Foundation.Collections
            new MappedType("CollectionChange", "Windows.Foundation.Collections", "CollectionChange"),
            new MappedType("IMapChangedEventArgs`1", "Windows.Foundation.Collections", "IMapChangedEventArgs`1"),
            new MappedType("IObservableMap`2", "Windows.Foundation.Collections", "IObservableMap`2"),
            new MappedType("IObservableVector`1", "Windows.Foundation.Collections", "IObservableVector`1"),
            new MappedType("IVectorChangedEventArgs", "Windows.Foundation.Collections", "IVectorChangedEventArgs"),
            new MappedType("MapChangedEventHandler`2", "Windows.Foundation.Collections", "MapChangedEventHandler`2"),
            new MappedType("VectorChangedEventHandler`1", "Windows.Foundation.Collections", "VectorChangedEventHandler`1"),
        ],

        // ---------------------------
        // System.Numerics
        // ---------------------------
        ["System.Numerics"] =
        [
            // From Windows.Foundation.Numerics
            new MappedType("Matrix3x2", "Windows.Foundation.Numerics", "Matrix3x2"),
            new MappedType("Matrix4x4", "Windows.Foundation.Numerics", "Matrix4x4"),
            new MappedType("Plane", "Windows.Foundation.Numerics", "Plane"),
            new MappedType("Quaternion", "Windows.Foundation.Numerics", "Quaternion"),
            new MappedType("Vector2", "Windows.Foundation.Numerics", "Vector2"),
            new MappedType("Vector3", "Windows.Foundation.Numerics", "Vector3"),
            new MappedType("Vector4", "Windows.Foundation.Numerics", "Vector4"),
        ]
    };

    internal static string FindMappedWinRTFullName(string? Namespace, string? Name)
    {
        if (Namespace is null || Name is null)
        {
            throw new ArgumentNullException("Namespace or Name is null");
        }
        if (!WinRTToABITypeMapping.TryGetValue(Namespace, out MappedType[]? results))
        {
            return Namespace + "." + Name;
        }

        // Match the default struct case by checking PublicName for non-null
        return results.FirstOrDefault(t => t.PublicName == Name) is { PublicName: not null } found
            ? found.WinRTNamespace + "." + found.WinRTName
            : Namespace + "." + Name;
    }
}
