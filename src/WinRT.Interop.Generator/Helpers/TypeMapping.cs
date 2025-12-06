// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// Contains mappings from managed types to their Windows Runtime counterparts, for all custom-mapped types.
/// </summary>
internal static class TypeMapping
{
    /// <summary>
    /// Represents a mapping from a managed type to its Windows Runtime counterpart, optionally including
    /// the hardcoded Windows Runtime type signature used for IID generation and marshalling.
    /// </summary>
    /// <param name="WindowsRuntimeNamespace">The Windows Runtime namespace.</param>
    /// <param name="WindowsRuntimeName">The Windows Runtime type name.</param>
    /// <param name="Signature">The hardcoded Windows Runtime type signature, if available.</param>
    private readonly record struct MappedType(
         string WindowsRuntimeNamespace,
         string WindowsRuntimeName,
         string? Signature = null);

    /// <summary>
    /// Mapping of custom-mapped or otherwise special managed types to their corresponding Windows Runtime types.
    /// </summary>
    /// <remarks>
    /// This should be in sync with the mapping from <c>cswinrt/helpers.h</c>.
    /// </remarks>
    private static readonly FrozenDictionary<string, MappedType> ProjectionTypeMapping = FrozenDictionary.Create<string, MappedType>(comparer: null,
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
    /// Mapping of projected <c>Microsoft.UI.Xaml</c> types to their corresponding <c>Windows.UI.Xaml</c> types.
    /// </summary>
    /// <remarks>
    /// If an entry has a signature in <see cref="ProjectionTypeMapping"/>, it should also have one here.
    /// </remarks>
    private static readonly FrozenDictionary<string, MappedType> WindowsUIXamlTypeMapping = FrozenDictionary.Create<string, MappedType>(comparer: null,
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
    /// Tries to get the mapped type name for a Windows Runtime type, from the full type of its corresponding managed type.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="mappedName">The resulting mapped type name, if found.</param>
    /// <returns>Whether <paramref name="mappedName"/> was retrieved successfully.</returns>
    public static bool TryFindMappedTypeName(
        ReadOnlySpan<char> fullName,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? mappedName)
    {
        if (!ProjectionTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(fullName, out MappedType result))
        {
            mappedName = null;

            return false;
        }

        DefaultInterpolatedStringHandler handler = $"{result.WindowsRuntimeNamespace}.{result.WindowsRuntimeName}";

        // If we're using Windows UI XAML projections and the type needs special mapping, get its System XAML name
        if (useWindowsUIXamlProjections && WindowsUIXamlTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(handler.Text, out MappedType mappedType))
        {
            handler.Clear();

            mappedName = $"{mappedType.WindowsRuntimeNamespace}.{mappedType.WindowsRuntimeName}";

            return true;
        }

        mappedName = handler.ToStringAndClear();

        return true;
    }

    /// <summary>
    /// Tries to get the mapped type signature for a Windows Runtime type, from the full type of its corresponding managed type.
    /// </summary>
    /// <param name="fullName">The full name of the type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="signature">The resulting mapped type signatre, if found.</param>
    /// <returns>Whether <paramref name="signature"/> was retrieved successfully.</returns>
    public static bool TryFindMappedTypeSignature(
        ReadOnlySpan<char> fullName,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        if (!ProjectionTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(fullName, out MappedType result))
        {
            signature = null;

            return false;
        }

        // If the result doesn't have a signature in the lookup table, there's nothing else to do
        if (result.Signature is null)
        {
            signature = null;

            return false;
        }

        DefaultInterpolatedStringHandler handler = $"{result.WindowsRuntimeNamespace}.{result.WindowsRuntimeName}";

        // If we're using Windows UI XAML projections and the type needs special mapping, get its System XAML signature
        if (useWindowsUIXamlProjections && WindowsUIXamlTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(handler.Text, out MappedType mappedType))
        {
            handler.Clear();

            // All entries here will always have a signature, if their corresponding entries
            // in the global table of type mappings also have signatures. If that's not the
            // case, then it means an entry in the second lokup is just malformed
            signature = mappedType.Signature!;

            return true;
        }

        handler.Clear();

        signature = result.Signature;

        return true;
    }
}
