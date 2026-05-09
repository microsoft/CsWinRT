// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Type-name emission helpers.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Writes a fundamental (primitive) type's projected name.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="t">The fundamental type.</param>
    public static void WriteFundamentalType(IndentedTextWriter writer, FundamentalType t)
    {
        writer.Write(FundamentalTypes.ToCSharpType(t));
    }

    /// <summary>Legacy <see cref="TextWriter"/> overload that delegates to <see cref="WriteFundamentalType(IndentedTextWriter, FundamentalType)"/>.</summary>
    public static void WriteFundamentalType(TextWriter w, FundamentalType t) => WriteFundamentalType(w.Writer, t);

    /// <summary>Writes a fundamental (primitive) type's non-projected (.NET BCL) name.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="t">The fundamental type.</param>
    public static void WriteFundamentalNonProjectedType(IndentedTextWriter writer, FundamentalType t)
    {
        writer.Write(FundamentalTypes.ToDotNetType(t));
    }

    /// <summary>Legacy <see cref="TextWriter"/> overload that delegates to <see cref="WriteFundamentalNonProjectedType(IndentedTextWriter, FundamentalType)"/>.</summary>
    public static void WriteFundamentalNonProjectedType(TextWriter w, FundamentalType t) => WriteFundamentalNonProjectedType(w.Writer, t);

    /// <summary>Writes the C# type name for a typed reference.</summary>
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
            writer.Write(typeNamespace);
            writer.Write(".");
            writer.Write(typeName);
            return;
        }

        MappedType? proj = MappedTypes.Get(typeNamespace, typeName);
        if (proj is not null)
        {
            typeNamespace = proj.MappedNamespace;
            typeName = proj.MappedName;
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
            writer.Write("global::");
            if (nameToWrite is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
            {
                writer.Write("ABI.");
            }
            else if (authoredType && nameToWrite == TypedefNameType.CCW)
            {
                writer.Write("ABI.Impl.");
            }
            writer.Write(typeNamespace);
            writer.Write(".");
        }

        if (nameToWrite == TypedefNameType.StaticAbiClass)
        {
            writer.Write(IdentifierEscaping.StripBackticks(typeName));
            writer.Write("Methods");
        }
        else if (nameToWrite == TypedefNameType.EventSource)
        {
            writer.Write(IdentifierEscaping.StripBackticks(typeName));
            writer.Write("EventSource");
        }
        else
        {
            writer.Write(IdentifierEscaping.StripBackticks(typeName));
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteTypedefName(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, TypedefNameType, bool)"/>.</summary>
    public static void WriteTypedefName(TypeWriter w, TypeDefinition type, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
        => WriteTypedefName(w.Writer, w.Context, type, nameType, forceWriteNamespace);

    /// <summary>Writes <c>&lt;T1, T2&gt;</c> for generic types.</summary>
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

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteTypeParams(IndentedTextWriter, TypeDefinition)"/>.</summary>
    public static void WriteTypeParams(TypeWriter w, TypeDefinition type) => WriteTypeParams(w.Writer, type);

    /// <summary>Writes the typedef name + generic params for a <see cref="TypeSemantics"/> handle.</summary>
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
                    if (mapped is not null)
                    {
                        ns = mapped.MappedNamespace;
                        name = mapped.MappedName;
                    }
                    if (nameType == TypedefNameType.EventSource && ns == "System")
                    {
                        writer.Write("global::WindowsRuntime.InteropServices.");
                    }
                    else if (!string.IsNullOrEmpty(ns))
                    {
                        writer.Write("global::");
                        if (nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
                        {
                            writer.Write("ABI.");
                        }
                        writer.Write(ns);
                        writer.Write(".");
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
                    if (mapped is not null)
                    {
                        ns = mapped.MappedNamespace;
                        name = mapped.MappedName;
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
                        writer.Write("global::");
                        if (nameType is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
                        {
                            writer.Write("ABI.");
                        }
                        writer.Write(ns);
                        writer.Write(".");
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

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteTypeName(IndentedTextWriter, ProjectionEmitContext, TypeSemantics, TypedefNameType, bool)"/>.</summary>
    public static void WriteTypeName(TypeWriter w, TypeSemantics semantics, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
        => WriteTypeName(w.Writer, w.Context, semantics, nameType, forceWriteNamespace);

    /// <summary>Writes a projected type name (.NET-style).</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="semantics">The semantic representation of the type.</param>
    public static void WriteProjectionType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        WriteTypeName(writer, context, semantics, TypedefNameType.Projected, false);
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteProjectionType(IndentedTextWriter, ProjectionEmitContext, TypeSemantics)"/>.</summary>
    public static void WriteProjectionType(TypeWriter w, TypeSemantics semantics) => WriteProjectionType(w.Writer, w.Context, semantics);

    /// <summary>
    /// Writes the event handler type for an EventDefinition. Handles all the cases:
    /// TypeDefinition, TypeReference, TypeSpecification (generic instances like <c>EventHandler&lt;T&gt;</c>),
    /// and any other ITypeDefOrRef.
    /// </summary>
    public static void WriteEventType(TypeWriter w, EventDefinition evt)
    {
        WriteEventType(w, evt, null);
    }

    /// <summary>
    /// Same as <see cref="WriteEventType(TypeWriter, EventDefinition)"/> but applies the supplied
    /// generic context for substitution (e.g., <c>T0</c>/<c>T1</c> -&gt; concrete type arguments
    /// when emitting members for an instantiated parent generic interface).
    /// </summary>
    public static void WriteEventType(TypeWriter w, EventDefinition evt, AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? currentInstance)
    {
        if (evt.EventType is null)
        {
            w.Write("global::Windows.Foundation.EventHandler");
            return;
        }
        AsmResolver.DotNet.Signatures.TypeSignature sig = evt.EventType.ToTypeSignature(false);
        if (currentInstance is not null)
        {
            sig = sig.InstantiateGenericTypes(new AsmResolver.DotNet.Signatures.GenericContext(currentInstance, null));
        }
        // Special case for Microsoft.UI.Xaml.Input.ICommand.CanExecuteChanged: the WinRT event
        // handler is EventHandler<object> but C# expects non-generic EventHandler. Mirrors C++:
        //   if (event.Name() == "CanExecuteChanged" && event_type == "global::System.EventHandler<object>")
        //       check parent_type_name == ICommand and override event_type
        if (evt.Name?.Value == "CanExecuteChanged"
            && evt.DeclaringType is { } declaringType
            && (declaringType.FullName == "Microsoft.UI.Xaml.Input.ICommand"
                || declaringType.FullName == "Windows.UI.Xaml.Input.ICommand"))
        {
            // Verify the event type matches EventHandler<object> before applying override.
            if (sig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi
                && gi.GenericType.Namespace?.Value == "Windows.Foundation"
                && gi.GenericType.Name?.Value == "EventHandler`1"
                && gi.TypeArguments.Count == 1
                && gi.TypeArguments[0] is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib
                && corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object)
            {
                w.Write("global::System.EventHandler");
                return;
            }
        }
        // The outer EventHandler still gets 'global::System.' from being in a different namespace,
        // but type args in the same namespace stay unqualified.
        WriteTypeName(w, TypeSemanticsFactory.Get(sig), TypedefNameType.Projected, false);
    }
}
