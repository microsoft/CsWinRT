// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the <c>ABI.&lt;Ns&gt;.&lt;Class&gt;</c> abstract base class that lets a runtime class defined in the
/// input metadata be authored in C#: the author extends it and overrides the generated <c>abstract</c>
/// members, so no required member can be missed. This is the 3.0 replacement for the 2.x
/// <c>CsWinRTPublicExclusiveToInterfaces</c> behavior.
/// </summary>
internal static class AbiImplementableClassFactory
{
    /// <summary>
    /// Returns whether an abstract implementable base class should be generated for <paramref name="type"/>.
    /// </summary>
    public static bool ShouldEmit(ProjectionEmitContext context, TypeDefinition type)
    {
        // The abstract base is separate from the (possibly sealed) projected class and bridges to it via an
        // implicit operator, so it applies to both sealed and unsealed runtime classes.
        return context.Settings.AuthorExclusiveToInterfaces
            && !type.IsStatic
            && !type.IsAttributeType;
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;</c> with one <c>abstract</c> member per projected
    /// instance member, plus an <c>implicit operator</c> to the projected type.
    /// </summary>
    public static void WriteImplementableClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string projectedType = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);

        writer.WriteLine();

        // Chain to the base runtime class's abstract type (if any) so inherited members are
        // implemented via the base and only this class's own members are declared here.
        TypeDefinition? baseType = GetImplementableBaseType(context, type);

        HashSet<TypeDefinition> baseClosure = [];

        if (baseType is not null)
        {
            CollectInterfaceClosure(context, baseType, baseClosure);
        }

        // Collect the interfaces to declare on the abstract base. Implementing them (with the abstract
        // members below) is what lets an authored 'MyFoo : ABI.Ns.Foo' satisfy the Windows Runtime
        // interfaces so the CCW dispatches to the author's overrides.
        List<string> bases = [];

        if (baseType is not null)
        {
            (string baseNs, string baseName) = baseType.Names();
            bases.Add($"global::ABI.{baseNs}.{IdentifierEscaping.StripBackticks(baseName)}");
        }

        CollectImplementedInterfaces(context, type, null, [.. baseClosure], bases);

        string inheritance = bases.Count > 0 ? " : " + string.Join(", ", bases) : string.Empty;

        writer.WriteLine($"public abstract class {nameStripped}{inheritance}");

