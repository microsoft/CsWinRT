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
    /// <summary>Mirrors C++ <c>is_fast_abi_class</c>.</summary>
    public static bool IsFastAbiClass(TypeDefinition type, Settings settings)
    {
        // Fast ABI is enabled when the type is marked [FastAbi] and netstandard_compat is off
        // (CsWinRT 3.0 always has netstandard_compat = false, but we keep the gate for fidelity).
        return !settings.NetstandardCompat &&
               TypeCategorization.HasAttribute(type, "Windows.Foundation.Metadata", "FastAbiAttribute");
    }

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

    /// <summary>
    /// Returns the [Default] interface and the [ExclusiveTo] interfaces (sorted) for fast ABI.
    /// Mirrors C++ <c>get_default_and_exclusive_interfaces</c> + <c>sort_fast_abi_ifaces</c>.
    /// </summary>
    public static (TypeDefinition? DefaultInterface, System.Collections.Generic.List<TypeDefinition> OtherInterfaces) GetFastAbiInterfaces(TypeDefinition classType)
    {
        TypeDefinition? defaultIface = null;
        System.Collections.Generic.List<TypeDefinition> exclusiveIfaces = new();
        foreach (InterfaceImplementation impl in classType.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? ifaceTd = impl.Interface as TypeDefinition;
            if (ifaceTd is null && _cacheRef is not null)
            {
                try { ifaceTd = impl.Interface.Resolve(_cacheRef.RuntimeContext); }
                catch { ifaceTd = null; }
            }
            if (ifaceTd is null) { continue; }

            if (Helpers.IsDefaultInterface(impl))
            {
                defaultIface = ifaceTd;
            }
            else if (TypeCategorization.IsExclusiveTo(ifaceTd))
            {
                exclusiveIfaces.Add(ifaceTd);
            }
        }
        // Sort exclusive interfaces by:
        // 1. Number of [PreviousContractVersion] attrs (ascending; newer interfaces have more)
        // 2. Contract version (ascending)
        // 3. Type version (ascending)
        // 4. Type namespace and name (ascending)
        exclusiveIfaces.Sort((a, b) =>
        {
            int aPrev = -CountAttributes(a, "Windows.Foundation.Metadata", "PreviousContractVersionAttribute");
            int bPrev = -CountAttributes(b, "Windows.Foundation.Metadata", "PreviousContractVersionAttribute");
            if (aPrev != bPrev) { return aPrev.CompareTo(bPrev); }

            int? aCV = Helpers.GetContractVersion(a);
            int? bCV = Helpers.GetContractVersion(b);
            if (aCV.HasValue && bCV.HasValue && aCV.Value != bCV.Value) { return aCV.Value.CompareTo(bCV.Value); }

            int? aV = Helpers.GetVersion(a);
            int? bV = Helpers.GetVersion(b);
            if (aV.HasValue && bV.HasValue && aV.Value != bV.Value) { return aV.Value.CompareTo(bV.Value); }

            string aNs = a.Namespace?.Value ?? string.Empty;
            string bNs = b.Namespace?.Value ?? string.Empty;
            if (aNs != bNs) { return System.StringComparer.Ordinal.Compare(aNs, bNs); }
            return System.StringComparer.Ordinal.Compare(a.Name?.Value ?? string.Empty, b.Name?.Value ?? string.Empty);
        });
        return (defaultIface, exclusiveIfaces);
    }

    private static int CountAttributes(IHasCustomAttribute member, string ns, string name)
    {
        int count = 0;
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? type = attr.Constructor?.DeclaringType;
            if (type is not null && type.Namespace == ns && type.Name == name) { count++; }
        }
        return count;
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
        w.Write(" static class ");
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
        // Per-property accessor state (origin tracking for getter/setter)
        Dictionary<string, StaticPropertyAccessorState> properties = new(System.StringComparer.Ordinal);
        // Track the static factory ifaces we've emitted objref fields for (to dedupe)
        HashSet<string> emittedObjRefs = new(System.StringComparer.Ordinal);

        string runtimeClassFullName = (type.Namespace?.Value ?? string.Empty) + "." + (type.Name?.Value ?? string.Empty);

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, _cacheRef))
        {
            AttributedType factory = kv.Value;
            if (!(factory.Statics && factory.Type is not null)) { continue; }
            TypeDefinition staticIface = factory.Type;

            // Compute the objref name for this static factory interface.
            string objRef = GetObjRefName(w, staticIface);
            // Compute the ABI Methods static class name (e.g. "global::ABI.Windows.System.ILauncherStaticsMethods")
            string abiClass = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
            {
                WriteTypedefName(w, staticIface, TypedefNameType.StaticAbiClass, true);
            }));
            if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal))
            {
                abiClass = "global::" + abiClass;
            }

            // Emit the lazy static objref field (mirrors truth's pattern) once per static iface.
            if (emittedObjRefs.Add(objRef))
            {
                WriteStaticFactoryObjRef(w, staticIface, runtimeClassFullName, objRef);
            }

            // Methods
            foreach (MethodDefinition method in staticIface.Methods)
            {
                if (Helpers.IsSpecial(method)) { continue; }
                MethodSig sig = new(method);
                string mname = method.Name?.Value ?? string.Empty;
                w.Write("\npublic static ");
                WriteProjectionReturnType(w, sig);
                w.Write(" ");
                w.Write(mname);
                w.Write("(");
                WriteParameterList(w, sig);
                if (w.Settings.ReferenceProjection)
                {
                    // Mirrors C++ write_abi_static_method_call (code_writers.h:1637): static
                    // method bodies become 'throw null' in reference projection mode.
                    w.Write(") => throw null;\n");
                }
                else
                {
                    w.Write(") => ");
                    w.Write(abiClass);
                    w.Write(".");
                    w.Write(mname);
                    w.Write("(");
                    w.Write(objRef);
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        w.Write(", ");
                        WriteParameterNameWithModifier(w, sig.Params[i]);
                    }
                    w.Write(");\n");
                }
            }
            // Events: dispatch via static ABI class which returns an event source.
            foreach (EventDefinition evt in staticIface.Events)
            {
                string evtName = evt.Name?.Value ?? string.Empty;
                w.Write("\npublic static event ");
                WriteEventType(w, evt);
                w.Write(" ");
                w.Write(evtName);
                w.Write("\n{\n");
                if (w.Settings.ReferenceProjection)
                {
                    // Mirrors C++ write_abi_event_source_static_method_call (code_writers.h:1711):
                    // event accessor bodies become 'throw null' in reference projection mode.
                    w.Write("    add => throw null;\n");
                    w.Write("    remove => throw null;\n");
                }
                else
                {
                    w.Write("    add => ");
                    w.Write(abiClass);
                    w.Write(".");
                    w.Write(evtName);
                    w.Write("(");
                    w.Write(objRef);
                    w.Write(", ");
                    w.Write(objRef);
                    w.Write(").Subscribe(value);\n");
                    w.Write("    remove => ");
                    w.Write(abiClass);
                    w.Write(".");
                    w.Write(evtName);
                    w.Write("(");
                    w.Write(objRef);
                    w.Write(", ");
                    w.Write(objRef);
                    w.Write(").Unsubscribe(value);\n");
                }
                w.Write("}\n");
            }
            // Properties (merge getter/setter across interfaces, tracking origin per accessor)
            foreach (PropertyDefinition prop in staticIface.Properties)
            {
                string propName = prop.Name?.Value ?? string.Empty;
                (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
                string propType = WritePropType(w, prop);
                if (!properties.TryGetValue(propName, out StaticPropertyAccessorState? state))
                {
                    state = new StaticPropertyAccessorState { PropTypeText = propType };
                    properties[propName] = state;
                }
                if (getter is not null && !state.HasGetter)
                {
                    state.HasGetter = true;
                    state.GetterAbiClass = abiClass;
                    state.GetterObjRef = objRef;
                }
                if (setter is not null && !state.HasSetter)
                {
                    state.HasSetter = true;
                    state.SetterAbiClass = abiClass;
                    state.SetterObjRef = objRef;
                }
            }
        }

        // Emit properties with merged accessors
        foreach (KeyValuePair<string, StaticPropertyAccessorState> kv in properties)
        {
            StaticPropertyAccessorState s = kv.Value;
            w.Write("\npublic static ");
            w.Write(s.PropTypeText);
            w.Write(" ");
            w.Write(kv.Key);
            // Getter-only -> expression body; otherwise -> accessor block (matches truth).
            // In ref mode, all accessor bodies emit '=> throw null;' (mirrors C++
            // write_abi_get/set_property_static_method_call, code_writers.h:1669, 1683).
            bool getterOnly = s.HasGetter && !s.HasSetter;
            if (getterOnly)
            {
                if (w.Settings.ReferenceProjection)
                {
                    w.Write(" => throw null;\n");
                }
                else
                {
                    w.Write(" => ");
                    w.Write(s.GetterAbiClass);
                    w.Write(".");
                    w.Write(kv.Key);
                    w.Write("(");
                    w.Write(s.GetterObjRef);
                    w.Write(");\n");
                }
            }
            else
            {
                w.Write(" { ");
                if (s.HasGetter)
                {
                    if (w.Settings.ReferenceProjection)
                    {
                        w.Write("get => throw null; ");
                    }
                    else
                    {
                        w.Write("get => ");
                        w.Write(s.GetterAbiClass);
                        w.Write(".");
                        w.Write(kv.Key);
                        w.Write("(");
                        w.Write(s.GetterObjRef);
                        w.Write("); ");
                    }
                }
                if (s.HasSetter)
                {
                    if (w.Settings.ReferenceProjection)
                    {
                        w.Write("set => throw null; ");
                    }
                    else
                    {
                        w.Write("set => ");
                        w.Write(s.SetterAbiClass);
                        w.Write(".");
                        w.Write(kv.Key);
                        w.Write("(");
                        w.Write(s.SetterObjRef);
                        w.Write(", value); ");
                    }
                }
                w.Write("}\n");
            }
        }
    }

    private sealed class StaticPropertyAccessorState
    {
        public bool HasGetter;
        public bool HasSetter;
        public string PropTypeText = string.Empty;
        public string GetterAbiClass = string.Empty;
        public string GetterObjRef = string.Empty;
        public string SetterAbiClass = string.Empty;
        public string SetterObjRef = string.Empty;
    }

    /// <summary>
    /// Emits the static lazy objref property for a static factory interface (mirrors truth's
    /// pattern: lazy <c>WindowsRuntimeObjectReference.GetActivationFactory(...)</c>).
    /// </summary>
    private static void WriteStaticFactoryObjRef(TypeWriter w, TypeDefinition staticIface, string runtimeClassFullName, string objRefName)
    {
        w.Write("\nprivate static WindowsRuntimeObjectReference ");
        w.Write(objRefName);
        w.Write("\n{\n");
        if (w.Settings.ReferenceProjection)
        {
            // Mirrors C++ write_static_objref_definition (code_writers.h:2789): in ref mode
            // the static factory objref getter body is just 'throw null;'.
            w.Write("    get\n    {\n        throw null;\n    }\n}\n");
            return;
        }
        w.Write("    get\n    {\n");
        w.Write("        var __");
        w.Write(objRefName);
        w.Write(" = field;\n");
        w.Write("        if (__");
        w.Write(objRefName);
        w.Write(" != null && __");
        w.Write(objRefName);
        w.Write(".IsInCurrentContext)\n        {\n");
        w.Write("            return __");
        w.Write(objRefName);
        w.Write(";\n        }\n");
        w.Write("        return field = WindowsRuntimeObjectReference.GetActivationFactory(\"");
        w.Write(runtimeClassFullName);
        w.Write("\", ");
        WriteIidExpression(w, staticIface);
        w.Write(");\n    }\n}\n");
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
        // Mirrors C++ write_class which uses '%class' (no partial) — runtime classes
        // are emitted as plain (non-partial) classes.
        w.Write("class ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        WriteTypeInheritance(w, type, false, true);
        w.Write("\n{\n");

        // ObjRef field definitions for each implemented interface (mirrors C++ write_class_objrefs_definition).
        // These back the per-interface dispatch in instance methods/properties and the
        // IWindowsRuntimeInterface<T>.GetInterface() implementations.
        WriteClassObjRefDefinitions(w, type);

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
                // For unsealed classes, the default interface objref needs to be initialized only
                // when GetType() matches the projected class exactly (derived classes have their own
                // default interface). The init; accessor on _objRef_<DefaultIface> allows this set.
                ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(type);
                if (defaultIface is not null)
                {
                    string defaultObjRefName = GetObjRefName(w, defaultIface);
                    w.Write("if (GetType() == typeof(");
                    w.Write(typeName);
                    w.Write("))\n{\n");
                    w.Write(defaultObjRefName);
                    w.Write(" = NativeObjectReference;\n");
                    w.Write("}\n");
                }
            }
            if (gcPressure > 0)
            {
                w.Write("GC.AddMemoryPressure(");
                w.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.Write(");\n");
            }
            w.Write("}\n");
        }
        else if (_cacheRef is not null)
        {
            // In ref mode, if WriteAttributedTypes will not emit any public constructors,
            // we need a 'private TypeName() { throw null; }' to suppress the C# compiler's
            // implicit public default constructor (which would expose an unintended API).
            // Mirrors C++ code_writers.h:9519-9538 exactly: a type has constructors when
            // either:
            //  - factory.activatable is true (parameterless or parameterized — Activatable
            //    always emits at least one ctor), OR
            //  - factory.composable && factory.type && factory.type.MethodList().size() > 0
            //    (composable factories with NO methods don't emit any ctors).
            bool hasRefModeCtors = false;
            foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, _cacheRef))
            {
                AttributedType factory = kv.Value;
                if (factory.Activatable)
                {
                    hasRefModeCtors = true;
                    break;
                }
                if (factory.Composable && factory.Type is not null && factory.Type.Methods.Count > 0)
                {
                    hasRefModeCtors = true;
                    break;
                }
            }
            if (!hasRefModeCtors)
            {
                w.Write("\nprivate ");
                w.Write(typeName);
                w.Write("() { throw null; }\n");
            }
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

        // Class members from interfaces (instance methods, properties, events)
        // Override hooks must be emitted BEFORE the public members to match the C++
        // ordering (write_class line 9591/9600/9601: hooks first, then write_class_members).
        // HasUnwrappableNativeObjectReference and IsOverridableInterface overrides.
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

            // IsOverridableInterface override (mirrors C++ write_custom_query_interface_impl).
            // Emit '|| <iidExpr> == iid' for each [Overridable] interface impl, then '|| base.IsOverridableInterface(in iid)'
            // if the type has a base class, finally fall back to 'false' if no entries.
            w.Write("\nprotected override bool IsOverridableInterface(in Guid iid) => ");
            bool firstClause = true;
            foreach (InterfaceImplementation impl in type.Interfaces)
            {
                if (!Helpers.IsOverridable(impl)) { continue; }
                ITypeDefOrRef? implRef = impl.Interface;
                if (implRef is null) { continue; }
                if (!firstClause) { w.Write(" || "); }
                firstClause = false;
                WriteIidExpression(w, implRef);
                w.Write(" == iid");
            }
            // base call when type has a non-object base class
            bool hasBaseClass = type.BaseType is not null
                && !(type.BaseType.Namespace?.Value == "System" && type.BaseType.Name?.Value == "Object")
                && !(type.BaseType.Namespace?.Value == "WindowsRuntime" && type.BaseType.Name?.Value == "WindowsRuntimeObject");
            if (hasBaseClass)
            {
                if (!firstClause) { w.Write(" || "); }
                w.Write("base.IsOverridableInterface(in iid)");
                firstClause = false;
            }
            if (firstClause) { w.Write("false"); }
            w.Write(";\n");
        }

        WriteClassMembers(w, type);

        // Static members from [Static] factory interfaces (e.g. GetForCurrentView).
        WriteStaticClassMembers(w, type);

        w.Write("}\n");
    }
}
