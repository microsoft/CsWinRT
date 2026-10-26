// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsRuntime.Generator.Helpers;

/// <summary>
/// Shared .NET to Windows Runtime mapping data for metadata generation and source analysis.
/// This file is also compiled directly into the Roslyn component to avoid a build-tool dependency.
/// </summary>
internal static class WindowsRuntimeTypeMappings
{
    /// <summary>
    /// A fixed Windows Runtime mapping. The metadata writer handles context-dependent attribute signatures separately.
    /// </summary>
    /// <param name="Namespace">The Windows Runtime namespace.</param>
    /// <param name="Name">The Windows Runtime type name.</param>
    /// <param name="Assembly">The Windows Runtime contract assembly.</param>
    /// <param name="IsValueType">Whether the mapped type is a value type.</param>
    /// <param name="IsBlittable">Whether the mapped value type is blittable.</param>
    public readonly record struct Mapping(
        string Namespace,
        string Name,
        string Assembly,
        bool IsValueType = false,
        bool IsBlittable = false);

    /// <summary>
    /// The mappings for WinUI XAML.
    /// </summary>
    private static readonly FrozenDictionary<string, Mapping> WinUI = Create(useWindowsUIXamlProjections: false);

    /// <summary>
    /// The mappings for UWP XAML.
    /// </summary>
    private static readonly FrozenDictionary<string, Mapping> Uwp = Create(useWindowsUIXamlProjections: true);

    /// <summary>
    /// Gets the .NET interfaces whose implementations are not exported to Windows Runtime metadata.
    /// </summary>
    public static FrozenSet<string> ImplementedInterfacesWithoutMapping { get; } = FrozenSet.Create<string>(equalityComparer: null,
        "System.Collections.Generic.ICollection`1",
        "System.Collections.Generic.IReadOnlyCollection`1",
        "System.Collections.ICollection",
        "System.Collections.IEnumerator",
        "System.ComponentModel.INotifyPropertyChanging",
        "System.IEquatable`1",
        "System.Runtime.InteropServices.ICustomQueryInterface",
        "System.Runtime.InteropServices.IDynamicInterfaceCastable");

    /// <summary>
    /// Gets the mappings for the selected XAML projection mode.
    /// </summary>
    /// <param name="useWindowsUIXamlProjections">Whether UWP XAML is selected instead of WinUI.</param>
    /// <returns>The managed metadata names and their Windows Runtime mappings.</returns>
    public static FrozenDictionary<string, Mapping> GetMappings(bool useWindowsUIXamlProjections)
    {
        return useWindowsUIXamlProjections ? Uwp : WinUI;
    }

    /// <summary>
    /// Creates the mapping table shared by the WinMD generator and authoring analyzers.
    /// </summary>
    /// <param name="useWindowsUIXamlProjections">Whether UWP XAML is selected instead of WinUI.</param>
    /// <returns>The selected mapping table.</returns>
    private static FrozenDictionary<string, Mapping> Create(bool useWindowsUIXamlProjections)
    {
        string xaml = useWindowsUIXamlProjections ? "Windows.UI.Xaml" : "Microsoft.UI.Xaml";

        Dictionary<string, Mapping> mappings = new()
        {
            ["System.DateTimeOffset"] = new("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true),
            ["System.Exception"] = new("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true),
            ["System.EventHandler`1"] = new("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract"),
            ["System.EventHandler`2"] = new("Windows.Foundation", "TypedEventHandler`2", "Windows.Foundation.FoundationContract"),
            ["System.FlagsAttribute"] = new("System", "FlagsAttribute", "mscorlib"),
            ["System.IDisposable"] = new("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract"),
            ["System.Nullable`1"] = new("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract"),
            ["System.Object"] = new("System", "Object", "mscorlib"),
            ["System.TimeSpan"] = new("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true),
            ["System.Uri"] = new("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract"),
            ["System.ComponentModel.INotifyPropertyChanged"] = new(xaml + ".Data", "INotifyPropertyChanged", xaml),
            ["System.ComponentModel.PropertyChangedEventArgs"] = new(xaml + ".Data", "PropertyChangedEventArgs", xaml),
            ["System.ComponentModel.PropertyChangedEventHandler"] = new(xaml + ".Data", "PropertyChangedEventHandler", xaml),
            ["System.Windows.Input.ICommand"] = new(xaml + ".Input", "ICommand", xaml),
            ["System.Collections.IEnumerable"] = new(xaml + ".Interop", "IBindableIterable", xaml),
            ["System.Collections.IList"] = new(xaml + ".Interop", "IBindableVector", xaml),
            ["System.Collections.Specialized.INotifyCollectionChanged"] = new(xaml + ".Interop", "INotifyCollectionChanged", xaml),
            ["System.Collections.Specialized.NotifyCollectionChangedAction"] = new(xaml + ".Interop", "NotifyCollectionChangedAction", xaml, true, true),
            ["System.Collections.Specialized.NotifyCollectionChangedEventArgs"] = new(xaml + ".Interop", "NotifyCollectionChangedEventArgs", xaml),
            ["System.Collections.Specialized.NotifyCollectionChangedEventHandler"] = new(xaml + ".Interop", "NotifyCollectionChangedEventHandler", xaml),
            ["WindowsRuntime.InteropServices.EventRegistrationToken"] =
                new("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true),
            ["System.AttributeTargets"] = new("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true),
            ["System.AttributeUsageAttribute"] = new("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract"),
            ["System.Numerics.Matrix3x2"] = new("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Matrix4x4"] = new("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Plane"] = new("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Quaternion"] = new("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Vector2"] = new("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Vector3"] = new("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true),
            ["System.Numerics.Vector4"] = new("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true),
            ["System.Type"] = new("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", true, true),
            ["System.Collections.Generic.IEnumerable`1"] = new("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.IEnumerator`1"] = new("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.KeyValuePair`2"] = new("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.IReadOnlyDictionary`2"] = new("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.IDictionary`2"] = new("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.IReadOnlyList`1"] = new("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract"),
            ["System.Collections.Generic.IList`1"] = new("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract"),
            ["Windows.UI.Color"] = new("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true)
        };

        if (!useWindowsUIXamlProjections)
        {
            mappings.Add("System.IServiceProvider", new(xaml, "IXamlServiceProvider", xaml));
            mappings.Add("System.ComponentModel.DataErrorsChangedEventArgs", new(xaml + ".Data", "DataErrorsChangedEventArgs", xaml));
            mappings.Add("System.ComponentModel.INotifyDataErrorInfo", new(xaml + ".Data", "INotifyDataErrorInfo", xaml));
        }

        return mappings.ToFrozenDictionary();
    }
}