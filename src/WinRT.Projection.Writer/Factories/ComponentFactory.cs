// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

#pragma warning disable IDE0061

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Component-mode helpers.
/// </summary>
internal static class ComponentFactory
{
    /// <summary>
    /// Adds a (projected -> CCW) type-name pair to the metadata-type map.
    /// </summary>
    public static void AddMetadataTypeEntry(ProjectionEmitContext context, TypeDefinition type, ConcurrentDictionary<string, string> map)
    {
        if (!context.Settings.Component)
        {
            return;
        }

        TypeKind kind = TypeKindResolver.Resolve(type);

        if ((kind == TypeKind.Class && type.IsStatic) ||
            (kind == TypeKind.Interface && type.IsExclusiveTo))
        {
            return;
        }

        string typeName = TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.Projected, true).Format();

        string metadataTypeName = TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.CCW, true).Format();

        _ = map.TryAdd(typeName, metadataTypeName);
    }

    /// <summary>
    /// Writes the per-runtime-class server-activation-factory type for component mode.
    /// </summary>
    /// <param name="writer">The writer to emit the factory class to.</param>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="type">The activatable runtime class to emit a factory for.</param>
    public static void WriteFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        (string typeNs, string typeName) = type.Names();
        string projectedTypeName = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, typeName);
        string factoryTypeName = $"{IdentifierEscaping.StripBackticks(typeName)}ServerActivationFactory";

        // The static constructor that forces the authored type's class constructor to run before
        // activation is only needed when the type registers dependency properties, so consult the
        // component's managed implementation assemblies (the .winmd doesn't carry those fields).
        bool emitStaticConstructor = context.StaticConstructorAnalyzer.RequiresStaticConstructor(type.FullName);

        // Writes the set of interfaces implemented by the factory class ('IActivationFactory' is always included)
        void WriteBaseInterfaceList(IndentedTextWriter writer)
        {
            writer.Write("global::WindowsRuntime.InteropServices.IActivationFactory");

            // Build the inheritance list from all factory interfaces carried by the runtime class.
            foreach ((_, AttributedType type) in AttributedTypes.Get(type, context.Cache))
            {
                if ((type.Activatable || type.Statics || type.Composable) && type.Type is not null)
                {
                    writer.Write(", ");

                    // CCW + non-forced namespace is the user-facing interface name (e.g. 'IButtonUtilsStatic').
                    TypedefNameWriter.WriteTypedefName(writer, context, type.Type, TypedefNameType.CCW, false);
                    TypedefNameWriter.WriteTypeParams(writer, type.Type);
                }
            }
        }

        // Writes the body of the 'ActivateInstance' method (it throws for non-activatable types)
        void WriteActivateInstanceBody(IndentedTextWriter writer)
        {
            // A type whose default constructor is removed ([Deprecated(DeprecationType.Remove)]) is no longer
            // default-activatable: 'new T()' cannot be emitted (it would call the removed authored member),
            // so default activation falls through to the 'throw' below, which marshals to E_NOTIMPL.
            bool isActivatable =
                !type.IsStatic &&
                type.HasWindowsFoundationMetadataAttribute(ActivatableAttribute) &&
                type.HasActivatableDefaultConstructor();

            if (isActivatable)
            {
                writer.Write($"return new {projectedTypeName}();");
            }
            else
            {
                writer.Write("throw new NotImplementedException();");
            }
        }

        // Writes the static constructor that forces the projected type's class constructor to run
        // before activation, so any dependency properties it registers (as static fields) are set
        // up in time. Types that don't register any don't need it, so the callback emits nothing
        // for them and the factory omits the constructor entirely, keeping the factory body a
        // single interpolated template in either case
        void WriteStaticConstructor(IndentedTextWriter writer)
        {
            if (!emitStaticConstructor)
            {
                return;
            }

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                static {{factoryTypeName}}()
                {
                    global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({{projectedTypeName}}).TypeHandle);
                }
                """);
        }

        // Helper wrapper to write additional methods
        void WriteAdditionalActivationFactoryMethods(IndentedTextWriter writer)
        {
            ComponentFactory.WriteAdditionalActivationFactoryMethods(writer, context, type, projectedTypeName);
        }

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            internal sealed class {{factoryTypeName}} : {{WriteBaseInterfaceList}}
            {
                private static readonly {{factoryTypeName}} _factory = new();
                {{WriteStaticConstructor}}

                public static unsafe void* Make()
                {
                    return global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<global::WindowsRuntime.InteropServices.IActivationFactory>
                        .ConvertToUnmanaged(_factory, in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IActivationFactory)
                        .DetachThisPtrUnsafe();
                }
            
                public object ActivateInstance()
                {
                    {{WriteActivateInstanceBody}}
                }
                {{WriteAdditionalActivationFactoryMethods}}
            }
            """);
    }

    /// <summary>
    /// Writes additional methods in an activation factory types (e.g. static methods)
    /// </summary>
    private static void WriteAdditionalActivationFactoryMethods(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition type,
        string projectedTypeName)
    {
        // Emit factory-class members: forwarding methods/properties/events for static factory
        // interfaces, plus activation and composition constructor wrappers.
        bool hasEmittedAggregationEntries = false;

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, context.Cache))
        {
            AttributedType info = kv.Value;

            if (info.Type is null)
            {
                continue;
            }

            if (info.Activatable)
            {
                foreach (MethodDefinition method in info.Type.Methods)
                {
                    // Removed members (DeprecationType.Remove) are omitted from the factory class: the
                    // projected factory/static interface drops them, their vtable slot is stubbed to
                    // E_NOTIMPL, and generated code cannot call the authored member anyway (the C#
                    // compiler treats a call to a '[Deprecated(Remove)]' member as an error).
                    if (method.IsConstructor || method.IsRemoved)
                    {
                        continue;
                    }

                    WriteFactoryActivatableMethod(writer, context, method, projectedTypeName);
                }
            }
            else if (info.Composable)
            {
                string defaultInterfaceIid = GetDefaultInterfaceIid(context, type);

                // A runtime class can carry more than one composition factory (that is how Windows Runtime
                // versions them), but they all compose the same class, so the entries are emitted just once
                if (!hasEmittedAggregationEntries)
                {
                    WriteAggregationEntries(writer, context, type);

                    hasEmittedAggregationEntries = true;
                }

                foreach (MethodDefinition method in info.Type.Methods)
                {
                    if (method.IsConstructor || method.IsRemoved)
                    {
                        continue;
                    }

                    WriteFactoryComposableMethod(writer, context, method, projectedTypeName, defaultInterfaceIid);
                }
            }
            else if (info.Statics)
            {
                foreach (MethodDefinition method in info.Type.Methods)
                {
                    if (method.IsConstructor || method.IsRemoved)
                    {
                        continue;
                    }

                    WriteStaticFactoryMethod(writer, context, method, projectedTypeName);
                }
                foreach (PropertyDefinition prop in info.Type.Properties)
                {
                    if ((prop.GetMethod ?? prop.SetMethod) is { IsRemoved: true })
                    {
                        continue;
                    }

                    WriteStaticFactoryProperty(writer, context, prop, projectedTypeName);
                }
                foreach (EventDefinition evt in info.Type.Events)
                {
                    if (evt.AddMethod is { IsRemoved: true })
                    {
                        continue;
                    }

                    WriteStaticFactoryEvent(writer, context, evt, projectedTypeName);
                }
            }
        }
    }

    /// <summary>
    /// Returns the IID expression for the default interface of an authored runtime class.
    /// </summary>
    private static string GetDefaultInterfaceIid(ProjectionEmitContext context, TypeDefinition classType)
    {
        ITypeDefOrRef? defaultInterface = classType.GetDefaultInterface();

        return defaultInterface is null
            ? "default(global::System.Guid)"
            : ObjRefNameGenerator.WriteIidExpression(context, defaultInterface).Format();
    }

    /// <summary>
    /// Writes the projected parameter list of a composition-factory method.
    /// </summary>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="sig">The signature of the composition-factory method.</param>
    /// <returns>A callback that writes the parameter list.</returns>
    /// <remarks>
    /// The authored constructor parameters keep their projected types, while the trailing controlling outer and
    /// non-delegating inner parameters stay raw <c>IInspectable</c> pointers (see <see cref="WriteFactoryComposableMethod"/>).
    /// </remarks>
    public static IndentedTextWriterCallback WriteComposableFactoryParameterList(ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        return writer =>
        {
            int userParameterCount = sig.Parameters.Count - 2;

            for (int i = 0; i < userParameterCount; i++)
            {
                writer.Write($"{MethodFactory.WriteProjectionParameter(context, sig.Parameters[i])}, ");
            }

            writer.Write($"void* {GetControllingOuterParameterName(sig)}, void** {GetNonDelegatingInnerParameterName(sig)}");
        };
    }

    /// <summary>
    /// Gets the name of the controlling outer parameter of a composition-factory method.
    /// </summary>
    public static string GetControllingOuterParameterName(MethodSignatureInfo sig)
    {
        return IdentifierEscaping.EscapeIdentifier(sig.Parameters[sig.Parameters.Count - 2].GetRawName());
    }

    /// <summary>
    /// Gets the name of the non-delegating inner parameter of a composition-factory method.
    /// </summary>
    public static string GetNonDelegatingInnerParameterName(MethodSignatureInfo sig)
    {
        return IdentifierEscaping.EscapeIdentifier(sig.Parameters[sig.Parameters.Count - 1].GetRawName());
    }

    /// <summary>
    /// Writes the cached COM aggregation entries and their initialization helper for a composable runtime class.
    /// </summary>
    /// <param name="writer">The writer to emit the field and helper to.</param>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="classType">The composable runtime class.</param>
    /// <remarks>
    /// <para>
    /// The entries describe every interface the CCW of the class can hand out while the instance is taking part in
    /// COM aggregation. The runtime uses them to build a private, per-aggregate copy of each of those vtables, with
    /// only the <c>IUnknown</c> and <c>IInspectable</c> entries replaced by ones delegating to the controlling outer
    /// object. That is why the size of each vtable is emitted alongside its address.
    /// </para>
    /// <para>
    /// Nothing here is used for a standalone (non-aggregated) instance: those keep the exact same CCW, and the same
    /// shared vtables, that every other authored object gets, including the <c>IUnknown</c> implementation the
    /// runtime provides in native code.
    /// </para>
    /// </remarks>
    private static void WriteAggregationEntries(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition classType)
    {
        void WriteEntries(IndentedTextWriter writer)
        {
            bool first = true;

            foreach (TypeDefinition interfaceType in ComposableTypeHelpers.GetAggregableInterfaces(context, classType))
            {
                string abiTypeName = TypedefNameWriter.WriteTypedefName(context, interfaceType, TypedefNameType.ABI, true).Format();
                string iid = ObjRefNameGenerator.WriteIidExpression(context, interfaceType).Format();

                writer.WriteLineIf(!first);
                writer.Write($"new(in {iid}, {abiTypeName}Impl.Vtable, sizeof({abiTypeName}Vftbl)),");

                first = false;
            }
        }

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeAggregationEntry[] AggregationEntries = GetAggregationEntries();

            private static unsafe global::WindowsRuntime.InteropServices.WindowsRuntimeAggregationEntry[] GetAggregationEntries()
            {
                return
                [
                    {{WriteEntries}}
                ];
            }
            """);
    }

    /// <summary>
    /// Writes a composition-factory method on the generated activation factory type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two trailing factory parameters are the controlling outer and the non-delegating inner, and are not
    /// authored constructor arguments. They are projected as raw <c>IInspectable</c> pointers: the controlling
    /// outer is only partially constructed while the factory runs, so it must not be wrapped in an RCW, and the
    /// non-delegating inner is a hand-written COM object owned by the runtime rather than a managed object.
    /// </para>
    /// <para>
    /// All the COM aggregation work (registering the controlling outer, creating the CCW with its per-aggregate
    /// delegating vtables, and producing both the non-delegating inner and the delegating default interface) is
    /// done by the runtime.
    /// </para>
    /// </remarks>
    private static void WriteFactoryComposableMethod(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        MethodDefinition method,
        string projectedTypeName,
        string defaultInterfaceIid)
    {
        if (!ComposableTypeHelpers.IsComposableFactoryMethod(method))
        {
            return;
        }

        MethodSignatureInfo sig = new(method);
        string methodName = method.GetRawName();
        int userParameterCount = sig.Parameters.Count - 2;

        void WriteArgumentNames(IndentedTextWriter writer)
        {
            for (int i = 0; i < userParameterCount; i++)
            {
                writer.WriteIf(i > 0, ", ");
                writer.Write(IdentifierEscaping.EscapeIdentifier(sig.Parameters[i].GetRawName()));
            }
        }

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            public unsafe void* {{methodName}}({{WriteComposableFactoryParameterList(context, sig)}})
            {
                return global::WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal.CreateComposableInstanceUnsafe(
                    new {{projectedTypeName}}({{WriteArgumentNames}}),
                    in {{defaultInterfaceIid}},
                    AggregationEntries,
                    {{GetControllingOuterParameterName(sig)}},
                    {{GetNonDelegatingInnerParameterName(sig)}});
            }
            """);
    }

    /// <summary>
    /// Writes a factory-class activatable wrapper method:
    /// <c>public T MethodName(args) =&gt; new T(args);</c>.
    /// </summary>
    private static void WriteFactoryActivatableMethod(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method, string projectedTypeName)
    {
        if (method.IsSpecialName)
        {
            return;
        }

        string methodName = method.GetRawName();
        IndentedTextWriterCallback typedParams = WriteFactoryMethodParameters(context, method, includeTypes: true);
        IndentedTextWriterCallback nameOnlyParams = WriteFactoryMethodParameters(context, method, includeTypes: false);

        writer.WriteLine();
        writer.WriteLine($"public {projectedTypeName} {methodName}({typedParams}) => new {projectedTypeName}({nameOnlyParams});");
    }

    /// <summary>
    /// Writes a static-factory forwarding method:
    /// <c>public Ret MethodName(args) =&gt; global::Ns.Type.MethodName(args);</c>.
    /// </summary>
    private static void WriteStaticFactoryMethod(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method, string projectedTypeName)
    {
        if (method.IsSpecialName)
        {
            return;
        }

        string methodName = method.GetRawName();
        IndentedTextWriterCallback retType = WriteFactoryReturnType(context, method);
        IndentedTextWriterCallback typedParams = WriteFactoryMethodParameters(context, method, includeTypes: true);
        IndentedTextWriterCallback nameOnlyParams = WriteFactoryMethodParameters(context, method, includeTypes: false);

        writer.WriteLine();
        writer.WriteLine($"public {retType} {methodName}({typedParams}) => {projectedTypeName}.{methodName}({nameOnlyParams});");
    }

    /// <summary>
    /// Writes a static-factory forwarding property (single-line getter or full block).
    /// </summary>
    private static void WriteStaticFactoryProperty(IndentedTextWriter writer, ProjectionEmitContext context, PropertyDefinition prop, string projectedTypeName)
    {
        string propName = prop.GetRawName();
        (MethodDefinition? getter, MethodDefinition? setter) = prop.GetMethods();
        string propType = GetFactoryPropertyType(context, prop);

        // Single-line form when no setter is present
        if (setter is null)
        {
            writer.WriteLine();
            writer.WriteLine($"public {propType} {propName} => {projectedTypeName}.{propName};");

            return;
        }

        string getterLine = getter is not null
            ? $"get => {projectedTypeName}.{propName};"
            : string.Empty;
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            public {{propType}} {{propName}}
            {
            {{getterLine}}
            set => {{projectedTypeName}}.{{propName}} = value;
            }
            """);
    }

    /// <summary>
    /// Writes a static-factory forwarding event as a multi-line block.
    /// </summary>
    private static void WriteStaticFactoryEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, string projectedTypeName)
    {
        string evtName = evt.GetRawName();
        string evtType = evt.EventType is null
            ? string.Empty
            : TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.GetFromTypeDefOrRef(evt.EventType), TypedefNameType.Projected, false).Format();

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            public event {{evtType}} {{evtName}}
            {
            add => {{projectedTypeName}}.{{evtName}} += value;
            remove => {{projectedTypeName}}.{{evtName}} -= value;
            }
            """);
    }

    /// <inheritdoc cref="WriteFactoryReturnType(IndentedTextWriter, ProjectionEmitContext, MethodDefinition)"/>
    /// <returns>A callback emitting the projected return type of <paramref name="method"/>.</returns>
    public static IndentedTextWriterCallback WriteFactoryReturnType(ProjectionEmitContext context, MethodDefinition method)
    {
        return writer => WriteFactoryReturnType(writer, context, method);
    }

    /// <summary>
    /// Writes the projected return type for a static-factory forwarding method.
    /// </summary>
    public static void WriteFactoryReturnType(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method)
    {
        TypeSignature? returnType = method.Signature?.ReturnType;

        if (returnType is null || returnType.ElementType == ElementType.Void)
        {
            writer.Write("void");
            return;
        }

        TypeSemantics semantics = TypeSemanticsFactory.Get(returnType);
        TypedefNameWriter.WriteTypeName(writer, context, semantics, TypedefNameType.Projected, true);
    }

    private static string GetFactoryPropertyType(ProjectionEmitContext context, PropertyDefinition prop)
    {
        TypeSignature? sig = prop.Signature?.ReturnType;

        if (sig is null)
        {
            return "object";
        }

        TypeSemantics semantics = TypeSemanticsFactory.Get(sig);
        return TypedefNameWriter.WriteTypeName(context, semantics, TypedefNameType.Projected, true).Format();
    }

    /// <inheritdoc cref="WriteFactoryMethodParameters(IndentedTextWriter, ProjectionEmitContext, MethodDefinition, bool)"/>
    /// <returns>A callback emitting the factory-method parameter list.</returns>
    public static IndentedTextWriterCallback WriteFactoryMethodParameters(ProjectionEmitContext context, MethodDefinition method, bool includeTypes)
    {
        return writer => WriteFactoryMethodParameters(writer, context, method, includeTypes);
    }

    /// <summary>
    /// Writes the parameter list for a factory wrapper/forwarding method. When
    /// <paramref name="includeTypes"/> is <see langword="true"/>, emits 'Type name'
    /// pairs; otherwise emits names only (for forwarding call sites).
    /// </summary>
    public static void WriteFactoryMethodParameters(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method, bool includeTypes)
    {
        MethodSignature? sig = method.Signature;

        if (sig is null)
        {
            return;
        }

        for (int i = 0; i < sig.ParameterTypes.Count; i++)
        {
            writer.WriteIf(i > 0, ", ");

            ParameterDefinition? p = method.Parameters.Count > i ? method.Parameters[i].Definition : null;
            string paramName = p?.Name?.Value ?? $"arg{i}";

            if (includeTypes)
            {
                IndentedTextWriterCallback projectedType = TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.Get(sig.ParameterTypes[i]), TypedefNameType.Projected, true);
                writer.Write($"{projectedType} {paramName}");
            }
            else
            {
                writer.Write(paramName);
            }
        }
    }

    /// <summary>
    /// Writes the per-module activation-factory dispatch helper.
    /// </summary>
    public static void WriteModuleActivationFactory(IndentedTextWriter writer, IReadOnlyDictionary<string, HashSet<TypeDefinition>> typesByModule)
    {
        writer.WriteLine();
        writer.WriteLine("using System;");
        foreach (KeyValuePair<string, HashSet<TypeDefinition>> kv in typesByModule)
        {
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                namespace ABI.{{kv.Key}}
                {
                public static class ManagedExports
                {
                public static unsafe void* GetActivationFactory(ReadOnlySpan<char> activatableClassId)
                {
                switch (activatableClassId)
                {
                """);

            // Sort by the type's metadata token / row index so cases appear in WinMD declaration order.
            List<TypeDefinition> orderedTypes = [.. kv.Value];
            orderedTypes.Sort((a, b) =>
            {
                uint ra = a.MetadataToken.Rid;
                uint rb = b.MetadataToken.Rid;
                return ra.CompareTo(rb);
            });
            foreach (TypeDefinition type in orderedTypes)
            {
                (string ns, string name) = type.Names();
                writer.WriteLine(isMultiline: true, $$"""
                    case "{{ns}}.{{name}}":
                        return global::ABI.Impl.{{ns}}.{{IdentifierEscaping.StripBackticks(name)}}ServerActivationFactory.Make();
                    """);
            }
            writer.WriteLine(isMultiline: true, """
                default:
                    return null;
                }
                }
                }
                }
                """);
        }
    }
}
