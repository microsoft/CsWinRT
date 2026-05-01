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
    /// Mirrors C++ <c>write_class_members</c> (simplified: emits stub bodies for now).
    /// </summary>
    public static void WriteClassMembers(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.ReferenceProjection) { return; }

        HashSet<string> writtenMethods = new(System.StringComparer.Ordinal);
        // For properties: track per-name accessor presence so we can merge get/set across interfaces.
        Dictionary<string, PropertyAccessorState> propertyState = new(System.StringComparer.Ordinal);
        HashSet<string> writtenEvents = new(System.StringComparer.Ordinal);
        HashSet<TypeDefinition> writtenInterfaces = new();

        // Pre-pass: walk all (transitive) implemented interfaces to identify mapped interfaces
        // that are *subsumed* by another mapped interface in the implemented set (e.g. IIterable`1
        // is subsumed by IVector`1 because IList<T>'s stub members already cover IEnumerable<T>'s).
        // Mark subsumed interfaces as already-written so the recursion skips them.
        HashSet<TypeDefinition> allMappedImplemented = new();
        CollectAllMappedInterfaces(type, allMappedImplemented);
        bool hasIVector = HasMapped(allMappedImplemented, "Windows.Foundation.Collections", "IVector`1");
        bool hasIVectorView = HasMapped(allMappedImplemented, "Windows.Foundation.Collections", "IVectorView`1");
        bool hasIMap = HasMapped(allMappedImplemented, "Windows.Foundation.Collections", "IMap`2");
        bool hasIMapView = HasMapped(allMappedImplemented, "Windows.Foundation.Collections", "IMapView`2");
        bool hasIBindableVector = HasMapped(allMappedImplemented, "Microsoft.UI.Xaml.Interop", "IBindableVector")
                                  || HasMapped(allMappedImplemented, "Windows.UI.Xaml.Interop", "IBindableVector");
        if (hasIVector || hasIVectorView || hasIMap || hasIMapView)
        {
            // IIterable`1 is subsumed by any of the above.
            foreach (TypeDefinition td in allMappedImplemented)
            {
                if (td.Namespace?.Value == "Windows.Foundation.Collections" && td.Name?.Value == "IIterable`1")
                {
                    _ = writtenInterfaces.Add(td);
                }
            }
        }
        if (hasIBindableVector)
        {
            foreach (TypeDefinition td in allMappedImplemented)
            {
                if ((td.Namespace?.Value == "Microsoft.UI.Xaml.Interop" || td.Namespace?.Value == "Windows.UI.Xaml.Interop")
                    && td.Name?.Value == "IBindableIterable")
                {
                    _ = writtenInterfaces.Add(td);
                }
            }
        }

        WriteInterfaceMembersRecursive(w, type, type, null, writtenMethods, propertyState, writtenEvents, writtenInterfaces);

        // After collecting all properties (with merged accessors), emit them.
        foreach (KeyValuePair<string, PropertyAccessorState> kvp in propertyState)
        {
            PropertyAccessorState s = kvp.Value;
            w.Write("\n");
            w.Write(s.Access);
            w.Write(s.MethodSpec);
            w.Write(s.PropTypeText);
            w.Write(" ");
            w.Write(kvp.Key);
            w.Write(" { ");
            if (s.HasGetter)
            {
                if (s.GetterIsGeneric)
                {
                    w.Write("get => throw null!; ");
                }
                else
                {
                    w.Write("get => ");
                    w.Write(s.GetterAbiClass);
                    w.Write(".");
                    w.Write(kvp.Key);
                    w.Write("(");
                    w.Write(s.GetterObjRef);
                    w.Write("); ");
                }
            }
            if (s.HasSetter)
            {
                if (s.SetterIsGeneric)
                {
                    w.Write("set => throw null!; ");
                }
                else
                {
                    w.Write("set => ");
                    w.Write(s.SetterAbiClass);
                    w.Write(".");
                    w.Write(kvp.Key);
                    w.Write("(");
                    w.Write(s.SetterObjRef);
                    w.Write(", value); ");
                }
            }
            w.Write("}\n");
        }

        // Emit explicit IWindowsRuntimeInterface<T>.GetInterface() implementations once at the end,
        // matching the inheritance list emitted by WriteTypeInheritance. We must skip interfaces
        // that were filtered out of the inheritance list (e.g. ExclusiveTo non-overridable interfaces).
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false)) { continue; }
            string objRefName = GetObjRefName(w, impl.Interface);
            w.Write("\n");
            w.Write("WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<");
            WriteInterfaceTypeNameForCcw(w, impl.Interface);
            w.Write(">.GetInterface() => ");
            w.Write(objRefName);
            w.Write(".AsValue();\n");
        }
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

    private static bool HasMapped(HashSet<TypeDefinition> set, string ns, string name)
    {
        foreach (TypeDefinition td in set)
        {
            if (td.Namespace?.Value == ns && td.Name?.Value == name) { return true; }
        }
        return false;
    }

    private static void CollectAllMappedInterfaces(TypeDefinition declaringType, HashSet<TypeDefinition> result)
    {
        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? td = ResolveInterface(impl.Interface);
            if (td is null) { continue; }
            string ns = td.Namespace?.Value ?? string.Empty;
            string name = td.Name?.Value ?? string.Empty;
            if (MappedTypes.Get(ns, name) is { HasCustomMembersOutput: true })
            {
                _ = result.Add(td);
            }
            CollectAllMappedInterfaces(td, result);
        }
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
        HashSet<string> writtenMethods, Dictionary<string, PropertyAccessorState> propertyState, HashSet<string> writtenEvents, HashSet<TypeDefinition> writtenInterfaces)
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

            // Determine the (possibly substituted) interface signature for the recursion.
            AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? nextInstance = null;
            if (impl.Interface is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
            {
                nextInstance = currentInstance is not null
                    ? gi.InstantiateGenericTypes(genCtx) as AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature
                    : gi;
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
                    string objRefName;
                    if (nextInstance is not null)
                    {
                        AsmResolver.DotNet.ITypeDefOrRef? specRef = nextInstance.ToTypeDefOrRef();
                        objRefName = specRef is not null ? GetObjRefName(w, specRef) : GetObjRefName(w, impl.Interface);
                    }
                    else
                    {
                        objRefName = GetObjRefName(w, impl.Interface);
                    }
                    WriteMappedInterfaceStubs(w, nextInstance, ifaceName, objRefName);
                    // Mark sibling/parent mapped interfaces whose members are already covered
                    // (e.g., IMap`2/IVector`1/etc. include the IIterable`1 GetEnumerator stubs).
                    MarkCoveredMappedInterfaces(declaringType, ifaceName, writtenInterfaces);
                }
                continue;
            }

            WriteInterfaceMembers(w, classType, ifaceType, impl.Interface, isOverridable, isProtected, nextInstance,
                writtenMethods, propertyState, writtenEvents);

            // Recurse into derived interfaces
            WriteInterfaceMembersRecursive(w, classType, ifaceType, nextInstance, writtenMethods, propertyState, writtenEvents, writtenInterfaces);
        }
    }

    /// <summary>
    /// When emitting stubs for a mapped interface (e.g. IMap`2 -> IDictionary&lt;K,V&gt;), mark
    /// other mapped interfaces whose member contracts are already covered (e.g. IIterable`1
    /// -> IEnumerable&lt;T&gt;) so they don't get re-emitted later in the recursion.
    /// </summary>
    private static void MarkCoveredMappedInterfaces(TypeDefinition declaringType, string emittedName, HashSet<TypeDefinition> writtenInterfaces)
    {
        // IMap/IMapView/IVector/IVectorView all include the IIterable`1 GetEnumerator stubs.
        bool coversIterable = emittedName is "IMap`2" or "IMapView`2" or "IVector`1" or "IVectorView`1";
        // IBindableVector covers IBindableIterable.
        bool coversBindableIterable = emittedName == "IBindableVector";

        if (!coversIterable && !coversBindableIterable) { return; }

        void Walk(TypeDefinition td)
        {
            foreach (InterfaceImplementation imp in td.Interfaces)
            {
                if (imp.Interface is null) { continue; }
                TypeDefinition? rt = ResolveInterface(imp.Interface);
                if (rt is null) { continue; }

                string n = rt.Name?.Value ?? string.Empty;
                string ns = rt.Namespace?.Value ?? string.Empty;
                if ((coversIterable && ns == "Windows.Foundation.Collections" && n == "IIterable`1") ||
                    (coversBindableIterable && ns == "Microsoft.UI.Xaml.Interop" && n == "IBindableIterable") ||
                    (coversBindableIterable && ns == "Windows.UI.Xaml.Interop" && n == "IBindableIterable"))
                {
                    _ = writtenInterfaces.Add(rt);
                }
                Walk(rt);
            }
        }
        Walk(declaringType);
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
        HashSet<string> writtenMethods, Dictionary<string, PropertyAccessorState> propertyState, HashSet<string> writtenEvents)
    {
        bool sealed_ = classType.IsSealed;
        // Determine accessibility and method modifier
        string access = (isOverridable || isProtected) ? "protected " : "public ";
        string methodSpec = string.Empty;
        if (isOverridable && !sealed_)
        {
            access = "protected ";
            methodSpec = "virtual ";
        }

        AsmResolver.DotNet.Signatures.GenericContext? genCtx = currentInstance is not null
            ? new AsmResolver.DotNet.Signatures.GenericContext(currentInstance, null)
            : null;

        // Generic interfaces require UnsafeAccessor-based dispatch (real ABI lives in the
        // post-build interop assembly). For now we keep the throw-null stubs for them — they
        // will be ported as part of the dedicated UnsafeAccessor work item.
        bool isGenericInterface = ifaceType.GenericParameters.Count > 0;

        // Compute the ABI Methods static class name (e.g. "global::ABI.Windows.Foundation.IDeferralMethods")
        // — note this is the ungenerified Methods class for generic interfaces (matches truth output).
        // The _objRef_ field name uses the full instantiated interface name so generic instantiations
        // (e.g. IAsyncOperation<uint>) get a per-instantiation field.
        string abiClass = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, ifaceType, TypedefNameType.StaticAbiClass, true);
        }));
        if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal))
        {
            abiClass = "global::" + abiClass;
        }
        string objRef = GetObjRefName(w, originalInterface);

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

            w.Write("\n");
            w.Write(access);
            w.Write(methodSpec);
            WriteProjectionReturnType(w, sig);
            w.Write(" ");
            w.Write(name);
            w.Write("(");
            WriteParameterList(w, sig);
            if (isGenericInterface)
            {
                w.Write(") => throw null!;\n");
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
                };
                propertyState[name] = state;
            }
            if (getter is not null && !state.HasGetter)
            {
                state.HasGetter = true;
                state.GetterAbiClass = abiClass;
                state.GetterObjRef = objRef;
                state.GetterIsGeneric = isGenericInterface;
            }
            if (setter is not null && !state.HasSetter)
            {
                state.HasSetter = true;
                state.SetterAbiClass = abiClass;
                state.SetterObjRef = objRef;
                state.SetterIsGeneric = isGenericInterface;
            }
        }

        // Events (deferred port: real implementation requires lazy event source field generation;
        // the throw-null stub remains compatible since events resolve at runtime via the adapter).
        foreach (EventDefinition evt in ifaceType.Events)
        {
            string name = evt.Name?.Value ?? string.Empty;
            if (!writtenEvents.Add(name)) { continue; }

            w.Write("\n");
            w.Write(access);
            w.Write(methodSpec);
            w.Write("event ");
            WriteEventType(w, evt, currentInstance);
            w.Write(" ");
            w.Write(name);
            w.Write(" { add => throw null!; remove => throw null!; }\n");
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
