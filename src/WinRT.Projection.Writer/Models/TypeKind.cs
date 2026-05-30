// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Indicates the kind of a given type definition.
/// </summary>
internal enum TypeKind
{
    /// <summary>
    /// The type is an interface.
    /// </summary>
    Interface,

    /// <summary>
    /// The type is a class (including static classes and runtime classes).
    /// </summary>
    Class,

    /// <summary>
    /// The type is an enum.
    /// </summary>
    Enum,

    /// <summary>
    /// The type is a struct (a non-enum value type).
    /// </summary>
    Struct,

    /// <summary>
    /// The type is a delegate.
    /// </summary>
    Delegate,
}
