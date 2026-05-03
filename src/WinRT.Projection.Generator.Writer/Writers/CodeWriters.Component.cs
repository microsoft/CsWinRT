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
        string factoryTypeName = $"{typeName}ServerActivationFactory";
        bool isActivatable = !TypeCategorization.IsStatic(type) && Helpers.HasDefaultConstructor(type);

        w.Write("\ninternal sealed class ");
        w.Write(factoryTypeName);
        w.Write(" : global::WindowsRuntime.InteropServices.IActivationFactory\n{\n");
        w.Write("static ");
        w.Write(factoryTypeName);
        w.Write("()\n{\n");
        w.Write("global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(");
        w.Write(typeName);
        w.Write(").TypeHandle);\n}\n");

        w.Write("\npublic static unsafe void* Make()\n{\nreturn global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<global::WindowsRuntime.InteropServices.IActivationFactory>\n    .ConvertToUnmanaged(_factory, in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IActivationFactory)\n    .DetachThisPtrUnsafe();\n}\n");

        w.Write("\nprivate static readonly ");
        w.Write(factoryTypeName);
        w.Write(" _factory = new();\n");

        w.Write("\npublic object ActivateInstance()\n{\n");
        if (isActivatable)
        {
            w.Write("return new ");
            w.Write(typeName);
            w.Write("();");
        }
        else
        {
            w.Write("throw new NotImplementedException();");
        }
        w.Write("\n}\n");

        w.Write("}\n");
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
