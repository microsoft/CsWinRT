// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for IID map keys in <c>[Assembly]Namespace.Type[arguments]</c> format.
/// </summary>
internal static class WindowsRuntimeIidTypeNameFactory
{
    /// <summary>
    /// Gets an IID map key using logical System identities and implementation projection assemblies.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="interopDefinitions">The projection definitions.</param>
    /// <param name="interopReferences">The references for this invocation.</param>
    /// <returns>The assembly-qualified structural type name.</returns>
    public static string GetName(
        TypeSignature type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences)
    {
        void Append(ref DefaultInterpolatedStringHandler handler, TypeSignature type)
        {
            if (type is SzArrayTypeSignature array)
            {
                Append(ref handler, array.BaseType);
                handler.AppendLiteral("[]");

                return;
            }

            ITypeDescriptor descriptor = type is GenericInstanceTypeSignature generic ? generic.GenericType : type;

            // Match the runtime formatter's identity checks without assuming any framework DLL placement
            bool isSystemType = type.IsFundamentalOrCustomMappedWindowsRuntimeType(interopReferences);
            TypeDefinition definition = descriptor.Resolve(interopReferences.RuntimeContext);
            string assemblyName = isSystemType ? "#System" : GetRuntimeAssemblyName(definition, interopDefinitions);

            handler.AppendFormatted('[');

            // Literal assembly names cannot occupy the reserved logical scope
            if (!isSystemType && assemblyName.StartsWith('#'))
            {
                handler.AppendFormatted('#');
            }

            handler.AppendLiteral(assemblyName);
            handler.AppendFormatted(']');
            handler.AppendLiteral(definition.FullName);

            if (type is GenericInstanceTypeSignature instance)
            {
                handler.AppendFormatted('[');

                for (int index = 0; index < instance.TypeArguments.Count; index++)
                {
                    if (index != 0)
                    {
                        handler.AppendFormatted(',');
                    }

                    Append(ref handler, instance.TypeArguments[index]);
                }

                handler.AppendFormatted(']');
            }
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        Append(ref handler, type);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Gets the implementation assembly associated with a type.
    /// </summary>
    /// <param name="type">The resolved type.</param>
    /// <param name="interopDefinitions">The projection definitions.</param>
    /// <returns>The runtime assembly's simple name.</returns>
    private static string GetRuntimeAssemblyName(TypeDefinition type, InteropDefinitions interopDefinitions)
    {
        // Distributed reference projections forward their types into the private implementation projection
        return type.IsReferenceProjectionWindowsRuntimeType && type.GetImplementationProjectionModule(interopDefinitions) is { } projectionModule
            ? projectionModule.Assembly!.Name!
            : type.DeclaringModule!.Assembly!.Name!;
    }
}
