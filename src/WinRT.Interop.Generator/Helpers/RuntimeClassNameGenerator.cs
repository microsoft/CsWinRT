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
    /// <param name="runtimeContext">The context to assume when resolving the type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting Windows Runtime class name for <paramref name="type"/>.</returns>
    public static string GetRuntimeClassName(TypeSignature type, RuntimeContext? runtimeContext, bool useWindowsUIXamlProjections)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        // We need to be able to resolve the type definition to determine whether we need
        // to wrap the metadata type name within "IReference`1<...>" (e.g. for delegates).
        if (!type.TryResolve(runtimeContext, out TypeDefinition? typeDefinition))
        {
            throw WellKnownInteropExceptions.RuntimeClassNameGenerationError(type);
        }

        // We need to wrap the metadata name with "IReference`1<...>" only for non-generic value types, and for delegate
        // types. We skip generic value types because the only possible type is 'KeyValuePair<TKey, TValue>', but that is
        // a Windows Runtime interface. We also need to make sure to skip SZ arrays manually. Note that when resolving the
        // signature of an SZ array type, we'd just get a definition for the array element type. So we need to manually
        // check for that scenario via the original type signature directly to make sure to not accidentally mark arrays
        // of some type that would require reference support, as needing reference support themselves.
        bool isReference = type is not SzArrayTypeSignature && ((type.IsValueType && !type.IsGenericType) || typeDefinition.IsDelegate);

        if (isReference)
        {
            handler.AppendLiteral("Windows.Foundation.IReference`1<");
        }

        // Aside from the possible "IReference`1<...>" prefix, the runtime class name will be the same
        // as the metadata type name for all cases, so here we just forward to that to generate it.
        MetadataTypeNameGenerator.AppendMetadataTypeName(ref handler, type, useWindowsUIXamlProjections);

        if (isReference)
        {
            handler.AppendLiteral(">");

        }

        return handler.ToStringAndClear();
    }
}
