// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the projected runtime class type and its members (constructors, properties,
/// methods, events, and the activation-factory glue), plus the static-only variant
/// for <c>[Static]</c> classes.
/// </summary>
internal static class ClassFactory
{
    public static bool IsFastAbiClass(TypeDefinition type)
    {
        // Fast ABI is enabled when the type is marked [FastAbi]. (CsWinRT 3.0 has no
        // netstandard_compat gate -- it was always false in the C# port.)
        return type.HasAttribute("Windows.Foundation.Metadata", "FastAbiAttribute");
    }
    /// <summary>Writes the class modifiers ('static '/'sealed ').</summary>
    public static void WriteClassModifiers(IndentedTextWriter writer, TypeDefinition type)
    {
        if (TypeCategorization.IsStatic(type))
        {
            writer.Write("static ");
            return;
        }
        if (type.IsSealed)
        {
            writer.Write("sealed ");
        }
    }
    /// <summary>
    /// Returns the fast-abi class type for <paramref name="iface"/> if the interface is
    /// exclusive_to a class marked <c>[FastAbi]</c>; otherwise <c>null</c>.
    /// </summary>
    public static TypeDefinition? FindFastAbiClassType(MetadataCache cache, TypeDefinition iface)
    {
        TypeDefinition? exclusiveToClass = AbiTypeHelpers.GetExclusiveToType(cache, iface);
        if (exclusiveToClass is null) { return null; }
        if (!IsFastAbiClass(exclusiveToClass)) { return null; }
        return exclusiveToClass;
    }

    /// <summary>
    /// Returns the fast-abi class info (class type + default interface + sorted other exclusive
    /// interfaces) for <paramref name="iface"/>, if the interface is exclusive_to a fast-abi
    /// class; otherwise <c>null</c>.
    /// </summary>
    public static (TypeDefinition Class, TypeDefinition? Default, System.Collections.Generic.List<TypeDefinition> Others)? GetFastAbiClassForInterface(MetadataCache cache, TypeDefinition iface)
    {
        TypeDefinition? cls = FindFastAbiClassType(cache, iface);
        if (cls is null) { return null; }
        (TypeDefinition? def, System.Collections.Generic.List<TypeDefinition> others) = GetFastAbiInterfaces(cache, cls);
        return (cls, def, others);
    }

    /// <summary>
    /// Whether <paramref name="iface"/> is a non-default exclusive interface of a fast-abi class
    /// (i.e. its members are merged into the default interface's vtable and dispatched through
    /// the default interface's ABI <c>Methods</c> class).
    /// </summary>
    public static bool IsFastAbiOtherInterface(MetadataCache cache, TypeDefinition iface)
    {
        (TypeDefinition Class, TypeDefinition? Default, List<TypeDefinition> Others)? fastAbi = GetFastAbiClassForInterface(cache, iface);
        if (fastAbi is null) { return false; }
        if (fastAbi.Value.Default is not null && InterfacesEqual(fastAbi.Value.Default, iface)) { return false; }
        foreach (TypeDefinition other in fastAbi.Value.Others)
        {
            if (InterfacesEqual(other, iface)) { return true; }
        }
        return false;
    }

    /// <summary>
    /// Returns true if <paramref name="iface"/> is the default interface of a fast-abi class.
    /// </summary>
    public static bool IsFastAbiDefaultInterface(MetadataCache cache, TypeDefinition iface)
    {
        (TypeDefinition Class, TypeDefinition? Default, List<TypeDefinition> Others)? fastAbi = GetFastAbiClassForInterface(cache, iface);
        if (fastAbi is null) { return false; }
        return fastAbi.Value.Default is not null && InterfacesEqual(fastAbi.Value.Default, iface);
    }

    private static bool InterfacesEqual(TypeDefinition a, TypeDefinition b)
    {
        if (a == b) { return true; }
        return (a.Namespace?.Value ?? string.Empty) == (b.Namespace?.Value ?? string.Empty)
            && (a.Name?.Value ?? string.Empty) == (b.Name?.Value ?? string.Empty);
    }

    // We don't have direct access to the active Settings from a static helper that only takes
    // a TypeDefinition. The fast-abi flag is purely determined by the [FastAbiAttribute] (the
    // netstandard_compat gate is always false in CsWinRT 3.0 -- the flag has been removed).

