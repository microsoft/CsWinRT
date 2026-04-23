// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Helpers;

/// <summary>
/// Maps .NET types to their Windows Runtime equivalents for WinMD generation.
/// </summary>
/// <remarks>
/// <para>
/// This class maintains the bidirectional mapping table between .NET types and their Windows Runtime
/// projections. The mapping is configuration-dependent: when <c>CsWinRTUseWindowsUIXamlProjections</c>
/// is enabled, XAML-related types map to <c>Windows.UI.Xaml.*</c> (UWP XAML); otherwise, they map
/// to <c>Microsoft.UI.Xaml.*</c> (WinUI).
/// </para>
/// <para>
/// The mapping table should be kept in sync with the reverse mapping from
/// <c>WinRT.Runtime/Projections.cs</c> and <c>cswinrt/helpers.h</c>.
/// </para>
/// </remarks>
/// <seealso cref="MappedType"/>
internal sealed class TypeMapper
{
    /// <summary>
    /// The dictionary mapping fully-qualified .NET type names to their <see cref="MappedType"/> entries.
    /// </summary>
    private readonly Dictionary<string, MappedType> _typeMapping;

    /// <summary>
    /// Gets the custom mapping for <c>System.Type</c> based on the containing type context.
    /// </summary>
    /// <remarks>
    /// <c>System.Type</c> is context-dependent: when used in an attribute declaration, it stays as
    /// <c>System.Type</c> (since WinMD attribute blobs use CLR types). In all other contexts, it maps
    /// to <c>Windows.UI.Xaml.Interop.TypeName</c>.
    /// </remarks>
    /// <param name="containingType">The type that contains the <c>System.Type</c> usage, or <see langword="null"/>.</param>
    /// <returns>A tuple with the resolved mapping information.</returns>
    private static (string, string, string, bool, bool) GetSystemTypeCustomMapping(TypeDefinition? containingType)
    {
        bool isDefinedInAttribute =
            containingType != null &&
                string.CompareOrdinal(containingType.BaseType?.FullName, "System.Attribute") == 0;
        return isDefinedInAttribute
            ? ("System", "Type", "mscorlib", true, false)
            : ("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", false, true);
    }

    /// <summary>
    /// Creates a new <see cref="TypeMapper"/> instance.
    /// </summary>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    public TypeMapper(bool useWindowsUIXamlProjections)
    {
#pragma warning disable IDE0045 // Keep if-else for readability with large dictionary initializations
        // This should be in sync with the reverse mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
        if (useWindowsUIXamlProjections)
        {
            _typeMapping = new(StringComparer.Ordinal)
            {
                { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true, false) },
                { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true, false) },
                { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract") },
                { "System.EventHandler`2", new MappedType("Windows.Foundation", "TypedEventHandler`2", "Windows.Foundation.FoundationContract") },
                { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib") },
                { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
                { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract") },
                { "System.Object", new MappedType("System", "Object", "mscorlib") },
                { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true, false) },
                { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
                { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Windows.UI.Xaml.Data", "INotifyPropertyChanged", "Windows.UI.Xaml") },
                { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "Windows.UI.Xaml") },
                { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Windows.UI.Xaml.Data", "PropertyChangedEventHandler", "Windows.UI.Xaml") },
                { "System.Windows.Input.ICommand", new MappedType("Windows.UI.Xaml.Input", "ICommand", "Windows.UI.Xaml") },
                { "System.Collections.IEnumerable", new MappedType("Windows.UI.Xaml.Interop", "IBindableIterable", "Windows.UI.Xaml") },
                { "System.Collections.IList", new MappedType("Windows.UI.Xaml.Interop", "IBindableVector", "Windows.UI.Xaml") },
                { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Windows.UI.Xaml.Interop", "INotifyCollectionChanged", "Windows.UI.Xaml") },
                { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Windows.UI.Xaml", true, true) },
                { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Windows.UI.Xaml") },
                { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Windows.UI.Xaml") },
                { "WindowsRuntime.InteropServices.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true) },
                { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true) },
                { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
                { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
                { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
                { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true) },
            };
        }
        else
        {
            _typeMapping = new(StringComparer.Ordinal)
            {
                { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true, false) },
                { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true, false) },
                { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract") },
                { "System.EventHandler`2", new MappedType("Windows.Foundation", "TypedEventHandler`2", "Windows.Foundation.FoundationContract") },
                { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib") },
                { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
                { "System.IServiceProvider", new MappedType("Microsoft.UI.Xaml", "IXamlServiceProvider", "Microsoft.UI.Xaml") },
                { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract") },
                { "System.Object", new MappedType("System", "Object", "mscorlib") },
                { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true, false) },
                { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
                { "System.ComponentModel.DataErrorsChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI.Xaml") },
                { "System.ComponentModel.INotifyDataErrorInfo", new MappedType("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo", "Microsoft.UI.Xaml") },
                { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged", "Microsoft.UI.Xaml") },
                { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "Microsoft.UI.Xaml") },
                { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI.Xaml") },
                { "System.Windows.Input.ICommand", new MappedType("Microsoft.UI.Xaml.Input", "ICommand", "Microsoft.UI.Xaml") },
                { "System.Collections.IEnumerable", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableIterable", "Microsoft.UI.Xaml") },
                { "System.Collections.IList", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableVector", "Microsoft.UI.Xaml") },
                { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged", "Microsoft.UI.Xaml") },
                { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Microsoft.UI.Xaml", true, true) },
                { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Microsoft.UI.Xaml") },
                { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI.Xaml") },
                { "WindowsRuntime.InteropServices.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true) },
                { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true) },
                { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
                { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true) },
                { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
                { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
                { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
                { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true) },
            };
        }
#pragma warning restore IDE0045
    }

    /// <summary>
    /// Checks whether a mapping exists for the given fully-qualified .NET type name.
    /// </summary>
    /// <param name="typeName">The fully-qualified .NET type name (e.g., <c>"System.Collections.Generic.IList`1"</c>).</param>
    /// <returns><see langword="true"/> if a Windows Runtime mapping exists for the type; otherwise, <see langword="false"/>.</returns>
    public bool HasMappingForType(string typeName)
    {
        return _typeMapping.ContainsKey(typeName);
    }

    /// <summary>
    /// Gets the <see cref="MappedType"/> for the given fully-qualified .NET type name.
    /// </summary>
    /// <param name="typeName">The fully-qualified .NET type name.</param>
    /// <returns>The <see cref="MappedType"/> containing the Windows Runtime mapping information.</returns>
    public MappedType GetMappedType(string typeName)
    {
        return _typeMapping[typeName];
    }

    /// <summary>
    /// The set of .NET interfaces that are implemented by C# types but have no Windows Runtime equivalent.
    /// </summary>
    /// <remarks>
    /// Members from these interfaces should be excluded from the WinMD class definition,
    /// similar to custom-mapped interfaces, but no Windows Runtime interface implementation is added.
    /// These include interfaces like <c>ICollection&lt;T&gt;</c> and <c>IEquatable&lt;T&gt;</c>
    /// that have no counterpart in the Windows Runtime type system.
    /// </remarks>
    internal static readonly HashSet<string> ImplementedInterfacesWithoutMapping = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.ICollection`1",
        "System.Collections.Generic.IReadOnlyCollection`1",
        "System.Collections.ICollection",
        "System.Collections.IEnumerator",
        "System.IEquatable`1",
        "System.Runtime.InteropServices.ICustomQueryInterface",
        "System.Runtime.InteropServices.IDynamicInterfaceCastable",
    };
}