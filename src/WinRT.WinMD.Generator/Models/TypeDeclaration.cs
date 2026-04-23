// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Models;

/// <summary>
/// Tracks the input and output type definitions for a given type during WinMD generation.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="TypeDeclaration"/> pairs an input <see cref="TypeDefinition"/> (from the compiled assembly)
/// with its corresponding output <see cref="TypeDefinition"/> (in the generated WinMD). For synthesized types
/// (like <c>IFooClass</c>, <c>IFooFactory</c>), the <see cref="InputType"/> is <see langword="null"/> since
/// they have no counterpart in the input assembly.
/// </para>
/// <para>
/// The <see cref="DefaultInterface"/> and <see cref="StaticInterface"/> properties track the synthesized
/// interfaces associated with a runtime class, which are used during the finalization phase to wire up
/// <c>MethodImpl</c> entries.
/// </para>
/// </remarks>
internal sealed class TypeDeclaration
{
    /// <summary>
    /// Gets the source type from the input assembly, or <see langword="null"/> for synthesized types.
    /// </summary>
    public TypeDefinition? InputType { get; }

    /// <summary>
    /// Gets the generated type in the output WinMD.
    /// </summary>
    public TypeDefinition? OutputType { get; }

    /// <summary>
    /// Gets whether this type is a component type (authored by the user, as opposed to synthesized).
    /// </summary>
    public bool IsComponentType { get; }

    /// <summary>
    /// Gets or sets the fully-qualified name of the default synthesized interface (e.g., <c>"Namespace.IFooClass"</c>).
    /// </summary>
    public string? DefaultInterface { get; set; }

    /// <summary>
    /// Gets or sets the fully-qualified name of the static synthesized interface (e.g., <c>"Namespace.IFooStatic"</c>).
    /// </summary>
    public string? StaticInterface { get; set; }

    /// <summary>
    /// Creates a new <see cref="TypeDeclaration"/> instance.
    /// </summary>
    /// <param name="inputType">The source type from the input assembly, or <see langword="null"/> for synthesized types.</param>
    /// <param name="outputType">The generated type in the output WinMD.</param>
    /// <param name="isComponentType">Whether this type is a component type (user-authored).</param>
    public TypeDeclaration(TypeDefinition? inputType, TypeDefinition? outputType, bool isComponentType = false)
    {
        InputType = inputType;
        OutputType = outputType;
        IsComponentType = isComponentType;
    }
}