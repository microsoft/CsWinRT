// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop UTF8 type names.
/// </summary>
internal static class InteropUtf8NameFactory
{
    /// <summary>
    /// Gets the namespace for a generated interop type.
    /// </summary>
    /// <param name="typeSignature">The source <see cref="TypeSignature"/> for the type to generate.</param>
    /// <returns>The namespace to use.</returns>
    public static Utf8String TypeNamespace(TypeSignature typeSignature)
    {
        // Special case for nested types: these don't have a namespace, as the namespace is only
        // on the declaring type. So, find the outer-most declaring type to read the namespace.
        if (typeSignature.TopLevelDeclaringType is ITypeDescriptor declaringType)
        {
            return TypeNamespace(declaringType.ToTypeSignature());
        }

        // Special case when there's no namespace: just put the generated types under 'ABI'.
        // Otherwise, we just add the "ABI." prefix to the namespace of the input type.
        if (typeSignature.Namespace?.Length is not > 0)
        {
            return new("ABI"u8);
        }

        // Interpolate into 'DefaultInterpolatedStringHandler' to access the buffer directly.
        // This allows this method to avoid materializing a temporary 'string' every time.
        DefaultInterpolatedStringHandler interpolatedStringHandler = $"ABI.{typeSignature.Namespace}";

        Utf8String result = new(interpolatedStringHandler.Text);

        // Clear the handler before returning, so the rented array can be reused
        interpolatedStringHandler.Clear();

        return result;
    }

    /// <summary>
    /// Gets the name for a generated interop type.
    /// </summary>
    /// <param name="typeSignature">The source <see cref="TypeSignature"/> for the type to generate.</param>
    /// <param name="nameSuffix">The optional name suffix to use.</param>
    /// <returns>The name to use.</returns>
    public static Utf8String TypeName(TypeSignature typeSignature, string? nameSuffix = null)
    {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new(literalLength: 2, formattedCount: 1);

        // Helper to recursively build the type name (to handle nested generic types too)
        static void AppendTypeName(
            ref DefaultInterpolatedStringHandler interpolatedStringHandler,
            TypeSignature typeSignature,
            int depth)
        {
            // Special case for well known type identifiers (eg. 'string', 'int', etc.)
            if (TryGetWellKnownIdentifier(typeSignature, out Utf8String? identifierUtf8))
            {
                interpolatedStringHandler.AppendFormatted(identifierUtf8);

                return;
            }

            // SZ arrays are enclosed in angle brackets
            if (typeSignature is SzArrayTypeSignature)
            {
                interpolatedStringHandler.AppendLiteral("<");
            }

            // Each type name uses this format: '<ASSEMBLY_NAME>TYPE_NAME'
            interpolatedStringHandler.AppendLiteral("<");
            interpolatedStringHandler.AppendFormatted(AssemblyNameOrWellKnownIdentifier(typeSignature.Scope!.GetAssembly()!.Name, typeSignature));
            interpolatedStringHandler.AppendLiteral(">");

            // If the type is generic, append the definition name and the type arguments
            if (typeSignature is GenericInstanceTypeSignature genericInstanceTypeSignature)
            {
                // Append the name of the generic type, without any type arguments (those are appended below)
                AppendRawTypeName(ref interpolatedStringHandler, genericInstanceTypeSignature.GenericType, depth);

                interpolatedStringHandler.AppendLiteral("<");

                foreach ((int i, TypeSignature typeArgumentSignature) in genericInstanceTypeSignature.TypeArguments.Index())
                {
                    // We use '|' to separate generic type arguments
                    if (i > 0)
                    {
                        interpolatedStringHandler.AppendLiteral("|");
                    }

                    // Append the type argument with the same format as the root type. This is
                    // important to ensure that nested generic types will be handled correctly.
                    AppendTypeName(ref interpolatedStringHandler, typeArgumentSignature, depth: depth + 1);
                }

                interpolatedStringHandler.AppendLiteral(">");
            }
            else if (typeSignature is SzArrayTypeSignature arrayTypeSignature)
            {
                // Same as below, but we use the element type name, not the array type name
                AppendRawTypeName(ref interpolatedStringHandler, arrayTypeSignature.BaseType, depth);
            }
            else
            {
                // Simple case for normal type signatures
                AppendRawTypeName(ref interpolatedStringHandler, typeSignature, depth);
            }

            // Complete the name mangling for SZ arrays
            if (typeSignature is SzArrayTypeSignature)
            {
                interpolatedStringHandler.AppendLiteral(">Array");
            }
        }

        // Helper to recursively build the type name (to handle nested generic types too)
        static void AppendRawTypeName(
            ref DefaultInterpolatedStringHandler interpolatedStringHandler,
            IMemberDescriptor type,
            int depth)
        {
            // We can skip the namespace when the indentation level is '0', as that means
            // the type will already be defined in the same namespace (so we can omit it).
            if (depth > 0)
            {
                interpolatedStringHandler.AppendLiteral(type.FullName);
            }
            else if (type.DeclaringType is null)
            {
                // Fast path when we have a top level type
                interpolatedStringHandler.AppendLiteral(type.Name!);
            }
            else
            {
                // Helper to recursively print all parts
                static void AppendRawTypeName(
                    ref DefaultInterpolatedStringHandler interpolatedStringHandler,
                    ITypeDescriptor typeDescriptor)
                {
                    if (typeDescriptor.DeclaringType is ITypeDescriptor declaringType)
                    {
                        AppendRawTypeName(ref interpolatedStringHandler, declaringType);
                    }

                    interpolatedStringHandler.AppendLiteral(typeDescriptor.Name!);
                    interpolatedStringHandler.AppendLiteral("+");
                }

                // If the type is nested, we need to print all declaring types in pre-order, top-down
                AppendRawTypeName(ref interpolatedStringHandler, type.DeclaringType);

                interpolatedStringHandler.AppendLiteral(type.Name!);
            }
        }

        // Append the full type name first
        AppendTypeName(ref interpolatedStringHandler, typeSignature, depth: 0);

        // Append the suffix, if we have one
        interpolatedStringHandler.AppendFormatted(nameSuffix);

        // Replace '.' characters with '-' instead, to avoid type name parsing issues.
        // Also replace '`' characters, as using backticks breaks some tooling (eg. ILSpy),
        // as the backtick character is assumed to indicate that a type is generic.
        interpolatedStringHandler.Text.AsSpanUnsafe().Replace('.', '-');
        interpolatedStringHandler.Text.AsSpanUnsafe().Replace('`', '\'');

        // Like above, create the final UTF8 string without materializing a temporary 'string'
        Utf8String result = new(interpolatedStringHandler.Text);

        // Return the rented buffer to the pool
        interpolatedStringHandler.Clear();

        return result;
    }

