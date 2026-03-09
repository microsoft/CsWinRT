// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsRuntime.ProjectionGenerator.Helpers;

/// <summary>
/// Provides type mapping information for WinRT to .NET type projections.
/// This is a port of the <c>get_mapped_types_in_namespace</c> function from the C++ cswinrt <c>helpers.h</c>.
/// </summary>
/// <remarks>
/// Keep this table consistent with the registrations in <c>WinRT.Runtime/Projections.cs</c>
/// and the reverse mapping in <c>WinRT.SourceGenerator/TypeMapper.cs</c>.
/// This table includes both MUX (Microsoft.UI.Xaml) and WUX (Windows.UI.Xaml) types
/// as only one will be selected at runtime.
/// </remarks>
internal static class TypeMappings
{
    /// <summary>
    /// Represents a mapping from a WinRT type to a .NET type.
    /// </summary>
    /// <param name="AbiName">The ABI type name.</param>
    /// <param name="MappedNamespace">The .NET namespace, or <see langword="null"/> if the type is suppressed.</param>
    /// <param name="MappedName">The .NET type name, or <see langword="null"/> if the type is suppressed.</param>
    /// <param name="RequiresMarshaling">Whether the type requires marshaling.</param>
    /// <param name="HasCustomMembersOutput">Whether custom member output is generated.</param>
    /// <param name="EmitAbi">Whether ABI code should be emitted.</param>
    internal readonly record struct MappedType(
        string AbiName,
        string? MappedNamespace,
        string? MappedName,
        bool RequiresMarshaling,
        bool HasCustomMembersOutput,
        bool EmitAbi);

