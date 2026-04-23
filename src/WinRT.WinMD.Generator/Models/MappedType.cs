// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Models;

/// <summary>
/// A mapped type from a .NET type to a Windows Runtime type.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="MappedType"/> instance represents a single .NET → Windows Runtime type mapping entry.
/// These mappings are used by <see cref="Helpers.TypeMapper"/> to translate .NET type references
/// in the input assembly to their Windows Runtime equivalents in the output WinMD.
/// </para>
/// <para>
/// Most mappings are fixed (e.g. <c>System.DateTimeOffset</c> → <c>Windows.Foundation.DateTime</c>),
/// but some require context-dependent resolution. For example, <see cref="Type"/> maps to
/// <c>Windows.UI.Xaml.Interop.TypeName</c> in most contexts, but stays as <see cref="Type"/> when
/// used in attribute declarations (since WinMD attribute blobs use CLR types).
/// </para>
/// </remarks>
internal readonly struct MappedType
{
    /// <summary>
    /// The Windows Runtime namespace for fixed mappings, or <see langword="null"/> for context-dependent mappings.
    /// </summary>
    private readonly string? _namespace;

    /// <summary>
    /// The Windows Runtime type name for fixed mappings, or <see langword="null"/> for context-dependent mappings.
    /// </summary>
    private readonly string? _name;

    /// <summary>
    /// The Windows Runtime contract assembly name for fixed mappings, or <see langword="null"/> for context-dependent mappings.
    /// </summary>
    private readonly string? _assembly;

    /// <summary>
    /// Whether this mapped type is a system/CLR type (i.e. from <c>mscorlib</c>).
    /// </summary>
    private readonly bool _isSystemType;

    /// <summary>
    /// Whether this mapped type is a value type.
    /// </summary>
    private readonly bool _isValueType;

    /// <summary>
    /// Whether this mapped type is blittable (can be directly copied between managed and native memory).
    /// </summary>
    private readonly bool _isBlittable;

    /// <summary>
    /// The delegate for context-dependent mappings, or <see langword="null"/> for fixed mappings.
    /// </summary>
    private readonly Func<TypeDefinition?, (string, string, string, bool, bool)>? _multipleMappingFunc;

    /// <summary>
    /// Creates a new <see cref="MappedType"/> with a fixed mapping.
    /// </summary>
    /// <param name="namespace">The Windows Runtime namespace.</param>
    /// <param name="name">The Windows Runtime type name.</param>
    /// <param name="assembly">The Windows Runtime contract assembly name.</param>
    /// <param name="isValueType">Whether this mapped type is a value type.</param>
    /// <param name="isBlittable">Whether this mapped type is blittable.</param>
    public MappedType(string @namespace, string name, string assembly, bool isValueType = false, bool isBlittable = false)
    {
        _namespace = @namespace;
        _name = name;
        _assembly = assembly;
        _isSystemType = assembly == "mscorlib";
        _isValueType = isValueType;
        _isBlittable = isBlittable;
        _multipleMappingFunc = null;
    }

    /// <summary>
    /// Creates a new <see cref="MappedType"/> with a context-dependent mapping.
    /// </summary>
    /// <param name="multipleMappingFunc">
    /// A delegate that resolves the mapping based on the containing <see cref="TypeDefinition"/>.
    /// This is used for types like <see cref="Type"/> that map differently depending on context.
    /// </param>
    public MappedType(Func<TypeDefinition?, (string, string, string, bool, bool)> multipleMappingFunc)
    {
        _namespace = null;
        _name = null;
        _assembly = null;
        _isSystemType = false;
        _isValueType = false;
        _isBlittable = false;
        _multipleMappingFunc = multipleMappingFunc;
    }

    /// <summary>
    /// Gets the mapping tuple (namespace, name, assembly, isSystemType, isValueType).
    /// </summary>
    /// <param name="containingType">The optional containing type for context-dependent mappings.</param>
    /// <returns>A tuple with the resolved Windows Runtime type information.</returns>
    public (string Namespace, string Name, string Assembly, bool IsSystemType, bool IsValueType) GetMapping(TypeDefinition? containingType = null)
    {
        return _multipleMappingFunc is not null
            ? _multipleMappingFunc(containingType)
            : (_namespace!, _name!, _assembly!, _isSystemType, _isValueType);
    }

    /// <summary>
    /// Gets whether the mapped type is blittable.
    /// </summary>
    /// <returns><see langword="true"/> if the mapped type is a blittable value type; otherwise, <see langword="false"/>.</returns>
    public bool IsBlittable()
    {
        return _isValueType && _isBlittable;
    }
}
