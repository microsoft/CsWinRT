// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Classification of an SZ-array element's ABI marshalling shape. Used by the per-array-element
/// pointer-type ladder in <c>RcwCaller</c> / <c>DoAbi</c> bodies so callers can dispatch via
/// <c>switch</c> instead of an if-else-if chain.
/// </summary>
internal enum AbiArrayElementKind
{
    /// <summary>The element flows as a <c>void*</c> at the ABI (strings, runtime classes/interfaces, objects).</summary>
    RefLikeVoidStar,

    /// <summary>The element is <see cref="System.Exception"/> (mapped from WinRT <c>HResult</c>).</summary>
    HResultException,

    /// <summary>The element is a mapped value type (e.g. WinRT <c>DateTime</c> -> <see cref="System.DateTimeOffset"/>).</summary>
    MappedValueType,

    /// <summary>The element is a complex struct (has reference-typed fields that require per-field marshalling).</summary>
    ComplexStruct,

    /// <summary>The element is a blittable struct (all fields are primitives or blittable structs).</summary>
    BlittableStruct,

    /// <summary>The element is a blittable primitive (<see cref="bool"/>, <see cref="int"/>, etc.).</summary>
    BlittablePrimitive,
}