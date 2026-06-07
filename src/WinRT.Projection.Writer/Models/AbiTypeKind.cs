// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Classification of a WinRT ABI type's marshalling shape, used to drive the writer's
/// decisions about how to emit converters and parameter handling.
/// </summary>
internal enum AbiTypeKind
{
    /// <summary>
    /// The shape could not be determined (e.g. unresolved reference).
    /// </summary>
    Unknown,

    /// <summary>
    /// A C# primitive type that is directly representable at the ABI (bool/byte/short/int/long/float/double/char + unsigned variants).
    /// </summary>
    BlittablePrimitive,

    /// <summary>
    /// A WinRT enum (marshalled as its underlying integer at the ABI).
    /// </summary>
    Enum,

    /// <summary>
    /// A WinRT struct whose fields are all blittable primitives or other blittable structs (no per-field marshalling needed).
    /// </summary>
    BlittableStruct,

    /// <summary>
    /// A WinRT struct with at least one reference-type field (string, generic instance, runtime class, etc.) that needs per-field marshalling via a generated <c>*Marshaller</c>.
    /// </summary>
    NonBlittableStruct,

    /// <summary>
    /// A WinRT struct that is mapped to a BCL value type and requires explicit marshalling (e.g. <c>Windows.Foundation.DateTime</c> -> <see cref="System.DateTimeOffset"/>).
    /// </summary>
    MappedAbiValueType,

    /// <summary>
    /// The corlib <see cref="string"/> primitive (marshalled via HSTRING).
    /// </summary>
    String,

    /// <summary>
    /// The corlib <see cref="object"/> primitive (marshalled via <c>WindowsRuntimeObjectMarshaller</c>).
    /// </summary>
    Object,

    /// <summary>
    /// A WinRT runtime class or interface (marshalled via the type's generated <c>*Marshaller</c>).
    /// </summary>
    RuntimeClassOrInterface,

    /// <summary>
    /// A WinRT delegate type (marshalled via the delegate's generated <c>*Marshaller</c>).
    /// </summary>
    Delegate,

    /// <summary>
    /// A generic instantiation that requires <c>WinRT.Interop</c> <c>UnsafeAccessor</c>-based marshalling.
    /// </summary>
    GenericInstance,

    /// <summary>
    /// A <see cref="System.Nullable{T}"/> / WinRT <c>IReference&lt;T&gt;</c> instantiation.
    /// </summary>
    NullableT,

    /// <summary>
    /// The <see cref="System.Type"/> projected type (mapped from the WinMD <c>Windows.UI.Xaml.Interop.TypeName</c> struct).
    /// </summary>
    SystemType,

    /// <summary>
    /// The <see cref="System.Exception"/> / <c>Windows.Foundation.HResult</c> pair, marshalled via <c>ABI.System.ExceptionMarshaller</c>.
    /// </summary>
    HResultException,

    /// <summary>
    /// A single-dimensional array.
    /// </summary>
    Array,
}
