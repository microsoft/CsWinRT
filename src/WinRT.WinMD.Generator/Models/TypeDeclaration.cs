// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Tracks the input and output type definitions for a given type.
/// </summary>
internal sealed class TypeDeclaration
{
    /// <summary>The source type from the input assembly.</summary>
    public TypeDefinition? InputType { get; }

    /// <summary>The generated type in the output WinMD.</summary>
    public TypeDefinition? OutputType { get; }

    /// <summary>Whether this type is a component type (authored by the user).</summary>
    public bool IsComponentType { get; }

    /// <summary>The name of the default synthesized interface.</summary>
    public string? DefaultInterface { get; set; }

    /// <summary>The name of the static synthesized interface.</summary>
    public string? StaticInterface { get; set; }

    public TypeDeclaration(TypeDefinition? inputType, TypeDefinition? outputType, bool isComponentType = false)
    {
        InputType = inputType;
        OutputType = outputType;
        IsComponentType = isComponentType;
    }
}