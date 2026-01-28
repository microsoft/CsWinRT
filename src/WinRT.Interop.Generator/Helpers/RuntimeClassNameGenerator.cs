// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for runtime class names of Windows Runtime types, or user-defined types exposed to native consumers.
/// </summary>
internal static class RuntimeClassNameGenerator
{
    /// <summary>
    /// Generates the Windows Runtime class name for a (potentially generic) type,
    /// applying known type-name mappings and recursively formatting generic arguments.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the Windows Runtime class name for.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting Windows Runtime class name for <paramref name="type"/>.</returns>
    public static string GetRuntimeClassName(TypeSignature type, bool useWindowsUIXamlProjections)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        // We need to be able to resolve the type definition to determine whether we need
        // to wrap the metadata type name within "IReference`1<...>" (e.g. for delegates).
        if (type.Resolve() is not TypeDefinition typeDefinition)
        {
            throw WellKnownInteropExceptions.RuntimeClassNameGenerationError(type);
        }

        // We need to wrap the metadata name with "IReference`1<...>" only for non-generic
        // value types, and for delegate types. We skip generic value types because the only
        // possible type is 'KeyValuePair<TKey, TValue>', but that is a Windows Runtime interface.
        bool isReference = (type.IsValueType && !type.IsGenericType) || typeDefinition.IsDelegate;

        if (isReference)
        {
            handler.AppendLiteral("Windows.Foundation.IReference`1<");
        }

        // Aside from the possible "IReference`1<...>" prefix, the runtime class name will be the same
        // as the metadata type name for all cases, so here we just forward to that to generate it.
        MetadataTypeNameGenerator.AppendMetadataTyoeName(ref handler, type, useWindowsUIXamlProjections);

        if (isReference)
        {
            handler.AppendLiteral(">");

        }

        return handler.ToStringAndClear();
    }
}
