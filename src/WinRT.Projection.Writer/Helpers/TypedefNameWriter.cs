// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
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
    /// Builds the fully-qualified <c>global::Ns.Name</c> form for a type, handling the empty-namespace case.
    /// The <paramref name="name"/> is run through <see cref="IdentifierEscaping.StripBackticks(string)"/>
    /// so callers may pass either a raw metadata name (e.g. <c>"IList`1"</c>) or an already-stripped name.
    /// </summary>
    /// <param name="ns">The type's namespace (may be <see langword="null"/> or empty for top-level types).</param>
    /// <param name="name">The type's name (raw or already stripped; a generic-arity backtick suffix is stripped before use).</param>
    /// <returns>The string <c>global::Name</c> when <paramref name="ns"/> is null/empty, otherwise <c>global::Ns.Name</c>.</returns>
    public static string BuildGlobalQualifiedName(string? ns, string name)
    {
        string stripped = IdentifierEscaping.StripBackticks(name);
        return string.IsNullOrEmpty(ns) ? $"global::{stripped}" : $"global::{ns}.{stripped}";
    }

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
    /// Writes the C# type name for a typed reference.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context (provides settings, current namespace, ABI/ABI.Impl mode flags).</param>
    /// <param name="type">The type definition to emit the name of.</param>
    /// <param name="nameType">The kind of name to emit (projected, non-projected, ABI, etc.).</param>
    /// <param name="forceWriteNamespace">When <see langword="true"/>, always prepend the <c>global::</c>-qualified namespace prefix.</param>
    public static void WriteTypedefName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
    {
        bool authoredType = context.Settings.Component && context.Settings.Filter.Includes(type.FullName);
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
            type.IsInterface &&
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
    /// Writes the typedef name immediately followed by the generic parameter list
    /// (e.g. <c>Foo&lt;T0, T1&gt;</c>). Equivalent to calling
    /// <see cref="WriteTypedefName(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, TypedefNameType, bool)"/>
    /// followed by <see cref="WriteTypeParams(IndentedTextWriter, TypeDefinition)"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The (potentially generic) type definition.</param>
    /// <param name="nameType">The kind of name to emit.</param>
    /// <param name="forceWriteNamespace">When <see langword="true"/>, always prepend the <c>global::</c>-qualified namespace prefix.</param>
    public static void WriteTypedefNameWithTypeParams(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypedefNameType nameType, bool forceWriteNamespace)
    {
        WriteTypedefName(writer, context, type, nameType, forceWriteNamespace);
        WriteTypeParams(writer, type);
    }

    /// <inheritdoc cref="WriteTypedefNameWithTypeParams(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, TypedefNameType, bool)"/>
    /// <returns>A callback that writes the typedef name + generic-parameter list to the writer it's appended to.</returns>
    public static WriteTypedefNameWithTypeParamsCallback WriteTypedefNameWithTypeParams(ProjectionEmitContext context, TypeDefinition type, TypedefNameType nameType, bool forceWriteNamespace)
    {
        return new(context, type, nameType, forceWriteNamespace);
    }

    /// <inheritdoc cref="WriteTypedefName(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, TypedefNameType, bool)"/>
    /// <returns>A callback that writes the typedef name to the writer it's appended to.</returns>
    public static WriteTypedefNameCallback WriteTypedefName(ProjectionEmitContext context, TypeDefinition type, TypedefNameType nameType, bool forceWriteNamespace)
    {
        return new(context, type, nameType, forceWriteNamespace);
    }

    /// <summary>
    /// Writes <c>&lt;T1, T2&gt;</c> for generic types.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="type">The (potentially generic) type definition.</param>
    public static void WriteTypeParams(IndentedTextWriter writer, TypeDefinition type)
    {
        if (type.GenericParameters.Count == 0)
        {
            return;
        }

        writer.Write("<");
        for (int i = 0; i < type.GenericParameters.Count; i++)
        {
            writer.WriteIf(i > 0, ", ");

            string? gpName = type.GenericParameters[i].Name?.Value;
            writer.Write(gpName ?? $"T{i}");
        }
        writer.Write(">");
    }

    /// <inheritdoc cref="WriteTypeParams(IndentedTextWriter, TypeDefinition)"/>
    /// <returns>A callback that writes the generic-parameter list to the writer it's appended to.</returns>
    public static WriteTypeParamsCallback WriteTypeParams(TypeDefinition type)
    {
        return new(type);
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
            case TypeSemantics.ObjectType:
                writer.Write("object");
                break;
            case TypeSemantics.GuidType:
                writer.Write("Guid");
                break;
            case TypeSemantics.SystemType:
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
                    writer.WriteIf(i > 0, ", ");

                    // Generic args ALWAYS use Projected, regardless of parent's nameType.
                    WriteTypeName(writer, context, gi.GenericArgs[i], TypedefNameType.Projected, forceWriteNamespace);
                }
                writer.Write(">");
                break;
            case TypeSemantics.GenericInstanceRef gir:
                {
                    (string ns, string name) = gir.GenericType.Names();
                    _ = MappedTypes.ApplyMapping(ref ns, ref name);

                    if (nameType == TypedefNameType.EventSource && ns == "System")
                    {
                        writer.Write("global::WindowsRuntime.InteropServices.");
                    }
                    else if (!string.IsNullOrEmpty(ns))
                    {
                        writer.Write(GlobalPrefix);

                        writer.WriteIf(nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource, "ABI.");

                        writer.Write($"{ns}.");
                    }

                    writer.Write(IdentifierEscaping.StripBackticks(name));

                    if (nameType == TypedefNameType.StaticAbiClass)
                    {
                        writer.Write("Methods");
                    }
                    else if (nameType == TypedefNameType.EventSource)
                    {
                        writer.Write("EventSource");
                    }

                    writer.Write("<");
                    for (int i = 0; i < gir.GenericArgs.Count; i++)
                    {
                        writer.WriteIf(i > 0, ", ");

                        WriteTypeName(writer, context, gir.GenericArgs[i], TypedefNameType.Projected, forceWriteNamespace);
                    }
                    writer.Write(">");
                }
                break;
            case TypeSemantics.Reference r:
                {
                    (string ns, string name) = r.Type.Names();
                    _ = MappedTypes.ApplyMapping(ref ns, ref name);

                    bool needsNsPrefix = !string.IsNullOrEmpty(ns) && (
                        forceWriteNamespace ||
                        ns != context.CurrentNamespace ||
                        (nameType == TypedefNameType.Projected && (context.InAbiNamespace || context.InAbiImplNamespace)) ||
                        nameType == TypedefNameType.ABI ||
                        nameType == TypedefNameType.EventSource);

                    if (needsNsPrefix)
                    {
                        writer.Write(GlobalPrefix);

                        writer.WriteIf(nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource, "ABI.");

                        writer.Write($"{ns}.");
                    }

                    writer.Write(IdentifierEscaping.StripBackticks(name));

                    if (nameType == TypedefNameType.StaticAbiClass)
                    {
                        writer.Write("Methods");
                    }
                    else if (nameType == TypedefNameType.EventSource)
                    {
                        writer.Write("EventSource");
                    }
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

    /// <inheritdoc cref="WriteTypeName(IndentedTextWriter, ProjectionEmitContext, TypeSemantics, TypedefNameType, bool)"/>
    /// <returns>A callback that writes the type name to the writer it's appended to.</returns>
    public static WriteTypeNameCallback WriteTypeName(ProjectionEmitContext context, TypeSemantics semantics, TypedefNameType nameType, bool forceWriteNamespace)
    {
        return new(context, semantics, nameType, forceWriteNamespace);
    }

    /// <inheritdoc cref="WriteProjectionType(IndentedTextWriter, ProjectionEmitContext, TypeSemantics)"/>
    /// <returns>A callback that writes the projected type name to the writer it's appended to.</returns>
    public static WriteProjectionTypeCallback WriteProjectionType(ProjectionEmitContext context, TypeSemantics semantics)
    {
        return new(context, semantics);
    }

    /// <summary>
    /// Writes the event handler type for an EventDefinition. Handles all the cases:
    /// TypeDefinition, TypeReference, TypeSpecification (generic instances like <c>EventHandler&lt;T&gt;</c>),
    /// and any other ITypeDefOrRef.
    /// </summary>
    public static void WriteEventType(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt)
        => WriteEventType(writer, context, evt, null);

    /// <inheritdoc cref="WriteEventType(IndentedTextWriter, ProjectionEmitContext, EventDefinition)"/>
    /// <returns>A callback that writes the event handler type to the writer it's appended to.</returns>
    public static WriteEventTypeCallback WriteEventType(ProjectionEmitContext context, EventDefinition evt)
    {
        return new(context, evt, null);
    }

    /// <inheritdoc cref="WriteEventType(IndentedTextWriter, ProjectionEmitContext, EventDefinition, GenericInstanceTypeSignature?)"/>
    /// <returns>A callback that writes the event handler type to the writer it's appended to.</returns>
    public static WriteEventTypeCallback WriteEventType(ProjectionEmitContext context, EventDefinition evt, GenericInstanceTypeSignature? currentInstance)
    {
        return new(context, evt, currentInstance);
    }

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
