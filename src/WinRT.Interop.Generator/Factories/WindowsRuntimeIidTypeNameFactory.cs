// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
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
    /// Framework types whose runtime assembly differs from their reference assembly.
    /// </summary>
    /// <remarks>
    /// This only covers framework types supported by the Windows Runtime type system.
    /// Other framework mappings retain their defining assembly's simple name.
    /// </remarks>
    private static readonly FrozenSet<string> CoreLibTypes = FrozenSet.Create(StringComparer.Ordinal,
        "System.Boolean", "System.Byte", "System.Char", "System.Double", "System.Single",
        "System.Int16", "System.Int32", "System.Int64", "System.UInt16", "System.UInt32", "System.UInt64",
        "System.Guid", "System.Object", "System.String", "System.Exception", "System.Type",
        "System.DateTimeOffset", "System.TimeSpan", "System.IDisposable", "System.Nullable`1",
        "System.EventHandler", "System.EventHandler`1", "System.EventHandler`2",
        "System.Collections.IEnumerable", "System.Collections.IEnumerator", "System.Collections.IList",
        "System.Collections.Generic.IEnumerable`1", "System.Collections.Generic.IEnumerator`1",
        "System.Collections.Generic.IList`1", "System.Collections.Generic.IReadOnlyList`1",
        "System.Collections.Generic.IDictionary`2", "System.Collections.Generic.IReadOnlyDictionary`2",
        "System.Collections.Generic.KeyValuePair`2",
        "System.Numerics.Matrix3x2", "System.Numerics.Matrix4x4", "System.Numerics.Plane",
        "System.Numerics.Quaternion", "System.Numerics.Vector2", "System.Numerics.Vector3", "System.Numerics.Vector4");

    /// <summary>
    /// Gets an IID map key using runtime assembly identities rather than reference-assembly facades.
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
            TypeDefinition definition = descriptor.Resolve(interopReferences.RuntimeContext);
            string assemblyName = GetRuntimeAssemblyName(definition, interopDefinitions);

            handler.AppendFormatted('[');
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

        DefaultInterpolatedStringHandler handler = new(0, 0, null, initialBuffer: stackalloc char[256]);

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
        if (type.DeclaringModule!.IsBaseClassLibraryModule)
        {
            if (CoreLibTypes.Contains(type.FullName))
            {
                return "System.Private.CoreLib";
            }

            if (type.FullName == "System.Uri")
            {
                return "System.Private.Uri";
            }
        }

        // Distributed reference projections forward their types into the private implementation projection.
        return type.IsReferenceProjectionWindowsRuntimeType && type.GetImplementationProjectionModule(interopDefinitions) is { } projectionModule
            ? projectionModule.Assembly!.Name!
            : type.DeclaringModule!.Assembly!.Name!;
    }
}
