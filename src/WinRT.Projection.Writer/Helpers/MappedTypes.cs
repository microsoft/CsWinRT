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
    private static readonly FrozenDictionary<string, FrozenDictionary<string, MappedType>> TypeMappings = Build();

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

    private static FrozenDictionary<string, FrozenDictionary<string, MappedType>> Build()
    {
        Dictionary<string, Dictionary<string, MappedType>> result = [];

        // helper to add a type entry
        void Add(string ns, MappedType mt)
        {
            if (!result.TryGetValue(ns, out Dictionary<string, MappedType>? bag))
            {
                bag = [];
                result[ns] = bag;
            }

            bag[mt.AbiName] = mt;
        }

        // Microsoft.UI.Xaml
        Add("Microsoft.UI.Xaml", new("CornerRadius", "Microsoft.UI.Xaml", "CornerRadius", false, false, true));
        Add("Microsoft.UI.Xaml", new("CornerRadiusHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("Duration", "Microsoft.UI.Xaml", "Duration", false, false, true));
        Add("Microsoft.UI.Xaml", new("DurationHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("GridLength", "Microsoft.UI.Xaml", "GridLength", false, false, true));
        Add("Microsoft.UI.Xaml", new("GridLengthHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("ICornerRadiusHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("ICornerRadiusHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml", new("IDurationHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("IDurationHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml", new("IGridLengthHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("IGridLengthHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml", new("IThicknessHelper", "", ""));
        Add("Microsoft.UI.Xaml", new("IThicknessHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml", new("IXamlServiceProvider", "System", "IServiceProvider"));
        Add("Microsoft.UI.Xaml", new("ThicknessHelper", "", ""));

        // Microsoft.UI.Xaml.Controls.Primitives
        Add("Microsoft.UI.Xaml.Controls.Primitives", new("GeneratorPositionHelper", "", ""));
        Add("Microsoft.UI.Xaml.Controls.Primitives", new("IGeneratorPositionHelper", "", ""));
        Add("Microsoft.UI.Xaml.Controls.Primitives", new("IGeneratorPositionHelperStatics", "", ""));

        // Microsoft.UI.Xaml.Data
        Add("Microsoft.UI.Xaml.Data", new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs"));
        Add("Microsoft.UI.Xaml.Data", new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true));
        Add("Microsoft.UI.Xaml.Data", new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged"));
        Add("Microsoft.UI.Xaml.Data", new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs"));
        Add("Microsoft.UI.Xaml.Data", new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler"));

        // Microsoft.UI.Xaml.Input
        Add("Microsoft.UI.Xaml.Input", new("ICommand", "System.Windows.Input", "ICommand", true));

        // Microsoft.UI.Xaml.Interop
        Add("Microsoft.UI.Xaml.Interop", new("IBindableIterable", "System.Collections", "IEnumerable", true, true));
        Add("Microsoft.UI.Xaml.Interop", new("IBindableIterator", "System.Collections", "IEnumerator", true, true));
        Add("Microsoft.UI.Xaml.Interop", new("IBindableVector", "System.Collections", "IList", true, true));
        Add("Microsoft.UI.Xaml.Interop", new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true));
        Add("Microsoft.UI.Xaml.Interop", new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction"));
        Add("Microsoft.UI.Xaml.Interop", new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true));
        Add("Microsoft.UI.Xaml.Interop", new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true));

        // Microsoft.UI.Xaml.Media
        Add("Microsoft.UI.Xaml.Media", new("IMatrixHelper", "", ""));
        Add("Microsoft.UI.Xaml.Media", new("IMatrixHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml.Media", new("MatrixHelper", "", ""));

        // Microsoft.UI.Xaml.Media.Animation
        Add("Microsoft.UI.Xaml.Media.Animation", new("IKeyTimeHelper", "", ""));
        Add("Microsoft.UI.Xaml.Media.Animation", new("IKeyTimeHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml.Media.Animation", new("IRepeatBehaviorHelper", "", ""));
        Add("Microsoft.UI.Xaml.Media.Animation", new("IRepeatBehaviorHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml.Media.Animation", new("KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime", false, false, true));
        Add("Microsoft.UI.Xaml.Media.Animation", new("KeyTimeHelper", "", ""));
        Add("Microsoft.UI.Xaml.Media.Animation", new("RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true));
        Add("Microsoft.UI.Xaml.Media.Animation", new("RepeatBehaviorHelper", "", ""));

        // Microsoft.UI.Xaml.Media.Media3D
        Add("Microsoft.UI.Xaml.Media.Media3D", new("IMatrix3DHelper", "", ""));
        Add("Microsoft.UI.Xaml.Media.Media3D", new("IMatrix3DHelperStatics", "", ""));
        Add("Microsoft.UI.Xaml.Media.Media3D", new("Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true));
        Add("Microsoft.UI.Xaml.Media.Media3D", new("Matrix3DHelper", "", ""));

        // Windows.Foundation
        Add(WindowsFoundation, new("AsyncActionCompletedHandler", WindowsFoundation, "AsyncActionCompletedHandler"));
        Add(WindowsFoundation, new("AsyncActionProgressHandler`1", WindowsFoundation, "AsyncActionProgressHandler`1"));
        Add(WindowsFoundation, new("AsyncActionWithProgressCompletedHandler`1", WindowsFoundation, "AsyncActionWithProgressCompletedHandler`1"));
        Add(WindowsFoundation, new("AsyncOperationCompletedHandler`1", WindowsFoundation, "AsyncOperationCompletedHandler`1"));
        Add(WindowsFoundation, new("AsyncOperationProgressHandler`2", WindowsFoundation, "AsyncOperationProgressHandler`2"));
        Add(WindowsFoundation, new("AsyncOperationWithProgressCompletedHandler`2", WindowsFoundation, "AsyncOperationWithProgressCompletedHandler`2"));
        Add(WindowsFoundation, new("AsyncStatus", WindowsFoundation, "AsyncStatus"));
        Add(WindowsFoundation, new("DateTime", "System", "DateTimeOffset", true));
        Add(WindowsFoundation, new("EventHandler`1", "System", "EventHandler`1", false));
        Add(WindowsFoundation, new("EventRegistrationToken", "WindowsRuntime.InteropServices", "EventRegistrationToken", false));
        Add(WindowsFoundation, new("FoundationContract", WindowsFoundation, "FoundationContract"));
        Add(WindowsFoundation, new(HResult, "System", "Exception", true));
        Add(WindowsFoundation, new("IAsyncAction", WindowsFoundation, "IAsyncAction"));
        Add(WindowsFoundation, new("IAsyncActionWithProgress`1", WindowsFoundation, "IAsyncActionWithProgress`1"));
        Add(WindowsFoundation, new("IAsyncInfo", WindowsFoundation, "IAsyncInfo"));
        Add(WindowsFoundation, new("IAsyncOperationWithProgress`2", WindowsFoundation, "IAsyncOperationWithProgress`2"));
        Add(WindowsFoundation, new("IAsyncOperation`1", WindowsFoundation, "IAsyncOperation`1"));
        Add(WindowsFoundation, new("IClosable", "System", "IDisposable", true, true));
        Add(WindowsFoundation, new("IMemoryBufferReference", WindowsFoundation, "IMemoryBufferReference"));
        Add(WindowsFoundation, new("IPropertyValue", WindowsFoundation, "IPropertyValue", true));
        Add(WindowsFoundation, new("IReferenceArray`1", WindowsFoundation, "IReferenceArray", true));
        Add(WindowsFoundation, new(IReferenceGeneric, "System", NullableGeneric, true));
        Add(WindowsFoundation, new("IStringable", WindowsFoundation, "IStringable"));
        Add(WindowsFoundation, new("Point", WindowsFoundation, "Point"));
        Add(WindowsFoundation, new("PropertyType", WindowsFoundation, "PropertyType"));
        Add(WindowsFoundation, new("Rect", WindowsFoundation, "Rect"));
        Add(WindowsFoundation, new("Size", WindowsFoundation, "Size"));
        Add(WindowsFoundation, new("TimeSpan", "System", "TimeSpan", true));
        Add(WindowsFoundation, new("TypedEventHandler`2", "System", "EventHandler`2", false));
        Add(WindowsFoundation, new("UniversalApiContract", WindowsFoundation, "UniversalApiContract"));
        Add(WindowsFoundation, new("Uri", "System", "Uri", true));

        // Windows.Foundation.Collections
        Add(WindowsFoundationCollections, new("CollectionChange", WindowsFoundationCollections, "CollectionChange"));
        Add(WindowsFoundationCollections, new("IIterable`1", "System.Collections.Generic", "IEnumerable`1", true, true));
        Add(WindowsFoundationCollections, new("IIterator`1", "System.Collections.Generic", "IEnumerator`1", true, true));
        Add(WindowsFoundationCollections, new("IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", true));
        Add(WindowsFoundationCollections, new("IMapChangedEventArgs`1", WindowsFoundationCollections, "IMapChangedEventArgs`1"));
        Add(WindowsFoundationCollections, new("IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", true, true));
        Add(WindowsFoundationCollections, new("IMap`2", "System.Collections.Generic", "IDictionary`2", true, true));
        Add(WindowsFoundationCollections, new("IObservableMap`2", WindowsFoundationCollections, "IObservableMap`2"));
        Add(WindowsFoundationCollections, new("IObservableVector`1", WindowsFoundationCollections, "IObservableVector`1"));
        Add(WindowsFoundationCollections, new("IVectorChangedEventArgs", WindowsFoundationCollections, "IVectorChangedEventArgs"));
        Add(WindowsFoundationCollections, new("IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", true, true));
        Add(WindowsFoundationCollections, new("IVector`1", "System.Collections.Generic", "IList`1", true, true));
        Add(WindowsFoundationCollections, new("MapChangedEventHandler`2", WindowsFoundationCollections, "MapChangedEventHandler`2"));
        Add(WindowsFoundationCollections, new("VectorChangedEventHandler`1", WindowsFoundationCollections, "VectorChangedEventHandler`1"));

        // Windows.Foundation.Metadata
        Add(WindowsFoundationMetadata, new("ApiContractAttribute", WindowsFoundationMetadata, "ApiContractAttribute"));
        Add(WindowsFoundationMetadata, new("AttributeTargets", "System", "AttributeTargets"));
        Add(WindowsFoundationMetadata, new("AttributeUsageAttribute", "System", "AttributeUsageAttribute"));
        Add(WindowsFoundationMetadata, new(ContractVersionAttribute, WindowsFoundationMetadata, ContractVersionAttribute));

        // Windows.Foundation.Numerics
        Add("Windows.Foundation.Numerics", new("Matrix3x2", "System.Numerics", "Matrix3x2"));
        Add("Windows.Foundation.Numerics", new("Matrix4x4", "System.Numerics", "Matrix4x4"));
        Add("Windows.Foundation.Numerics", new("Plane", "System.Numerics", "Plane"));
        Add("Windows.Foundation.Numerics", new("Quaternion", "System.Numerics", "Quaternion"));
        Add("Windows.Foundation.Numerics", new("Vector2", "System.Numerics", "Vector2"));
        Add("Windows.Foundation.Numerics", new("Vector3", "System.Numerics", "Vector3"));
        Add("Windows.Foundation.Numerics", new("Vector4", "System.Numerics", "Vector4"));

        // Windows.Storage.Streams
        Add("Windows.Storage.Streams", new("IBuffer", "Windows.Storage.Streams", "IBuffer"));
        Add("Windows.Storage.Streams", new("IInputStream", "Windows.Storage.Streams", "IInputStream"));
        Add("Windows.Storage.Streams", new("IOutputStream", "Windows.Storage.Streams", "IOutputStream"));
        Add("Windows.Storage.Streams", new("IRandomAccessStream", "Windows.Storage.Streams", "IRandomAccessStream"));
        Add("Windows.Storage.Streams", new("InputStreamOptions", "Windows.Storage.Streams", "InputStreamOptions"));

        // Windows.UI.Xaml
        Add("Windows.UI.Xaml", new("CornerRadius", "Windows.UI.Xaml", "CornerRadius", false, false, true));
        Add("Windows.UI.Xaml", new("CornerRadiusHelper", "", ""));
        Add("Windows.UI.Xaml", new("Duration", "Windows.UI.Xaml", "Duration", false, false, true));
        Add("Windows.UI.Xaml", new("DurationHelper", "", ""));
        Add("Windows.UI.Xaml", new("GridLength", "Windows.UI.Xaml", "GridLength", false, false, true));
        Add("Windows.UI.Xaml", new("GridLengthHelper", "", ""));
        Add("Windows.UI.Xaml", new("ICornerRadiusHelper", "", ""));
        Add("Windows.UI.Xaml", new("ICornerRadiusHelperStatics", "", ""));
        Add("Windows.UI.Xaml", new("IDurationHelper", "", ""));
        Add("Windows.UI.Xaml", new("IDurationHelperStatics", "", ""));
        Add("Windows.UI.Xaml", new("IGridLengthHelper", "", ""));
        Add("Windows.UI.Xaml", new("IGridLengthHelperStatics", "", ""));
        Add("Windows.UI.Xaml", new("IThicknessHelper", "", ""));
        Add("Windows.UI.Xaml", new("IThicknessHelperStatics", "", ""));
        Add("Windows.UI.Xaml", new("IXamlServiceProvider", "System", "IServiceProvider"));
        Add("Windows.UI.Xaml", new("ThicknessHelper", "", ""));

        // Windows.UI.Xaml.Controls.Primitives
        Add("Windows.UI.Xaml.Controls.Primitives", new("GeneratorPositionHelper", "", ""));
        Add("Windows.UI.Xaml.Controls.Primitives", new("IGeneratorPositionHelper", "", ""));
        Add("Windows.UI.Xaml.Controls.Primitives", new("IGeneratorPositionHelperStatics", "", ""));

        // Windows.UI.Xaml.Data
        Add("Windows.UI.Xaml.Data", new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs"));
        Add("Windows.UI.Xaml.Data", new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true));
        Add("Windows.UI.Xaml.Data", new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged"));
        Add("Windows.UI.Xaml.Data", new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs"));
        Add("Windows.UI.Xaml.Data", new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler"));

        // Windows.UI.Xaml.Input
        Add("Windows.UI.Xaml.Input", new("ICommand", "System.Windows.Input", "ICommand", true));

        // Windows.UI.Xaml.Interop
        Add(WindowsUIXamlInterop, new("IBindableIterable", "System.Collections", "IEnumerable", true, true));
        Add(WindowsUIXamlInterop, new("IBindableIterator", "System.Collections", "IEnumerator", true, true));
        Add(WindowsUIXamlInterop, new("IBindableVector", "System.Collections", "IList", true, true));
        Add(WindowsUIXamlInterop, new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true));
        Add(WindowsUIXamlInterop, new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction"));
        Add(WindowsUIXamlInterop, new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true));
        Add(WindowsUIXamlInterop, new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true));
        Add(WindowsUIXamlInterop, new("TypeKind", WindowsUIXamlInterop, "TypeKind", true));
        Add(WindowsUIXamlInterop, new(TypeName, "System", "Type", true));

        // Windows.UI.Xaml.Media
        Add("Windows.UI.Xaml.Media", new("IMatrixHelper", "", ""));
        Add("Windows.UI.Xaml.Media", new("IMatrixHelperStatics", "", ""));
        Add("Windows.UI.Xaml.Media", new("MatrixHelper", "", ""));

        // Windows.UI.Xaml.Media.Animation
        Add("Windows.UI.Xaml.Media.Animation", new("IKeyTimeHelper", "", ""));
        Add("Windows.UI.Xaml.Media.Animation", new("IKeyTimeHelperStatics", "", ""));
        Add("Windows.UI.Xaml.Media.Animation", new("IRepeatBehaviorHelper", "", ""));
        Add("Windows.UI.Xaml.Media.Animation", new("IRepeatBehaviorHelperStatics", "", ""));
        Add("Windows.UI.Xaml.Media.Animation", new("KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime", false, false, true));
        Add("Windows.UI.Xaml.Media.Animation", new("KeyTimeHelper", "", ""));
        Add("Windows.UI.Xaml.Media.Animation", new("RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true));
        Add("Windows.UI.Xaml.Media.Animation", new("RepeatBehaviorHelper", "", ""));

        // Windows.UI.Xaml.Media.Media3D
        Add("Windows.UI.Xaml.Media.Media3D", new("IMatrix3DHelper", "", ""));
        Add("Windows.UI.Xaml.Media.Media3D", new("IMatrix3DHelperStatics", "", ""));
        Add("Windows.UI.Xaml.Media.Media3D", new("Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true));
        Add("Windows.UI.Xaml.Media.Media3D", new("Matrix3DHelper", "", ""));

        // WindowsRuntime.Internal
        Add(WindowsRuntimeInternal, new("HWND", "System", "IntPtr"));
        Add(WindowsRuntimeInternal, new("ProjectionInternalAttribute", "", ""));

        Dictionary<string, FrozenDictionary<string, MappedType>> frozenInner = [];
        foreach (KeyValuePair<string, Dictionary<string, MappedType>> kvp in result)
        {
            frozenInner[kvp.Key] = kvp.Value.ToFrozenDictionary();
        }
        return frozenInner.ToFrozenDictionary();
    }
}