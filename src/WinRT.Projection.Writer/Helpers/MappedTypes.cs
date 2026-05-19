// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;
using static WindowsRuntime.ProjectionWriter.References.WellKnownTypeNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Maps a Windows Runtime type to the corresponding .NET type.
/// </summary>
/// <param name="AbiName">The Windows Runtime ABI type name.</param>
/// <param name="MappedNamespace">The .NET namespace the WinRT type maps to.</param>
/// <param name="MappedName">The .NET type name the WinRT type maps to.</param>
/// <param name="RequiresMarshaling">Whether values of the mapped type require marshalling at the projection boundary.</param>
/// <param name="HasCustomMembersOutput">Whether the writer should emit custom member projections for the mapped type.</param>
/// <param name="EmitAbi">Whether the writer should emit an ABI projection for the mapped type.</param>
internal readonly record struct MappedType(
    string AbiName,
    string MappedNamespace,
    string MappedName,
    bool RequiresMarshaling = false,
    bool HasCustomMembersOutput = false,
    bool EmitAbi = false);

/// <summary>
/// Static lookup table for Windows Runtime → .NET type mappings.
/// </summary>
internal static class MappedTypes
{
    /// <summary>
    /// The mapping of all custom-mapped types.
    /// </summary>
    private static readonly FrozenDictionary<string, FrozenDictionary<string, MappedType>> TypeMappings = new Dictionary<string, FrozenDictionary<string, MappedType>>
    {
        ["Microsoft.UI.Xaml"] = new Dictionary<string, MappedType>
        {
            ["CornerRadius"] = new("CornerRadius", "Microsoft.UI.Xaml", "CornerRadius", false, false, true),
            ["CornerRadiusHelper"] = new("CornerRadiusHelper", "", ""),
            ["Duration"] = new("Duration", "Microsoft.UI.Xaml", "Duration", false, false, true),
            ["DurationHelper"] = new("DurationHelper", "", ""),
            ["GridLength"] = new("GridLength", "Microsoft.UI.Xaml", "GridLength", false, false, true),
            ["GridLengthHelper"] = new("GridLengthHelper", "", ""),
            ["ICornerRadiusHelper"] = new("ICornerRadiusHelper", "", ""),
            ["ICornerRadiusHelperStatics"] = new("ICornerRadiusHelperStatics", "", ""),
            ["IDurationHelper"] = new("IDurationHelper", "", ""),
            ["IDurationHelperStatics"] = new("IDurationHelperStatics", "", ""),
            ["IGridLengthHelper"] = new("IGridLengthHelper", "", ""),
            ["IGridLengthHelperStatics"] = new("IGridLengthHelperStatics", "", ""),
            ["IThicknessHelper"] = new("IThicknessHelper", "", ""),
            ["IThicknessHelperStatics"] = new("IThicknessHelperStatics", "", ""),
            ["IXamlServiceProvider"] = new("IXamlServiceProvider", "System", "IServiceProvider"),
            ["ThicknessHelper"] = new("ThicknessHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Controls.Primitives"] = new Dictionary<string, MappedType>
        {
            ["GeneratorPositionHelper"] = new("GeneratorPositionHelper", "", ""),
            ["IGeneratorPositionHelper"] = new("IGeneratorPositionHelper", "", ""),
            ["IGeneratorPositionHelperStatics"] = new("IGeneratorPositionHelperStatics", "", ""),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Data"] = new Dictionary<string, MappedType>
        {
            ["DataErrorsChangedEventArgs"] = new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs"),
            ["INotifyDataErrorInfo"] = new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true),
            ["INotifyPropertyChanged"] = new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged"),
            ["PropertyChangedEventArgs"] = new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs"),
            ["PropertyChangedEventHandler"] = new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler"),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Input"] = new Dictionary<string, MappedType>
        {
            ["ICommand"] = new("ICommand", "System.Windows.Input", "ICommand", true),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Interop"] = new Dictionary<string, MappedType>
        {
            ["IBindableIterable"] = new("IBindableIterable", "System.Collections", "IEnumerable", true, true),
            ["IBindableIterator"] = new("IBindableIterator", "System.Collections", "IEnumerator", true, true),
            ["IBindableVector"] = new("IBindableVector", "System.Collections", "IList", true, true),
            ["INotifyCollectionChanged"] = new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true),
            ["NotifyCollectionChangedAction"] = new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction"),
            ["NotifyCollectionChangedEventArgs"] = new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true),
            ["NotifyCollectionChangedEventHandler"] = new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Media"] = new Dictionary<string, MappedType>
        {
            ["IMatrixHelper"] = new("IMatrixHelper", "", ""),
            ["IMatrixHelperStatics"] = new("IMatrixHelperStatics", "", ""),
            ["MatrixHelper"] = new("MatrixHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Media.Animation"] = new Dictionary<string, MappedType>
        {
            ["IKeyTimeHelper"] = new("IKeyTimeHelper", "", ""),
            ["IKeyTimeHelperStatics"] = new("IKeyTimeHelperStatics", "", ""),
            ["IRepeatBehaviorHelper"] = new("IRepeatBehaviorHelper", "", ""),
            ["IRepeatBehaviorHelperStatics"] = new("IRepeatBehaviorHelperStatics", "", ""),
            ["KeyTime"] = new("KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime", false, false, true),
            ["KeyTimeHelper"] = new("KeyTimeHelper", "", ""),
            ["RepeatBehavior"] = new("RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true),
            ["RepeatBehaviorHelper"] = new("RepeatBehaviorHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Microsoft.UI.Xaml.Media.Media3D"] = new Dictionary<string, MappedType>
        {
            ["IMatrix3DHelper"] = new("IMatrix3DHelper", "", ""),
            ["IMatrix3DHelperStatics"] = new("IMatrix3DHelperStatics", "", ""),
            ["Matrix3D"] = new("Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true),
            ["Matrix3DHelper"] = new("Matrix3DHelper", "", ""),
        }.ToFrozenDictionary(),
        [WindowsFoundation] = new Dictionary<string, MappedType>
        {
            ["AsyncActionCompletedHandler"] = new("AsyncActionCompletedHandler", WindowsFoundation, "AsyncActionCompletedHandler"),
            ["AsyncActionProgressHandler`1"] = new("AsyncActionProgressHandler`1", WindowsFoundation, "AsyncActionProgressHandler`1"),
            ["AsyncActionWithProgressCompletedHandler`1"] = new("AsyncActionWithProgressCompletedHandler`1", WindowsFoundation, "AsyncActionWithProgressCompletedHandler`1"),
            ["AsyncOperationCompletedHandler`1"] = new("AsyncOperationCompletedHandler`1", WindowsFoundation, "AsyncOperationCompletedHandler`1"),
            ["AsyncOperationProgressHandler`2"] = new("AsyncOperationProgressHandler`2", WindowsFoundation, "AsyncOperationProgressHandler`2"),
            ["AsyncOperationWithProgressCompletedHandler`2"] = new("AsyncOperationWithProgressCompletedHandler`2", WindowsFoundation, "AsyncOperationWithProgressCompletedHandler`2"),
            ["AsyncStatus"] = new("AsyncStatus", WindowsFoundation, "AsyncStatus"),
            ["DateTime"] = new("DateTime", "System", "DateTimeOffset", true),
            ["EventHandler`1"] = new("EventHandler`1", "System", "EventHandler`1", false),
            ["EventRegistrationToken"] = new("EventRegistrationToken", "WindowsRuntime.InteropServices", "EventRegistrationToken", false),
            ["FoundationContract"] = new("FoundationContract", WindowsFoundation, "FoundationContract"),
            [HResult] = new(HResult, "System", "Exception", true),
            ["IAsyncAction"] = new("IAsyncAction", WindowsFoundation, "IAsyncAction"),
            ["IAsyncActionWithProgress`1"] = new("IAsyncActionWithProgress`1", WindowsFoundation, "IAsyncActionWithProgress`1"),
            ["IAsyncInfo"] = new("IAsyncInfo", WindowsFoundation, "IAsyncInfo"),
            ["IAsyncOperationWithProgress`2"] = new("IAsyncOperationWithProgress`2", WindowsFoundation, "IAsyncOperationWithProgress`2"),
            ["IAsyncOperation`1"] = new("IAsyncOperation`1", WindowsFoundation, "IAsyncOperation`1"),
            ["IClosable"] = new("IClosable", "System", "IDisposable", true, true),
            ["IMemoryBufferReference"] = new("IMemoryBufferReference", WindowsFoundation, "IMemoryBufferReference"),
            ["IPropertyValue"] = new("IPropertyValue", WindowsFoundation, "IPropertyValue", true),
            ["IReferenceArray`1"] = new("IReferenceArray`1", WindowsFoundation, "IReferenceArray", true),
            [IReferenceGeneric] = new(IReferenceGeneric, "System", NullableGeneric, true),
            ["IStringable"] = new("IStringable", WindowsFoundation, "IStringable"),
            ["Point"] = new("Point", WindowsFoundation, "Point"),
            ["PropertyType"] = new("PropertyType", WindowsFoundation, "PropertyType"),
            ["Rect"] = new("Rect", WindowsFoundation, "Rect"),
            ["Size"] = new("Size", WindowsFoundation, "Size"),
            ["TimeSpan"] = new("TimeSpan", "System", "TimeSpan", true),
            ["TypedEventHandler`2"] = new("TypedEventHandler`2", "System", "EventHandler`2", false),
            ["UniversalApiContract"] = new("UniversalApiContract", WindowsFoundation, "UniversalApiContract"),
            ["Uri"] = new("Uri", "System", "Uri", true),
        }.ToFrozenDictionary(),
        [WindowsFoundationCollections] = new Dictionary<string, MappedType>
        {
            ["CollectionChange"] = new("CollectionChange", WindowsFoundationCollections, "CollectionChange"),
            ["IIterable`1"] = new("IIterable`1", "System.Collections.Generic", "IEnumerable`1", true, true),
            ["IIterator`1"] = new("IIterator`1", "System.Collections.Generic", "IEnumerator`1", true, true),
            ["IKeyValuePair`2"] = new("IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", true),
            ["IMapChangedEventArgs`1"] = new("IMapChangedEventArgs`1", WindowsFoundationCollections, "IMapChangedEventArgs`1"),
            ["IMapView`2"] = new("IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", true, true),
            ["IMap`2"] = new("IMap`2", "System.Collections.Generic", "IDictionary`2", true, true),
            ["IObservableMap`2"] = new("IObservableMap`2", WindowsFoundationCollections, "IObservableMap`2"),
            ["IObservableVector`1"] = new("IObservableVector`1", WindowsFoundationCollections, "IObservableVector`1"),
            ["IVectorChangedEventArgs"] = new("IVectorChangedEventArgs", WindowsFoundationCollections, "IVectorChangedEventArgs"),
            ["IVectorView`1"] = new("IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", true, true),
            ["IVector`1"] = new("IVector`1", "System.Collections.Generic", "IList`1", true, true),
            ["MapChangedEventHandler`2"] = new("MapChangedEventHandler`2", WindowsFoundationCollections, "MapChangedEventHandler`2"),
            ["VectorChangedEventHandler`1"] = new("VectorChangedEventHandler`1", WindowsFoundationCollections, "VectorChangedEventHandler`1"),
        }.ToFrozenDictionary(),
        [WindowsFoundationMetadata] = new Dictionary<string, MappedType>
        {
            ["ApiContractAttribute"] = new("ApiContractAttribute", WindowsFoundationMetadata, "ApiContractAttribute"),
            ["AttributeTargets"] = new("AttributeTargets", "System", "AttributeTargets"),
            ["AttributeUsageAttribute"] = new("AttributeUsageAttribute", "System", "AttributeUsageAttribute"),
            [ContractVersionAttribute] = new(ContractVersionAttribute, WindowsFoundationMetadata, ContractVersionAttribute),
        }.ToFrozenDictionary(),
        ["Windows.Foundation.Numerics"] = new Dictionary<string, MappedType>
        {
            ["Matrix3x2"] = new("Matrix3x2", "System.Numerics", "Matrix3x2"),
            ["Matrix4x4"] = new("Matrix4x4", "System.Numerics", "Matrix4x4"),
            ["Plane"] = new("Plane", "System.Numerics", "Plane"),
            ["Quaternion"] = new("Quaternion", "System.Numerics", "Quaternion"),
            ["Vector2"] = new("Vector2", "System.Numerics", "Vector2"),
            ["Vector3"] = new("Vector3", "System.Numerics", "Vector3"),
            ["Vector4"] = new("Vector4", "System.Numerics", "Vector4"),
        }.ToFrozenDictionary(),
        ["Windows.Storage.Streams"] = new Dictionary<string, MappedType>
        {
            ["IBuffer"] = new("IBuffer", "Windows.Storage.Streams", "IBuffer"),
            ["IInputStream"] = new("IInputStream", "Windows.Storage.Streams", "IInputStream"),
            ["IOutputStream"] = new("IOutputStream", "Windows.Storage.Streams", "IOutputStream"),
            ["IRandomAccessStream"] = new("IRandomAccessStream", "Windows.Storage.Streams", "IRandomAccessStream"),
            ["InputStreamOptions"] = new("InputStreamOptions", "Windows.Storage.Streams", "InputStreamOptions"),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml"] = new Dictionary<string, MappedType>
        {
            ["CornerRadius"] = new("CornerRadius", "Windows.UI.Xaml", "CornerRadius", false, false, true),
            ["CornerRadiusHelper"] = new("CornerRadiusHelper", "", ""),
            ["Duration"] = new("Duration", "Windows.UI.Xaml", "Duration", false, false, true),
            ["DurationHelper"] = new("DurationHelper", "", ""),
            ["GridLength"] = new("GridLength", "Windows.UI.Xaml", "GridLength", false, false, true),
            ["GridLengthHelper"] = new("GridLengthHelper", "", ""),
            ["ICornerRadiusHelper"] = new("ICornerRadiusHelper", "", ""),
            ["ICornerRadiusHelperStatics"] = new("ICornerRadiusHelperStatics", "", ""),
            ["IDurationHelper"] = new("IDurationHelper", "", ""),
            ["IDurationHelperStatics"] = new("IDurationHelperStatics", "", ""),
            ["IGridLengthHelper"] = new("IGridLengthHelper", "", ""),
            ["IGridLengthHelperStatics"] = new("IGridLengthHelperStatics", "", ""),
            ["IThicknessHelper"] = new("IThicknessHelper", "", ""),
            ["IThicknessHelperStatics"] = new("IThicknessHelperStatics", "", ""),
            ["IXamlServiceProvider"] = new("IXamlServiceProvider", "System", "IServiceProvider"),
            ["ThicknessHelper"] = new("ThicknessHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Controls.Primitives"] = new Dictionary<string, MappedType>
        {
            ["GeneratorPositionHelper"] = new("GeneratorPositionHelper", "", ""),
            ["IGeneratorPositionHelper"] = new("IGeneratorPositionHelper", "", ""),
            ["IGeneratorPositionHelperStatics"] = new("IGeneratorPositionHelperStatics", "", ""),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Data"] = new Dictionary<string, MappedType>
        {
            ["DataErrorsChangedEventArgs"] = new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs"),
            ["INotifyDataErrorInfo"] = new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true),
            ["INotifyPropertyChanged"] = new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged"),
            ["PropertyChangedEventArgs"] = new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs"),
            ["PropertyChangedEventHandler"] = new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler"),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Input"] = new Dictionary<string, MappedType>
        {
            ["ICommand"] = new("ICommand", "System.Windows.Input", "ICommand", true),
        }.ToFrozenDictionary(),
        [WindowsUIXamlInterop] = new Dictionary<string, MappedType>
        {
            ["IBindableIterable"] = new("IBindableIterable", "System.Collections", "IEnumerable", true, true),
            ["IBindableIterator"] = new("IBindableIterator", "System.Collections", "IEnumerator", true, true),
            ["IBindableVector"] = new("IBindableVector", "System.Collections", "IList", true, true),
            ["INotifyCollectionChanged"] = new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true),
            ["NotifyCollectionChangedAction"] = new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction"),
            ["NotifyCollectionChangedEventArgs"] = new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true),
            ["NotifyCollectionChangedEventHandler"] = new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true),
            ["TypeKind"] = new("TypeKind", WindowsUIXamlInterop, "TypeKind", true),
            [TypeName] = new(TypeName, "System", "Type", true),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Media"] = new Dictionary<string, MappedType>
        {
            ["IMatrixHelper"] = new("IMatrixHelper", "", ""),
            ["IMatrixHelperStatics"] = new("IMatrixHelperStatics", "", ""),
            ["MatrixHelper"] = new("MatrixHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Media.Animation"] = new Dictionary<string, MappedType>
        {
            ["IKeyTimeHelper"] = new("IKeyTimeHelper", "", ""),
            ["IKeyTimeHelperStatics"] = new("IKeyTimeHelperStatics", "", ""),
            ["IRepeatBehaviorHelper"] = new("IRepeatBehaviorHelper", "", ""),
            ["IRepeatBehaviorHelperStatics"] = new("IRepeatBehaviorHelperStatics", "", ""),
            ["KeyTime"] = new("KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime", false, false, true),
            ["KeyTimeHelper"] = new("KeyTimeHelper", "", ""),
            ["RepeatBehavior"] = new("RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true),
            ["RepeatBehaviorHelper"] = new("RepeatBehaviorHelper", "", ""),
        }.ToFrozenDictionary(),
        ["Windows.UI.Xaml.Media.Media3D"] = new Dictionary<string, MappedType>
        {
            ["IMatrix3DHelper"] = new("IMatrix3DHelper", "", ""),
            ["IMatrix3DHelperStatics"] = new("IMatrix3DHelperStatics", "", ""),
            ["Matrix3D"] = new("Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true),
            ["Matrix3DHelper"] = new("Matrix3DHelper", "", ""),
        }.ToFrozenDictionary(),
        [WindowsRuntimeInternal] = new Dictionary<string, MappedType>
        {
            ["HWND"] = new("HWND", "System", "IntPtr"),
            ["ProjectionInternalAttribute"] = new("ProjectionInternalAttribute", "", ""),
        }.ToFrozenDictionary(),
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns the <see cref="MappedType"/> entry for the type identified by
    /// (<paramref name="typeNamespace"/>, <paramref name="typeName"/>), or <see langword="null"/>
    /// if no mapping exists.
    /// </summary>
    /// <param name="typeNamespace">The Windows Runtime namespace.</param>
    /// <param name="typeName">The Windows Runtime type name.</param>
    /// <returns>The mapping, or <see langword="null"/>.</returns>
    public static MappedType? Get(string typeNamespace, string typeName)
    {
        if (TypeMappings.TryGetValue(typeNamespace, out FrozenDictionary<string, MappedType>? namesp) &&
            namesp.TryGetValue(typeName, out MappedType mapped))
        {
            return mapped;
        }

        return null;
    }

    /// <summary>
    /// Returns whether <paramref name="typeNamespace"/> contains at least one mapped type.
    /// </summary>
    /// <param name="typeNamespace">The Windows Runtime namespace.</param>
    /// <returns><see langword="true"/> if there is at least one mapping in this namespace.</returns>
    public static bool HasNamespace(string typeNamespace) => TypeMappings.ContainsKey(typeNamespace);

    /// <summary>
    /// Returns whether a mapping exists for the type identified by
    /// (<paramref name="typeNamespace"/>, <paramref name="typeName"/>).
    /// </summary>
    /// <param name="typeNamespace">The Windows Runtime namespace.</param>
    /// <param name="typeName">The Windows Runtime type name.</param>
    /// <returns><see langword="true"/> if a mapping exists; otherwise <see langword="false"/>.</returns>
    public static bool IsMapped(string typeNamespace, string typeName)
        => Get(typeNamespace, typeName) is not null;

    /// <summary>
    /// Applies the type mapping in-place if one exists: when a mapping is found for the type
    /// identified by (<paramref name="typeNamespace"/>, <paramref name="typeName"/>), replaces
    /// both fields with the mapped namespace and name. Does nothing if no mapping exists.
    /// </summary>
    /// <param name="typeNamespace">The Windows Runtime namespace; replaced with the mapped namespace on a hit.</param>
    /// <param name="typeName">The Windows Runtime type name; replaced with the mapped name on a hit.</param>
    /// <returns><see langword="true"/> if a mapping was applied; otherwise <see langword="false"/>.</returns>
    public static bool ApplyMapping(ref string typeNamespace, ref string typeName)
    {
        if (Get(typeNamespace, typeName) is { } m)
        {
            typeNamespace = m.MappedNamespace;
            typeName = m.MappedName;
            return true;
        }

        return false;
    }
}