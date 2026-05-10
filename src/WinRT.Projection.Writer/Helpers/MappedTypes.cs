// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Maps a Windows Runtime type to the corresponding .NET type.
/// </summary>
internal sealed record MappedType(
    string AbiName,
    string MappedNamespace,
    string MappedName,
    bool RequiresMarshaling = false,
    bool HasCustomMembersOutput = false,
    bool EmitAbi = false);

/// <summary>
/// Static lookup table for Windows Runtime → .NET type mappings
/// </summary>
internal static class MappedTypes
{
    private static readonly Dictionary<string, Dictionary<string, MappedType>> _byNamespace = Build();

    public static MappedType? Get(string typeNamespace, string typeName)
    {
        if (_byNamespace.TryGetValue(typeNamespace, out Dictionary<string, MappedType>? namesp) &&
            namesp.TryGetValue(typeName, out MappedType? mapped))
        {
            return mapped;
        }
        return null;
    }

    public static bool HasNamespace(string typeNamespace) => _byNamespace.ContainsKey(typeNamespace);

    private static Dictionary<string, Dictionary<string, MappedType>> Build()
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
        Add("Windows.Foundation", new("AsyncActionCompletedHandler", "Windows.Foundation", "AsyncActionCompletedHandler"));
        Add("Windows.Foundation", new("AsyncActionProgressHandler`1", "Windows.Foundation", "AsyncActionProgressHandler`1"));
        Add("Windows.Foundation", new("AsyncActionWithProgressCompletedHandler`1", "Windows.Foundation", "AsyncActionWithProgressCompletedHandler`1"));
        Add("Windows.Foundation", new("AsyncOperationCompletedHandler`1", "Windows.Foundation", "AsyncOperationCompletedHandler`1"));
        Add("Windows.Foundation", new("AsyncOperationProgressHandler`2", "Windows.Foundation", "AsyncOperationProgressHandler`2"));
        Add("Windows.Foundation", new("AsyncOperationWithProgressCompletedHandler`2", "Windows.Foundation", "AsyncOperationWithProgressCompletedHandler`2"));
        Add("Windows.Foundation", new("AsyncStatus", "Windows.Foundation", "AsyncStatus"));
        Add("Windows.Foundation", new("DateTime", "System", "DateTimeOffset", true));
        Add("Windows.Foundation", new("EventHandler`1", "System", "EventHandler`1", false));
        Add("Windows.Foundation", new("EventRegistrationToken", "WindowsRuntime.InteropServices", "EventRegistrationToken", false));
        Add("Windows.Foundation", new("FoundationContract", "Windows.Foundation", "FoundationContract"));
        Add("Windows.Foundation", new("HResult", "System", "Exception", true));
        Add("Windows.Foundation", new("IAsyncAction", "Windows.Foundation", "IAsyncAction"));
        Add("Windows.Foundation", new("IAsyncActionWithProgress`1", "Windows.Foundation", "IAsyncActionWithProgress`1"));
        Add("Windows.Foundation", new("IAsyncInfo", "Windows.Foundation", "IAsyncInfo"));
        Add("Windows.Foundation", new("IAsyncOperationWithProgress`2", "Windows.Foundation", "IAsyncOperationWithProgress`2"));
        Add("Windows.Foundation", new("IAsyncOperation`1", "Windows.Foundation", "IAsyncOperation`1"));
        Add("Windows.Foundation", new("IClosable", "System", "IDisposable", true, true));
        Add("Windows.Foundation", new("IMemoryBufferReference", "Windows.Foundation", "IMemoryBufferReference"));
        Add("Windows.Foundation", new("IPropertyValue", "Windows.Foundation", "IPropertyValue", true));
        Add("Windows.Foundation", new("IReferenceArray`1", "Windows.Foundation", "IReferenceArray", true));
        Add("Windows.Foundation", new("IReference`1", "System", "Nullable`1", true));
        Add("Windows.Foundation", new("IStringable", "Windows.Foundation", "IStringable"));
        Add("Windows.Foundation", new("Point", "Windows.Foundation", "Point"));
        Add("Windows.Foundation", new("PropertyType", "Windows.Foundation", "PropertyType"));
        Add("Windows.Foundation", new("Rect", "Windows.Foundation", "Rect"));
        Add("Windows.Foundation", new("Size", "Windows.Foundation", "Size"));
        Add("Windows.Foundation", new("TimeSpan", "System", "TimeSpan", true));
        Add("Windows.Foundation", new("TypedEventHandler`2", "System", "EventHandler`2", false));
        Add("Windows.Foundation", new("UniversalApiContract", "Windows.Foundation", "UniversalApiContract"));
        Add("Windows.Foundation", new("Uri", "System", "Uri", true));

