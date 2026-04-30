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
        HashSet<string> writtenProperties = new(System.StringComparer.Ordinal);
        HashSet<string> writtenEvents = new(System.StringComparer.Ordinal);
        HashSet<TypeDefinition> writtenInterfaces = new();

        WriteInterfaceMembersRecursive(w, type, type, writtenMethods, writtenProperties, writtenEvents, writtenInterfaces);
    }

    private static void WriteInterfaceMembersRecursive(TypeWriter w, TypeDefinition classType, TypeDefinition declaringType,
        HashSet<string> writtenMethods, HashSet<string> writtenProperties, HashSet<string> writtenEvents, HashSet<TypeDefinition> writtenInterfaces)
    {
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

            // Skip mapped interfaces (they're handled by the runtime's adapter classes)
            string ifaceNs = ifaceType.Namespace?.Value ?? string.Empty;
            string ifaceName = ifaceType.Name?.Value ?? string.Empty;
            if (MappedTypes.Get(ifaceNs, ifaceName) is { HasCustomMembersOutput: true })
            {
                continue;
            }

            WriteInterfaceMembers(w, classType, ifaceType, isOverridable, isProtected,
                writtenMethods, writtenProperties, writtenEvents);

            // Recurse into derived interfaces
            WriteInterfaceMembersRecursive(w, classType, ifaceType, writtenMethods, writtenProperties, writtenEvents, writtenInterfaces);
        }
    }

    /// <summary>Resolves a TypeRef to a TypeDef using the cache.</summary>
    private static TypeDefinition? ResolveInterface(ITypeDefOrRef typeRef)
    {
        if (typeRef is TypeDefinition td) { return td; }
        if (typeRef is TypeReference tr && _cacheRef is not null)
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
        bool isOverridable, bool isProtected,
        HashSet<string> writtenMethods, HashSet<string> writtenProperties, HashSet<string> writtenEvents)
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

        // Methods
        foreach (MethodDefinition method in ifaceType.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            string name = method.Name?.Value ?? string.Empty;
            // Track by signature key (name + param count) to avoid trivial overload duplicates
            MethodSig sig = new(method);
            string key = name + ":" + sig.Params.Count;
            if (!writtenMethods.Add(key)) { continue; }

            w.Write("\n");
            w.Write(access);
            w.Write(methodSpec);
            WriteProjectionReturnType(w, sig);
            w.Write(" ");
            w.Write(name);
            w.Write("(");
            WriteParameterList(w, sig);
            w.Write(") => throw null!;\n");
        }

        // Properties
        foreach (PropertyDefinition prop in ifaceType.Properties)
        {
            string name = prop.Name?.Value ?? string.Empty;
            if (!writtenProperties.Add(name)) { continue; }

            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            string propType = WritePropType(w, prop);

            w.Write("\n");
            w.Write(access);
            w.Write(methodSpec);
            w.Write(propType);
            w.Write(" ");
            w.Write(name);
            w.Write(" { ");
            if (getter is not null) { w.Write("get => throw null!; "); }
            if (setter is not null) { w.Write("set => throw null!; "); }
            w.Write("}\n");
        }

        // Events
        foreach (EventDefinition evt in ifaceType.Events)
        {
            string name = evt.Name?.Value ?? string.Empty;
            if (!writtenEvents.Add(name)) { continue; }

            w.Write("\n");
            w.Write(access);
            w.Write(methodSpec);
            w.Write("event ");
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
            w.Write(name);
            w.Write(" { add => throw null!; remove => throw null!; }\n");
        }
    }
}
