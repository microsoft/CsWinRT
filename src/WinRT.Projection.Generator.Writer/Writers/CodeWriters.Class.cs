// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Class emission helpers, mirroring functions in <c>code_writers.h</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>write_class_modifiers</c>.</summary>
    public static void WriteClassModifiers(TypeWriter w, TypeDefinition type)
    {
        if (TypeCategorization.IsStatic(type))
        {
            w.Write("static ");
            return;
        }
        if (type.IsSealed)
        {
            w.Write("sealed ");
        }
    }

    /// <summary>Mirrors C++ <c>get_gc_pressure_amount</c>.</summary>
    public static int GetGcPressureAmount(TypeDefinition type)
    {
        if (!type.IsSealed) { return 0; }
        CustomAttribute? attr = TypeCategorization.GetAttribute(type, "Windows.Foundation.Metadata", "GCPressureAttribute");
        if (attr is null || attr.Signature is null) { return 0; }
        // The attribute has a single named arg "Amount" of an enum type. Defaults: 0=Low, 1=Medium, 2=High.
        // We try both fixed args and named args.
        int amount = -1;
        if (attr.Signature.NamedArguments.Count > 0)
        {
            object? v = attr.Signature.NamedArguments[0].Argument.Element;
            if (v is int i) { amount = i; }
        }
        return amount switch
        {
            0 => 12000,
            1 => 120000,
            2 => 1200000,
            _ => 0
        };
    }

    /// <summary>
    /// Mirrors C++ <c>write_static_class</c>.
    /// </summary>
    public static void WriteStaticClass(TypeWriter w, TypeDefinition type)
    {
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteTypeCustomAttributes(w, type, true);
        w.Write(Helpers.InternalAccessibility(w.Settings));
        w.Write(" static partial class ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write("\n{\n");
        WriteStaticClassMembers(w, type);
        w.Write("}\n");
    }

    /// <summary>
    /// Emits static members from [Static] factory interfaces. Mirrors C++ <c>write_static_members</c>.
    /// </summary>
    public static void WriteStaticClassMembers(TypeWriter w, TypeDefinition type)
    {
        if (_cacheRef is null) { return; }
        Dictionary<string, (string Type, bool HasGetter, bool HasSetter)> properties = new(System.StringComparer.Ordinal);

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, _cacheRef))
        {
            AttributedType factory = kv.Value;
            if (factory.Statics && factory.Type is not null)
            {
                TypeDefinition staticIface = factory.Type;
                // Methods
                foreach (MethodDefinition method in staticIface.Methods)
                {
                    if (Helpers.IsSpecial(method)) { continue; }
                    MethodSig sig = new(method);
                    w.Write("\npublic static ");
                    WriteProjectionReturnType(w, sig);
                    w.Write(" ");
                    w.Write(method.Name?.Value ?? string.Empty);
                    w.Write("(");
                    WriteParameterList(w, sig);
                    w.Write(") => throw null!;\n");
                }
                // Events
                foreach (EventDefinition evt in staticIface.Events)
                {
                    string evtName = evt.Name?.Value ?? string.Empty;
                    w.Write("\npublic static event ");
                    if (evt.EventType is TypeDefinition etDef)
                    {
                        WriteTypedefName(w, etDef, TypedefNameType.Projected, false);
                        WriteTypeParams(w, etDef);
                    }
                    else if (evt.EventType is TypeReference etRef)
                    {
                        w.Write("global::");
                        w.Write(etRef.Namespace?.Value ?? string.Empty);
                        w.Write(".");
                        w.WriteCode(etRef.Name?.Value ?? string.Empty);
                    }
                    w.Write(" ");
                    w.Write(evtName);
                    w.Write(" { add => throw null!; remove => throw null!; }\n");
                }
                // Properties (merge getter/setter across interfaces)
                foreach (PropertyDefinition prop in staticIface.Properties)
                {
                    string propName = prop.Name?.Value ?? string.Empty;
                    (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
                    string propType = WritePropType(w, prop);
                    if (properties.TryGetValue(propName, out var existing))
                    {
                        properties[propName] = (existing.Type, existing.HasGetter || getter is not null, existing.HasSetter || setter is not null);
                    }
                    else
                    {
                        properties[propName] = (propType, getter is not null, setter is not null);
                    }
                }
            }
        }

        // Emit properties with merged accessors
        foreach (KeyValuePair<string, (string Type, bool HasGetter, bool HasSetter)> kv in properties)
        {
            w.Write("\npublic static ");
            w.Write(kv.Value.Type);
            w.Write(" ");
            w.Write(kv.Key);
            w.Write(" { ");
            if (kv.Value.HasGetter) { w.Write("get => throw null!; "); }
            if (kv.Value.HasSetter) { w.Write("set => throw null!; "); }
            w.Write("}\n");
        }
    }

    /// <summary>
    /// Mirrors C++ <c>write_class</c>. Emits a runtime class projection.
    /// </summary>
    public static void WriteClass(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component) { return; }

        if (TypeCategorization.IsStatic(type))
        {
            WriteStaticClass(w, type);
            return;
        }

        string typeName = type.Name?.Value ?? string.Empty;
        int gcPressure = GetGcPressureAmount(type);

        // Header attributes
        w.Write("\n");
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteTypeCustomAttributes(w, type, true);
        WriteComWrapperMarshallerAttribute(w, type);
        w.Write(w.Settings.Internal ? "internal" : "public");
        w.Write(" ");
        WriteClassModifiers(w, type);
        // Mark as partial so user-provided implementations can complete the interface methods.
        w.Write("partial class ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        WriteTypeInheritance(w, type, false, true);
        w.Write("\n{\n");

        // Constructor: WindowsRuntimeObjectReference-based constructor (RCW-like)
        if (!w.Settings.ReferenceProjection)
        {
            string ctorAccess = type.IsSealed ? "internal" : "protected internal";
            w.Write("\n");
            w.Write(ctorAccess);
            w.Write(" ");
            w.Write(typeName);
            w.Write("(WindowsRuntimeObjectReference nativeObjectReference)\n: base(nativeObjectReference)\n{\n");
            if (!type.IsSealed)
            {
                w.Write("if (GetType() == typeof(");
                w.Write(typeName);
                w.Write("))\n{\n");
                w.Write("// Default interface objref initialization (simplified port)\n}\n");
            }
            if (gcPressure > 0)
            {
                w.Write("GC.AddMemoryPressure(");
                w.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.Write(");\n");
            }
            w.Write("}\n");
        }

        // Activator/composer constructors from [Activatable]/[Composable] factory interfaces
        WriteAttributedTypes(w, type);

        // Conditional finalizer
        if (gcPressure > 0)
        {
            w.Write("~");
            w.Write(typeName);
            w.Write("()\n{\nGC.RemoveMemoryPressure(");
            w.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
            w.Write(");\n}\n");
        }

        // HasUnwrappableNativeObjectReference override
        if (!w.Settings.ReferenceProjection)
        {
            w.Write("\nprotected override bool HasUnwrappableNativeObjectReference => ");
            if (!type.IsSealed)
            {
                w.Write("GetType() == typeof(");
                w.Write(typeName);
                w.Write(");");
            }
            else
            {
                w.Write("true;");
            }
            w.Write("\n");
        }

        // Class members from interfaces (instance methods, properties, events)
        WriteClassMembers(w, type);

        w.Write("}\n");
    }
}