    /// <summary>
    /// Gets the assembly name or a well known identifier based on the assembly name or the given type signature.
    /// </summary>
    /// <param name="assemblyName">The input assembly name to convert.</param>
    /// <param name="typeSignature">The type signature for which to convert.</param>
    /// <returns>The resulting assembly name to use.</returns>
    [return: NotNullIfNotNull(nameof(assemblyName))]
    private static Utf8String? AssemblyNameOrWellKnownIdentifier(Utf8String? assemblyName, TypeSignature typeSignature)
    {
        // Replace some assembly names with well known constants, to make the names more compact
        return assemblyName switch
        {
            { Value: "System.Runtime" } => "#corlib"u8,
            { Value: "Microsoft.Windows.SDK.NET" or "Microsoft.Windows.UI.Xaml" } => "#Windows"u8,
            { Value: "WinRT.Runtime" } => "#CsWinRT"u8,
            { Value: "WinRT.Runtime2" } => "#CsWinRT"u8,
            _ => typeSignature.GetWindowsRuntimeMetadataName() ?? assemblyName
        };
    }

    /// <summary>
    /// Tries to get the well known identifier for a given type signature.
    /// </summary>
    /// <param name="typeSignature">The input type signature.</param>
    /// <param name="identifier">The resulting well known identifier, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="identifier"/> was successfully retrieved.</returns>
    [UnconditionalSuppressMessage("Style", "IDE0072", Justification = "We only special case some known primitive types.")]
    private static bool TryGetWellKnownIdentifier(TypeSignature typeSignature, [NotNullWhen(true)] out Utf8String? identifier)
    {
        if (typeSignature is CorLibTypeSignature corLibTypeSignature)
        {
            // If the type is a corlib type, we can use the well known identifier
            identifier = corLibTypeSignature.ElementType switch
            {
                ElementType.Boolean => "bool"u8,
                ElementType.Char => "char"u8,
                ElementType.I1 => "sbyte"u8,
                ElementType.U1 => "byte"u8,
                ElementType.I2 => "short"u8,
                ElementType.U2 => "ushort"u8,
                ElementType.I4 => "int"u8,
                ElementType.U4 => "uint"u8,
                ElementType.I8 => "long"u8,
                ElementType.U8 => "ulong"u8,
                ElementType.R4 => "float"u8,
                ElementType.R8 => "double"u8,
                ElementType.String => "string"u8,
                ElementType.Object => "object"u8,
                _ => null
            };

            return identifier is not null;
        }

        identifier = null;

        return false;
    }
}
