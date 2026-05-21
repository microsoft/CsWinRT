// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Categorization of a Windows Runtime type definition.
/// </summary>
internal enum TypeKind
{
    Interface,
    Class,
    Enum,
    Struct,
    Delegate,
}
