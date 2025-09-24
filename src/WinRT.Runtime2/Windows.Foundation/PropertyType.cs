// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Specifies property value types.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the value types and delegates, but marshalling it is not supported.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.propertytype"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[EditorBrowsable(EditorBrowsableState.Never)]
public enum PropertyType : uint
{
    /// <summary>
    /// No type is specified.
    /// </summary>
    Empty = 0,

    /// <summary>
    /// A byte.
    /// </summary>
    /// <remarks>Maps to <see cref="byte"/>.</remarks>
    UInt8 = 1,

    /// <summary>
    /// A signed 16-bit (2-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="short"/>.</remarks>
    Int16 = 2,

    /// <summary>
    /// An unsigned 16-bit (2-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="ushort"/>.</remarks>
    UInt16 = 3,

    /// <summary>
    /// A signed 32-bit (4-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="int"/>.</remarks>
    Int32 = 4,

    /// <summary>
    /// An unsigned 32-bit (4-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="uint"/>.</remarks>
    UInt32 = 5,

    /// <summary>
    /// A signed 64-bit (8-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="long"/>.</remarks>
    Int64 = 6,

    /// <summary>
    /// An unsigned 64-bit (8-byte) integer.
    /// </summary>
    /// <remarks>Maps to <see cref="ulong"/>.</remarks>
    UInt64 = 7,

    /// <summary>
    /// A signed 32-bit (4-byte) floating-point number.
    /// </summary>
    /// <remarks>Maps to <see cref="float"/>.</remarks>
    Single = 8,

    /// <summary>
    /// A signed 64-bit (8-byte) floating-point number.
    /// </summary>
    /// <remarks>Maps to <see cref="double"/>.</remarks>
    Double = 9,

    /// <summary>
    /// An unsigned 16-bit (2-byte) code point.
    /// </summary>
    /// <remarks>Maps to <see cref="char"/>.</remarks>
    Char16 = 10,

    /// <summary>
    /// A value that can be only true or false.
    /// </summary>
    /// <remarks>Maps to <see cref="bool"/>.</remarks>
    Boolean = 11,

    /// <summary>
    /// A Windows Runtime <c>HSTRING</c>.
    /// </summary>
    /// <remarks>Maps to <see cref="string"/>.</remarks>
    String = 12,

    /// <summary>
    /// An object implementing the <c>IInspectable</c> interface.
    /// </summary>
    /// <remarks>Maps to <see cref="object"/>.</remarks>
    Inspectable = 13,

    /// <summary>
    /// An instant in time, typically expressed as a date and time of day.
    /// </summary>
    /// <remarks>Maps to <see cref="System.DateTimeOffset"/>.</remarks>
    DateTime = 14,

    /// <summary>
    /// A time interval.
    /// </summary>
    /// <remarks>Maps to <see cref="System.TimeSpan"/>.</remarks>
    TimeSpan = 15,

    /// <summary>
    /// A globally unique identifier.
    /// </summary>
    /// <remarks>Maps to <see cref="System.Guid"/>.</remarks>
    Guid = 16,

    /// <summary>
    /// An ordered pair of floating-point x- and y-coordinates that defines a point in a two-dimensional plane.
    /// </summary>
    /// <remarks>Maps to <see cref="Foundation.Point"/>.</remarks>
    Point = 17,

    /// <summary>
    /// An ordered pair of float-point numbers that specify a height and width.
    /// </summary>
    /// <remarks>Maps to <see cref="Foundation.Size"/>.</remarks>
    Size = 18,

    /// <summary>
    /// A set of four floating-point numbers that represent the location and size of a rectangle.
    /// </summary>
    /// <remarks>Maps to <see cref="Foundation.Rect"/>.</remarks>
    Rect = 19,

    /// <summary>
    /// A type not specified in this enumeration.
    /// </summary>
    OtherType = 20,

    /// <summary>
    /// An array of Byte values.
    /// </summary>
    UInt8Array = UInt8 | 1024,

    /// <summary>
    /// An array of Int16 values.
    /// </summary>
    Int16Array = Int16 | 1024,

    /// <summary>
    /// An array of UInt16 values.
    /// </summary>
    UInt16Array = UInt16 | 1024,

    /// <summary>
    /// An array of Int32 values.
    /// </summary>
    Int32Array = Int32 | 1024,

    /// <summary>
    /// An array of UInt32 values.
    /// </summary>
    UInt32Array = UInt32 | 1024,

    /// <summary>
    /// An array of Int64 values.
    /// </summary>
    Int64Array = Int64 | 1024,

    /// <summary>
    /// An array of UInt64 values.
    /// </summary>
    UInt64Array = UInt64 | 1024,

    /// <summary>
    /// An array of Single values.
    /// </summary>
    SingleArray = Single | 1024,

    /// <summary>
    /// An array of Double values.
    /// </summary>
    DoubleArray = Double | 1024,

    /// <summary>
    /// An array of Char values.
    /// </summary>
    Char16Array = Char16 | 1024,

    /// <summary>
    /// An array of Boolean values.
    /// </summary>
    BooleanArray = Boolean | 1024,

    /// <summary>
    /// An array of String values.
    /// </summary>
    StringArray = String | 1024,

    /// <summary>
    /// An array of Inspectable values.
    /// </summary>
    InspectableArray = Inspectable | 1024,

    /// <summary>
    /// An array of DateTime values.
    /// </summary>
    DateTimeArray = DateTime | 1024,

    /// <summary>
    /// An array of TimeSpan values.
    /// </summary>
    TimeSpanArray = TimeSpan | 1024,

    /// <summary>
    /// An array of Guid values.
    /// </summary>
    GuidArray = Guid | 1024,

    /// <summary>
    /// An array of Point structures.
    /// </summary>
    PointArray = Point | 1024,

    /// <summary>
    /// An array of Size structures.
    /// </summary>
    SizeArray = Size | 1024,

    /// <summary>
    /// An array of Rect structures.
    /// </summary>
    RectArray = Rect | 1024,

    /// <summary>
    /// An array of an unspecified type.
    /// </summary>
    OtherTypeArray = OtherType | 1024
}
