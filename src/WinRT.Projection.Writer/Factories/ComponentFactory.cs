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
    public static void WriteFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        (string typeNs, string typeName) = type.Names();
        string projectedTypeName = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, typeName);
        string factoryTypeName = $"{IdentifierEscaping.StripBackticks(typeName)}ServerActivationFactory";

        // Writes the set of interfaces implemented by the factory class ('IActivationFactory' is always included)
        void WriteBaseInterfaceList(IndentedTextWriter writer)
        {
            writer.Write("global::WindowsRuntime.InteropServices.IActivationFactory");

            // Build the inheritance list: factory interfaces ('[Activatable]' or '[Static]') only
            foreach ((_, AttributedType type) in AttributedTypes.Get(type, context.Cache))
            {
                if ((type.Activatable || type.Statics) && type.Type is not null)
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
            bool isActivatable = !type.IsStatic && type.HasDefaultConstructor();

            if (isActivatable)
            {
                writer.Write($"return new {projectedTypeName}();");
            }
            else
            {
                writer.Write("throw new NotImplementedException();");
            }
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

                static {{factoryTypeName}}()
                {
                    global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof({{projectedTypeName}}).TypeHandle);
                }
            
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
        // interfaces, and constructor wrappers for activatable factory interfaces.
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
                    if (method.IsConstructor)
                    {
                        continue;
                    }

                    WriteFactoryActivatableMethod(writer, context, method, projectedTypeName);
                }
            }
            else if (info.Statics)
            {
                foreach (MethodDefinition method in info.Type.Methods)
                {
                    if (method.IsConstructor)
                    {
                        continue;
                    }

                    WriteStaticFactoryMethod(writer, context, method, projectedTypeName);
                }
                foreach (PropertyDefinition prop in info.Type.Properties)
                {
                    WriteStaticFactoryProperty(writer, context, prop, projectedTypeName);
                }
                foreach (EventDefinition evt in info.Type.Events)
                {
                    WriteStaticFactoryEvent(writer, context, evt, projectedTypeName);
                }
            }
        }
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