        // Windows.Foundation.Collections
        Add("Windows.Foundation.Collections", new("CollectionChange", "Windows.Foundation.Collections", "CollectionChange"));
        Add("Windows.Foundation.Collections", new("IIterable`1", "System.Collections.Generic", "IEnumerable`1", true, true));
        Add("Windows.Foundation.Collections", new("IIterator`1", "System.Collections.Generic", "IEnumerator`1", true, true));
        Add("Windows.Foundation.Collections", new("IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", true));
        Add("Windows.Foundation.Collections", new("IMapChangedEventArgs`1", "Windows.Foundation.Collections", "IMapChangedEventArgs`1"));
        Add("Windows.Foundation.Collections", new("IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", true, true));
        Add("Windows.Foundation.Collections", new("IMap`2", "System.Collections.Generic", "IDictionary`2", true, true));
        Add("Windows.Foundation.Collections", new("IObservableMap`2", "Windows.Foundation.Collections", "IObservableMap`2"));
        Add("Windows.Foundation.Collections", new("IObservableVector`1", "Windows.Foundation.Collections", "IObservableVector`1"));
        Add("Windows.Foundation.Collections", new("IVectorChangedEventArgs", "Windows.Foundation.Collections", "IVectorChangedEventArgs"));
        Add("Windows.Foundation.Collections", new("IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", true, true));
        Add("Windows.Foundation.Collections", new("IVector`1", "System.Collections.Generic", "IList`1", true, true));
        Add("Windows.Foundation.Collections", new("MapChangedEventHandler`2", "Windows.Foundation.Collections", "MapChangedEventHandler`2"));
        Add("Windows.Foundation.Collections", new("VectorChangedEventHandler`1", "Windows.Foundation.Collections", "VectorChangedEventHandler`1"));

        // Windows.Foundation.Metadata
        Add("Windows.Foundation.Metadata", new("ApiContractAttribute", "Windows.Foundation.Metadata", "ApiContractAttribute"));
        Add("Windows.Foundation.Metadata", new("AttributeTargets", "System", "AttributeTargets"));
        Add("Windows.Foundation.Metadata", new("AttributeUsageAttribute", "System", "AttributeUsageAttribute"));
        Add("Windows.Foundation.Metadata", new("ContractVersionAttribute", "Windows.Foundation.Metadata", "ContractVersionAttribute"));

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
        Add("Windows.UI.Xaml.Interop", new("IBindableIterable", "System.Collections", "IEnumerable", true, true));
        Add("Windows.UI.Xaml.Interop", new("IBindableIterator", "System.Collections", "IEnumerator", true, true));
        Add("Windows.UI.Xaml.Interop", new("IBindableVector", "System.Collections", "IList", true, true));
        Add("Windows.UI.Xaml.Interop", new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true));
        Add("Windows.UI.Xaml.Interop", new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction"));
        Add("Windows.UI.Xaml.Interop", new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true));
        Add("Windows.UI.Xaml.Interop", new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true));
        Add("Windows.UI.Xaml.Interop", new("TypeKind", "Windows.UI.Xaml.Interop", "TypeKind", true));
        Add("Windows.UI.Xaml.Interop", new("TypeName", "System", "Type", true));

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
        Add("WindowsRuntime.Internal", new("HWND", "System", "IntPtr"));
        Add("WindowsRuntime.Internal", new("ProjectionInternalAttribute", "", ""));

        return result;
    }
}