// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerationFingerprint"/>
internal partial class InteropGenerationFingerprint
{
    /// <summary>
    /// Formats each signature once, rather than repeatedly formatting it in sort comparisons.
    /// </summary>
    /// <param name="type">The signature to format.</param>
    /// <returns>The unambiguous, assembly-qualified name of <paramref name="type"/>.</returns>
    private string GetTypeName(TypeSignature type)
    {
        // Reuse names by signature object to avoid rebuilding them during dependency traversal and sorting
        if (!_typeNames.TryGetValue(type, out string? name))
        {
            DefaultInterpolatedStringHandler builder = new(0, 0, CultureInfo.InvariantCulture);

            // Prefix the signature kind so wrappers and their underlying types cannot share a key
            builder.AppendFormatted((int)type.ElementType);
            builder.AppendLiteral(":");

            switch (type)
            {
                // Include the generic definition and recursively scoped arguments, not just their display names
                case GenericInstanceTypeSignature generic:
                    builder.AppendFormatted(generic.IsValueType);
                    AppendDescriptor(ref builder, generic.GenericType);
                    builder.AppendFormatted(generic.TypeArguments.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature argument in generic.TypeArguments)
                    {
                        AppendPart(ref builder, GetTypeName(argument));
                    }

                    break;

                // Preserve modifier identity and array bounds in addition to the wrapped element type
                case TypeSpecificationSignature specification:
                    if (specification is CustomModifierTypeSignature modifier)
                    {
                        builder.AppendFormatted(modifier.IsRequired);
                        AppendDescriptor(ref builder, modifier.ModifierType);
                    }
                    else if (specification is ArrayTypeSignature array)
                    {
                        builder.AppendFormatted(array.Dimensions.Count);
                        builder.AppendLiteral(":");

                        foreach (ArrayDimension dimension in array.Dimensions)
                        {
                            AppendPart(ref builder, dimension.Size?.ToString(CultureInfo.InvariantCulture));
                            AppendPart(ref builder, dimension.LowerBound?.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    AppendPart(ref builder, GetTypeName(specification.BaseType));
                    break;

                // Type and method parameters can have the same index but represent different slots
                case GenericParameterSignature parameter:
                    builder.AppendFormatted((int)parameter.ParameterType);
                    builder.AppendLiteral(":");
                    builder.AppendFormatted(parameter.Index);
                    break;

                // Function pointer identity includes calling convention, return type, and parameter signatures
                case FunctionPointerTypeSignature pointer:
                    builder.AppendFormatted((int)pointer.Signature.Attributes);
                    builder.AppendLiteral(":");
                    builder.AppendFormatted(pointer.Signature.GenericParameterCount);
                    builder.AppendLiteral(":");
                    AppendPart(ref builder, GetTypeName(pointer.Signature.ReturnType));
                    builder.AppendFormatted(pointer.Signature.ParameterTypes.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature parameterType in pointer.Signature.ParameterTypes)
                    {
                        AppendPart(ref builder, GetTypeName(parameterType));
                    }

                    // Vararg parameters after the sentinel are distinct from the fixed parameter list
                    builder.AppendFormatted(pointer.Signature.IncludeSentinel);
                    builder.AppendFormatted(pointer.Signature.SentinelParameterTypes.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature parameterType in pointer.Signature.SentinelParameterTypes)
                    {
                        AppendPart(ref builder, GetTypeName(parameterType));
                    }

                    break;

                // Ordinary named types are identified by their resolved definition and assembly scope
                default:
                    AppendDescriptor(ref builder, type);
                    break;
            }

            // Share one string across equivalent signatures from different modules without allocating for a hit
            if (_uniqueTypeNames.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(builder.Text, out string? existingName))
            {
                name = existingName;

                builder.Clear();
            }
            else
            {
                name = builder.ToStringAndClear();

                _ = _uniqueTypeNames.Add(name);
            }

            _typeNames.Add(type, name);
        }

        return name;
    }

    /// <summary>
    /// Appends a fully scoped identity, distinguishing nested types from namespace-qualified names.
    /// </summary>
    /// <param name="builder">The target handler.</param>
    /// <param name="type">The type to format.</param>
    private void AppendDescriptor(ref DefaultInterpolatedStringHandler builder, ITypeDescriptor type)
    {
        // Discovery compares forwarded references by their resolved identity. Fingerprint the
        // same identity rather than whichever forwarding scope won a parallel dictionary insert.
        if (type.TryResolve(_state.RuntimeContext, out TypeDefinition? definition))
        {
            type = definition;
        }

        AppendPart(ref builder, type.Namespace);
        AppendPart(ref builder, type.Name);

        if (type.DeclaringType is { } declaringType)
        {
            builder.AppendLiteral("+");
            AppendDescriptor(ref builder, declaringType);
        }
        else
        {
            builder.AppendLiteral("@");
            AppendPart(ref builder, GetAssemblyName(type.Scope?.GetAssembly()));
        }
    }

    /// <summary>
    /// Length-prefixes names so unusual metadata identifiers cannot collide with separators.
    /// </summary>
    /// <param name="builder">The target handler.</param>
    /// <param name="value">The value to append, if available.</param>
    private static void AppendPart(ref DefaultInterpolatedStringHandler builder, string? value)
    {
        builder.AppendFormatted(value?.Length ?? -1);
        builder.AppendLiteral(":");
        builder.AppendFormatted(value);
    }

    /// <summary>
    /// Gets the canonical name of a resolved type definition.
    /// </summary>
    /// <param name="type">The type definition to format.</param>
    /// <returns>The unambiguous, assembly-qualified name of <paramref name="type"/>.</returns>
    private string GetDefinitionName(TypeDefinition type)
    {
        if (!_definitionNames.TryGetValue(type, out string? name))
        {
            name = GetTypeName(type.ToTypeSignature());
            _definitionNames.Add(type, name);
        }

        return name;
    }

    /// <summary>
    /// Formats an assembly identity only once per metadata object.
    /// </summary>
    /// <param name="assembly">The assembly to format, if available.</param>
    /// <returns>The full name of <paramref name="assembly"/>, or an empty string.</returns>
    private string GetAssemblyName(AssemblyDescriptor? assembly)
    {
        if (assembly is null)
        {
            return "";
        }

        if (!_assemblyNames.TryGetValue(assembly, out string? name))
        {
            name = assembly.FullName;
            _assemblyNames.Add(assembly, name);
        }

        return name;
    }
}
