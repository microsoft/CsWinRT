// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// ObjRef field emission for runtime classes.
/// </summary>
internal static class ObjRefNameGenerator
{
    /// <summary>
    /// Returns the field name for the given interface impl (e.g. <c>_objRef_System_IDisposable</c>).
    /// Strips the <c>global::</c> prefix and replaces non-identifier characters with <c>_</c>.
    /// </summary>
    public static string GetObjRefName(ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        // Build the projected, fully-qualified name with global::.
        string projected;
        if (ifaceType is TypeDefinition td)
        {
            (string ns, string name) = td.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is { } m)
            {
                ns = m.MappedNamespace;
                name = m.MappedName;
            }
            projected = GlobalPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
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
            projected = GlobalPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
        }
        else
        {
            // Generic instantiation: always use fully qualified name (with global::) for the objref
            // name computation, so the resulting field name is unique across namespaces.
            IndentedTextWriter scratch = IndentedTextWriterPool.GetOrCreate();
            WriteFullyQualifiedInterfaceName(scratch, context, ifaceType);
            projected = scratch.ToString();
            IndentedTextWriterPool.Return(scratch);
        }
        return "_objRef_" + IIDExpressionGenerator.EscapeTypeNameForIdentifier(projected, stripGlobal: true);
    }
    /// <summary>
    /// Like <see cref="InterfaceFactory.WriteInterfaceTypeName(IndentedTextWriter, ProjectionEmitContext, ITypeDefOrRef)"/>
    /// but always emits a fully qualified name with <c>global::</c> prefix on every type
    /// (even same-namespace ones). Used for objref name computation where uniqueness across
    /// namespaces matters.
    /// </summary>
    private static void WriteFullyQualifiedInterfaceName(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        if (ifaceType is TypeDefinition td)
        {
            (string ns, string name) = td.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is { } m)
            {
                ns = m.MappedNamespace;
                name = m.MappedName;
            }
            writer.Write(GlobalPrefix);
            if (!string.IsNullOrEmpty(ns)) { writer.Write($"{ns}."); }
            writer.Write(IdentifierEscaping.StripBackticks(name));
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
            writer.Write(GlobalPrefix);
            if (!string.IsNullOrEmpty(ns)) { writer.Write($"{ns}."); }
            writer.Write(IdentifierEscaping.StripBackticks(name));
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
            writer.Write(GlobalPrefix);
            if (!string.IsNullOrEmpty(ns)) { writer.Write($"{ns}."); }
            writer.Write($"{IdentifierEscaping.StripBackticks(name)}<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                if (i > 0) { writer.Write(", "); }
                // forceWriteNamespace=true so generic args also get global:: prefix.
                TypedefNameWriter.WriteTypeName(writer, context, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, true);
            }
            writer.Write(">");
        }
    }

    /// <summary>
    /// Writes the IID expression for the given interface impl (used as the second arg to
    /// <c>NativeObjectReference.As(...)</c>).
    /// </summary>
    public static void WriteIidExpression(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        // Generic instantiation: use the UnsafeAccessor extern method declared above the field.
        if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            string propName = BuildIidPropertyNameForGenericInterface(context, gi);
            writer.Write($"{propName}(null)");
            return;
        }

        string ns;
        string name;
        bool isMapped;
        if (ifaceType is TypeDefinition td)
        {
            ns = td.Namespace?.Value ?? string.Empty;
            name = td.Name?.Value ?? string.Empty;
            isMapped = MappedTypes.Get(ns, name) is not null;
        }
        else if (ifaceType is TypeReference tr)
        {
            ns = tr.Namespace?.Value ?? string.Empty;
            name = tr.Name?.Value ?? string.Empty;
            isMapped = MappedTypes.Get(ns, name) is not null;
        }
        else
        {
            writer.Write("default(global::System.Guid)");
            return;
        }

        if (isMapped)
        {
            // IStringable maps to a simpler IID name in WellKnownInterfaceIIDs.
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is { MappedName: "IStringable" })
            {
                writer.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable");
                return;
            }
            // Mapped interface: use WellKnownInterfaceIIDs.IID_<EscapedNonProjectedName>.
            string id = EscapeIdentifier(ns + "." + IdentifierEscaping.StripBackticks(name));
            writer.Write($"global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_{id}");
        }
        else
        {
            // Non-mapped, non-generic: ABI.InterfaceIIDs.IID_<EscapedABIName>.
            string abiQualified = GlobalAbiPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
            string id = IIDExpressionGenerator.EscapeTypeNameForIdentifier(abiQualified, stripGlobal: false, stripGlobalABI: true);
            writer.Write($"global::ABI.InterfaceIIDs.IID_{id}");
        }
    }
    /// <summary>
    /// Builds the IID property name for a generic interface instantiation.
    /// E.g. <c>IObservableMap&lt;string, object&gt;</c> -> <c>IID_Windows_Foundation_Collections_IObservableMap_string__object_</c>.
    /// </summary>
    internal static string BuildIidPropertyNameForGenericInterface(ProjectionEmitContext context, GenericInstanceTypeSignature gi)
    {
        TypeSemantics sem = TypeSemanticsFactory.Get(gi);
        IndentedTextWriter scratch = IndentedTextWriterPool.GetOrCreate();
        TypedefNameWriter.WriteTypeName(scratch, context, sem, TypedefNameType.ABI, forceWriteNamespace: true);
        string result = "IID_" + IIDExpressionGenerator.EscapeTypeNameForIdentifier(scratch.ToString(), stripGlobal: true, stripGlobalABI: true);
        IndentedTextWriterPool.Return(scratch);
        return result;
    }
    /// <summary>
    /// Emits the [UnsafeAccessor] extern method declaration that exposes the IID for a generic
    /// interface instantiation.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="gi">The generic interface instantiation whose IID accessor is being emitted.</param>
    /// <param name="isInNullableContext">When <c>true</c>, the accessor's parameter type is
    /// <c>object?</c> (used inside <c>#nullable enable</c> regions); otherwise <c>object</c>.</param>
    internal static void EmitUnsafeAccessorForIid(IndentedTextWriter writer, ProjectionEmitContext context, GenericInstanceTypeSignature gi, bool isInNullableContext = false)
    {
        string propName = BuildIidPropertyNameForGenericInterface(context, gi);
        string interopName = InteropTypeNameWriter.EncodeInteropTypeName(gi, TypedefNameType.InteropIID);
        writer.Write($$"""
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_{{interopName}}")]
            static extern ref readonly Guid {{propName}}([UnsafeAccessorType("ABI.InterfaceIIDs, WinRT.Interop")] object
            """, isMultiline: true);
        if (isInNullableContext) { writer.Write("?"); }
        writer.WriteLine(" _);");
    }
    private static string EscapeIdentifier(string s)
    {
        System.Text.StringBuilder sb = new(s.Length);
        foreach (char c in s)
        {
            _ = sb.Append(c is ' ' or ':' or '<' or '>' or '`' or ',' or '.' ? '_' : c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Writes the IReference&lt;T&gt; IID expression for a value type (used by BoxToUnmanaged).
    /// </summary>
    public static void WriteIidReferenceExpression(IndentedTextWriter writer, TypeDefinition type)
    {
        (string ns, string name) = type.Names();
        string abiQualified = GlobalAbiPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
        string id = IIDExpressionGenerator.EscapeTypeNameForIdentifier(abiQualified, stripGlobal: false, stripGlobalABI: true);
        writer.Write($"global::ABI.InterfaceIIDs.IID_{id}Reference");
    }
    /// <summary>
    /// Emits the lazy <c>_objRef_*</c> field definitions for each interface implementation on
    /// the given runtime class. For sealed classes, the default interface is emitted as a
    /// simple <c>=> NativeObjectReference</c> expression; for unsealed classes, ALL interfaces
    /// (including the default) use the lazy MakeObjectReference pattern, and the default also
    /// gets an <c>init;</c> accessor so the constructor can set it via
    /// <c>_objRef_X = NativeObjectReference</c>.
    /// </summary>
    public static void WriteClassObjRefDefinitions(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Per-interface _objRef_* getters are emitted in BOTH impl and ref modes with full
        // bodies. (Only the static factory _objRef_* getters become `throw null;` in ref mode --
        // see WriteStaticFactoryObjRef and WriteAttributedTypes.)

        // Track names emitted so we don't emit duplicates (e.g. when both IFoo and IFoo2
        // produce the same _objRef_<name>).
        HashSet<string> emitted = [];
        bool isSealed = type.IsSealed;

        // Pass 1: emit objrefs for ALL directly-declared interfaces first (in InterfaceImpl
        // declaration order). Pass 2 then walks transitive parents to cover any not yet emitted.
        // So for a class that declares 'IFoo, IBar' where IFoo : IBaz, the order is IFoo, IBar,
        // IBaz, NOT IFoo, IBaz, IBar.
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!ClassMembersFactory.IsInterfaceInInheritanceList(context.Cache, impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }
            // For FastAbi classes, skip non-default exclusive interfaces -- their methods
            // dispatch through the default interface's vtable so a separate objref is unnecessary.
            bool isDefault = impl.HasAttribute(WindowsFoundationMetadata, DefaultAttribute);
            if (!isDefault && ClassFactory.IsFastAbiClass(type))
            {
                TypeDefinition? implTypeDef = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl.Interface);
                if (implTypeDef is not null && TypeCategorization.IsExclusiveTo(implTypeDef))
                {
                    continue;
                }
            }

            EmitObjRefForInterface(writer, context, impl.Interface, emitted, isDefault: isDefault, useSimplePattern: isSealed && isDefault);
        }
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!ClassMembersFactory.IsInterfaceInInheritanceList(context.Cache, impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }
            // Same fast-abi guard as the first pass.
            bool isDefault2 = impl.HasAttribute(WindowsFoundationMetadata, DefaultAttribute);
            if (!isDefault2 && ClassFactory.IsFastAbiClass(type))
            {
                TypeDefinition? implTypeDef = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl.Interface);
                if (implTypeDef is not null && TypeCategorization.IsExclusiveTo(implTypeDef))
                {
                    continue;
                }
            }
            EmitTransitiveInterfaceObjRefs(writer, context, impl.Interface, emitted);
        }
    }
    /// <summary>
    /// Emits an _objRef_ field for a single interface impl reference.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="ifaceRef">The interface reference being objref-d.</param>
    /// <param name="emitted">A set tracking objref names already emitted into the current class (deduplication).</param>
    /// <param name="isDefault">When true, the interface is the runtime class's default interface (which the WindowsRuntimeObject base already exposes).</param>
    /// <param name="useSimplePattern">When true, emit the simple expression-bodied form
    /// <c>=> NativeObjectReference</c>. Otherwise emit the lazy MakeObjectReference pattern.</param>
    private static void EmitObjRefForInterface(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceRef, HashSet<string> emitted, bool isDefault, bool useSimplePattern = false)
    {
        string objRefName = GetObjRefName(context, ifaceRef);
        if (!emitted.Add(objRefName)) { return; }
        // The [UnsafeAccessor] extern method declaration is used by the IID expression in both
        // simple and lazy patterns.
        bool isGenericInstance = ifaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature;
        GenericInstanceTypeSignature? gi = isGenericInstance
            ? (GenericInstanceTypeSignature)((TypeSpecification)ifaceRef).Signature!
            : null;

        if (useSimplePattern)
        {
            // Sealed-class default interface: simple expression-bodied property pointing at NativeObjectReference.
            writer.WriteLine($"private WindowsRuntimeObjectReference {objRefName} => NativeObjectReference;");
            // Emit the unsafe accessor AFTER the field so it can be used to pass the IID in the
            // constructor for the default interface.
            if (gi is not null)
            {
                EmitUnsafeAccessorForIid(writer, context, gi);
            }
        }
        else
        {
            // Emit the unsafe accessor BEFORE the lazy field so it's referenced inside the As(...) call.
            if (gi is not null)
            {
                EmitUnsafeAccessorForIid(writer, context, gi);
            }
            // Lazy CompareExchange pattern. For unsealed-class defaults, also emit 'init;' so the
            // constructor can assign NativeObjectReference for the exact-type case.
            writer.Write($$"""
                private WindowsRuntimeObjectReference {{objRefName}}
                {
                    get
                    {
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        WindowsRuntimeObjectReference MakeObjectReference()
                        {
                            _ = global::System.Threading.Interlocked.CompareExchange(
                                location1: ref field,
                                value: NativeObjectReference.As(
                """, isMultiline: true);
            WriteIidExpression(writer, context, ifaceRef);
            writer.Write("""
                ),
                                comparand: null);
                
                            return field;
                        }
                
                        return field ?? MakeObjectReference();
                    }
                """, isMultiline: true);
            if (isDefault) { writer.WriteLine("    init;"); }
            writer.WriteLine("}");
        }
    }

    /// <summary>
    /// Walks transitively-inherited interfaces and emits an objref field for each one.
    /// </summary>
    private static void EmitTransitiveInterfaceObjRefs(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceRef, HashSet<string> emitted)
    {
        // Resolve the interface to its TypeDefinition; if cross-module, look it up in the cache.
        TypeDefinition? ifaceTd = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, ifaceRef);
        if (ifaceTd is null) { return; }

        // Compute a substitution context if the parent is a closed generic instance.
        GenericContext? ctx = null;
        if (ifaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ctx = new GenericContext(gi, null);
        }

        foreach (InterfaceImplementation childImpl in ifaceTd.Interfaces)
        {
            if (childImpl.Interface is null) { continue; }

            // If the parent is a closed generic, substitute the child's signature.
            ITypeDefOrRef childRef = childImpl.Interface;
            if (ctx is not null)
            {
                TypeSignature childSig = childRef.ToTypeSignature(false);
                TypeSignature substitutedSig = childSig.InstantiateGenericTypes(ctx.Value);
                ITypeDefOrRef? newRef = substitutedSig.ToTypeDefOrRef();
                if (newRef is not null) { childRef = newRef; }
            }

            // Emitting an _objRef_* field for an exclusive-to-someone-else parent interface is
            // harmless (the field stores the result of a QI; if QI fails the field stays unset).
            EmitObjRefForInterface(writer, context, childRef, emitted, isDefault: false);
            EmitTransitiveInterfaceObjRefs(writer, context, childRef, emitted);
        }
    }

    /// <summary>
    /// Whether this interface impl needs an _objRef_* field even though it isn't part of the
    /// inheritance list (e.g. ExclusiveTo interfaces still need their objref since instance
    /// methods/properties dispatch through it).
    /// </summary>
    public static bool IsInterfaceForObjRef(InterfaceImplementation impl)
    {
        // Emit objrefs for ALL implemented interfaces -- instance member dispatch needs to be
        // able to reach them.
        return impl.Interface is not null;
    }
}