    /// <summary>
    /// Returns the [Default] interface and the [ExclusiveTo] interfaces (sorted) for fast ABI.
    /// </summary>
    public static (TypeDefinition? DefaultInterface, System.Collections.Generic.List<TypeDefinition> OtherInterfaces) GetFastAbiInterfaces(MetadataCache cache, TypeDefinition classType)
    {
        TypeDefinition? defaultIface = null;
        System.Collections.Generic.List<TypeDefinition> exclusiveIfaces = [];
        foreach (InterfaceImplementation impl in classType.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? ifaceTd = impl.Interface as TypeDefinition;
            if (ifaceTd is null)
            {
                try { ifaceTd = impl.Interface.Resolve(cache.RuntimeContext); }
                catch { ifaceTd = null; }
            }
            if (ifaceTd is null) { continue; }

            if (impl.IsDefaultInterface())
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

            int? aCV = a.GetContractVersion();
            int? bCV = b.GetContractVersion();
            if (aCV.HasValue && bCV.HasValue && aCV.Value != bCV.Value) { return aCV.Value.CompareTo(bCV.Value); }

            int? aV = a.GetVersion();
            int? bV = b.GetVersion();
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
    public static int GetGcPressureAmount(TypeDefinition type)
    {
        if (!type.IsSealed) { return 0; }
        CustomAttribute? attr = type.GetAttribute("Windows.Foundation.Metadata", "GCPressureAttribute");
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
    /// <summary>Writes a static class declaration with [ContractVersion]-derived platform suppression.</summary>
    public static void WriteStaticClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        bool prevCheckPlatform = context.CheckPlatform;
        string prevPlatform = context.Platform;
        context.CheckPlatform = true;
        context.Platform = string.Empty;
        try
        {
            MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
            CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, true);
            writer.Write($"{AccessibilityHelper.InternalAccessibility(context.Settings)} static class ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
            TypedefNameWriter.WriteTypeParams(writer, type);
            writer.WriteLine("");
            writer.WriteLine("{");
            WriteStaticClassMembers(writer, context, type);
            writer.WriteLine("}");
        }
        finally
        {
            context.CheckPlatform = prevCheckPlatform;
            context.Platform = prevPlatform;
        }
    }
    /// <summary>Emits static members from [Static] factory interfaces.</summary>
    public static void WriteStaticClassMembers(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Cache is null) { return; }
        // Per-property accessor state (origin tracking for getter/setter)
        Dictionary<string, StaticPropertyAccessorState> properties = new(System.StringComparer.Ordinal);
        // Track the static factory ifaces we've emitted objref fields for (to dedupe)
        HashSet<string> emittedObjRefs = new(System.StringComparer.Ordinal);

        string runtimeClassFullName = (type.Namespace?.Value ?? string.Empty) + "." + (type.Name?.Value ?? string.Empty);

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, context.Cache))
        {
            AttributedType factory = kv.Value;
            if (!(factory.Statics && factory.Type is not null)) { continue; }
            TypeDefinition staticIface = factory.Type;

            // Compute the objref name for this static factory interface.
            string objRef = ObjRefNameGenerator.GetObjRefName(context, staticIface);
            // Compute the ABI Methods static class name (e.g. "global::ABI.Windows.System.ILauncherStaticsMethods")
            IndentedTextWriter __scratchAbiClass = new();
            TypedefNameWriter.WriteTypedefName(__scratchAbiClass, context, staticIface, TypedefNameType.StaticAbiClass, true);
            string abiClass = __scratchAbiClass.ToString();
            if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal))
            {
                abiClass = "global::" + abiClass;
            }

            // Emit the lazy static objref field (mirrors truth's pattern) once per static iface.
            if (emittedObjRefs.Add(objRef))
            {
                WriteStaticFactoryObjRef(writer, context, staticIface, runtimeClassFullName, objRef);
            }

            // Compute the platform attribute string from the static factory interface's
            // [ContractVersion] attribute
            IndentedTextWriter __scratchPlatform = new();
            CustomAttributeFactory.WritePlatformAttribute(__scratchPlatform, context, staticIface);
            string platformAttribute = __scratchPlatform.ToString();

            // Methods
            foreach (MethodDefinition method in staticIface.Methods)
            {
                if (method.IsSpecial()) { continue; }
                MethodSig sig = new(method);
                string mname = method.Name?.Value ?? string.Empty;
                writer.WriteLine("");
                if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
                writer.Write("public static ");
                MethodFactory.WriteProjectionReturnType(writer, context, sig);
                writer.Write($" {mname}(");
                MethodFactory.WriteParameterList(writer, context, sig);
                if (context.Settings.ReferenceProjection)
                {
                    // method bodies become 'throw null' in reference projection mode.
                    writer.WriteLine(") => throw null;");
                }
                else
                {
                    writer.Write($") => {abiClass}.{mname}({objRef}");
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        writer.Write(", ");
                        ClassMembersFactory.WriteParameterNameWithModifier(writer, context, sig.Params[i]);
                    }
                    writer.WriteLine(");");
                }
            }
            // Events: dispatch via static ABI class which returns an event source.
            foreach (EventDefinition evt in staticIface.Events)
            {
                string evtName = evt.Name?.Value ?? string.Empty;
                writer.WriteLine("");
                if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
                writer.Write("public static event ");
                TypedefNameWriter.WriteEventType(writer, context, evt);
                writer.Write($" {evtName}\n{{\n");
                if (context.Settings.ReferenceProjection)
                {
                    // event accessor bodies become 'throw null' in reference projection mode.
                    writer.Write("""
                            add => throw null;
                            remove => throw null;
                        """, isMultiline: true);
                }
                else
                {
                    writer.Write($$"""
                            add => {{abiClass}}.{{evtName}}({{objRef}}, {{objRef}}).Subscribe(value);
                            remove => {{abiClass}}.{{evtName}}({{objRef}}, {{objRef}}).Unsubscribe(value);
                        """, isMultiline: true);
                }
                writer.WriteLine("}");
            }
            // Properties (merge getter/setter across interfaces, tracking origin per accessor)
            foreach (PropertyDefinition prop in staticIface.Properties)
            {
                string propName = prop.Name?.Value ?? string.Empty;
                (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
                string propType = InterfaceFactory.WritePropType(context, prop);
                if (!properties.TryGetValue(propName, out StaticPropertyAccessorState? state))
                {
                    state = new StaticPropertyAccessorState
                    {
                        PropTypeText = propType,
                    };
                    properties[propName] = state;
                }
                if (getter is not null && !state.HasGetter)
                {
                    state.HasGetter = true;
                    state.GetterAbiClass = abiClass;
                    state.GetterObjRef = objRef;
                    state.GetterPlatformAttribute = platformAttribute;
                }
                if (setter is not null && !state.HasSetter)
                {
                    state.HasSetter = true;
                    state.SetterAbiClass = abiClass;
                    state.SetterObjRef = objRef;
                    state.SetterPlatformAttribute = platformAttribute;
                }
            }
        }

        // Emit properties with merged accessors
        foreach (KeyValuePair<string, StaticPropertyAccessorState> kv in properties)
        {
            StaticPropertyAccessorState s = kv.Value;
            writer.WriteLine("");
            // when getter and setter platforms match; otherwise emit per-accessor.
            string getterPlat = s.GetterPlatformAttribute;
            string setterPlat = s.SetterPlatformAttribute;
            string propertyPlat = string.Empty;
            bool bothSidesPresent = s.HasGetter && s.HasSetter;
            if (!bothSidesPresent || getterPlat == setterPlat)
            {
                propertyPlat = !string.IsNullOrEmpty(getterPlat) ? getterPlat : setterPlat;
                getterPlat = string.Empty;
                setterPlat = string.Empty;
            }
            if (!string.IsNullOrEmpty(propertyPlat)) { writer.Write(propertyPlat); }
            writer.Write($"public static {s.PropTypeText} {kv.Key}");
            // Getter-only -> expression body; otherwise -> accessor block (matches truth).
            // In ref mode, all accessor bodies emit '=> throw null;'
            bool getterOnly = s.HasGetter && !s.HasSetter;
            if (getterOnly)
            {
                if (context.Settings.ReferenceProjection)
                {
                    writer.WriteLine(" => throw null;");
                }
                else
                {
                    writer.WriteLine($" => {s.GetterAbiClass}.{kv.Key}({s.GetterObjRef});");
                }
            }
            else
            {
                writer.WriteLine("");
                writer.WriteLine("{");
                if (s.HasGetter)
                {
                    if (!string.IsNullOrEmpty(getterPlat)) { writer.Write(getterPlat); }
                    if (context.Settings.ReferenceProjection)
                    {
                        writer.WriteLine("get => throw null;");
                    }
                    else
                    {
                        writer.WriteLine($"get => {s.GetterAbiClass}.{kv.Key}({s.GetterObjRef});");
                    }
                }
                if (s.HasSetter)
                {
                    if (!string.IsNullOrEmpty(setterPlat)) { writer.Write(setterPlat); }
                    if (context.Settings.ReferenceProjection)
                    {
                        writer.WriteLine("set => throw null;");
                    }
                    else
                    {
                        writer.WriteLine($"set => {s.SetterAbiClass}.{kv.Key}({s.SetterObjRef}, value);");
                    }
                }
                writer.WriteLine("}");
            }
        }
    }

    /// <summary>
    /// Emits the static lazy objref property for a static factory interface (mirrors truth's
    /// pattern: lazy <c>WindowsRuntimeObjectReference.GetActivationFactory(...)</c>).
    /// </summary>
    internal static void WriteStaticFactoryObjRef(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition staticIface, string runtimeClassFullName, string objRefName)
    {
        writer.Write($"\nprivate static WindowsRuntimeObjectReference {objRefName}\n{{\n");
        if (context.Settings.ReferenceProjection)
        {
            // the static factory objref getter body is just 'throw null;'.
            writer.Write("""
                    get
                    {
                        throw null;
                    }
                }
                """, isMultiline: true);
            return;
        }
        writer.WriteLine("    get");
        writer.WriteLine("    {");
        writer.Write("        var __");
        writer.Write(objRefName);
        writer.WriteLine(" = field;");
        writer.Write($"        if (__{objRefName} != null && __{objRefName}.IsInCurrentContext)\n        {{\n            return __{objRefName};\n        }}\n        return field = WindowsRuntimeObjectReference.GetActivationFactory(\"{runtimeClassFullName}\", ");
        ObjRefNameGenerator.WriteIidExpression(writer, context, staticIface);
        writer.Write("""
            );
                }
            }
            """, isMultiline: true);
    }
    /// <summary>Writes a projected runtime class.</summary>
    public static void WriteClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        if (TypeCategorization.IsStatic(type))
        {
            WriteStaticClass(writer, context, type);
            return;
        }
        // Tracks the highest platform seen within this class to suppress redundant
        // [SupportedOSPlatform(...)] emissions across interface boundaries.
        bool prevCheckPlatform = context.CheckPlatform;
        string prevPlatform = context.Platform;
        context.CheckPlatform = true;
        context.Platform = string.Empty;
        try
        {
            WriteClassCore(writer, context, type);
        }
        finally
        {
            context.CheckPlatform = prevCheckPlatform;
            context.Platform = prevPlatform;
        }
    }

    private static void WriteClassCore(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;
        int gcPressure = GetGcPressureAmount(type);

        // Header attributes
        writer.WriteLine("");
        MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, true);
        MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(writer, context, type);
        writer.Write($"{(context.Settings.Internal ? "internal" : "public")} ");
        WriteClassModifiers(writer, type);
        // are emitted as plain (non-partial) classes.
        writer.Write("class ");
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        InterfaceFactory.WriteTypeInheritance(writer, context, type, false, true);
        writer.WriteLine("");
        writer.WriteLine("{");

        // ObjRef field definitions for each implemented interface.
        // These back the per-interface dispatch in instance methods/properties and the
        // IWindowsRuntimeInterface<T>.GetInterface() implementations.
        ObjRefNameGenerator.WriteClassObjRefDefinitions(writer, context, type);

        // Constructor: WindowsRuntimeObjectReference-based constructor (RCW-like)
        if (!context.Settings.ReferenceProjection)
        {
            string ctorAccess = type.IsSealed ? "internal" : "protected internal";
            writer.Write($"\n{ctorAccess} {typeName}(WindowsRuntimeObjectReference nativeObjectReference)\n: base(nativeObjectReference)\n{{\n");
            if (!type.IsSealed)
            {
                // For unsealed classes, the default interface objref needs to be initialized only
                // when GetType() matches the projected class exactly (derived classes have their own
                // default interface). The init; accessor on _objRef_<DefaultIface> allows this set.
                ITypeDefOrRef? defaultIface = type.GetDefaultInterface();
                if (defaultIface is not null)
                {
                    string defaultObjRefName = ObjRefNameGenerator.GetObjRefName(context, defaultIface);
                    writer.Write($$"""
                        if (GetType() == typeof({{typeName}}))
                        {
                        {{defaultObjRefName}} = NativeObjectReference;
                        }
                        """, isMultiline: true);
                }
            }
            if (gcPressure > 0)
            {
                writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
            }
            writer.WriteLine("}");
        }
        else if (context.Cache is not null)
        {
            // In ref mode, if WriteAttributedTypes will not emit any public constructors,
            // we need a 'private TypeName() { throw null; }' to suppress the C# compiler's
            // implicit public default constructor (which would expose an unintended API).
            // either:
            //  - factory.activatable is true (parameterless or parameterized — Activatable
            //    always emits at least one ctor), OR
            //  - factory.composable && factory.type && factory.type.MethodList().size() > 0
            //    (composable factories with NO methods don't emit any ctors).
            bool hasRefModeCtors = false;
            foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(type, context.Cache))
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
                RefModeStubFactory.EmitSyntheticPrivateCtor(writer, typeName);
            }
        }

        // Activator/composer constructors from [Activatable]/[Composable] factory interfaces.
        // write_static_members) BEFORE the override hooks and instance members.
        ConstructorFactory.WriteAttributedTypes(writer, context, type);

        // Static members from [Static] factory interfaces (e.g. GetForCurrentView).
        // C++ emits these inside write_attributed_types -> write_static_members; emit them
        // here right after to preserve the same overall ordering.
        WriteStaticClassMembers(writer, context, type);

        // Conditional finalizer
        if (gcPressure > 0)
        {
            writer.Write($"~{typeName}()\n{{\nGC.RemoveMemoryPressure({gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture)});\n}}\n");
        }

        // Class members from interfaces (instance methods, properties, events)
        // Override hooks must be emitted BEFORE the public members to match the C++
        // ordering (write_class line 9591/9600/9601: hooks first, then write_class_members).
        // HasUnwrappableNativeObjectReference and IsOverridableInterface overrides.
        if (!context.Settings.ReferenceProjection)
        {
            writer.WriteLine("");
            writer.Write("protected override bool HasUnwrappableNativeObjectReference => ");
            if (!type.IsSealed)
            {
                writer.Write($"GetType() == typeof({typeName});");
            }
            else
            {
                writer.Write("true;");
            }
            writer.WriteLine("");
            writer.WriteLine("");
            writer.Write("protected override bool IsOverridableInterface(in Guid iid) => ");
            bool firstClause = true;
            foreach (InterfaceImplementation impl in type.Interfaces)
            {
                if (!impl.IsOverridable()) { continue; }
                ITypeDefOrRef? implRef = impl.Interface;
                if (implRef is null) { continue; }
                if (!firstClause) { writer.Write(" || "); }
                firstClause = false;
                ObjRefNameGenerator.WriteIidExpression(writer, context, implRef);
                writer.Write(" == iid");
            }
            // base call when type has a non-object base class
            bool hasBaseClass = type.BaseType is not null
                && !(type.BaseType.Namespace?.Value == "System" && type.BaseType.Name?.Value == "Object")
                && !(type.BaseType.Namespace?.Value == "WindowsRuntime" && type.BaseType.Name?.Value == "WindowsRuntimeObject");
            if (hasBaseClass)
            {
                if (!firstClause) { writer.Write(" || "); }
                writer.Write("base.IsOverridableInterface(in iid)");
                firstClause = false;
            }
            if (firstClause) { writer.Write("false"); }
            writer.WriteLine(";");
        }

        ClassMembersFactory.WriteClassMembers(writer, context, type);

        writer.WriteLine("}");
    }
}