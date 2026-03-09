// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionGenerator.Models;

/// <summary>
/// Represents the category of a Windows Runtime type definition.
/// </summary>
internal enum TypeCategory
{
    /// <summary>An interface type.</summary>
    Interface,

    /// <summary>A runtime class type.</summary>
    Class,

    /// <summary>An enum type.</summary>
    Enum,

    /// <summary>A struct type.</summary>
    Struct,

    /// <summary>A delegate type.</summary>
    Delegate,

    /// <summary>An attribute type.</summary>
    Attribute,

    /// <summary>An API contract type.</summary>
    ApiContract
}
