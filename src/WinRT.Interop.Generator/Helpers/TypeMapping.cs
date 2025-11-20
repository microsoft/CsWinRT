// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class TypeMapping
{
    /// <summary>
    /// Represents a mapping from a CLR/public type name to its Windows Runtime (ABI) counterpart,
    /// optionally including the hardcoded WinRT type signature used for GUID generation and marshalling.
    /// </summary>
    /// <variable name="WindowsRuntimeNamespace">The Windows Runtime namespace.</variable>
    /// <variable name="WindowsRuntimeName">The Windows Runtime type name.</variable>
    /// <variable name="Signature">Optional hardcoded WinRT type signature</variable>
    public readonly record struct MappedType(
         string WindowsRuntimeNamespace,
         string WindowsRuntimeName,
         string? Signature = null
    );

    /// <summary>
    /// Immutable map from CLR fullnames to the set of CLR→Windows Runtime type mappings.
    /// Keys are CLR fullnames (e.g., "System.String"), and values are the materialized mappings for types
    /// within that namespace.
    /// </summary>
    /// <remarks>
    /// This should be in sync with the mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
    /// </remarks>
    private static readonly FrozenDictionary<string, MappedType> WinRTToABITypeMapping = FrozenDictionary.Create<string, MappedType>(comparer: null,
        new("System.IServiceProvider", new("Microsoft.UI.Xaml", "IXamlServiceProvider")),
        new("System.DateTimeOffset", new("Windows.Foundation", "DateTime", "struct(Windows.Foundation.DateTime;i8)")),
        new("System.EventHandler`1", new("Windows.Foundation", "EventHandler`1")),
        new("System.Exception", new("Windows.Foundation", "HResult", "struct(Windows.Foundation.HResult;i4)")),
        new("System.IDisposable", new("Windows.Foundation", "IClosable")),
        new("System.Nullable`1", new("Windows.Foundation", "IReference`1")),
        new("System.TimeSpan", new("Windows.Foundation", "TimeSpan", "struct(Windows.Foundation.TimeSpan;i8)")),
        new("System.EventHandler`2", new("Windows.Foundation", "TypedEventHandler`2")),
        new("System.Uri", new("Windows.Foundation", "Uri", "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})")),
        new("System.AttributeTargets", new("Windows.Foundation.Metadata", "AttributeTargets")),
        new("System.AttributeUsageAttribute", new("Windows.Foundation.Metadata", "AttributeUsageAttribute")),
        new("System.Type", new("Windows.UI.Xaml.Interop", "TypeName", "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))")),
        new("System.ComponentModel.DataErrorsChangedEventArgs", new("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})")),
        new("System.ComponentModel.INotifyDataErrorInfo", new("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo")),
        new("System.ComponentModel.INotifyPropertyChanged", new("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged")),
        new("System.ComponentModel.PropertyChangedEventArgs", new("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Microsoft.UI.Xaml.Data.PropertyChangedEventArgs;{63d0c952-396b-54f4-af8c-ba8724a427bf})")),
        new("System.ComponentModel.PropertyChangedEventHandler", new("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler")),
        new("System.Windows.Input.ICommand", new("Microsoft.UI.Xaml.Input", "ICommand")),
        new("System.Collections.IEnumerable", new("Microsoft.UI.Xaml.Interop", "IBindableIterable")),
        new("System.Collections.IList", new("Microsoft.UI.Xaml.Interop", "IBindableVector")),
        new("System.Collections.Specialized.INotifyCollectionChanged", new("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged")),
        new("System.Collections.Specialized.NotifyCollectionChangedAction", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)")),
        new("System.Collections.Specialized.NotifyCollectionChangedEventArgs", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{da049ff2-d2e0-5fe8-8c7b-f87f26060b6f})")),
        new("System.Collections.Specialized.NotifyCollectionChangedEventHandler", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.CornerRadius", new("Microsoft.UI.Xaml", "CornerRadius")),
        new("Microsoft.UI.Xaml.Duration", new("Microsoft.UI.Xaml", "Duration")),
        new("Microsoft.UI.Xaml.GridLength", new("Microsoft.UI.Xaml", "GridLength")),
        new("Microsoft.UI.Xaml.Media.Animation.KeyTime", new("Microsoft.UI.Xaml.Media.Animation", "KeyTime")),
        new("Microsoft.UI.Xaml.Media.Animation.RepeatBehavior", new("Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior")),
        new("Microsoft.UI.Xaml.Media.Media3D.Matrix3D", new("Microsoft.UI.Xaml.Media.Media3D", "Matrix3D")),
        new("WindowsRuntime.InteropServices.EventRegistrationToken", new("Windows.Foundation", "EventRegistrationToken", "struct(Windows.Foundation.EventRegistrationToken;i8)")),
        new("Windows.Foundation.IReferenceArray", new("Windows.Foundation", "IReferenceArray`1")),
        new("System.Collections.Generic.IEnumerable`1", new("Windows.Foundation.Collections", "IIterable`1")),
        new("System.Collections.Generic.IEnumerator`1", new("Windows.Foundation.Collections", "IIterator`1")),
        new("System.Collections.Generic.KeyValuePair`2", new("Windows.Foundation.Collections", "IKeyValuePair`2")),
        new("System.Collections.Generic.IReadOnlyDictionary`2", new("Windows.Foundation.Collections", "IMapView`2")),
        new("System.Collections.Generic.IDictionary`2", new("Windows.Foundation.Collections", "IMap`2")),
        new("System.Collections.Generic.IReadOnlyList`1", new("Windows.Foundation.Collections", "IVectorView`1")),
        new("System.Collections.Generic.IList`1", new("Windows.Foundation.Collections", "IVector`1")),
        new("System.Numerics.Matrix3x2", new("Windows.Foundation.Numerics", "Matrix3x2", "struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4)")),
        new("System.Numerics.Matrix4x4", new("Windows.Foundation.Numerics", "Matrix4x4", "struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4)")),
        new("System.Numerics.Plane", new("Windows.Foundation.Numerics", "Plane", "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)")),
        new("System.Numerics.Quaternion", new("Windows.Foundation.Numerics", "Quaternion", "struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4)")),
        new("System.Numerics.Vector2", new("Windows.Foundation.Numerics", "Vector2", "struct(Windows.Foundation.Numerics.Vector2;f4;f4)")),
        new("System.Numerics.Vector3", new("Windows.Foundation.Numerics", "Vector3", "struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4)")),
        new("System.Numerics.Vector4", new("Windows.Foundation.Numerics", "Vector4", "struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4)")),
        new("Windows.Foundation.Point", new("Windows.Foundation", "Point", "struct(Windows.Foundation.Point;f4;f4)")),
        new("Windows.Foundation.Size", new("Windows.Foundation", "Size", "struct(Windows.Foundation.Size;f4;f4)")),
        new("Windows.Foundation.Rect", new("Windows.Foundation", "Rect", "struct(Windows.Foundation.Rect;f4;f4;f4;f4)")));

    /// <summary>
    /// Immutable map from projected Microsoft.UI.Xaml type full names (keys) to their corresponding
    /// Windows.UI.Xaml mapping descriptor (values).
    /// </summary>
    /// <remarks>
    /// If the type has a signature in WinRTToABITypeMapping, it should also have one here.
    /// </remarks>
    private static readonly FrozenDictionary<string, MappedType> WindowsUIXamlProjectionTypeMapping = FrozenDictionary.Create<string, MappedType>(comparer: null,
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)")),
        new("Microsoft.UI.Xaml.Interop.INotifyCollectionChanged", new("Windows.UI.Xaml.Interop", "INotifyCollectionChanged")),
        new("Microsoft.UI.Xaml.Interop.IBindableIterable", new("Windows.UI.Xaml.Interop", "IBindableIterable")),
        new("Microsoft.UI.Xaml.Interop.IBindableVector", new("Windows.UI.Xaml.Interop", "IBindableVector")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.Input.ICommand", new("Windows.UI.Xaml.Input", "ICommand")),
        new("Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", new("Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs", "rc(Windows.UI.Xaml.Data.DataErrorsChangedEventArgs;{d026dd64-5f26-5f15-a86a-0dec8a431796})")),
        new("Microsoft.UI.Xaml.Data.INotifyDataErrorInfo", new("Windows.UI.Xaml.Data", "INotifyDataErrorInfo")),
        new("Microsoft.UI.Xaml.Data.INotifyPropertyChanged", new("Windows.UI.Xaml.Data", "INotifyPropertyChanged")),
        new("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", new("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "rc(Windows.UI.Xaml.Data.PropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})")),
        new("Microsoft.UI.Xaml.Data.PropertyChangedEventHandler", new("Windows.UI.Xaml.Data", "PropertyChangedEventHandler")));

    /// <summary>
    /// Returns the mapped WinRT full type name for a given CLR fullName.
    /// </summary>
    /// <param name="fullName">The fullName of the type.</param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <param name="mappedName">Resulting string representing the mapped full name.</param>
    /// <returns><c>true</c> if mapped name was found; otherwise, <c>false</c>.</returns>
    internal static bool TryFindMappedWinRTFullName(string fullName, bool useWindowsUIXamlProjections, [NotNullWhen(true)] out string mappedName)
    {
        if (!WinRTToABITypeMapping.TryGetValue(fullName, out MappedType result))
        {
            mappedName = string.Empty;
            return false;
        }
        string fullname = result.WindowsRuntimeNamespace + "." + result.WindowsRuntimeName;
        // Match the default struct case by checking PublicName for non-null
        mappedName = (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(fullname))
                ? WindowsUIXamlProjectionTypeMapping[fullname].WindowsRuntimeNamespace + "." + WindowsUIXamlProjectionTypeMapping[fullname].WindowsRuntimeName : fullname;
        return true;
    }

    /// <summary>
    /// Retrieves the WinRT signature string for a mapped CLR type.
    /// </summary>
    /// <param name="fullName">The fullName of the type.</param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <param name="signature">Resulting string represent the guid signature if found.</param>
    /// <returns><c>true</c> if hardcoded GUID signature was found; otherwise, <c>false</c>.</returns>
    internal static bool TryFindGuidSignatureForMappedType(string fullName, bool useWindowsUIXamlProjections, [NotNullWhen(true)] out string signature)
    {
        if (!WinRTToABITypeMapping.TryGetValue(fullName, out MappedType result))
        {
            signature = string.Empty;
            return false;
        }

        if (result.Signature is not null)
        {
            signature = result.Signature;
            string resultFullName = result.WindowsRuntimeNamespace + "." + result.WindowsRuntimeName;
            if (useWindowsUIXamlProjections && WindowsUIXamlProjectionTypeMapping.ContainsKey(resultFullName))
            {
                string? projSignature = WindowsUIXamlProjectionTypeMapping[resultFullName].Signature;
                if (projSignature is not null)
                {
                    signature = projSignature;
                    return true;
                }
                throw new InvalidOperationException($"Signature missing for Windows UI Xaml projection mapping for type {resultFullName}");
            }
            return true;
        }
        signature = string.Empty;
        return false;
    }
}
