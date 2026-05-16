// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the per-class member surface (instance methods, properties, events) plus the
/// per-required-interface DIM thunks for runtime classes.
/// </summary>
/// <remarks>
/// The implementation is split across several partial files:
/// <list type="bullet">
///   <item><description><c>ClassMembersFactory.WriteClassMembers.cs</c> - top-level class-member emission entry point + the per-method dedupe key helper.</description></item>
///   <item><description><c>ClassMembersFactory.WriteInterfaceMembers.cs</c> - per-required-interface member emission (recursive walk + the per-interface emitter).</description></item>
/// </list>
/// </remarks>
internal static partial class ClassMembersFactory
{
    /// <summary>
    /// Returns true if the given interface implementation should appear in the class's inheritance list
    /// (i.e., it has [Overridable], or is not [ExclusiveTo], or includeExclusiveInterface is set).
    /// </summary>
    internal static bool IsInterfaceInInheritanceList(MetadataCache cache, InterfaceImplementation impl, bool includeExclusiveInterface)
    {
        if (impl.Interface is null)
        {
            return false;
        }

        if (impl.IsOverridable())
        {
            return true;
        }

        if (includeExclusiveInterface)
        {
            return true;
        }

        TypeDefinition? td = ResolveInterface(cache, impl.Interface);

        if (td is null)
        {
            return true;
        }

        return !TypeCategorization.IsExclusiveTo(td);
    }
    internal static TypeDefinition? ResolveInterface(MetadataCache cache, ITypeDefOrRef typeRef)
    {
        if (typeRef is TypeDefinition td)
        {
            return td;
        }

        TypeDefinition? resolved = typeRef.TryResolve(cache.RuntimeContext);

        if (resolved is not null)
        {
            return resolved;
        }

        // Fall back to local lookup by full name
        if (typeRef is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            return cache.Find(fullName);
        }

        if (typeRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            return ResolveInterface(cache, gi.GenericType);
        }

        return null;
    }

    /// <summary>
    /// Writes a parameter name prefixed with its modifier (in/out/ref) for use as a call argument.
    /// </summary>
    internal static void WriteParameterNameWithModifier(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
    {
        ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
        switch (cat)
        {
            case ParameterCategory.Out:
                writer.Write("out ");
                break;
            case ParameterCategory.Ref:
                writer.Write("in ");
                break;
            case ParameterCategory.ReceiveArray:
                writer.Write("out ");
                break;
        }
        MethodFactory.WriteParameterName(writer, p);
    }

    /// <summary>
    /// Writes the projected name for an interface reference (TypeDefinition, TypeReference, or
    /// generic instance), applying mapped-type remapping. Used inside <c>IWindowsRuntimeInterface&lt;T&gt;</c>.
    /// </summary>
    internal static void WriteInterfaceTypeNameForCcw(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        // If the reference is to a type in the same module, resolve to TypeDefinition so
        // WriteTypedefName can drop the 'global::<NS>.' prefix when the namespace matches.
        if (ifaceType is not TypeDefinition && ifaceType is not TypeSpecification && context.Cache is not null)
        {
            TypeDefinition? resolved = ifaceType.TryResolve(context.Cache.RuntimeContext);

            if (resolved is not null)
            {
                ifaceType = resolved;
            }
        }

        if (ifaceType is TypeDefinition td)
        {
            TypedefNameWriter.WriteTypedefName(writer, context, td, TypedefNameType.CCW, false);
            TypedefNameWriter.WriteTypeParams(writer, td);
        }
        else if (ifaceType is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);

            if (mapped is { } m)
            {
                ns = m.MappedNamespace;
                name = m.MappedName;
            }

            writer.Write($"global::{ns}.{IdentifierEscaping.StripBackticks(name)}");
        }
        else if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef gt = gi.GenericType;
            (string ns, string name) = gt.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);

            if (mapped is { } m)
            {
                ns = m.MappedNamespace;
                name = m.MappedName;
            }

            writer.Write($"global::{ns}.{IdentifierEscaping.StripBackticks(name)}<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                writer.WriteIf(i > 0, ", ");

                TypedefNameWriter.WriteTypeName(writer, context, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, true);
            }
            writer.Write(">");
        }
    }

    /// <inheritdoc cref="WriteInterfaceTypeNameForCcw(IndentedTextWriter, ProjectionEmitContext, ITypeDefOrRef)"/>
    /// <returns>A callback that writes the CCW interface type name to the writer it's appended to.</returns>
    internal static WriteInterfaceTypeNameForCcwCallback WriteInterfaceTypeNameForCcw(ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        return new(context, ifaceType);
    }
}
