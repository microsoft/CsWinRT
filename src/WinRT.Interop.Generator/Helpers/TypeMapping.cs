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
    /// <param name="DefaultInterfaceIid">The default interface IID for a mapped runtime class, if available.</param>
    private readonly record struct MappedType(
         string WindowsRuntimeNamespace,
         string WindowsRuntimeName,
         string? Signature = null,
         Guid? DefaultInterfaceIid = null)
    {
        /// <summary>
        /// Creates a mapped runtime class with a signature derived from its default interface IID.
        /// </summary>
        /// <param name="ns">The Windows Runtime namespace.</param>
        /// <param name="name">The Windows Runtime class name.</param>
        /// <param name="defaultInterfaceIid">The default interface IID.</param>
        public MappedType(string ns, string name, Guid defaultInterfaceIid)
            : this(ns, name, $"rc({ns}.{name};{{{defaultInterfaceIid}}})", defaultInterfaceIid)
        {
        }
    }

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
        new("System.Uri", new("Windows.Foundation", "Uri", new Guid("9E365E57-48B2-4160-956F-C7385120BBFC"))),
        new("System.AttributeTargets", new("Windows.Foundation.Metadata", "AttributeTargets")),
        new("System.AttributeUsageAttribute", new("Windows.Foundation.Metadata", "AttributeUsageAttribute")),
        new("System.Type", new("Windows.UI.Xaml.Interop", "TypeName", "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))")),
        new("System.ComponentModel.DataErrorsChangedEventArgs", new("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", new Guid("D026DD64-5F26-5F15-A86A-0DEC8A431796"))),
        new("System.ComponentModel.INotifyDataErrorInfo", new("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo")),
        new("System.ComponentModel.INotifyPropertyChanged", new("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged")),
        new("System.ComponentModel.PropertyChangedEventArgs", new("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", new Guid("63D0C952-396B-54F4-AF8C-BA8724A427BF"))),
        new("System.ComponentModel.PropertyChangedEventHandler", new("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler")),
        new("System.Windows.Input.ICommand", new("Microsoft.UI.Xaml.Input", "ICommand")),
        new("System.Collections.IEnumerable", new("Microsoft.UI.Xaml.Interop", "IBindableIterable")),
        new("System.Collections.IEnumerator", new("Microsoft.UI.Xaml.Interop", "IBindableIterator")),
        new("System.Collections.IList", new("Microsoft.UI.Xaml.Interop", "IBindableVector")),
        new("System.Collections.Specialized.INotifyCollectionChanged", new("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged")),
        new("System.Collections.Specialized.NotifyCollectionChangedAction", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)")),
        new("System.Collections.Specialized.NotifyCollectionChangedEventArgs", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", new Guid("DA049FF2-D2E0-5FE8-8C7B-F87F26060B6F"))),
        new("System.Collections.Specialized.NotifyCollectionChangedEventHandler", new("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.CornerRadius", new("Microsoft.UI.Xaml", "CornerRadius")),
        new("Microsoft.UI.Xaml.Duration", new("Microsoft.UI.Xaml", "Duration")),
        new("Microsoft.UI.Xaml.GridLength", new("Microsoft.UI.Xaml", "GridLength")),
        new("Microsoft.UI.Xaml.Media.Animation.KeyTime", new("Microsoft.UI.Xaml.Media.Animation", "KeyTime")),
        new("Microsoft.UI.Xaml.Media.Animation.RepeatBehavior", new("Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior")),
        new("Microsoft.UI.Xaml.Media.Media3D.Matrix3D", new("Microsoft.UI.Xaml.Media.Media3D", "Matrix3D")),
        new("WindowsRuntime.InteropServices.EventRegistrationToken", new("Windows.Foundation", "EventRegistrationToken", "struct(Windows.Foundation.EventRegistrationToken;i8)")),
        new("WindowsRuntime.InteropServices.IActivationFactory", new("Windows.Foundation", "IActivationFactory")),
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
    /// Mapping of built-in <c>System.*</c> to their corresponding Windows Runtime fundamental types.
    /// </summary>
    private static readonly FrozenDictionary<string, string> FundamentalTypeMapping = FrozenDictionary.Create<string, string>(comparer: null,
        new("System.Boolean", "Boolean"),
        new("System.Byte", "UInt8"),
        new("System.Char", "Char"),
        new("System.Double", "Double"),
        new("System.Guid", "Guid"),
        new("System.Int16", "Int16"),
        new("System.Int32", "Int32"),
        new("System.Int64", "Int64"),
        new("System.Object", "Object"),
        new("System.Single", "Single"),
        new("System.String", "String"),
        new("System.UInt16", "UInt16"),
        new("System.UInt32", "UInt32"));

    /// <summary>
    /// Mapping of projected <c>Microsoft.UI.Xaml</c> types to their corresponding <c>Windows.UI.Xaml</c> types.
    /// </summary>
    /// <remarks>
    /// If an entry has a signature or default interface IID in <see cref="ProjectionTypeMapping"/>, it should also have one here.
    /// </remarks>
    private static readonly FrozenDictionary<string, MappedType> WindowsUIXamlTypeMapping = FrozenDictionary.Create<string, MappedType>(comparer: null,
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", new Guid("4CF68D33-E3F2-4964-B85E-945B4F7E2F21"))),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)")),
        new("Microsoft.UI.Xaml.Interop.INotifyCollectionChanged", new("Windows.UI.Xaml.Interop", "INotifyCollectionChanged")),
        new("Microsoft.UI.Xaml.Interop.IBindableIterable", new("Windows.UI.Xaml.Interop", "IBindableIterable")),
        new("Microsoft.UI.Xaml.Interop.IBindableIterator", new("Windows.UI.Xaml.Interop", "IBindableIterator")),
        new("Microsoft.UI.Xaml.Interop.IBindableVector", new("Windows.UI.Xaml.Interop", "IBindableVector")),
        new("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", new("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")),
        new("Microsoft.UI.Xaml.Input.ICommand", new("Windows.UI.Xaml.Input", "ICommand")),
        new("Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", new("Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs", new Guid("D026DD64-5F26-5F15-A86A-0DEC8A431796"))),
        new("Microsoft.UI.Xaml.Data.INotifyDataErrorInfo", new("Windows.UI.Xaml.Data", "INotifyDataErrorInfo")),
        new("Microsoft.UI.Xaml.Data.INotifyPropertyChanged", new("Windows.UI.Xaml.Data", "INotifyPropertyChanged")),
        new("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", new("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", new Guid("4F33A9A0-5CF4-47A4-B16F-D7FAAF17457E"))),
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
        if (FundamentalTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(fullName, out mappedName))
        {
            return true;
        }

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
    /// <param name="signature">The resulting mapped type signature, if found.</param>
    /// <returns>Whether <paramref name="signature"/> was retrieved successfully.</returns>
    public static bool TryFindMappedTypeSignature(
        ReadOnlySpan<char> fullName,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        if (!TryFindMappedType(fullName, useWindowsUIXamlProjections, out MappedType result))
        {
            signature = null;

            return false;
        }

        // A mapping without a registered signature must fall back to ordinary signature generation
        signature = result.Signature;

        return signature is not null;
    }

    /// <summary>
    /// Gets the default interface IID of a custom-mapped runtime class.
    /// </summary>
    /// <param name="fullName">The managed type's full name.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="iid">The default interface IID, if found.</param>
    /// <returns>Whether the mapping represents a runtime class with an IID.</returns>
    public static bool TryFindMappedTypeIID(ReadOnlySpan<char> fullName, bool useWindowsUIXamlProjections, out Guid iid)
    {
        // Only mapped runtime classes carry a stored default-interface IID here (value signatures have no IID,
        // while interface, delegate, and constructed-type IIDs are resolved by 'GuidGenerator' instead).
        if (TryFindMappedType(fullName, useWindowsUIXamlProjections, out MappedType result) &&
            result.DefaultInterfaceIid is Guid defaultInterfaceIid)
        {
            iid = defaultInterfaceIid;

            return true;
        }

        iid = default;

        return false;
    }

    /// <summary>
    /// Looks up custom-mapped metadata with the selected XAML projection mode.
    /// </summary>
    /// <param name="fullName">The managed type's full name.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="result">The selected type mapping.</param>
    /// <returns>Whether a mapping was found.</returns>
    private static bool TryFindMappedType(ReadOnlySpan<char> fullName, bool useWindowsUIXamlProjections, out MappedType result)
    {
        if (!ProjectionTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(fullName, out result))
        {
            return false;
        }

        if (!useWindowsUIXamlProjections)
        {
            return true;
        }

        DefaultInterpolatedStringHandler handler = $"{result.WindowsRuntimeNamespace}.{result.WindowsRuntimeName}";

        // If we're using Windows UI XAML projections and the type needs special mapping, get its System XAML metadata
        if (WindowsUIXamlTypeMapping.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(handler.Text, out MappedType mappedType))
        {
            // The override must preserve any signature and default-interface IID from the base mapping.
            // Missing either would indicate a malformed mapping table, not an unmapped type.
            result = mappedType;
        }

        handler.Clear();

        return true;
    }
}
