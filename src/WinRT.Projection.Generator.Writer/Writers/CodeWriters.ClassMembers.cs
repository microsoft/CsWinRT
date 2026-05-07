// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Class member emission: walks implemented interfaces and emits the public/protected
/// instance methods, properties, and events (mirrors C++ <c>write_class_members</c>).
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Emits all instance members (methods, properties, events) inherited from implemented interfaces.
    /// Mirrors C++ <c>write_class_members</c>. In ref-projection mode, this is still called: type
    /// declarations and per-interface objref getters are emitted, but non-mapped instance
    /// method/property/event bodies are emitted as <c>=> throw null;</c> stubs.
    /// </summary>
    public static void WriteClassMembers(TypeWriter w, TypeDefinition type)
    {
        HashSet<string> writtenMethods = new(System.StringComparer.Ordinal);
        // For properties: track per-name accessor presence so we can merge get/set across interfaces.
        // Use insertion-order Dictionary so the per-class property emission order matches the
        // .winmd metadata definition order (mirrors C++ which uses type.PropertyList() order).
        Dictionary<string, PropertyAccessorState> propertyState = new(System.StringComparer.Ordinal);
        HashSet<string> writtenEvents = new(System.StringComparer.Ordinal);
        HashSet<TypeDefinition> writtenInterfaces = new();

        // Mirror C++ class member ordering: emit GetInterface()/GetDefaultInterface() per
        // interface inside WriteInterfaceMembersRecursive (right before that interface's
        // members), instead of one upfront block. This interleaves the GetInterface() impls
        // with their corresponding interface body, matching truth's per-interface layout.
        WriteInterfaceMembersRecursive(w, type, type, null, writtenMethods, propertyState, writtenEvents, writtenInterfaces);

        // After collecting all properties (with merged accessors), emit them.
        foreach (KeyValuePair<string, PropertyAccessorState> kvp in propertyState)
        {
            PropertyAccessorState s = kvp.Value;
            // For generic-interface properties, emit the UnsafeAccessor static externs above the
            // property declaration. Note: getter and setter use the same accessor name (because
            // C# allows method overloading on parameter list for the static externs).
            if (s.HasGetter && s.GetterIsGeneric && !string.IsNullOrEmpty(s.GetterGenericInteropType))
            {
                w.Write("\n[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"");
                w.Write(kvp.Key);
                w.Write("\")]\n");
                w.Write("static extern ");
                w.Write(s.GetterPropTypeText);
                w.Write(" ");
                w.Write(s.GetterGenericAccessorName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(s.GetterGenericInteropType);
                w.Write("\")] object _, WindowsRuntimeObjectReference thisReference);\n");
            }
            if (s.HasSetter && s.SetterIsGeneric && !string.IsNullOrEmpty(s.SetterGenericInteropType))
            {
                w.Write("\n[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"");
                w.Write(kvp.Key);
                w.Write("\")]\n");
                w.Write("static extern void ");
                w.Write(s.SetterGenericAccessorName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(s.SetterGenericInteropType);
                w.Write("\")] object _, WindowsRuntimeObjectReference thisReference, ");
                w.Write(s.SetterPropTypeText);
                w.Write(" value);\n");
            }

            w.Write("\n");
            // Mirrors C++ code_writers.h:2041-2046: collapse to property-level platform attribute
            // when getter and setter platforms match; otherwise emit per-accessor.
            string getterPlat = s.GetterPlatformAttribute;
            string setterPlat = s.SetterPlatformAttribute;
            string propertyPlat = string.Empty;
            // C++: if (getter_platform == setter_platform) { property_platform = getter_platform; getter_platform = ""; setter_platform = ""; }
            // For getter-only or setter-only properties, only one side is set; compare the relevant side.
            bool bothSidesPresent = s.HasGetter && s.HasSetter;
            if (!bothSidesPresent || getterPlat == setterPlat)
            {
                // Collapse: prefer the populated side (matches C++ which compares string_view equality
                // including both being empty).
                propertyPlat = !string.IsNullOrEmpty(getterPlat) ? getterPlat : setterPlat;
                getterPlat = string.Empty;
                setterPlat = string.Empty;
            }
            if (!string.IsNullOrEmpty(propertyPlat)) { w.Write(propertyPlat); }
            w.Write(s.Access);
            w.Write(s.MethodSpec);
            w.Write(s.PropTypeText);
            w.Write(" ");
            w.Write(kvp.Key);
            // For getter-only properties, emit expression body: 'public T Prop => Expr;'
            // For getter+setter or setter-only, use accessor block: 'public T Prop { get => ...; set => ...; }'
            // (mirrors C++ which uses '%' template substitution where get-only collapses to '=> %').
            //
            // In ref mode, all property bodies emit '=> throw null;' (mirrors C++
            // write_abi_get/set_property_static_method_call + write_unsafe_accessor_property_static_method_call,
            // code_writers.h:1669, 1683, 1697).
            bool getterOnly = s.HasGetter && !s.HasSetter;
            if (getterOnly)
            {
                w.Write(" => ");
                if (w.Settings.ReferenceProjection)
                {
                    w.Write("throw null;");
                }
                else if (s.GetterIsGeneric)
                {
                    if (!string.IsNullOrEmpty(s.GetterGenericInteropType))
                    {
                        w.Write(s.GetterGenericAccessorName);
                        w.Write("(null, ");
                        w.Write(s.GetterObjRef);
                        w.Write(");");
                    }
                    else
                    {
                        w.Write("throw null!;");
                    }
                }
                else
                {
                    w.Write(s.GetterAbiClass);
                    w.Write(".");
                    w.Write(kvp.Key);
                    w.Write("(");
                    w.Write(s.GetterObjRef);
                    w.Write(");");
                }
                w.Write("\n");
            }
            else
            {
                w.Write("\n{\n");
                if (s.HasGetter)
                {
                    if (!string.IsNullOrEmpty(getterPlat))
                    {
                        w.Write("    ");
                        w.Write(getterPlat);
                    }
                    if (w.Settings.ReferenceProjection)
                    {
                        w.Write("    get => throw null;\n");
                    }
                    else if (s.GetterIsGeneric)
                    {
                        if (!string.IsNullOrEmpty(s.GetterGenericInteropType))
                        {
                            w.Write("    get => ");
                            w.Write(s.GetterGenericAccessorName);
                            w.Write("(null, ");
                            w.Write(s.GetterObjRef);
                            w.Write(");\n");
                        }
                        else
                        {
                            w.Write("    get => throw null!;\n");
                        }
                    }
                    else
                    {
                        w.Write("    get => ");
                        w.Write(s.GetterAbiClass);
                        w.Write(".");
                        w.Write(kvp.Key);
                        w.Write("(");
                        w.Write(s.GetterObjRef);
                        w.Write(");\n");
                    }
                }
                if (s.HasSetter)
                {
                    if (!string.IsNullOrEmpty(setterPlat))
                    {
                        w.Write("    ");
                        w.Write(setterPlat);
                    }
                    if (w.Settings.ReferenceProjection)
                    {
                        w.Write("    set => throw null;\n");
                    }
                    else if (s.SetterIsGeneric)
                    {
                        if (!string.IsNullOrEmpty(s.SetterGenericInteropType))
                        {
                            w.Write("    set => ");
                            w.Write(s.SetterGenericAccessorName);
                            w.Write("(null, ");
                            w.Write(s.SetterObjRef);
                            w.Write(", value);\n");
                        }
                        else
                        {
                            w.Write("    set => throw null!;\n");
                        }
                    }
                    else
                    {
                        w.Write("    set => ");
                        w.Write(s.SetterAbiClass);
                        w.Write(".");
                        w.Write(kvp.Key);
                        w.Write("(");
                        w.Write(s.SetterObjRef);
                        w.Write(", value);\n");
                    }
                }
                w.Write("}\n");
            }

            // For overridable properties, emit an explicit interface implementation that
            // delegates to the protected property. Mirrors truth pattern:
            //   T InterfaceName.PropName { get => PropName; }
            //   T InterfaceName.PropName { set => PropName = value; }
            if (s.IsOverridable && s.OverridableInterface is not null)
            {
                w.Write(s.PropTypeText);
                w.Write(" ");
                WriteInterfaceTypeNameForCcw(w, s.OverridableInterface);
                w.Write(".");
                w.Write(kvp.Key);
                w.Write(" {");
                if (s.HasGetter)
                {
                    w.Write("get => ");
                    w.Write(kvp.Key);
                    w.Write("; ");
                }
                if (s.HasSetter)
                {
                    w.Write("set => ");
                    w.Write(kvp.Key);
                    w.Write(" = value; ");
                }
                w.Write("}\n");
            }
        }

        // GetInterface() / GetDefaultInterface() impls are emitted per-interface inside
        // WriteInterfaceMembersRecursive (matches the C++ tool's per-interface ordering).
    }

    private static string BuildMethodSignatureKey(string name, MethodSig sig)
    {
        System.Text.StringBuilder sb = new();
        sb.Append(name);
        sb.Append('(');
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { sb.Append(','); }
            sb.Append(sig.Params[i].Type?.FullName ?? "?");
        }
        sb.Append(')');
        return sb.ToString();
    }

    private sealed class PropertyAccessorState
    {
        public bool HasGetter;
        public bool HasSetter;
        public string PropTypeText = string.Empty;
        public string Access = "public ";
        public string MethodSpec = string.Empty;
        public string GetterAbiClass = string.Empty;
        public string GetterObjRef = string.Empty;
        public string SetterAbiClass = string.Empty;
        public string SetterObjRef = string.Empty;
        public string Name = string.Empty;
        public bool GetterIsGeneric;
        public bool SetterIsGeneric;
        public string GetterGenericInteropType = string.Empty;
        public string GetterGenericAccessorName = string.Empty;
        public string GetterPropTypeText = string.Empty;
        public string SetterGenericInteropType = string.Empty;
        public string SetterGenericAccessorName = string.Empty;
        public string SetterPropTypeText = string.Empty;
        // True if this property comes from an Overridable interface (needs explicit interface impl).
        public bool IsOverridable;
        // The originating interface (used to qualify the explicit interface impl).
        public ITypeDefOrRef? OverridableInterface;
        // Per-accessor platform attribute strings from the originating interface's [ContractVersion],
        // emitted before the property in ref mode. Mirrors C++ getter_platform/setter_platform
        // tracking in code_writers.h:4306-4308 / 4323/4330. When both match, emit at the property
        // level only; when they differ (getter and setter come from different interfaces with
        // different platforms), emit per-accessor.
        public string GetterPlatformAttribute = string.Empty;
        public string SetterPlatformAttribute = string.Empty;
    }

    /// <summary>
    /// Returns true if the given interface implementation should appear in the class's inheritance list
    /// (i.e., it has [Overridable], or is not [ExclusiveTo], or includeExclusiveInterface is set).
    /// </summary>
    private static bool IsInterfaceInInheritanceList(InterfaceImplementation impl, bool includeExclusiveInterface)
    {
        if (impl.Interface is null) { return false; }
        if (Helpers.IsOverridable(impl)) { return true; }
        if (includeExclusiveInterface) { return true; }
        TypeDefinition? td = ResolveInterface(impl.Interface);
        if (td is null) { return true; }
        return !TypeCategorization.IsExclusiveTo(td);
    }

    private static void WriteInterfaceMembersRecursive(TypeWriter w, TypeDefinition classType, TypeDefinition declaringType,
        AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? currentInstance,
        HashSet<string> writtenMethods, IDictionary<string, PropertyAccessorState> propertyState, HashSet<string> writtenEvents, HashSet<TypeDefinition> writtenInterfaces)
    {
        AsmResolver.DotNet.Signatures.GenericContext genCtx = new(currentInstance, null);

        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (impl.Interface is null) { continue; }

            // Resolve TypeRef to TypeDef using our cache
            TypeDefinition? ifaceType = ResolveInterface(impl.Interface);
            if (ifaceType is null) { continue; }

            if (writtenInterfaces.Contains(ifaceType)) { continue; }
            _ = writtenInterfaces.Add(ifaceType);

            bool isOverridable = Helpers.IsOverridable(impl);
            bool isProtected = TypeCategorization.HasAttribute(impl, "Windows.Foundation.Metadata", "ProtectedAttribute");

            // Substitute generic type arguments using the current generic context BEFORE emitting
            // any references to this interface. This is critical for nested recursion: e.g. when
            // emitting members for IObservableMap<string, object>'s base IMap<!0, !1>, we need to
            // substitute !0/!1 with string/object so the generated code references
            // IDictionary<string, object> instead of IDictionary<T0, T1>. Mirrors the C++ tool's
            // writer.push_generic_args() stack inside for_typedef().
            ITypeDefOrRef substitutedInterface = impl.Interface;
            AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? nextInstance = null;
            if (impl.Interface is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
            {
                if (currentInstance is not null)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature subSig = gi.InstantiateGenericTypes(genCtx);
                    if (subSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature subGi)
                    {
                        nextInstance = subGi;
                        AsmResolver.DotNet.ITypeDefOrRef? newRef = subGi.ToTypeDefOrRef();
                        if (newRef is not null) { substitutedInterface = newRef; }
                    }
                    else
                    {
                        nextInstance = gi;
                    }
                }
                else
                {
                    nextInstance = gi;
                }
            }

            // Emit GetInterface() / GetDefaultInterface() impl for this interface BEFORE its
            // members (mirrors C++ write_class_interface at code_writers.h:4257-4280). For
            // overridable interfaces or non-exclusive direct interfaces, emit
            // IWindowsRuntimeInterface<T>.GetInterface(). For the default interface on an
            // unsealed class with an exclusive default, emit "internal new GetDefaultInterface()".
            //
            // The IWindowsRuntimeInterface<T> markers are NOT emitted in ref mode (gated by
            // !w.Settings.ReferenceProjection here, mirrors C++ code_writers.h:4257
            // '&& !settings.reference_projection' in the corresponding condition). The
            // 'internal new GetDefaultInterface()' helper IS emitted in both modes since
            // it's referenced by overrides on derived classes.
            if (IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false) && !w.Settings.ReferenceProjection)
            {
                string giObjRefName = GetObjRefName(w, substitutedInterface);
                w.Write("\nWindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<");
                WriteInterfaceTypeNameForCcw(w, substitutedInterface);
                w.Write(">.GetInterface()\n{\nreturn ");
                w.Write(giObjRefName);
                w.Write(".AsValue();\n}\n");
            }
            else if (Helpers.IsDefaultInterface(impl) && !classType.IsSealed)
            {
                // Mirrors C++ code_writers.h:4263-4280. The C++ source emits the
                // 'internal new GetDefaultInterface()' helper whenever the interface is the
                // default interface and the class is unsealed -- regardless of exclusive-to
                // status. In ref-projection mode this is the only branch that emits the helper
                // (the prior 'IWindowsRuntimeInterface<T>.GetInterface' branch is gated off).
                // In non-ref mode this branch is only reached when the prior branch's
                // IsInterfaceInInheritanceList check fails (i.e., ExclusiveTo default interfaces),
                // because non-exclusive default interfaces are routed to the prior branch.
                string giObjRefName = GetObjRefName(w, substitutedInterface);
                bool hasBaseType = false;
                if (classType.BaseType is not null)
                {
                    string? baseNs = classType.BaseType.Namespace?.Value;
                    string? baseName = classType.BaseType.Name?.Value;
                    hasBaseType = !(baseNs == "System" && baseName == "Object");
                }
                w.Write("\ninternal ");
                if (hasBaseType) { w.Write("new "); }
                w.Write("WindowsRuntimeObjectReferenceValue GetDefaultInterface()\n{\nreturn ");
                w.Write(giObjRefName);
                w.Write(".AsValue();\n}\n");
            }

            // For mapped interfaces with custom members output (e.g. IClosable -> IDisposable, IMap`2
            // -> IDictionary<K,V>), emit stubs for the C# interface's required members so the class
            // satisfies its inheritance contract. The runtime's adapter actually services them.
            string ifaceNs = ifaceType.Namespace?.Value ?? string.Empty;
            string ifaceName = ifaceType.Name?.Value ?? string.Empty;
            if (MappedTypes.Get(ifaceNs, ifaceName) is { HasCustomMembersOutput: true })
            {
                if (IsMappedInterfaceRequiringStubs(ifaceNs, ifaceName))
                {
                    // For generic interfaces, use the substituted nextInstance to compute the
                    // objref name so type arguments are concrete (matches the field name emitted
                    // by WriteClassObjRefDefinitions). For non-generic, fall back to impl.Interface.
                    string objRefName = GetObjRefName(w, substitutedInterface);
                    WriteMappedInterfaceStubs(w, nextInstance, ifaceName, objRefName);
                }
                continue;
            }

            WriteInterfaceMembers(w, classType, ifaceType, impl.Interface, isOverridable, isProtected, nextInstance,
                writtenMethods, propertyState, writtenEvents);

            // Recurse into derived interfaces
            WriteInterfaceMembersRecursive(w, classType, ifaceType, nextInstance, writtenMethods, propertyState, writtenEvents, writtenInterfaces);
        }
    }

    private static TypeDefinition? ResolveInterface(ITypeDefOrRef typeRef)
    {
        if (typeRef is TypeDefinition td) { return td; }
        if (_cacheRef is null) { return null; }
        // Try the runtime context resolver first (handles cross-module references via the resolver)
        try
        {
            TypeDefinition? resolved = typeRef.Resolve(_cacheRef.RuntimeContext);
            if (resolved is not null) { return resolved; }
        }
        catch
        {
            // Fall through to local lookup
        }
        // Fall back to local lookup by full name
        if (typeRef is TypeReference tr)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            return _cacheRef.Find(fullName);
        }
        if (typeRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            return ResolveInterface(gi.GenericType);
        }
        return null;
    }

    private static void WriteInterfaceMembers(TypeWriter w, TypeDefinition classType, TypeDefinition ifaceType,
        ITypeDefOrRef originalInterface,
        bool isOverridable, bool isProtected, AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? currentInstance,
        HashSet<string> writtenMethods, IDictionary<string, PropertyAccessorState> propertyState, HashSet<string> writtenEvents)
    {
        bool sealed_ = classType.IsSealed;
        // Determine accessibility and method modifier.
        // Overridable interfaces are emitted with 'protected' visibility, plus 'virtual' on
        // non-sealed classes. Sealed classes still get 'protected' (without virtual).
        string access = (isOverridable || isProtected) ? "protected " : "public ";
        string methodSpec = string.Empty;
        if (isOverridable && !sealed_)
        {
            methodSpec = "virtual ";
        }

        AsmResolver.DotNet.Signatures.GenericContext? genCtx = currentInstance is not null
            ? new AsmResolver.DotNet.Signatures.GenericContext(currentInstance, null)
            : null;

        // Generic interfaces require UnsafeAccessor-based dispatch (real ABI lives in the
        // post-build interop assembly).
        bool isGenericInterface = ifaceType.GenericParameters.Count > 0;

        // Fast ABI: when this interface is exclusive_to a fast-abi class (and we're emitting
        // class members, classType is that fast-abi class), dispatch routes through the
        // default interface's ABI Methods class and objref instead of through this interface's
        // own ABI Methods class. The native vtable bundles all exclusive interfaces' methods
        // into the default interface's vtable in a fixed order. Mirrors C++
        // code_writers.h:4250-4251 (semantics_for_abi_call assignment) which redirects both
        // static_iface_target and the objref to the default interface for fast-abi cases.
        TypeDefinition abiInterface = ifaceType;
        ITypeDefOrRef abiInterfaceRef = originalInterface;
        if (IsFastAbiClass(classType, w.Settings) && TypeCategorization.IsExclusiveTo(ifaceType))
        {
            (TypeDefinition? defaultIface, _) = GetFastAbiInterfaces(classType);
            if (defaultIface is not null)
            {
                abiInterface = defaultIface;
                abiInterfaceRef = defaultIface;
            }
        }

        // Compute the ABI Methods static class name (e.g. "global::ABI.Windows.Foundation.IDeferralMethods")
        // — note this is the ungenerified Methods class for generic interfaces (matches truth output).
        // The _objRef_ field name uses the full instantiated interface name so generic instantiations
        // (e.g. IAsyncOperation<uint>) get a per-instantiation field.
        string abiClass = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, abiInterface, TypedefNameType.StaticAbiClass, true);
        }));
        if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal))
        {
            abiClass = "global::" + abiClass;
        }
        string objRef = GetObjRefName(w, abiInterfaceRef);

        // For generic interfaces, also compute the encoded parent type name (used in UnsafeAccessor
        // function names) and the WinRT.Interop accessor type string (passed to UnsafeAccessorType).
        string genericParentEncoded = string.Empty;
        string genericInteropType = string.Empty;
        if (isGenericInterface && currentInstance is not null)
        {
            string projectedParent = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
                WriteTypeName(w, TypeSemanticsFactory.Get(currentInstance), TypedefNameType.Projected, true)));
            genericParentEncoded = EscapeTypeNameForIdentifier(projectedParent, stripGlobal: true);
            genericInteropType = EncodeInteropTypeName(currentInstance, TypedefNameType.StaticAbiClass) + ", WinRT.Interop";
        }

        // Compute the platform attribute string from the interface type's [ContractVersion]
        // attribute. In ref mode, this is prepended to each member emission so the projected
        // class members carry [SupportedOSPlatform("WindowsX.Y.Z.0")] mirroring the interface's
        // contract version. Only emitted in ref mode (WritePlatformAttribute internally returns
        // immediately if not ref). Mirrors C++ code_writers.h:4290
        // 'auto platform_attribute = write_platform_attribute_temp(w, interface_type);'.
        string platformAttribute = w.WriteTemp("%", new System.Action<TextWriter>(_ => WritePlatformAttribute(w, ifaceType)));

        // Methods
        foreach (MethodDefinition method in ifaceType.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            string name = method.Name?.Value ?? string.Empty;
            // Track by full signature (name + each param's element-type code) to avoid trivial overload duplicates.
            // This prevents collapsing distinct overloads like Format(double) and Format(ulong).
            MethodSig sig = new(method, genCtx);
            string key = BuildMethodSignatureKey(name, sig);
            if (!writtenMethods.Add(key)) { continue; }

            // Detect a 'string ToString()' that overrides Object.ToString(). C++ uses 'override'
            // here (and even forces 'string' as the return type). See code_writers.h:1942-1959.
            string methodSpecForThis = methodSpec;
            if (name == "ToString" && sig.Params.Count == 0
                && sig.ReturnType is AsmResolver.DotNet.Signatures.CorLibTypeSignature crt
                && crt.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String)
            {
                methodSpecForThis = "override ";
            }

            // Detect 'bool Equals(object obj)' and 'int GetHashCode()' that override their
            // System.Object counterparts. Mirrors C++ helpers.h:566 (is_object_equals_method) and
            // helpers.h:625 (is_object_hashcode_method) + code_writers.h:1962-1974: matching
            // signature and return type -> 'override'; matching name only -> 'new'.
            if (name == "Equals" && sig.Params.Count == 1)
            {
                AsmResolver.DotNet.Signatures.TypeSignature p0 = sig.Params[0].Type;
                bool paramIsObject = p0 is AsmResolver.DotNet.Signatures.CorLibTypeSignature po
                    && po.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object;
                bool returnsBool = sig.ReturnType is AsmResolver.DotNet.Signatures.CorLibTypeSignature ro
                    && ro.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean;
                if (paramIsObject)
                {
                    methodSpecForThis = returnsBool ? "override " : (methodSpecForThis + "new ");
                }
            }
            else if (name == "GetHashCode" && sig.Params.Count == 0)
            {
                bool returnsInt = sig.ReturnType is AsmResolver.DotNet.Signatures.CorLibTypeSignature ri
                    && ri.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4;
                methodSpecForThis = returnsInt ? "override " : (methodSpecForThis + "new ");
            }

            if (isGenericInterface && !string.IsNullOrEmpty(genericInteropType))
            {
                // Emit UnsafeAccessor static extern + body that dispatches through it.
                string accessorName = genericParentEncoded + "_" + name;
                w.Write("\n[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"");
                w.Write(name);
                w.Write("\")]\n");
                w.Write("static extern ");
                WriteProjectionReturnType(w, sig);
                w.Write(" ");
                w.Write(accessorName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(genericInteropType);
                w.Write("\")] object _, WindowsRuntimeObjectReference thisReference");
                for (int i = 0; i < sig.Params.Count; i++)
                {
                    w.Write(", ");
                    WriteProjectionParameter(w, sig.Params[i]);
                }
                w.Write(");\n");

                // Mirrors C++ code_writers.h:4292 — prepend the per-interface platform attribute
                // string to each public method emission. In ref mode this produces e.g.
                // [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.16299.0")].
                if (!string.IsNullOrEmpty(platformAttribute)) { w.Write(platformAttribute); }
                w.Write(access);
                w.Write(methodSpecForThis);
                WriteProjectionReturnType(w, sig);
                w.Write(" ");
                w.Write(name);
                w.Write("(");
                WriteParameterList(w, sig);
                if (w.Settings.ReferenceProjection)
                {
                    // Mirrors C++ write_unsafe_accessor_static_method_call (code_writers.h:1653)
                    // which emits 'throw null' in reference projection mode.
                    w.Write(") => throw null;\n");
                }
                else
                {
                    w.Write(") => ");
                    w.Write(accessorName);
                    w.Write("(null, ");
                    w.Write(objRef);
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        w.Write(", ");
                        WriteParameterNameWithModifier(w, sig.Params[i]);
                    }
                    w.Write(");\n");
                }
            }
            else
            {
                w.Write("\n");
                if (!string.IsNullOrEmpty(platformAttribute)) { w.Write(platformAttribute); }
                w.Write(access);
                w.Write(methodSpecForThis);
                WriteProjectionReturnType(w, sig);
                w.Write(" ");
                w.Write(name);
                w.Write("(");
                WriteParameterList(w, sig);
                if (w.Settings.ReferenceProjection)
                {
                    // Mirrors C++ write_abi_static_method_call (code_writers.h:1637)
                    // which emits 'throw null' in reference projection mode.
                    w.Write(") => throw null;\n");
                }
                else
                {
                    w.Write(") => ");
                    w.Write(abiClass);
                    w.Write(".");
                    w.Write(name);
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

            // For overridable interface methods, emit an explicit interface implementation
            // that delegates to the protected (and virtual on non-sealed) method. Mirrors C++
            // overridable interface pattern:
            //   T InterfaceName.MethodName(args) => MethodName(args);
            if (isOverridable)
            {
                // Mirror C++ which carries the platform attribute on the explicit interface
                // impl as well (since it shares the same originating interface).
                if (!string.IsNullOrEmpty(platformAttribute)) { w.Write(platformAttribute); }
                WriteProjectionReturnType(w, sig);
                w.Write(" ");
                WriteInterfaceTypeNameForCcw(w, originalInterface);
                w.Write(".");
                w.Write(name);
                w.Write("(");
                WriteParameterList(w, sig);
                w.Write(") => ");
                w.Write(name);
                w.Write("(");
                for (int i = 0; i < sig.Params.Count; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    WriteParameterNameWithModifier(w, sig.Params[i]);
                }
                w.Write(");\n");
            }
        }

        // Properties: collect into propertyState (merging accessors from multiple interfaces).
        // Track per-accessor origin so that the getter/setter dispatch to the right ABI Methods
        // class on the right _objRef_ field.
        foreach (PropertyDefinition prop in ifaceType.Properties)
        {
            string name = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            if (!propertyState.TryGetValue(name, out PropertyAccessorState? state))
            {
                state = new PropertyAccessorState
                {
                    PropTypeText = WritePropType(w, prop, genCtx),
                    Access = access,
                    MethodSpec = methodSpec,
                    IsOverridable = isOverridable,
                    OverridableInterface = isOverridable ? originalInterface : null,
                };
                propertyState[name] = state;
            }
            if (getter is not null && !state.HasGetter)
            {
                state.HasGetter = true;
                state.GetterAbiClass = abiClass;
                state.GetterObjRef = objRef;
                state.GetterIsGeneric = isGenericInterface;
                state.GetterGenericInteropType = genericInteropType;
                state.GetterGenericAccessorName = isGenericInterface ? (genericParentEncoded + "_" + name) : string.Empty;
                state.GetterPropTypeText = WritePropType(w, prop, genCtx);
                // Mirror C++ getter_platform tracking (code_writers.h:4306, 4323).
                state.GetterPlatformAttribute = platformAttribute;
            }
            if (setter is not null && !state.HasSetter)
            {
                state.HasSetter = true;
                state.SetterAbiClass = abiClass;
                state.SetterObjRef = objRef;
                state.SetterIsGeneric = isGenericInterface;
                state.SetterGenericInteropType = genericInteropType;
                state.SetterGenericAccessorName = isGenericInterface ? (genericParentEncoded + "_" + name) : string.Empty;
                state.SetterPropTypeText = WritePropType(w, prop, genCtx);
                // Mirror C++ setter_platform tracking (code_writers.h:4308, 4330).
                state.SetterPlatformAttribute = platformAttribute;
            }
        }

        // Events: emit the event with Subscribe/Unsubscribe through a per-event _eventSource_
        // backing property field that lazily constructs an EventHandlerEventSource for the event
        // handler type. Mirrors C++ write_class_events_using_static_abi_methods + write_event.
        foreach (EventDefinition evt in ifaceType.Events)
        {
            string name = evt.Name?.Value ?? string.Empty;
            if (!writtenEvents.Add(name)) { continue; }

            // Compute event handler type and event source type strings.
            AsmResolver.DotNet.Signatures.TypeSignature evtSig = evt.EventType!.ToTypeSignature(false);
            if (currentInstance is not null)
            {
                evtSig = evtSig.InstantiateGenericTypes(new AsmResolver.DotNet.Signatures.GenericContext(currentInstance, null));
            }
            bool isGenericEvent = evtSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;

            // Special case for ICommand.CanExecuteChanged: the WinRT event handler is
            // EventHandler<object> but C# expects non-generic EventHandler. Use the non-generic
            // EventHandlerEventSource backing field. Mirrors C++ write_event hard-coded fix.
            bool isICommandCanExecuteChanged = name == "CanExecuteChanged"
                && (ifaceType.FullName == "Microsoft.UI.Xaml.Input.ICommand"
                    || ifaceType.FullName == "Windows.UI.Xaml.Input.ICommand");

            string eventSourceType;
            if (isICommandCanExecuteChanged)
            {
                eventSourceType = "global::WindowsRuntime.InteropServices.EventHandlerEventSource";
                isGenericEvent = false;
            }
            else
            {
                eventSourceType = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
                    WriteTypeName(w, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, false)));
            }
            string eventSourceTypeFull = eventSourceType;
            if (!eventSourceTypeFull.StartsWith("global::", System.StringComparison.Ordinal))
            {
                eventSourceTypeFull = "global::" + eventSourceTypeFull;
            }
            // The "interop" type name string for the EventSource UnsafeAccessor (only needed for generic events).
            string eventSourceInteropType = isGenericEvent
                ? EncodeInteropTypeName(evtSig, TypedefNameType.EventSource) + ", WinRT.Interop"
                : string.Empty;

            // Compute vtable index = method index in the interface vtable + 6 (for IInspectable methods).
            // The add method is the first method of the event in the interface.
            int methodIndex = 0;
            foreach (MethodDefinition m in ifaceType.Methods)
            {
                if (m == evt.AddMethod) { break; }
                methodIndex++;
            }
            int vtableIndex = 6 + methodIndex;

            // Emit the _eventSource_<name> property field — skipped in ref mode (the event
            // accessors below become 'add => throw null;' / 'remove => throw null;' which
            // don't reference the field, mirrors C++ where the inline_event_source_field
            // path emits 'throw null' at code_writers.h:2215, 2238).
            if (!w.Settings.ReferenceProjection)
            {
                w.Write("\nprivate ");
                w.Write(eventSourceTypeFull);
                w.Write(" _eventSource_");
                w.Write(name);
                w.Write("\n{\n    get\n    {\n");
                if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
                {
                    w.Write("        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]\n");
                    w.Write("        [return: UnsafeAccessorType(\"");
                    w.Write(eventSourceInteropType);
                    w.Write("\")]\n");
                    w.Write("        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);\n\n");
                }
                w.Write("        [MethodImpl(MethodImplOptions.NoInlining)]\n");
                w.Write("        ");
                w.Write(eventSourceTypeFull);
                w.Write(" MakeEventSource()\n        {\n");
                w.Write("            _ = global::System.Threading.Interlocked.CompareExchange(\n");
                w.Write("                location1: ref field,\n");
                w.Write("                value: ");
                if (isGenericEvent)
                {
                    w.Write("Unsafe.As<");
                    w.Write(eventSourceTypeFull);
                    w.Write(">(ctor(");
                    w.Write(objRef);
                    w.Write(", ");
                    w.Write(vtableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write("))");
                }
                else
                {
                    w.Write("new ");
                    w.Write(eventSourceTypeFull);
                    w.Write("(");
                    w.Write(objRef);
                    w.Write(", ");
                    w.Write(vtableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write(")");
                }
                w.Write(",\n");
                w.Write("                comparand: null);\n\n");
                w.Write("            return field;\n        }\n\n");
                w.Write("        return field ?? MakeEventSource();\n    }\n}\n");
            }

            // Emit the public/protected event with Subscribe/Unsubscribe.
            w.Write("\n");
            // Mirrors C++ code_writers.h:4293 — prepend the per-interface platform attribute
            // string to each event emission. In ref mode this produces e.g.
            // [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.16299.0")].
            if (!string.IsNullOrEmpty(platformAttribute)) { w.Write(platformAttribute); }
            w.Write(access);
            w.Write(methodSpec);
            w.Write("event ");
            WriteEventType(w, evt, currentInstance);
            w.Write(" ");
            w.Write(name);
            w.Write("\n{\n");
            if (w.Settings.ReferenceProjection)
            {
                w.Write("    add => throw null;\n");
                w.Write("    remove => throw null;\n");
            }
            else
            {
                w.Write("    add => _eventSource_");
                w.Write(name);
                w.Write(".Subscribe(value);\n");
                w.Write("    remove => _eventSource_");
                w.Write(name);
                w.Write(".Unsubscribe(value);\n");
            }
            w.Write("}\n");
        }
    }

    /// <summary>
    /// Writes a parameter name prefixed with its modifier (in/out/ref) for use as a call argument.
    /// </summary>
    private static void WriteParameterNameWithModifier(TypeWriter w, ParamInfo p)
    {
        ParamCategory cat = ParamHelpers.GetParamCategory(p);
        switch (cat)
        {
            case ParamCategory.Out:
                w.Write("out ");
                break;
            case ParamCategory.Ref:
                w.Write("in ");
                break;
            case ParamCategory.ReceiveArray:
                w.Write("out ");
                break;
        }
        WriteParameterName(w, p);
    }

    /// <summary>
    /// Writes the projected name for an interface reference (TypeDefinition, TypeReference, or
    /// generic instance), applying mapped-type remapping. Used inside <c>IWindowsRuntimeInterface&lt;T&gt;</c>.
    /// </summary>
    private static void WriteInterfaceTypeNameForCcw(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        // If the reference is to a type in the same module, resolve to TypeDefinition so
        // WriteTypedefName can drop the 'global::<NS>.' prefix when the namespace matches.
        // Mirrors the C++ tool's behavior of emitting the bare interface name when in scope.
        if (ifaceType is not TypeDefinition && ifaceType is not TypeSpecification && _cacheRef is not null)
        {
            try
            {
                TypeDefinition? resolved = ifaceType.Resolve(_cacheRef.RuntimeContext);
                if (resolved is not null) { ifaceType = resolved; }
            }
            catch { /* leave as TypeReference */ }
        }
        if (ifaceType is TypeDefinition td)
        {
            WriteTypedefName(w, td, TypedefNameType.CCW, false);
            WriteTypeParams(w, td);
        }
        else if (ifaceType is TypeReference tr)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            w.Write(ns);
            w.Write(".");
            w.WriteCode(name);
        }
        else if (ifaceType is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef gt = gi.GenericType;
            string ns = gt.Namespace?.Value ?? string.Empty;
            string name = gt.Name?.Value ?? string.Empty;
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            w.Write(ns);
            w.Write(".");
            w.WriteCode(name);
            w.Write("<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                if (i > 0) { w.Write(", "); }
                WriteTypeName(w, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, true);
            }
            w.Write(">");
        }
    }
}
