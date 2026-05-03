// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Component-mode helpers, mirroring functions in <c>code_writers.h</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>add_metadata_type_entry</c>.</summary>
    public static void AddMetadataTypeEntry(TypeWriter w, TypeDefinition type, ConcurrentDictionary<string, string> map)
    {
        if (!w.Settings.Component) { return; }
        TypeCategory cat = TypeCategorization.GetCategory(type);
        if ((cat == TypeCategory.Class && TypeCategorization.IsStatic(type)) ||
            (cat == TypeCategory.Interface && TypeCategorization.IsExclusiveTo(type)))
        {
            return;
        }
        string typeName = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            WriteTypeParams(w, type);
        }));
        string metadataTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, type, TypedefNameType.CCW, true);
            WriteTypeParams(w, type);
        }));
        _ = map.TryAdd(typeName, metadataTypeName);
    }

    /// <summary>Mirrors C++ <c>write_factory_class</c> (simplified).</summary>
    public static void WriteFactoryClass(TypeWriter w, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;
        string typeNs = type.Namespace?.Value ?? string.Empty;
        // Mirror C++ 'write_type_name(type, Projected)' which for an authored type produces 'global::<ns>.<name>'.
        string projectedTypeName = string.IsNullOrEmpty(typeNs)
            ? $"global::{Helpers.StripBackticks(typeName)}"
            : $"global::{typeNs}.{Helpers.StripBackticks(typeName)}";
        string factoryTypeName = $"{Helpers.StripBackticks(typeName)}ServerActivationFactory";
        bool isActivatable = !TypeCategorization.IsStatic(type) && Helpers.HasDefaultConstructor(type);

        // Build the inheritance list: factory interfaces ([Activatable]/[Static]) only.
        // Mirrors C++ write_factory_class_inheritance.
        MetadataCache? cache = GetMetadataCache();
        List<TypeDefinition> factoryInterfaces = new();
        if (cache is not null)
        {
            foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, cache))
            {
                AttributedType info = kv.Value;
                if ((info.Activatable || info.Statics) && info.Type is not null)
                {
                    factoryInterfaces.Add(info.Type);
                }
            }
        }

        w.Write("\ninternal sealed class ");
        w.Write(factoryTypeName);
        w.Write(" : global::WindowsRuntime.InteropServices.IActivationFactory");
        foreach (TypeDefinition iface in factoryInterfaces)
        {
            w.Write(", ");
            // Mirror C++ 'write_type_name(factory.type, CCW, false)'. For factory interfaces,
            // CCW + non-forced namespace is the user-facing interface name (e.g. 'IButtonUtilsStatic').
            WriteTypedefName(w, iface, TypedefNameType.CCW, false);
            WriteTypeParams(w, iface);
        }
        w.Write("\n{\n");

        w.Write("static ");
        w.Write(factoryTypeName);
        w.Write("()\n{\n");
        w.Write("global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(");
        w.Write(projectedTypeName);
        w.Write(").TypeHandle);\n}\n");

        w.Write("\npublic static unsafe void* Make()\n{\nreturn global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<global::WindowsRuntime.InteropServices.IActivationFactory>\n    .ConvertToUnmanaged(_factory, in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IActivationFactory)\n    .DetachThisPtrUnsafe();\n}\n");

        w.Write("\nprivate static readonly ");
        w.Write(factoryTypeName);
        w.Write(" _factory = new();\n");

        w.Write("\npublic object ActivateInstance()\n{\n");
        if (isActivatable)
        {
            w.Write("return new ");
            w.Write(projectedTypeName);
            w.Write("();");
        }
        else
        {
            w.Write("throw new NotImplementedException();");
        }
        w.Write("\n}\n");

        // Emit factory-class members: forwarding methods/properties/events for static factory
        // interfaces, and constructor wrappers for activatable factory interfaces.
        // Mirrors C++ write_factory_class_members.
        if (cache is not null)
        {
            foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, cache))
            {
                AttributedType info = kv.Value;
                if (info.Type is null) { continue; }

                if (info.Activatable)
                {
                    foreach (MethodDefinition method in info.Type.Methods)
                    {
                        if (method.IsConstructor) { continue; }
                        WriteFactoryActivatableMethod(w, method, projectedTypeName);
                    }
                }
                else if (info.Statics)
                {
                    foreach (MethodDefinition method in info.Type.Methods)
                    {
                        if (method.IsConstructor) { continue; }
                        WriteStaticFactoryMethod(w, method, projectedTypeName);
                    }
                    foreach (PropertyDefinition prop in info.Type.Properties)
                    {
                        WriteStaticFactoryProperty(w, prop, projectedTypeName);
                    }
                    foreach (EventDefinition evt in info.Type.Events)
                    {
                        WriteStaticFactoryEvent(w, evt, projectedTypeName);
                    }
                }
            }
        }

        w.Write("}\n");
    }

    /// <summary>
    /// Writes a factory-class activatable wrapper method: <c>public T MethodName(args) =&gt; new T(args);</c>.
    /// Mirrors C++ <c>write_factory_activatable_method</c>.
    /// </summary>
    private static void WriteFactoryActivatableMethod(TypeWriter w, MethodDefinition method, string projectedTypeName)
    {
        if (method.IsSpecialName) { return; }
        string methodName = method.Name?.Value ?? string.Empty;
        w.Write("\npublic ");
        w.Write(projectedTypeName);
        w.Write(" ");
        w.Write(methodName);
        w.Write("(");
        WriteFactoryMethodParameters(w, method, includeTypes: true);
        w.Write(") => new ");
        w.Write(projectedTypeName);
        w.Write("(");
        WriteFactoryMethodParameters(w, method, includeTypes: false);
        w.Write(");\n");
    }

    /// <summary>
    /// Writes a static-factory forwarding method: <c>public Ret MethodName(args) =&gt; global::Ns.Type.MethodName(args);</c>.
    /// Mirrors C++ <c>write_static_factory_method</c>.
    /// </summary>
    private static void WriteStaticFactoryMethod(TypeWriter w, MethodDefinition method, string projectedTypeName)
    {
        if (method.IsSpecialName) { return; }
        string methodName = method.Name?.Value ?? string.Empty;
        w.Write("\npublic ");
        WriteFactoryReturnType(w, method);
        w.Write(" ");
        w.Write(methodName);
        w.Write("(");
        WriteFactoryMethodParameters(w, method, includeTypes: true);
        w.Write(") => ");
        w.Write(projectedTypeName);
        w.Write(".");
        w.Write(methodName);
        w.Write("(");
        WriteFactoryMethodParameters(w, method, includeTypes: false);
        w.Write(");\n");
    }

    /// <summary>
    /// Writes a static-factory forwarding property: a multi-line block matching C++
    /// <c>write_property</c> + <c>write_static_factory_property</c>.
    /// </summary>
    private static void WriteStaticFactoryProperty(TypeWriter w, PropertyDefinition prop, string projectedTypeName)
    {
        string propName = prop.Name?.Value ?? string.Empty;
        (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
        // Single-line form when no setter is present (mirrors C++ early-return path).
        if (setter is null)
        {
            w.Write("\npublic ");
            WriteFactoryPropertyType(w, prop);
            w.Write(" ");
            w.Write(propName);
            w.Write(" => ");
            w.Write(projectedTypeName);
            w.Write(".");
            w.Write(propName);
            w.Write(";\n");
            return;
        }
        w.Write("\npublic ");
        WriteFactoryPropertyType(w, prop);
        w.Write(" ");
        w.Write(propName);
        w.Write("\n{\n");
        if (getter is not null)
        {
            w.Write("get => ");
            w.Write(projectedTypeName);
            w.Write(".");
            w.Write(propName);
            w.Write(";\n");
        }
        w.Write("set => ");
        w.Write(projectedTypeName);
        w.Write(".");
        w.Write(propName);
        w.Write(" = value;\n");
        w.Write("}\n");
    }

    /// <summary>
    /// Writes a static-factory forwarding event as a multi-line block matching C++
    /// <c>write_event</c> + <c>write_static_factory_event</c>.
    /// </summary>
    private static void WriteStaticFactoryEvent(TypeWriter w, EventDefinition evt, string projectedTypeName)
    {
        string evtName = evt.Name?.Value ?? string.Empty;
        w.Write("\npublic event ");
        if (evt.EventType is not null)
        {
            TypeSemantics evtSemantics = TypeSemanticsFactory.GetFromTypeDefOrRef(evt.EventType);
            WriteTypeName(w, evtSemantics, TypedefNameType.Projected, false);
        }
        w.Write(" ");
        w.Write(evtName);
        w.Write("\n{\n");
        w.Write("add => ");
        w.Write(projectedTypeName);
        w.Write(".");
        w.Write(evtName);
        w.Write(" += value;\n");
        w.Write("remove => ");
        w.Write(projectedTypeName);
        w.Write(".");
        w.Write(evtName);
        w.Write(" -= value;\n");
        w.Write("}\n");
    }

    private static void WriteFactoryReturnType(TypeWriter w, MethodDefinition method)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? returnType = method.Signature?.ReturnType;
        if (returnType is null || returnType.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Void)
        {
            w.Write("void");
            return;
        }
        TypeSemantics semantics = TypeSemanticsFactory.Get(returnType);
        WriteTypeName(w, semantics, TypedefNameType.Projected, true);
    }

    private static void WriteFactoryPropertyType(TypeWriter w, PropertyDefinition prop)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? sig = prop.Signature?.ReturnType;
        if (sig is null) { w.Write("object"); return; }
        TypeSemantics semantics = TypeSemanticsFactory.Get(sig);
        WriteTypeName(w, semantics, TypedefNameType.Projected, true);
    }

    private static void WriteFactoryMethodParameters(TypeWriter w, MethodDefinition method, bool includeTypes)
    {
        AsmResolver.DotNet.Signatures.MethodSignature? sig = method.Signature;
        if (sig is null) { return; }
        for (int i = 0; i < sig.ParameterTypes.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            ParameterDefinition? p = method.Parameters.Count > i + (method.IsStatic ? 0 : 0) ? method.Parameters[i].Definition : null;
            string paramName = p?.Name?.Value ?? $"arg{i}";
            if (includeTypes)
            {
                TypeSemantics semantics = TypeSemanticsFactory.Get(sig.ParameterTypes[i]);
                WriteTypeName(w, semantics, TypedefNameType.Projected, true);
                w.Write(" ");
                w.Write(paramName);
            }
            else
            {
                w.Write(paramName);
            }
        }
    }

    /// <summary>Mirrors C++ <c>write_module_activation_factory</c> (simplified).</summary>
    public static void WriteModuleActivationFactory(TextWriter w, IReadOnlyDictionary<string, HashSet<TypeDefinition>> typesByModule)
    {
        w.Write("\nusing System;\n");
        foreach (KeyValuePair<string, HashSet<TypeDefinition>> kv in typesByModule)
        {
            w.Write("\nnamespace ABI.");
            w.Write(kv.Key);
            w.Write("\n{\npublic static class ManagedExports\n{\npublic static unsafe void* GetActivationFactory(ReadOnlySpan<char> activatableClassId)\n{\nswitch (activatableClassId)\n{\n");
            foreach (TypeDefinition type in kv.Value)
            {
                string ns = type.Namespace?.Value ?? string.Empty;
                string name = type.Name?.Value ?? string.Empty;
                w.Write("case \"");
                w.Write(ns);
                w.Write(".");
                w.Write(name);
                w.Write("\":\n    return ");
                // Mirror C++ 'write_type_name(type, CCW, true)' which for an authored type
                // emits 'global::ABI.Impl.<ns>.<name>'.
                w.Write("global::ABI.Impl.");
                w.Write(ns);
                w.Write(".");
                w.Write(Helpers.StripBackticks(name));
                w.Write("ServerActivationFactory.Make();\n");
            }
            w.Write("default:\n    return null;\n}\n}\n}\n}\n");
        }
    }
}
