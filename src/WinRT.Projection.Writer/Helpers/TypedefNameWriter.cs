// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Type-name emission helpers.
/// </summary>
internal static class TypedefNameWriter
{
    /// <summary>
    /// Writes a fundamental (primitive) type's projected name.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="t">The fundamental type.</param>
    public static void WriteFundamentalType(IndentedTextWriter writer, FundamentalType t)
    {
        writer.Write(FundamentalTypes.ToCSharpType(t));
    }

    /// <summary>
    /// Writes a fundamental (primitive) type's non-projected (.NET BCL) name.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="t">The fundamental type.</param>
    public static void WriteFundamentalNonProjectedType(IndentedTextWriter writer, FundamentalType t)
    {
        writer.Write(FundamentalTypes.ToDotNetType(t));
    }

    /// <summary>
    /// Writes the C# type name for a typed reference.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context (provides settings, current namespace, ABI/ABI.Impl mode flags).</param>
    /// <param name="type">The type definition to emit the name of.</param>
    /// <param name="nameType">The kind of name to emit (projected, non-projected, ABI, etc.).</param>
    /// <param name="forceWriteNamespace">When <see langword="true"/>, always prepend the <c>global::</c>-qualified namespace prefix.</param>
    public static void WriteTypedefName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
    {
        bool authoredType = context.Settings.Component && context.Settings.Filter.Includes(type);
        (string typeNamespace, string typeName) = type.Names();

        if (nameType == TypedefNameType.NonProjected)
        {
            writer.Write($"{typeNamespace}.{typeName}");
            return;
        }

        MappedType? proj = MappedTypes.Get(typeNamespace, typeName);
        if (proj is { } p)
        {
            typeNamespace = p.MappedNamespace;
            typeName = p.MappedName;
        }

        TypedefNameType nameToWrite = nameType;
        if (authoredType && TypeCategorization.IsExclusiveTo(type) && nameToWrite == TypedefNameType.Projected)
        {
            nameToWrite = TypedefNameType.CCW;
        }

        // Authored interfaces that aren't exclusive use the same authored interface.
        if (authoredType && nameToWrite == TypedefNameType.CCW &&
            TypeCategorization.GetCategory(type) == TypeCategory.Interface &&
            !TypeCategorization.IsExclusiveTo(type))
        {
            nameToWrite = TypedefNameType.Projected;
        }

        if (nameToWrite == TypedefNameType.EventSource && typeNamespace == "System")
        {
            writer.Write("global::WindowsRuntime.InteropServices.");
        }
        else if (forceWriteNamespace ||
            typeNamespace != context.CurrentNamespace ||
            (nameToWrite == TypedefNameType.Projected && (context.InAbiNamespace || context.InAbiImplNamespace)) ||
            (nameToWrite == TypedefNameType.ABI && !context.InAbiNamespace) ||
            (nameToWrite == TypedefNameType.EventSource && !context.InAbiNamespace) ||
            (nameToWrite == TypedefNameType.CCW && authoredType && !context.InAbiImplNamespace) ||
            (nameToWrite == TypedefNameType.CCW && !authoredType && (context.InAbiNamespace || context.InAbiImplNamespace)))
        {
            writer.Write(GlobalPrefix);
            if (nameToWrite is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
            {
                writer.Write("ABI.");
            }
            else if (authoredType && nameToWrite == TypedefNameType.CCW)
            {
                writer.Write("ABI.Impl.");
            }
            writer.Write($"{typeNamespace}.");
        }

        if (nameToWrite == TypedefNameType.StaticAbiClass)
        {
            writer.Write($"{IdentifierEscaping.StripBackticks(typeName)}Methods");
        }
        else if (nameToWrite == TypedefNameType.EventSource)
        {
            writer.Write($"{IdentifierEscaping.StripBackticks(typeName)}EventSource");
        }
        else
        {
            writer.Write(IdentifierEscaping.StripBackticks(typeName));
        }
    }

    /// <summary>
    /// Writes <c>&lt;T1, T2&gt;</c> for generic types.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="type">The (potentially generic) type definition.</param>
    public static void WriteTypeParams(IndentedTextWriter writer, TypeDefinition type)
    {
        if (type.GenericParameters.Count == 0) { return; }
        writer.Write("<");
        for (int i = 0; i < type.GenericParameters.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            string? gpName = type.GenericParameters[i].Name?.Value;
            writer.Write(gpName ?? $"T{i}");
        }
        writer.Write(">");
    }

    /// <summary>
    /// Writes the typedef name + generic params for a <see cref="TypeSemantics"/> handle.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="semantics">The semantic representation of the type.</param>
    /// <param name="nameType">The kind of name to emit.</param>
    /// <param name="forceWriteNamespace">When <see langword="true"/>, always prepend the <c>global::</c>-qualified namespace prefix.</param>
    public static void WriteTypeName(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
    {
        switch (semantics)
        {
            case TypeSemantics.Fundamental f:
                WriteFundamentalType(writer, f.Type);
                break;
            case TypeSemantics.Object_:
                writer.Write("object");
                break;
            case TypeSemantics.Guid_:
                writer.Write("Guid");
                break;
            case TypeSemantics.Type_:
                writer.Write("Type");
                break;
            case TypeSemantics.Definition d:
                WriteTypedefName(writer, context, d.Type, nameType, forceWriteNamespace);
                WriteTypeParams(writer, d.Type);
                break;
            case TypeSemantics.GenericInstance gi:
                WriteTypedefName(writer, context, gi.GenericType, nameType, forceWriteNamespace);
                writer.Write("<");
                for (int i = 0; i < gi.GenericArgs.Count; i++)
                {
                    if (i > 0) { writer.Write(", "); }
                    // Generic args ALWAYS use Projected, regardless of parent's nameType.
                    WriteTypeName(writer, context, gi.GenericArgs[i], TypedefNameType.Projected, forceWriteNamespace);
                }
                writer.Write(">");
                break;
            case TypeSemantics.GenericInstanceRef gir:
                {
                    (string ns, string name) = gir.GenericType.Names();
                    MappedType? mapped = MappedTypes.Get(ns, name);
                    if (mapped is { } m)
                    {
                        ns = m.MappedNamespace;
                        name = m.MappedName;
                    }
                    if (nameType == TypedefNameType.EventSource && ns == "System")
                    {
                        writer.Write("global::WindowsRuntime.InteropServices.");
                    }
                    else if (!string.IsNullOrEmpty(ns))
                    {
                        writer.Write(GlobalPrefix);
                        if (nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
                        {
                            writer.Write("ABI.");
                        }
                        writer.Write($"{ns}.");
                    }
                    writer.Write(IdentifierEscaping.StripBackticks(name));
                    if (nameType == TypedefNameType.StaticAbiClass) { writer.Write("Methods"); }
                    else if (nameType == TypedefNameType.EventSource) { writer.Write("EventSource"); }

                    writer.Write("<");
                    for (int i = 0; i < gir.GenericArgs.Count; i++)
                    {
                        if (i > 0) { writer.Write(", "); }
                        WriteTypeName(writer, context, gir.GenericArgs[i], TypedefNameType.Projected, forceWriteNamespace);
                    }
                    writer.Write(">");
                }
                break;
            case TypeSemantics.Reference r:
                {
                    (string ns, string name) = r.Reference_.Names();
                    MappedType? mapped = MappedTypes.Get(ns, name);
                    if (mapped is { } m)
                    {
                        ns = m.MappedNamespace;
                        name = m.MappedName;
                    }
                    bool needsNsPrefix = !string.IsNullOrEmpty(ns) && (
                        forceWriteNamespace ||
                        ns != context.CurrentNamespace ||
                        (nameType == TypedefNameType.Projected && (context.InAbiNamespace || context.InAbiImplNamespace)) ||
                        (nameType == TypedefNameType.ABI && !context.InAbiNamespace) ||
                        (nameType == TypedefNameType.EventSource && !context.InAbiNamespace) ||
                        (nameType == TypedefNameType.CCW && (context.InAbiNamespace || context.InAbiImplNamespace)));
                    if (needsNsPrefix)
                    {
                        writer.Write(GlobalPrefix);
                        if (nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
                        {
                            writer.Write("ABI.");
                        }
                        writer.Write($"{ns}.");
                    }
                    writer.Write(IdentifierEscaping.StripBackticks(name));
                    if (nameType == TypedefNameType.StaticAbiClass) { writer.Write("Methods"); }
                    else if (nameType == TypedefNameType.EventSource) { writer.Write("EventSource"); }
                }
                break;
            case TypeSemantics.GenericTypeIndex gti:
                writer.Write($"T{gti.Index}");
                break;
        }
    }

    /// <summary>
    /// Writes a projected type name (.NET-style).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="semantics">The semantic representation of the type.</param>
    public static void WriteProjectionType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        WriteTypeName(writer, context, semantics, TypedefNameType.Projected, false);
    }

    /// <summary>
    /// Writes the event handler type for an EventDefinition. Handles all the cases:
    /// TypeDefinition, TypeReference, TypeSpecification (generic instances like <c>EventHandler&lt;T&gt;</c>),
    /// and any other ITypeDefOrRef.
    /// </summary>
    public static void WriteEventType(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt)
        => WriteEventType(writer, context, evt, null);

    /// <summary>
    /// Same as <see cref="WriteEventType(IndentedTextWriter, ProjectionEmitContext, EventDefinition)"/>
    /// but applies the supplied generic context for substitution (e.g., <c>T0</c>/<c>T1</c> -&gt;
    /// concrete type arguments when emitting members for an instantiated parent generic interface).
    /// </summary>
    public static void WriteEventType(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, GenericInstanceTypeSignature? currentInstance)
    {
        if (evt.EventType is null)
        {
            writer.Write("global::Windows.Foundation.EventHandler");
            return;
        }
        TypeSignature sig = evt.EventType.ToTypeSignature(false);
        if (currentInstance is not null)
        {
            sig = sig.InstantiateGenericTypes(new GenericContext(currentInstance, null));
        }
        // Special case for Microsoft.UI.Xaml.Input.ICommand.CanExecuteChanged: the WinRT event
        // handler is EventHandler<object> but C# expects non-generic EventHandler.
        if (evt.Name?.Value == "CanExecuteChanged"
            && evt.DeclaringType is { } declaringType
            && (declaringType.FullName is "Microsoft.UI.Xaml.Input.ICommand" or "Windows.UI.Xaml.Input.ICommand"))
        {
            // Verify the event type matches EventHandler<object> before applying override.
            if (sig is GenericInstanceTypeSignature gi
                && gi.GenericType.Namespace?.Value == WindowsFoundation
                && gi.GenericType.Name?.Value == "EventHandler`1"
                && gi.TypeArguments.Count == 1
                && gi.TypeArguments[0] is CorLibTypeSignature corlib
                && corlib.ElementType == ElementType.Object)
            {
                writer.Write("global::System.EventHandler");
                return;
            }
        }
        // The outer EventHandler still gets 'global::System.' from being in a different namespace,
        // but type args in the same namespace stay unqualified.
        WriteTypeName(writer, context, TypeSemanticsFactory.Get(sig), TypedefNameType.Projected, false);
    }
}