    /// <summary>
    /// The lookup dictionary from (WinRT namespace, WinRT type name) to the corresponding mapped type information.
    /// </summary>
    private static readonly FrozenDictionary<(string Namespace, string Name), MappedType> Mappings = new Dictionary<(string, string), MappedType>
    {
        // -----------------------------------------------
        // Microsoft.UI.Xaml
        // -----------------------------------------------
        [("Microsoft.UI.Xaml", "CornerRadius")] = new("CornerRadius", "Microsoft.UI.Xaml", "CornerRadius", false, false, true),
        [("Microsoft.UI.Xaml", "CornerRadiusHelper")] = new("CornerRadiusHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "Duration")] = new("Duration", "Microsoft.UI.Xaml", "Duration", false, false, true),
        [("Microsoft.UI.Xaml", "DurationHelper")] = new("DurationHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "GridLength")] = new("GridLength", "Microsoft.UI.Xaml", "GridLength", false, false, true),
        [("Microsoft.UI.Xaml", "GridLengthHelper")] = new("GridLengthHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "ICornerRadiusHelper")] = new("ICornerRadiusHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "ICornerRadiusHelperStatics")] = new("ICornerRadiusHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IDurationHelper")] = new("IDurationHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IDurationHelperStatics")] = new("IDurationHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IGridLengthHelper")] = new("IGridLengthHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IGridLengthHelperStatics")] = new("IGridLengthHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IThicknessHelper")] = new("IThicknessHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IThicknessHelperStatics")] = new("IThicknessHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml", "IXamlServiceProvider")] = new("IXamlServiceProvider", "System", "IServiceProvider", false, false, false),
        [("Microsoft.UI.Xaml", "ThicknessHelper")] = new("ThicknessHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Controls.Primitives
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Controls.Primitives", "GeneratorPositionHelper")] = new("GeneratorPositionHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Controls.Primitives", "IGeneratorPositionHelper")] = new("IGeneratorPositionHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Controls.Primitives", "IGeneratorPositionHelperStatics")] = new("IGeneratorPositionHelperStatics", null, null, false, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Data
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs")] = new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs", false, false, false),
        [("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo")] = new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true, false),
        [("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged")] = new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged", false, false, false),
        [("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs")] = new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs", false, false, false),
        [("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler")] = new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler", false, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Input
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Input", "ICommand")] = new("ICommand", "System.Windows.Input", "ICommand", true, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Interop
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Interop", "IBindableIterable")] = new("IBindableIterable", "System.Collections", "IEnumerable", true, true, false),
        [("Microsoft.UI.Xaml.Interop", "IBindableIterator")] = new("IBindableIterator", "System.Collections", "IEnumerator", true, true, false),
        [("Microsoft.UI.Xaml.Interop", "IBindableVector")] = new("IBindableVector", "System.Collections", "IList", true, true, false),
        [("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged")] = new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true, false, false),
        [("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction")] = new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction", false, false, false),
        [("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs")] = new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true, false, false),
        [("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")] = new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Media
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Media", "IMatrixHelper")] = new("IMatrixHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media", "IMatrixHelperStatics")] = new("IMatrixHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media", "MatrixHelper")] = new("MatrixHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Media.Animation
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Media.Animation", "IKeyTimeHelper")] = new("IKeyTimeHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Animation", "IKeyTimeHelperStatics")] = new("IKeyTimeHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Animation", "IRepeatBehaviorHelper")] = new("IRepeatBehaviorHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Animation", "IRepeatBehaviorHelperStatics")] = new("IRepeatBehaviorHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Animation", "KeyTime")] = new("KeyTime", "Microsoft.UI.Xaml.Media.Animation", "KeyTime", false, false, true),
        [("Microsoft.UI.Xaml.Media.Animation", "KeyTimeHelper")] = new("KeyTimeHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior")] = new("RepeatBehavior", "Microsoft.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true),
        [("Microsoft.UI.Xaml.Media.Animation", "RepeatBehaviorHelper")] = new("RepeatBehaviorHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Microsoft.UI.Xaml.Media.Media3D
        // -----------------------------------------------
        [("Microsoft.UI.Xaml.Media.Media3D", "IMatrix3DHelper")] = new("IMatrix3DHelper", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Media3D", "IMatrix3DHelperStatics")] = new("IMatrix3DHelperStatics", null, null, false, false, false),
        [("Microsoft.UI.Xaml.Media.Media3D", "Matrix3D")] = new("Matrix3D", "Microsoft.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true),
        [("Microsoft.UI.Xaml.Media.Media3D", "Matrix3DHelper")] = new("Matrix3DHelper", null, null, false, false, false),

        // -----------------------------------------------
        // WinRT.Interop
        // -----------------------------------------------
        [("WinRT.Interop", "HWND")] = new("HWND", "System", "IntPtr", false, false, false),
        [("WinRT.Interop", "ProjectionInternalAttribute")] = new("ProjectionInternalAttribute", null, null, false, false, false),

        // -----------------------------------------------
        // Windows.Foundation
        // -----------------------------------------------
        [("Windows.Foundation", "AsyncActionCompletedHandler")] = new("AsyncActionCompletedHandler", "Windows.Foundation", "AsyncActionCompletedHandler", false, false, false),
        [("Windows.Foundation", "AsyncActionProgressHandler`1")] = new("AsyncActionProgressHandler`1", "Windows.Foundation", "AsyncActionProgressHandler`1", false, false, false),
        [("Windows.Foundation", "AsyncActionWithProgressCompletedHandler`1")] = new("AsyncActionWithProgressCompletedHandler`1", "Windows.Foundation", "AsyncActionWithProgressCompletedHandler`1", false, false, false),
        [("Windows.Foundation", "AsyncOperationCompletedHandler`1")] = new("AsyncOperationCompletedHandler`1", "Windows.Foundation", "AsyncOperationCompletedHandler`1", false, false, false),
        [("Windows.Foundation", "AsyncOperationProgressHandler`2")] = new("AsyncOperationProgressHandler`2", "Windows.Foundation", "AsyncOperationProgressHandler`2", false, false, false),
        [("Windows.Foundation", "AsyncOperationWithProgressCompletedHandler`2")] = new("AsyncOperationWithProgressCompletedHandler`2", "Windows.Foundation", "AsyncOperationWithProgressCompletedHandler`2", false, false, false),
        [("Windows.Foundation", "AsyncStatus")] = new("AsyncStatus", "Windows.Foundation", "AsyncStatus", false, false, false),
        [("Windows.Foundation", "DateTime")] = new("DateTime", "System", "DateTimeOffset", true, false, false),
        [("Windows.Foundation", "EventHandler`1")] = new("EventHandler`1", "System", "EventHandler`1", false, false, false),
        [("Windows.Foundation", "EventRegistrationToken")] = new("EventRegistrationToken", "WindowsRuntime.InteropServices", "EventRegistrationToken", false, false, false),
        [("Windows.Foundation", "FoundationContract")] = new("FoundationContract", "Windows.Foundation", "FoundationContract", false, false, false),
        [("Windows.Foundation", "HResult")] = new("HResult", "System", "Exception", true, false, false),
        [("Windows.Foundation", "IAsyncAction")] = new("IAsyncAction", "Windows.Foundation", "IAsyncAction", false, false, false),
        [("Windows.Foundation", "IAsyncActionWithProgress`1")] = new("IAsyncActionWithProgress`1", "Windows.Foundation", "IAsyncActionWithProgress`1", false, false, false),
        [("Windows.Foundation", "IAsyncInfo")] = new("IAsyncInfo", "Windows.Foundation", "IAsyncInfo", false, false, false),
        [("Windows.Foundation", "IAsyncOperationWithProgress`2")] = new("IAsyncOperationWithProgress`2", "Windows.Foundation", "IAsyncOperationWithProgress`2", false, false, false),
        [("Windows.Foundation", "IAsyncOperation`1")] = new("IAsyncOperation`1", "Windows.Foundation", "IAsyncOperation`1", false, false, false),
        [("Windows.Foundation", "IClosable")] = new("IClosable", "System", "IDisposable", true, true, false),
        [("Windows.Foundation", "IMemoryBufferReference")] = new("IMemoryBufferReference", "Windows.Foundation", "IMemoryBufferReference", false, false, false),
        [("Windows.Foundation", "IPropertyValue")] = new("IPropertyValue", "Windows.Foundation", "IPropertyValue", true, false, false),
        [("Windows.Foundation", "IReferenceArray`1")] = new("IReferenceArray`1", "Windows.Foundation", "IReferenceArray", true, false, false),
        [("Windows.Foundation", "IReference`1")] = new("IReference`1", "System", "Nullable`1", true, false, false),
        [("Windows.Foundation", "IStringable")] = new("IStringable", "Windows.Foundation", "IStringable", false, false, false),
        [("Windows.Foundation", "Point")] = new("Point", "Windows.Foundation", "Point", false, false, false),
        [("Windows.Foundation", "PropertyType")] = new("PropertyType", "Windows.Foundation", "PropertyType", false, false, false),
        [("Windows.Foundation", "Rect")] = new("Rect", "Windows.Foundation", "Rect", false, false, false),
        [("Windows.Foundation", "Size")] = new("Size", "Windows.Foundation", "Size", false, false, false),
        [("Windows.Foundation", "TimeSpan")] = new("TimeSpan", "System", "TimeSpan", true, false, false),
        [("Windows.Foundation", "TypedEventHandler`2")] = new("TypedEventHandler`2", "System", "EventHandler`2", false, false, false),
        [("Windows.Foundation", "UniversalApiContract")] = new("UniversalApiContract", "Windows.Foundation", "UniversalApiContract", false, false, false),
        [("Windows.Foundation", "Uri")] = new("Uri", "System", "Uri", true, false, false),

        // -----------------------------------------------
        // Windows.Foundation.Collections
        // -----------------------------------------------
        [("Windows.Foundation.Collections", "CollectionChange")] = new("CollectionChange", "Windows.Foundation.Collections", "CollectionChange", false, false, false),
        [("Windows.Foundation.Collections", "IIterable`1")] = new("IIterable`1", "System.Collections.Generic", "IEnumerable`1", true, true, false),
        [("Windows.Foundation.Collections", "IIterator`1")] = new("IIterator`1", "System.Collections.Generic", "IEnumerator`1", true, true, false),
        [("Windows.Foundation.Collections", "IKeyValuePair`2")] = new("IKeyValuePair`2", "System.Collections.Generic", "KeyValuePair`2", true, false, false),
        [("Windows.Foundation.Collections", "IMapChangedEventArgs`1")] = new("IMapChangedEventArgs`1", "Windows.Foundation.Collections", "IMapChangedEventArgs`1", false, false, false),
        [("Windows.Foundation.Collections", "IMapView`2")] = new("IMapView`2", "System.Collections.Generic", "IReadOnlyDictionary`2", true, true, false),
        [("Windows.Foundation.Collections", "IMap`2")] = new("IMap`2", "System.Collections.Generic", "IDictionary`2", true, true, false),
        [("Windows.Foundation.Collections", "IObservableMap`2")] = new("IObservableMap`2", "Windows.Foundation.Collections", "IObservableMap`2", false, false, false),
        [("Windows.Foundation.Collections", "IObservableVector`1")] = new("IObservableVector`1", "Windows.Foundation.Collections", "IObservableVector`1", false, false, false),
        [("Windows.Foundation.Collections", "IVectorChangedEventArgs")] = new("IVectorChangedEventArgs", "Windows.Foundation.Collections", "IVectorChangedEventArgs", false, false, false),
        [("Windows.Foundation.Collections", "IVectorView`1")] = new("IVectorView`1", "System.Collections.Generic", "IReadOnlyList`1", true, true, false),
        [("Windows.Foundation.Collections", "IVector`1")] = new("IVector`1", "System.Collections.Generic", "IList`1", true, true, false),
        [("Windows.Foundation.Collections", "MapChangedEventHandler`2")] = new("MapChangedEventHandler`2", "Windows.Foundation.Collections", "MapChangedEventHandler`2", false, false, false),
        [("Windows.Foundation.Collections", "VectorChangedEventHandler`1")] = new("VectorChangedEventHandler`1", "Windows.Foundation.Collections", "VectorChangedEventHandler`1", false, false, false),

        // -----------------------------------------------
        // Windows.Foundation.Metadata
        // -----------------------------------------------
        [("Windows.Foundation.Metadata", "AttributeTargets")] = new("AttributeTargets", "System", "AttributeTargets", false, false, false),
        [("Windows.Foundation.Metadata", "AttributeUsageAttribute")] = new("AttributeUsageAttribute", "System", "AttributeUsageAttribute", false, false, false),
        [("Windows.Foundation.Metadata", "ContractVersionAttribute")] = new("ContractVersionAttribute", "Windows.Foundation.Metadata", "ContractVersionAttribute", false, false, false),

        // -----------------------------------------------
        // Windows.Foundation.Numerics
        // -----------------------------------------------
        [("Windows.Foundation.Numerics", "Matrix3x2")] = new("Matrix3x2", "System.Numerics", "Matrix3x2", false, false, false),
        [("Windows.Foundation.Numerics", "Matrix4x4")] = new("Matrix4x4", "System.Numerics", "Matrix4x4", false, false, false),
        [("Windows.Foundation.Numerics", "Plane")] = new("Plane", "System.Numerics", "Plane", false, false, false),
        [("Windows.Foundation.Numerics", "Quaternion")] = new("Quaternion", "System.Numerics", "Quaternion", false, false, false),
        [("Windows.Foundation.Numerics", "Vector2")] = new("Vector2", "System.Numerics", "Vector2", false, false, false),
        [("Windows.Foundation.Numerics", "Vector3")] = new("Vector3", "System.Numerics", "Vector3", false, false, false),
        [("Windows.Foundation.Numerics", "Vector4")] = new("Vector4", "System.Numerics", "Vector4", false, false, false),

        // -----------------------------------------------
        // Windows.Storage.Streams
        // -----------------------------------------------
        [("Windows.Storage.Streams", "IBuffer")] = new("IBuffer", "Windows.Storage.Streams", "IBuffer", false, false, false),
        [("Windows.Storage.Streams", "IInputStream")] = new("IInputStream", "Windows.Storage.Streams", "IInputStream", false, false, false),
        [("Windows.Storage.Streams", "IOutputStream")] = new("IOutputStream", "Windows.Storage.Streams", "IOutputStream", false, false, false),
        [("Windows.Storage.Streams", "IRandomAccessStream")] = new("IRandomAccessStream", "Windows.Storage.Streams", "IRandomAccessStream", false, false, false),
        [("Windows.Storage.Streams", "InputStreamOptions")] = new("InputStreamOptions", "Windows.Storage.Streams", "InputStreamOptions", false, false, false),

        // -----------------------------------------------
        // Windows.UI
        // -----------------------------------------------
        [("Windows.UI", "Color")] = new("Color", "Windows.UI", "Color", false, false, true),

        // -----------------------------------------------
        // Windows.UI.Xaml
        // -----------------------------------------------
        [("Windows.UI.Xaml", "CornerRadius")] = new("CornerRadius", "Windows.UI.Xaml", "CornerRadius", false, false, true),
        [("Windows.UI.Xaml", "CornerRadiusHelper")] = new("CornerRadiusHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "Duration")] = new("Duration", "Windows.UI.Xaml", "Duration", false, false, true),
        [("Windows.UI.Xaml", "DurationHelper")] = new("DurationHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "GridLength")] = new("GridLength", "Windows.UI.Xaml", "GridLength", false, false, true),
        [("Windows.UI.Xaml", "GridLengthHelper")] = new("GridLengthHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "ICornerRadiusHelper")] = new("ICornerRadiusHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "ICornerRadiusHelperStatics")] = new("ICornerRadiusHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml", "IDurationHelper")] = new("IDurationHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "IDurationHelperStatics")] = new("IDurationHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml", "IGridLengthHelper")] = new("IGridLengthHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "IGridLengthHelperStatics")] = new("IGridLengthHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml", "IThicknessHelper")] = new("IThicknessHelper", null, null, false, false, false),
        [("Windows.UI.Xaml", "IThicknessHelperStatics")] = new("IThicknessHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml", "IXamlServiceProvider")] = new("IXamlServiceProvider", "System", "IServiceProvider", false, false, false),
        [("Windows.UI.Xaml", "ThicknessHelper")] = new("ThicknessHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Controls.Primitives
        // -----------------------------------------------
        [("Windows.UI.Xaml.Controls.Primitives", "GeneratorPositionHelper")] = new("GeneratorPositionHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Controls.Primitives", "IGeneratorPositionHelper")] = new("IGeneratorPositionHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Controls.Primitives", "IGeneratorPositionHelperStatics")] = new("IGeneratorPositionHelperStatics", null, null, false, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Data
        // -----------------------------------------------
        [("Windows.UI.Xaml.Data", "DataErrorsChangedEventArgs")] = new("DataErrorsChangedEventArgs", "System.ComponentModel", "DataErrorsChangedEventArgs", false, false, false),
        [("Windows.UI.Xaml.Data", "INotifyDataErrorInfo")] = new("INotifyDataErrorInfo", "System.ComponentModel", "INotifyDataErrorInfo", true, true, false),
        [("Windows.UI.Xaml.Data", "INotifyPropertyChanged")] = new("INotifyPropertyChanged", "System.ComponentModel", "INotifyPropertyChanged", false, false, false),
        [("Windows.UI.Xaml.Data", "PropertyChangedEventArgs")] = new("PropertyChangedEventArgs", "System.ComponentModel", "PropertyChangedEventArgs", false, false, false),
        [("Windows.UI.Xaml.Data", "PropertyChangedEventHandler")] = new("PropertyChangedEventHandler", "System.ComponentModel", "PropertyChangedEventHandler", false, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Input
        // -----------------------------------------------
        [("Windows.UI.Xaml.Input", "ICommand")] = new("ICommand", "System.Windows.Input", "ICommand", true, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Interop
        // -----------------------------------------------
        [("Windows.UI.Xaml.Interop", "IBindableIterable")] = new("IBindableIterable", "System.Collections", "IEnumerable", true, true, false),
        [("Windows.UI.Xaml.Interop", "IBindableIterator")] = new("IBindableIterator", "System.Collections", "IEnumerator", true, true, false),
        [("Windows.UI.Xaml.Interop", "IBindableVector")] = new("IBindableVector", "System.Collections", "IList", true, true, false),
        [("Windows.UI.Xaml.Interop", "INotifyCollectionChanged")] = new("INotifyCollectionChanged", "System.Collections.Specialized", "INotifyCollectionChanged", true, false, false),
        [("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction")] = new("NotifyCollectionChangedAction", "System.Collections.Specialized", "NotifyCollectionChangedAction", false, false, false),
        [("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs")] = new("NotifyCollectionChangedEventArgs", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", true, false, false),
        [("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler")] = new("NotifyCollectionChangedEventHandler", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", true, false, false),
        [("Windows.UI.Xaml.Interop", "TypeKind")] = new("TypeKind", "Windows.UI.Xaml.Interop", "TypeKind", true, false, false),
        [("Windows.UI.Xaml.Interop", "TypeName")] = new("TypeName", "System", "Type", true, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Media
        // -----------------------------------------------
        [("Windows.UI.Xaml.Media", "IMatrixHelper")] = new("IMatrixHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Media", "IMatrixHelperStatics")] = new("IMatrixHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml.Media", "MatrixHelper")] = new("MatrixHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Media.Animation
        // -----------------------------------------------
        [("Windows.UI.Xaml.Media.Animation", "IKeyTimeHelper")] = new("IKeyTimeHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Animation", "IKeyTimeHelperStatics")] = new("IKeyTimeHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Animation", "IRepeatBehaviorHelper")] = new("IRepeatBehaviorHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Animation", "IRepeatBehaviorHelperStatics")] = new("IRepeatBehaviorHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Animation", "KeyTime")] = new("KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime", false, false, true),
        [("Windows.UI.Xaml.Media.Animation", "KeyTimeHelper")] = new("KeyTimeHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Animation", "RepeatBehavior")] = new("RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", false, false, true),
        [("Windows.UI.Xaml.Media.Animation", "RepeatBehaviorHelper")] = new("RepeatBehaviorHelper", null, null, false, false, false),

        // -----------------------------------------------
        // Windows.UI.Xaml.Media.Media3D
        // -----------------------------------------------
        [("Windows.UI.Xaml.Media.Media3D", "IMatrix3DHelper")] = new("IMatrix3DHelper", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Media3D", "IMatrix3DHelperStatics")] = new("IMatrix3DHelperStatics", null, null, false, false, false),
        [("Windows.UI.Xaml.Media.Media3D", "Matrix3D")] = new("Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", false, false, true),
        [("Windows.UI.Xaml.Media.Media3D", "Matrix3DHelper")] = new("Matrix3DHelper", null, null, false, false, false),
    }.ToFrozenDictionary();

    /// <summary>
    /// Tries to get the mapped type for a given WinRT type.
    /// </summary>
    /// <param name="namespaceName">The WinRT namespace of the type.</param>
    /// <param name="typeName">The WinRT type name (ABI name).</param>
    /// <returns>The <see cref="MappedType"/> if found; otherwise, <see langword="null"/>.</returns>
    public static MappedType? GetMappedType(string namespaceName, string typeName)
    {
        return Mappings.TryGetValue((namespaceName, typeName), out MappedType mapped) ? mapped : null;
    }

    /// <summary>
    /// Checks whether a type with the given mapped .NET namespace and name belongs to
    /// <c>System.Collections.ObjectModel</c> (i.e., it originates from one of the mapped
    /// <c>System.Collections.Specialized</c>, <c>System.ComponentModel</c>, or
    /// <c>System.Windows.Input</c> namespaces).
    /// </summary>
    /// <param name="namespaceName">The mapped .NET namespace of the type.</param>
    /// <param name="typeName">The mapped .NET type name.</param>
    /// <returns><see langword="true"/> if the type is in the System.ObjectModel mapping group; otherwise, <see langword="false"/>.</returns>
    public static bool IsMappedTypeInSystemObjectModel(string namespaceName, string typeName)
    {
        return namespaceName switch
        {
            "System.Collections.Specialized" => typeName is
                "INotifyCollectionChanged" or
                "NotifyCollectionChangedAction" or
                "NotifyCollectionChangedEventArgs" or
                "NotifyCollectionChangedEventHandler",
            "System.ComponentModel" => typeName is
                "INotifyDataErrorInfo" or
                "INotifyPropertyChanged" or
                "DataErrorsChangedEventArgs" or
                "PropertyChangedEventArgs" or
                "PropertyChangedEventHandler",
            "System.Windows.Input" => typeName is "ICommand",
            _ => false,
        };
    }

    /// <summary>
    /// Checks whether a type is in the <c>System.Numerics</c> vectors mapping group.
    /// </summary>
    /// <param name="namespaceName">The mapped .NET namespace of the type.</param>
    /// <returns><see langword="true"/> if the namespace is <c>System.Numerics</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsMappedTypeInSystemNumericsVectors(string namespaceName)
    {
        return namespaceName == "System.Numerics";
    }
}
