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
         string WinRTNamespace, // WinRT (ABI) namespace
         string WinRTName,      // WinRT (ABI) type name (same as winrt_name)
         string? Signature = null
    );

    internal readonly record struct WindowsUIXamlMappedType(
         string WinRTNamespace, // WinRT (ABI) namespace
         string WinRTName,      // WinRT (ABI) type name (same as winrt_name)
         string? Signature = null
    );

    private static readonly Dictionary<string, MappedType[]> WinRTToABITypeMapping = new(StringComparer.Ordinal)
    {
        ["System"] =
        [
            new MappedType("IServiceProvider", "Microsoft.UI.Xaml", "IXamlServiceProvider"),

            new MappedType("IntPtr", "WinRT.Interop", "HWND"),

            new MappedType("DateTimeOffset", "Windows.Foundation", "DateTime", "struct(Windows.Foundation.DateTime;i8)"),
            new MappedType("EventHandler`1", "Windows.Foundation", "EventHandler`1"),
            new MappedType("Exception", "Windows.Foundation", "HResult", "struct(Windows.Foundation.HResult;i4)"),
            new MappedType("IDisposable", "Windows.Foundation", "IClosable"),
            new MappedType("Nullable`1", "Windows.Foundation", "IReference`1"),
            new MappedType("TimeSpan", "Windows.Foundation", "TimeSpan", "struct(Windows.Foundation.TimeSpan;i8)"),
            new MappedType("EventHandler`2", "Windows.Foundation", "TypedEventHandler`2"),
            new MappedType("Uri", "Windows.Foundation", "Uri", "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})"),

            new MappedType("AttributeTargets", "Windows.Foundation.Metadata", "AttributeTargets"),
            new MappedType("AttributeUsageAttribute", "Windows.Foundation.Metadata", "AttributeUsageAttribute"),

            new MappedType("IServiceProvider", "Windows.UI.Xaml", "IXamlServiceProvider"),

            new MappedType("Type", "Windows.UI.Xaml.Interop", "TypeName", "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))"),
        ],


        ["System.ComponentModel"] =
        [
            new MappedType("DataErrorsChangedEventArgs", "Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})"),
            new MappedType("INotifyDataErrorInfo", "Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo"),
            new MappedType("INotifyPropertyChanged", "Microsoft.UI.Xaml.Data", "INotifyPropertyChanged"),
            new MappedType("PropertyChangedEventArgs", "Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.PropertyChangedEventArgs;{63d0c952-396b-54f4-af8c-ba8724a427bf})"),
            new MappedType("PropertyChangedEventHandler", "Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler"),
        ],

        ["System.Windows.Input"] =
        [
            new MappedType("ICommand", "Microsoft.UI.Xaml.Input", "ICommand"),
        ],


        ["System.Collections"] =
        [
            new MappedType("IEnumerable", "Microsoft.UI.Xaml.Interop", "IBindableIterable"),
            new MappedType("IList", "Microsoft.UI.Xaml.Interop", "IBindableVector"),

        ],

        ["System.Collections.Specialized"] =
        [
            new MappedType("INotifyCollectionChanged", "Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged"),
            new MappedType("NotifyCollectionChangedAction", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction" , "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)"),
            new MappedType("NotifyCollectionChangedEventArgs", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{da049ff2-d2e0-5fe8-8c7b-f87f26060b6f})"),
            new MappedType("NotifyCollectionChangedEventHandler", "Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler"),
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
            new MappedType("EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken", "struct(Windows.Foundation.EventRegistrationToken;i8)"),
        ],

        ["Windows.Foundation"] =
        [
            new MappedType("IReferenceArray", "Windows.Foundation", "IReferenceArray`1"),
        ],

        ["System.Collections.Generic"] =
        [
            new MappedType("IEnumerable`1", "Windows.Foundation.Collections", "IIterable`1"),
            new MappedType("IEnumerator`1", "Windows.Foundation.Collections", "IIterator`1"),
            new MappedType("KeyValuePair`2", "Windows.Foundation.Collections", "IKeyValuePair`2"),
            new MappedType("IReadOnlyDictionary`2", "Windows.Foundation.Collections", "IMapView`2"),
            new MappedType("IDictionary`2", "Windows.Foundation.Collections", "IMap`2"),
            new MappedType("IReadOnlyList`1", "Windows.Foundation.Collections", "IVectorView`1"),
            new MappedType("IList`1", "Windows.Foundation.Collections", "IVector`1"),
        ],

        ["System.Numerics"] =
        [
            new MappedType("Matrix3x2", "Windows.Foundation.Numerics", "Matrix3x2", "struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4)"),
            new MappedType("Matrix4x4", "Windows.Foundation.Numerics", "Matrix4x4", "struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4)"),
            new MappedType("Plane", "Windows.Foundation.Numerics", "Plane", "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)"),
            new MappedType("Quaternion", "Windows.Foundation.Numerics", "Quaternion", "struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4)"),
            new MappedType("Vector2", "Windows.Foundation.Numerics", "Vector2", "struct(Windows.Foundation.Numerics.Vector2;f4;f4)"),
            new MappedType("Vector3", "Windows.Foundation.Numerics", "Vector3", "struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4)"),
            new MappedType("Vector4", "Windows.Foundation.Numerics", "Vector4", "struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4)"),
        ],

        ["Windows.Foundation"] =
        [
            new MappedType("Point", "Windows.Foundation", "Point", "struct(Windows.Foundation.Point;f4;f4)"),
            new MappedType("Size", "Windows.Foundation", "Size", "struct(Windows.Foundation.Size;f4;f4)"),
            new MappedType("Rect", "Windows.Foundation", "Rect", "struct(Windows.Foundation.Rect;f4;f4;f4;f4)")
        ]
    };



    private static readonly Dictionary<string, WindowsUIXamlMappedType> WindowsUIXamlProjectionTypeMapping = new(StringComparer.Ordinal)
    {
        ["Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler"),
        ["Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})"),
        ["Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)"),
        ["Microsoft.UI.Xaml.Interop.INotifyCollectionChanged"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "INotifyCollectionChanged"),
        ["Microsoft.UI.Xaml.Interop.IBindableIterable"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "IBindableIterable"),
        ["Microsoft.UI.Xaml.Interop.IBindableVector"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "IBindableVector"),
        ["Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler"),

        ["Microsoft.UI.Xaml.Input.ICommand"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Input", "ICommand"),

        ["Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Windows.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})"),
        ["Microsoft.UI.Xaml.Data.INotifyDataErrorInfo"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "INotifyDataErrorInfo"),
        ["Microsoft.UI.Xaml.Data.INotifyPropertyChanged"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "INotifyPropertyChanged"),
        ["Microsoft.UI.Xaml.Data.PropertyChangedEventArgs"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Windows.UI.Xaml.Data.PropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})"),
        ["Microsoft.UI.Xaml.Data.PropertyChangedEventHandler"] = new WindowsUIXamlMappedType("Windows.UI.Xaml.Data", "PropertyChangedEventHandler")
    };



    internal static string FindMappedWinRTFullName(string? Namespace, string? Name, bool useWindowsUIXamlProjections)
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
        string? result = results.FirstOrDefault(t => t.PublicName == Name) is { PublicName: not null } found ? found.WinRTNamespace + "." + found.WinRTName : null;
        return result is null
            ? Namespace + "." + Name
            : (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(result))
                ? WindowsUIXamlProjectionTypeMapping[result].WinRTNamespace + "." + WindowsUIXamlProjectionTypeMapping[result].WinRTName : result;
    }

    internal static string? FindGuidSignatureForMappedType(string? Namespace, string? Name, bool useWindowsUIXamlProjections)
    {
        if (Namespace is null || Name is null)
        {
            return null;
        }

        if (!WinRTToABITypeMapping.TryGetValue(Namespace, out MappedType[]? results))
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

        string? resultFullName = result.Value.WinRTNamespace + "." + result.Value.WinRTName;

        if (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(resultFullName))
        {
            resultSignature = WindowsUIXamlProjectionTypeMapping[resultFullName].Signature;
        }

        return resultSignature;
    }
}