        using (writer.WriteBlock())
        {
            HashSet<string> writtenMethods = [];
            HashSet<string> writtenEvents = [];
            HashSet<TypeDefinition> writtenInterfaces = [.. baseClosure];

            // Merge property accessors across all implemented interfaces.
            Dictionary<string, PropertyInfo> properties = [];

            WriteInterfaceMembersRecursive(writer, context, type, null, writtenMethods, properties, writtenEvents, writtenInterfaces);

            EmitMergedProperties(writer, properties);

            // The implicit conversion reuses the projection's own marshaller (its
            // 'ConvertToManaged(void*)' resolves the correct RCW via the ComWrappers callback).
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                public static implicit operator {{projectedType}}?({{nameStripped}}? value)
                {
                    if (value is null)
                    {
                        return null;
                    }

                    using WindowsRuntimeObjectReferenceValue objectReferenceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

                    return global::ABI.{{typeNs}}.{{nameStripped}}Marshaller.ConvertToManaged(objectReferenceValue.GetThisPtrUnsafe());
                }
                """);
        }
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;Factory</c> implementing the class's <c>[Activatable]</c>/<c>[Static]</c>
    /// factory interfaces, so the factory (statics and factory methods) can be authored in C# by extending it.
    /// </summary>
    public static void WriteImplementableFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Gather the '[Activatable]'/'[Static]' factory interfaces for this runtime class.
        List<TypeDefinition> factoryInterfaces = [];

        foreach (KeyValuePair<string, AttributedType> entry in AttributedTypes.Get(type, context.Cache))
        {
            if ((entry.Value.Activatable || entry.Value.Statics) && entry.Value.Type is not null)
            {
                factoryInterfaces.Add(entry.Value.Type);
            }
        }

        if (factoryInterfaces.Count == 0)
        {
            return;
        }

        string nameStripped = type.GetStrippedName();

        // Inheritance list: the factory/statics interfaces (and any of their base interfaces).
        List<string> bases = [];
        HashSet<TypeDefinition> declared = [];

        foreach (TypeDefinition iface in factoryInterfaces)
        {
            if (declared.Add(iface))
            {
                bases.Add(TypedefNameWriter.WriteTypedefName(context, iface, TypedefNameType.CCW, false).Format());
                CollectImplementedInterfaces(context, iface, null, declared, bases);
            }
        }

        writer.WriteLine();
        writer.WriteLine($"public abstract class {nameStripped}Factory : {string.Join(", ", bases)}");
        using (writer.WriteBlock())
        {
            HashSet<string> writtenMethods = [];
            HashSet<string> writtenEvents = [];
            HashSet<TypeDefinition> writtenInterfaces = [];
            Dictionary<string, PropertyInfo> properties = [];

            foreach (TypeDefinition iface in factoryInterfaces)
            {
                if (writtenInterfaces.Add(iface))
                {
                    EmitInterfaceMembers(writer, context, iface, null, writtenMethods, properties, writtenEvents, writtenInterfaces);
                }
            }

            EmitMergedProperties(writer, properties);
        }
    }

    /// <summary>
    /// Emits the merged <c>abstract</c> properties collected during the interface walk.
    /// </summary>
    private static void EmitMergedProperties(IndentedTextWriter writer, Dictionary<string, PropertyInfo> properties)
    {
        foreach (KeyValuePair<string, PropertyInfo> kvp in properties)
        {
            PropertyInfo info = kvp.Value;
            string accessors = (info.HasGetter, info.HasSetter) switch
            {
                (true, true) => "{ get; set; }",
                (true, false) => "{ get; }",
                _ => "{ set; }"
            };

            writer.WriteLine();
            writer.WriteLine($"public abstract {info.TypeText} {kvp.Key} {accessors}");
        }
    }

    /// <summary>
    /// Recursively walks the interfaces implemented by <paramref name="declaringType"/>, emitting
    /// <c>abstract</c> methods and events inline and accumulating properties into <paramref name="properties"/>.
    /// </summary>
    private static void WriteInterfaceMembersRecursive(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition declaringType,
        GenericInstanceTypeSignature? currentInstance,
        HashSet<string> writtenMethods,
        IDictionary<string, PropertyInfo> properties,
        HashSet<string> writtenEvents,
        HashSet<TypeDefinition> writtenInterfaces)
    {
        GenericContext genericContext = new(currentInstance, null);

        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (impl.Interface is null)
            {
                continue;
            }

            if (!impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceType))
            {
                continue;
            }

            if (!writtenInterfaces.Add(ifaceType))
            {
                continue;
            }

            GenericInstanceTypeSignature? nextInstance = null;

            if (impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? gi))
            {
                if (currentInstance is not null && gi.InstantiateGenericTypes(genericContext) is GenericInstanceTypeSignature subGi)
                {
                    nextInstance = subGi;
                }
                else
                {
                    nextInstance = gi;
                }
            }

            EmitInterfaceMembers(writer, context, ifaceType, nextInstance, writtenMethods, properties, writtenEvents, writtenInterfaces);
        }
    }

    /// <summary>
    /// Emits the <c>abstract</c> members declared directly on <paramref name="ifaceType"/> and recurses into its
    /// base interfaces. Methods and events are emitted inline; properties are merged into <paramref name="properties"/>.
    /// </summary>
    private static void EmitInterfaceMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition ifaceType,
        GenericInstanceTypeSignature? nextInstance,
        HashSet<string> writtenMethods,
        IDictionary<string, PropertyInfo> properties,
        HashSet<string> writtenEvents,
        HashSet<TypeDefinition> writtenInterfaces)
    {
            GenericContext? memberContext = nextInstance is not null ? new GenericContext(nextInstance, null) : null;

            // Methods
            foreach (MethodDefinition method in ifaceType.GetNonSpecialMethods())
            {
                string name = method.GetRawName();
                MethodSignatureInfo sig = new(method, memberContext);

                if (!writtenMethods.Add(sig.GetDedupeKey(name)))
                {
                    continue;
                }

                IndentedTextWriterCallback ret = MethodFactory.WriteProjectionReturnType(context, sig);
                IndentedTextWriterCallback parms = MethodFactory.WriteParameterList(context, sig);

                writer.WriteLine();
                writer.WriteLine($"public abstract {ret} {name}({parms});");
            }

            // Properties (merged after the walk)
            foreach (PropertyDefinition prop in ifaceType.Properties)
            {
                string name = prop.GetRawName();
                (MethodDefinition? getter, MethodDefinition? setter) = prop.GetMethods();

                if (!properties.TryGetValue(name, out PropertyInfo? info))
                {
                    info = new PropertyInfo { TypeText = InterfaceFactory.WritePropType(context, prop, memberContext) };
                    properties[name] = info;
                }

                info.HasGetter |= getter is not null;
                info.HasSetter |= setter is not null;
            }

            // Events
            foreach (EventDefinition evt in ifaceType.Events)
            {
                string name = evt.GetRawName();

                if (!writtenEvents.Add(name))
                {
                    continue;
                }

                IndentedTextWriterCallback eventType = TypedefNameWriter.WriteEventType(context, evt, nextInstance);

                writer.WriteLine();
                writer.WriteLine($"public abstract event {eventType} {name};");
            }

            // Recurse into base interfaces.
            WriteInterfaceMembersRecursive(writer, context, ifaceType, nextInstance, writtenMethods, properties, writtenEvents, writtenInterfaces);
    }

    /// <summary>
    /// Collects the CCW names of the Windows Runtime interfaces implemented by <paramref name="declaringType"/>
    /// (transitively), skipping those already provided by the base class closure, so they can be declared on
    /// the abstract base's inheritance list.
    /// </summary>
    private static void CollectImplementedInterfaces(
        ProjectionEmitContext context,
        TypeDefinition declaringType,
        GenericInstanceTypeSignature? currentInstance,
        HashSet<TypeDefinition> visited,
        List<string> collected)
    {
        GenericContext genericContext = new(currentInstance, null);

        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (impl.Interface is null || !impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceType))
            {
                continue;
            }

            if (!visited.Add(ifaceType))
            {
                continue;
            }

            GenericInstanceTypeSignature? nextInstance = null;

            if (impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? gi))
            {
                nextInstance = currentInstance is not null && gi.InstantiateGenericTypes(genericContext) is GenericInstanceTypeSignature subGi ? subGi : gi;
            }
            else
            {
                // Only non-generic interfaces are declared on the base (their signatures need no type args).
                collected.Add(TypedefNameWriter.WriteTypedefName(context, ifaceType, TypedefNameType.CCW, false).Format());
            }

            CollectImplementedInterfaces(context, ifaceType, nextInstance, visited, collected);
        }
    }

    /// <summary>
    /// Returns the base runtime class of <paramref name="type"/> that has its own generated abstract
    /// base, or <see langword="null"/> if the class derives directly from <c>WindowsRuntimeObject</c>.
    /// </summary>
    private static TypeDefinition? GetImplementableBaseType(ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.BaseType is not { } baseRef)
        {
            return null;
        }

        (string baseNs, string baseName) = baseRef.Names();

        if (baseNs == "System" && baseName == "Object")
        {
            return null;
        }

        TypeDefinition? baseType = baseRef.ResolveAsTypeDefinition(context.Cache);

        // Only chain to another runtime class (which will have its own generated ABI base).
        if (baseType is null || baseType.IsStatic || TypeKindResolver.Resolve(baseType) != TypeKind.Class)
        {
            return null;
        }

        return baseType;
    }

    /// <summary>
    /// Adds all interfaces implemented by <paramref name="type"/> (transitively, including its own base
    /// classes) into <paramref name="interfaces"/> so a derived class does not re-declare inherited members.
    /// </summary>
    private static void CollectInterfaceClosure(ProjectionEmitContext context, TypeDefinition type, HashSet<TypeDefinition> interfaces)
    {
        for (TypeDefinition? current = type; current is not null;)
        {
            foreach (InterfaceImplementation impl in current.Interfaces)
            {
                if (impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceType) && interfaces.Add(ifaceType))
                {
                    CollectInterfaceClosure(context, ifaceType, interfaces);
                }
            }

            current = current.BaseType is { } baseRef ? baseRef.ResolveAsTypeDefinition(context.Cache) : null;
        }
    }

    /// <summary>
    /// Accumulated getter/setter presence and projected type text for a merged property.
    /// </summary>
    private sealed class PropertyInfo
    {
        public required string TypeText { get; init; }

        public bool HasGetter { get; set; }

        public bool HasSetter { get; set; }
    }
}
