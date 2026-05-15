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
using WindowsRuntime.ProjectionWriter.Writers;

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

        TypeCategory cat = TypeCategorization.GetCategory(type);

        if ((cat == TypeCategory.Class && TypeCategorization.IsStatic(type)) ||
            (cat == TypeCategory.Interface && TypeCategorization.IsExclusiveTo(type)))
        {
            return;
        }

        string typeName = TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.Projected, true);

        string metadataTypeName = TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.CCW, true);

        _ = map.TryAdd(typeName, metadataTypeName);
    }

    /// <summary>
    /// Writes the per-runtime-class server-activation-factory type for component mode.
    /// </summary>
    public static void WriteFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string projectedTypeName = string.IsNullOrEmpty(typeNs)
            ? $"global::{IdentifierEscaping.StripBackticks(typeName)}"
            : $"global::{typeNs}.{IdentifierEscaping.StripBackticks(typeName)}";
        string factoryTypeName = $"{IdentifierEscaping.StripBackticks(typeName)}ServerActivationFactory";
        bool isActivatable = !TypeCategorization.IsStatic(type) && type.HasDefaultConstructor();

        // Build the inheritance list: factory interfaces ([Activatable]/[Static]) only.
        MetadataCache cache = context.Cache;
        List<TypeDefinition> factoryInterfaces = [];
        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, cache))
        {
            AttributedType info = kv.Value;

            if ((info.Activatable || info.Statics) && info.Type is not null)
            {
                factoryInterfaces.Add(info.Type);
            }
        }

        writer.WriteLine();
        writer.Write($"internal sealed class {factoryTypeName} : global::WindowsRuntime.InteropServices.IActivationFactory");
        foreach (TypeDefinition iface in factoryInterfaces)
        {
            writer.Write(", ");
            // CCW + non-forced namespace is the user-facing interface name (e.g. 'IButtonUtilsStatic').
            TypedefNameWriter.WriteTypedefName(writer, context, iface, TypedefNameType.CCW, false);
            TypedefNameWriter.WriteTypeParams(writer, iface);
        }
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            {
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
            
            private static readonly {{factoryTypeName}} _factory = new();
            
            public object ActivateInstance()
            {
            """);
        if (isActivatable)
        {
            writer.Write($"return new {projectedTypeName}();");
        }
        else
        {
            writer.Write("throw new NotImplementedException();");
        }

        writer.WriteLine();
        writer.WriteLine("}");

        // Emit factory-class members: forwarding methods/properties/events for static factory
        // interfaces, and constructor wrappers for activatable factory interfaces.
        if (cache is not null)
        {
            foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, cache))
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

        writer.WriteLine("}");
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

        string methodName = method.Name?.Value ?? string.Empty;
        writer.WriteLine();
        writer.Write($"public {projectedTypeName} {methodName}(");
        WriteFactoryMethodParameters(writer, context, method, includeTypes: true);
        writer.Write($") => new {projectedTypeName}(");
        WriteFactoryMethodParameters(writer, context, method, includeTypes: false);
        writer.WriteLine(");");
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

        string methodName = method.Name?.Value ?? string.Empty;
        writer.WriteLine();
        writer.Write("public ");
        WriteFactoryReturnType(writer, context, method);
        writer.Write($" {methodName}(");
        WriteFactoryMethodParameters(writer, context, method, includeTypes: true);
        writer.Write($") => {projectedTypeName}.{methodName}(");
        WriteFactoryMethodParameters(writer, context, method, includeTypes: false);
        writer.WriteLine(");");
    }

    /// <summary>
    /// Writes a static-factory forwarding property (single-line getter or full block).
    /// </summary>
    private static void WriteStaticFactoryProperty(IndentedTextWriter writer, ProjectionEmitContext context, PropertyDefinition prop, string projectedTypeName)
    {
        string propName = prop.Name?.Value ?? string.Empty;
        (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
        // Single-line form when no setter is present.
        if (setter is null)
        {
            writer.WriteLine();
            writer.Write("public ");
            WriteFactoryPropertyType(writer, context, prop);
            writer.WriteLine($" {propName} => {projectedTypeName}.{propName};");
            return;
        }

        writer.WriteLine();
        writer.Write("public ");
        WriteFactoryPropertyType(writer, context, prop);
        writer.WriteLine(isMultiline: true, $$"""
             {{propName}}
            {
            """);
        if (getter is not null)
        {
            writer.WriteLine($"get => {projectedTypeName}.{propName};");
        }

        writer.WriteLine(isMultiline: true, $$"""
            set => {{projectedTypeName}}.{{propName}} = value;
            }
            """);
    }

    /// <summary>
    /// Writes a static-factory forwarding event as a multi-line block.
    /// </summary>
    private static void WriteStaticFactoryEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, string projectedTypeName)
    {
        string evtName = evt.Name?.Value ?? string.Empty;
        writer.WriteLine();
        writer.Write("public event ");

        if (evt.EventType is not null)
        {
            TypeSemantics evtSemantics = TypeSemanticsFactory.GetFromTypeDefOrRef(evt.EventType);
            TypedefNameWriter.WriteTypeName(writer, context, evtSemantics, TypedefNameType.Projected, false);
        }

        writer.WriteLine(isMultiline: true, $$"""
             {{evtName}}
            {
            add => {{projectedTypeName}}.{{evtName}} += value;
            remove => {{projectedTypeName}}.{{evtName}} -= value;
            }
            """);
    }

    private static void WriteFactoryReturnType(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method)
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

    private static void WriteFactoryPropertyType(IndentedTextWriter writer, ProjectionEmitContext context, PropertyDefinition prop)
    {
        TypeSignature? sig = prop.Signature?.ReturnType;

        if (sig is null)
        {
            writer.Write("object"); return;
        }

        TypeSemantics semantics = TypeSemanticsFactory.Get(sig);
        TypedefNameWriter.WriteTypeName(writer, context, semantics, TypedefNameType.Projected, true);
    }

    private static void WriteFactoryMethodParameters(IndentedTextWriter writer, ProjectionEmitContext context, MethodDefinition method, bool includeTypes)
    {
        MethodSignature? sig = method.Signature;

        if (sig is null)
        {
            return;
        }

        for (int i = 0; i < sig.ParameterTypes.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(", ");
            }

            ParameterDefinition? p = method.Parameters.Count > i ? method.Parameters[i].Definition : null;
            string paramName = p?.Name?.Value ?? $"arg{i}";

            if (includeTypes)
            {
                TypeSemantics semantics = TypeSemanticsFactory.Get(sig.ParameterTypes[i]);
                TypedefNameWriter.WriteTypeName(writer, context, semantics, TypedefNameType.Projected, true);
                writer.Write($" {paramName}");
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
