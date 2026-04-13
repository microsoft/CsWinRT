// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Models;

/// <summary>
/// A mapped type from a .NET type to a WinRT type.
/// </summary>
internal readonly struct MappedType
{
    private readonly string? _namespace;
    private readonly string? _name;
    private readonly string? _assembly;
    private readonly bool _isSystemType;
    private readonly bool _isValueType;
    private readonly bool _isBlittable;
    private readonly Func<TypeDefinition?, (string, string, string, bool, bool)>? _multipleMappingFunc;

    /// <summary>
    /// Creates a new <see cref="MappedType"/> with a fixed mapping.
    /// </summary>
    public MappedType(string @namespace, string name, string assembly, bool isValueType = false, bool isBlittable = false)
    {
        _namespace = @namespace;
        _name = name;
        _assembly = assembly;
        _isSystemType = string.CompareOrdinal(assembly, "mscorlib") == 0;
        _isValueType = isValueType;
        _isBlittable = isBlittable;
        _multipleMappingFunc = null;
    }

    /// <summary>
    /// Creates a new <see cref="MappedType"/> with a context-dependent mapping.
    /// </summary>
    public MappedType(Func<TypeDefinition?, (string, string, string, bool, bool)> multipleMappingFunc)
    {
        _namespace = null;
        _name = null;
        _assembly = null;
        _isSystemType = false;
        _isValueType = false;
        _isBlittable = false;
        _multipleMappingFunc = multipleMappingFunc;
    }

    /// <summary>
    /// Gets the mapping tuple (namespace, name, assembly, isSystemType, isValueType).
    /// </summary>
    /// <param name="containingType">The optional containing type for context-dependent mappings.</param>
    public (string Namespace, string Name, string Assembly, bool IsSystemType, bool IsValueType) GetMapping(TypeDefinition? containingType = null)
    {
        return _multipleMappingFunc != null
            ? _multipleMappingFunc(containingType)
            : (_namespace!, _name!, _assembly!, _isSystemType, _isValueType);
    }

    /// <summary>
    /// Gets whether the mapped type is blittable.
    /// </summary>
    public bool IsBlittable()
    {
        return _isValueType && _isBlittable;
    }
}

/// <summary>
/// Maps .NET types to their WinRT equivalents.
/// </summary>
internal sealed class TypeMapper
{
    private readonly Dictionary<string, MappedType> _typeMapping;

    // Based on whether System.Type is used in an attribute declaration or elsewhere,
    // we need to choose the correct custom mapping as attributes don't use the TypeName mapping.
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
    /// Checks whether a mapping exists for the given fully-qualified type name.
    /// </summary>
    public bool HasMappingForType(string typeName)
    {
        return _typeMapping.ContainsKey(typeName);
    }

    /// <summary>
    /// Gets the mapped type for the given fully-qualified type name.
    /// </summary>
    public MappedType GetMappedType(string typeName)
    {
        return _typeMapping[typeName];
    }

    /// <summary>
    /// The list of interfaces that are implemented by C# types but should not be mapped to WinRT interfaces.
    /// </summary>
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