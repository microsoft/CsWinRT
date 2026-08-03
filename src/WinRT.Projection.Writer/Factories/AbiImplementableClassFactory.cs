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
/// members, so no required member can be missed.
/// </summary>
internal static class AbiImplementableClassFactory
{
    /// <summary>
    /// Returns whether an <c>[exclusiveto]</c> interface belongs to a runtime class that can be implemented
    /// (authored) in C#, and so needs its projection emitted even though it stays <c>internal</c>.
    /// </summary>
    /// <remarks>
    /// Such an interface needs its COM Callable Wrapper vtable, since that is what a native call dispatches
    /// through to reach the author's overrides, and its declaration, so the abstract base can implement it.
    /// </remarks>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The interface to inspect.</param>
    public static bool IsImplementableExclusiveToInterface(ProjectionEmitContext context, TypeDefinition type)
    {
        if (!type.IsExclusiveTo)
        {
            return false;
        }

        // The whole projection was asked to support authoring, so every runtime class in it can be implemented
        if (context.Settings.ImplementWinMDTypes)
        {
            return true;
        }

        // Otherwise only the classes an application actually implements need this (see 'ImplementableTypes')
        if (context.Settings.ImplementableTypes.Count == 0)
        {
            return false;
        }

        return AbiTypeHelpers.GetExclusiveToType(context.Cache, type) is { } exclusiveToType
            && context.Settings.ImplementableTypes.Contains(exclusiveToType.FullName);
    }

    /// <summary>
    /// Returns whether an abstract implementable base class should be generated for <paramref name="type"/>.
    /// </summary>
    public static bool ShouldEmit(ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.IsAttributeType)
        {
            return false;
        }

        if (!context.Settings.ImplementWinMDTypes && !context.Settings.ImplementableTypes.Contains(type.FullName))
        {
            return false;
        }

        // A custom-mapped type is projected as its .NET counterpart (e.g. 'Windows.Foundation.Uri' is
        // 'System.Uri'), so there is no Windows Runtime type left for an author to implement.
        if (IsCustomMappedType(type))
        {
            return false;
        }

        // A custom-mapped interface is projected as its .NET counterpart, whose members the input metadata
        // does not describe. Those are declared from a known .NET shape, so only interfaces without one skip.
        if (ImplementsUnsupportedMappedInterface(context, type))
        {
            return false;
        }

        // Redeclaring a property already declared 'abstract' by a base would hide it, leaving the inherited
        // member impossible to implement. C# cannot express both (e.g. 'MapControl.Style' of type 'MapStyle'
        // alongside the inherited 'FrameworkElement.Style' of type 'Style').
        if (HasConflictingBaseProperty(context, type))
        {
            return false;
        }

        // The factory base's name is reserved (see 'GetFactoryClassName'), so a runtime class cannot get one
        // if a real Windows Runtime class in the same namespace already has that name.
        if (IsFactoryClassNameTaken(context, type))
        {
            return false;
        }

        // Static runtime classes have no instances, so they only get a factory base (and only if
        // they actually declare any statics). All other runtime classes always get an instance base:
        // it is separate from the (possibly sealed) projected class and bridges to it via an implicit
        // operator, so it applies to both sealed and unsealed runtime classes.
        return !type.IsStatic || HasFactoryInterfaces(context, type);
    }

    /// <summary>
    /// Returns whether the name a type's factory base would take is already used by a real Windows Runtime
    /// class in the same namespace (which would generate an implementable base of its own under that name).
    /// </summary>
    private static bool IsFactoryClassNameTaken(ProjectionEmitContext context, TypeDefinition type)
    {
        (string ns, string name) = type.Names();

        return context.Cache.Find(ns, GetFactoryClassName(IdentifierEscaping.StripBackticks(name))) is not null;
    }

    /// <summary>
    /// Returns the name to give a class's generated factory base.
    /// </summary>
    /// <remarks>
    /// The suffix is always <c>ActivationFactory</c>, never the shorter <c>Factory</c>: that collides with
    /// real Windows Runtime classes named <c>&lt;Name&gt;Factory</c> (e.g. <c>ActionEntityFactory</c> next to
    /// <c>ActionEntity</c>). Choosing conditionally would make the name depend on what else the namespace
    /// happens to contain, so adding such a class later would silently rename an existing base.
    /// </remarks>
    private static string GetFactoryClassName(string nameStripped)
    {
        return $"{nameStripped}ActivationFactory";
    }

    /// <summary>
    /// Returns whether a type declares a property whose name is also declared by an interface a base
    /// implementable class already covers, which C# cannot express (see the caller for details).
    /// </summary>
    private static bool HasConflictingBaseProperty(ProjectionEmitContext context, TypeDefinition type)
    {
        if (GetImplementableBaseType(context, type) is not TypeDefinition baseType)
        {
            return false;
        }

        HashSet<TypeDefinition> baseClosure = [];

        CollectInterfaceClosure(context, baseType, baseClosure);

        HashSet<string> baseProperties = [];

        foreach (TypeDefinition baseInterface in baseClosure)
        {
            foreach (PropertyDefinition baseProperty in baseInterface.Properties)
            {
                _ = baseProperties.Add(baseProperty.GetRawName());
            }
        }

        HashSet<TypeDefinition> closure = [];

        CollectInterfaceClosure(context, type, closure);

        foreach (TypeDefinition interfaceType in closure)
        {
            // Interfaces the base already covers contribute the base's own declarations, not new ones.
            if (baseClosure.Contains(interfaceType))
            {
                continue;
            }

            foreach (PropertyDefinition property in interfaceType.Properties)
            {
                if (baseProperties.Contains(property.GetRawName()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether a type is custom-mapped to a .NET type, and so is not projected as a Windows Runtime
    /// type that an author could implement.
    /// </summary>
    private static bool IsCustomMappedType(TypeDefinition type)
    {
        (string ns, string name) = type.Names();

        return MappedTypes.Get(ns, name) is not null;
    }

    /// <summary>
    /// Returns whether a type implements, directly or transitively, any interface that is custom-mapped to a
    /// .NET interface that no known abstract member shape exists for.
    /// </summary>
    private static bool ImplementsUnsupportedMappedInterface(ProjectionEmitContext context, TypeDefinition type)
    {
        HashSet<TypeDefinition> closure = [];

        CollectInterfaceClosure(context, type, closure);

        foreach (TypeDefinition interfaceType in closure)
        {
            if (IsCustomMappedInterface(interfaceType) && !MappedInterfaceAbstractMemberFactory.IsSupported(interfaceType.Name ?? ""))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;</c> with one <c>abstract</c> member per projected
    /// instance member, plus an <c>implicit operator</c> to the projected type.
    /// </summary>
    public static void WriteImplementableClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Static runtime classes have no instances, so there is nothing to implement beyond their factory.
        if (type.IsStatic)
        {
            return;
        }

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

        // Identify the Windows Runtime class this base stands for. The CCW of a type deriving from it
        // reports that class name (not the deriving type's own name), and the CsWinRT build tools use
        // this marker to recognize the generated bases.
        writer.WriteLine($"[WindowsRuntimeImplementableClass(typeof({projectedType}))]");
        writer.WriteLine($"public abstract class {nameStripped}{inheritance}");

        using (writer.WriteBlock())
        {
            HashSet<string> writtenMethods = [];
            HashSet<string> writtenEvents = [];
            HashSet<TypeDefinition> writtenInterfaces = [.. baseClosure];

            // Merge property accessors across all implemented interfaces.
            Dictionary<string, PropertyInfo> properties = [];

            WriteInterfaceMembersRecursive(writer, context, type, null, writtenMethods, properties, writtenEvents, writtenInterfaces);

            EmitMergedProperties(writer, properties, writtenMethods);

            // The conversion creates a CCW for the authored object and then resolves the projected RCW for
            // it (which goes through the usual 'ComWrappers' callback). A reference assembly carries no
            // implementation, so it only declares the operator; the real body is regenerated at publish
            // time, when this is compiled against the runtime implementation assembly.
            writer.WriteLine();

            if (context.Settings.ReferenceProjection)
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static implicit operator {{projectedType}}?({{nameStripped}}? value)
                    {
                        throw null;
                    }
                    """);
            }
            else
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static unsafe implicit operator {{projectedType}}?({{nameStripped}}? value)
                    {
                        if (value is null)
                        {
                            return null;
                        }

                        using WindowsRuntimeObjectReferenceValue objectReferenceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

                        return ({{projectedType}})WindowsRuntimeObjectMarshaller.ConvertToManaged(objectReferenceValue.GetThisPtrUnsafe())!;
                    }
                    """);
            }
        }
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;Factory</c> implementing the class's <c>[Activatable]</c>,
    /// <c>[Static]</c> and <c>[Composable]</c> factory interfaces, so the factory (statics, factory methods and
    /// default activation) can be authored in C# by extending it.
    /// </summary>
    public static void WriteImplementableFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        CollectFactoryInterfaces(context, type, out List<TypeDefinition> factoryInterfaces, out HashSet<TypeDefinition> composableInterfaces, out bool hasDefaultActivation);

        if (factoryInterfaces.Count == 0 && !hasDefaultActivation)
        {
            return;
        }

        string nameStripped = type.GetStrippedName();
        string projectedType = TypedefNameWriter.BuildGlobalQualifiedName(type.GetRawNamespace(), nameStripped);

        // Inheritance list: the factory/statics interfaces (and any of their base interfaces). A class with a
        // default (parameterless) activation is activated through 'IActivationFactory' instead, which carries no
        // dedicated factory interface in metadata, so it is added explicitly.
        List<string> bases = [];
        HashSet<TypeDefinition> declared = [];

        if (hasDefaultActivation)
        {
            bases.Add("global::WindowsRuntime.InteropServices.IActivationFactory");
        }

        foreach (TypeDefinition iface in factoryInterfaces)
        {
            if (declared.Add(iface))
            {
                bases.Add(TypedefNameWriter.WriteTypedefName(context, iface, TypedefNameType.CCW, false).Format());
                CollectImplementedInterfaces(context, iface, null, declared, bases);
            }
        }

        writer.WriteLine();
        writer.WriteLine($"[WindowsRuntimeImplementableClassFactory(typeof({projectedType}))]");

        string factoryName = GetFactoryClassName(nameStripped);

        writer.WriteLine($"public abstract class {factoryName} : {string.Join(", ", bases)}");

        using (writer.WriteBlock())
        {
            HashSet<string> writtenMethods = [];
            HashSet<string> writtenEvents = [];
            HashSet<TypeDefinition> writtenInterfaces = [];
            Dictionary<string, PropertyInfo> properties = [];

            if (hasDefaultActivation)
            {
                writer.WriteLine();
                writer.WriteLine("public abstract object ActivateInstance();");
            }

            foreach (TypeDefinition iface in factoryInterfaces)
            {
                if (!writtenInterfaces.Add(iface))
                {
                    continue;
                }

                // A composable factory method carries the raw COM aggregation contract. That is infrastructure,
                // not something an author should implement, so it is generated here and forwards to a hook.
                if (composableInterfaces.Contains(iface))
                {
                    EmitComposableFactoryMembers(writer, context, iface, nameStripped, projectedType, writtenMethods);
                }
                else
                {
                    EmitInterfaceMembers(writer, context, iface, null, isProtectedSurface: false, writtenMethods, properties, writtenEvents, writtenInterfaces);
                }
            }

            EmitMergedProperties(writer, properties, writtenMethods);

            // The activation entry point generated into the component's own assembly needs to hand out a CCW
            // for the factory, but it is compiled against the runtime reference assembly, where the
            // marshalling APIs are stripped. So the conversion lives here instead, alongside the rest of the
            // projection: declared in the reference projection and implemented when the application is built.
            writer.WriteLine();

            if (context.Settings.ReferenceProjection)
            {
                writer.WriteLine(isMultiline: true, $$"""
                    [EditorBrowsable(EditorBrowsableState.Never)]
                    public static nint GetActivationFactoryUnsafe({{factoryName}} value)
                    {
                        throw null;
                    }
                    """);
            }
            else
            {
                writer.WriteLine(isMultiline: true, $$"""
                    [EditorBrowsable(EditorBrowsableState.Never)]
                    public static unsafe nint GetActivationFactoryUnsafe({{factoryName}} value)
                    {
                        return (nint)WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value).DetachThisPtrUnsafe();
                    }
                    """);
            }
        }
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> declares any factory (activation, statics or composition) surface.
    /// </summary>
    private static bool HasFactoryInterfaces(ProjectionEmitContext context, TypeDefinition type)
    {
        CollectFactoryInterfaces(context, type, out List<TypeDefinition> factoryInterfaces, out _, out bool hasDefaultActivation);

        return factoryInterfaces.Count > 0 || hasDefaultActivation;
    }

    /// <summary>
    /// Collects the <c>[Activatable]</c>, <c>[Static]</c> and <c>[Composable]</c> factory interfaces of a runtime class.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to inspect.</param>
    /// <param name="factoryInterfaces">The collected factory interfaces.</param>
    /// <param name="composableInterfaces">The subset of <paramref name="factoryInterfaces"/> that are <c>[Composable]</c>.</param>
    /// <param name="hasDefaultActivation">Whether the class supports default (parameterless) activation.</param>
    private static void CollectFactoryInterfaces(
        ProjectionEmitContext context,
        TypeDefinition type,
        out List<TypeDefinition> factoryInterfaces,
        out HashSet<TypeDefinition> composableInterfaces,
        out bool hasDefaultActivation)
    {
        factoryInterfaces = [];
        composableInterfaces = [];
        hasDefaultActivation = false;

        foreach (KeyValuePair<string, AttributedType> entry in AttributedTypes.Get(type, context.Cache))
        {
            AttributedType attributedType = entry.Value;

            if (!attributedType.Activatable && !attributedType.Statics && !attributedType.Composable)
            {
                continue;
            }

            // An '[Activatable]' attribute with no factory interface means default (parameterless) activation
            if (attributedType.Type is null)
            {
                hasDefaultActivation |= attributedType.Activatable;
            }
            else
            {
                factoryInterfaces.Add(attributedType.Type);

                if (attributedType.Composable)
                {
                    _ = composableInterfaces.Add(attributedType.Type);
                }
            }
        }
    }

    /// <summary>
    /// Emits the members of a <c>[Composable]</c> factory interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A composable factory method is declared as
    /// <c>T CreateInstance(&lt;args&gt;, object baseInterface, out object innerInterface)</c>, where
    /// <c>baseInterface</c> is the controlling outer and <c>innerInterface</c> the non-delegating inner. That
    /// COM aggregation plumbing is generated here, forwarding to a <c>protected abstract</c> hook that takes
    /// only the real arguments.
    /// </para>
    /// <para>
    /// A non-<see langword="null"/> outer is rejected: it needs a managed object to act as an aggregated
    /// inner, and <c>ComWrappers</c> only supports aggregation in the consuming direction. Standalone
    /// activation passes a <see langword="null"/> outer and the caller ignores the inner, so returning the
    /// instance for both is correct, and matches C++/WinRT.
    /// </para>
    /// </remarks>
    private static void EmitComposableFactoryMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition ifaceType,
        string nameStripped,
        string projectedType,
        HashSet<string> writtenMethods)
    {
        foreach (MethodDefinition method in ifaceType.GetNonSpecialMethods())
        {
            string name = method.GetRawName();
            MethodSignatureInfo sig = new(method);

            if (!writtenMethods.Add(sig.GetDedupeKey(name)))
            {
                continue;
            }

            // The trailing two parameters are always the outer and the inner (see the remarks above)
            int userParameterCount = sig.Parameters.Count >= 2 ? sig.Parameters.Count - 2 : sig.Parameters.Count;

            void WriteUserParameters(IndentedTextWriter writer)
            {
                for (int i = 0; i < userParameterCount; i++)
                {
                    IndentedTextWriterCallback parameter = MethodFactory.WriteProjectionParameter(context, sig.Parameters[i]);

                    writer.Write($"{(i > 0 ? ", " : "")}{parameter}");
                }
            }

            void WriteUserArguments(IndentedTextWriter writer)
            {
                for (int i = 0; i < userParameterCount; i++)
                {
                    IndentedTextWriterCallback argument = IdentifierEscaping.WriteEscapedIdentifier(sig.Parameters[i].GetRawName());

                    writer.Write($"{(i > 0 ? ", " : "")}{argument}");
                }
            }

            IndentedTextWriterCallback parameters = MethodFactory.WriteParameterList(context, sig);
            string displayName = projectedType.Replace("global::", "");

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                {{projectedType}} {{TypedefNameWriter.WriteTypedefName(context, ifaceType, TypedefNameType.CCW, false).Format()}}.{{name}}({{parameters}})
                {
                    if (baseInterface is not null)
                    {
                        throw new global::System.NotSupportedException(
                            "Composing a Windows Runtime type implemented in C# is not supported. " +
                            "Only activating '{{displayName}}' directly is.");
                    }

                    {{nameStripped}} instance = {{name}}({{WriteUserArguments}});

                    innerInterface = instance;

                    return instance;
                }
                """);

            writer.WriteLine();
            writer.WriteLine($"protected abstract {nameStripped} {name}({WriteUserParameters});");
        }
    }

    /// <summary>
    /// Emits the merged <c>abstract</c> properties collected during the interface walk.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="properties">The properties collected during the walk.</param>
    /// <param name="writtenMembers">Signature keys of members already written, so a property is not declared twice when a mapped interface's .NET shape already supplied it.</param>
    private static void EmitMergedProperties(IndentedTextWriter writer, Dictionary<string, PropertyInfo> properties, HashSet<string> writtenMembers)
    {
        foreach (KeyValuePair<string, PropertyInfo> kvp in properties)
        {
            // A property of the same name may already have been declared from a mapped interface's .NET
            // shape (e.g. 'Count', or 'Item' for an indexer), and that declaration is the one that
            // satisfies the interface.
            if (writtenMembers.Contains($"P:{kvp.Key}"))
            {
                continue;
            }

            PropertyInfo info = kvp.Value;
            string accessors = (info.HasGetter, info.HasSetter) switch
            {
                (true, true) => "{ get; set; }",
                (true, false) => "{ get; }",
                _ => "{ set; }"
            };

            writer.WriteLine();

            if (!info.IsProtectedSurface)
            {
                writer.WriteLine($"public abstract {info.TypeText} {kvp.Key} {accessors}");

                continue;
            }

            string forwarders = (info.HasGetter, info.HasSetter) switch
            {
                (true, true) => $"get => {kvp.Key}; set => {kvp.Key} = value;",
                (true, false) => $"get => {kvp.Key};",
                _ => $"set => {kvp.Key} = value;"
            };

            writer.WriteLine($"protected abstract {info.TypeText} {kvp.Key} {accessors}");
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                {{info.TypeText}} {{info.InterfaceName}}.{{kvp.Key}}
                {
                    {{forwarders}}
                }
                """);
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

            // A custom-mapped interface is declared in its .NET form, so its members are declared from that
            // .NET shape rather than from the Windows Runtime metadata shape (which describes different
            // members entirely, and would satisfy nothing).
            if (IsCustomMappedInterface(ifaceType))
            {
                GenericInstanceTypeSignature? mappedInstance = null;

                if (impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? mappedGi))
                {
                    mappedInstance = currentInstance is not null && mappedGi.InstantiateGenericTypes(genericContext) is GenericInstanceTypeSignature mappedSubGi ? mappedSubGi : mappedGi;
                }

                MappedInterfaceAbstractMemberFactory.WriteAbstractMembers(writer, context, mappedInstance, ifaceType.Name ?? "", writtenMethods);

                continue;
            }

            GenericInstanceTypeSignature? nextInstance = null;

            if (impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? gi))
            {
                nextInstance = currentInstance is not null && gi.InstantiateGenericTypes(genericContext) is GenericInstanceTypeSignature subGi ? subGi : gi;
            }

            // An '[Overridable]' or '[Protected]' interface is part of a runtime class's derivation contract
            // rather than its public surface, so the projected class declares its members 'protected'. The
            // implementable base has to match, or an authored type would expose them publicly.
            bool isProtectedSurface = impl.IsOverridable() || impl.HasWindowsFoundationMetadataAttribute(References.WellKnownAttributeNames.ProtectedAttribute);

            EmitInterfaceMembers(writer, context, ifaceType, nextInstance, isProtectedSurface, writtenMethods, properties, writtenEvents, writtenInterfaces);
        }
    }

    /// <summary>
    /// Emits the <c>abstract</c> members declared directly on <paramref name="ifaceType"/> and recurses into its
    /// base interfaces. Methods and events are emitted inline; properties are merged into <paramref name="properties"/>.
    /// </summary>
    /// <remarks>
    /// When <c>isProtectedSurface</c> is set, the interface is <c>[Overridable]</c> or <c>[Protected]</c>, i.e.
    /// part of the runtime class's derivation contract rather than its public surface. Its members are then
    /// declared <c>protected abstract</c> and paired with an explicit interface implementation, matching the
    /// shape of the projected class.
    /// </remarks>
    private static void EmitInterfaceMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition ifaceType,
        GenericInstanceTypeSignature? nextInstance,
        bool isProtectedSurface,
        HashSet<string> writtenMethods,
        IDictionary<string, PropertyInfo> properties,
        HashSet<string> writtenEvents,
        HashSet<TypeDefinition> writtenInterfaces)
    {
        GenericContext? memberContext = nextInstance is not null ? new GenericContext(nextInstance, null) : null;

        // The explicit interface implementations below need the interface named as it is declared on the base
        string interfaceName = isProtectedSurface ? FormatInterfaceName(context, nextInstance?.ToTypeDefOrRef() ?? ifaceType) : string.Empty;

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

            if (!isProtectedSurface)
            {
                writer.WriteLine($"public abstract {ret} {name}({parms});");

                continue;
            }

            IndentedTextWriterCallback args = MethodFactory.WriteCallArguments(context, sig, leadingComma: false);

            writer.WriteLine($"protected abstract {ret} {name}({parms});");
            writer.WriteLine();
            writer.WriteLine($"{ret} {interfaceName}.{name}({parms}) => {name}({args});");
        }

        // Properties (merged after the walk)
        foreach (PropertyDefinition prop in ifaceType.Properties)
        {
            string name = prop.GetRawName();
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetMethods();

            if (!properties.TryGetValue(name, out PropertyInfo? info))
            {
                info = new PropertyInfo
                {
                    TypeText = InterfaceFactory.WritePropType(context, prop, memberContext),
                    IsProtectedSurface = isProtectedSurface,
                    InterfaceName = interfaceName
                };
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

            if (!isProtectedSurface)
            {
                writer.WriteLine($"public abstract event {eventType} {name};");

                continue;
            }

            writer.WriteLine($"protected abstract event {eventType} {name};");
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                event {{eventType}} {{interfaceName}}.{{name}}
                {
                    add => {{name}} += value;
                    remove => {{name}} -= value;
                }
                """);
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

            // Declare the interface, constructed with its type arguments when it is generic. Custom-mapped
            // interfaces are declared in their .NET form (e.g. 'IMap<K, V>' as 'IDictionary<K, V>'), which is
            // what an author implements; the runtime's collection adapters service the Windows Runtime side.
            GenericInstanceTypeSignature? nextInstance = null;

            if (impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? gi))
            {
                nextInstance = currentInstance is not null && gi.InstantiateGenericTypes(genericContext) is GenericInstanceTypeSignature subGi ? subGi : gi;
            }

            // Format the substituted signature, so an inherited generic interface is declared with the
            // enclosing type's arguments (e.g. 'IMap<K, V>' reached through 'IObservableMap<string, object>').
            collected.Add(FormatInterfaceName(context, nextInstance?.ToTypeDefOrRef() ?? impl.Interface));

            // A custom-mapped interface's .NET form already implies its base interfaces (e.g.
            // 'IDictionary<K, V>' implies 'IEnumerable<KeyValuePair<K, V>>'), and the Windows Runtime bases
            // are not what the author implements, so the walk stops here.
            if (IsCustomMappedInterface(ifaceType))
            {
                continue;
            }

            CollectImplementedInterfaces(context, ifaceType, nextInstance, visited, collected);
        }
    }

    /// <summary>
    /// Formats an implemented interface for an inheritance list, including type arguments when generic.
    /// </summary>
    private static string FormatInterfaceName(ProjectionEmitContext context, ITypeDefOrRef interfaceType)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();

        IndentedTextWriter writer = writerOwner.Writer;

        InterfaceFactory.WriteInterfaceTypeName(writer, context, interfaceType, forceWriteNamespace: true);

        return writer.ToString();
    }

    /// <summary>
    /// Returns whether an interface is custom-mapped to a .NET interface with its own member shape (e.g.
    /// <c>IMap&lt;K, V&gt;</c> to <c>IDictionary&lt;K, V&gt;</c>), so its Windows Runtime members must not be
    /// declared on the abstract base.
    /// </summary>
    private static bool IsCustomMappedInterface(TypeDefinition interfaceType)
    {
        (string ns, string name) = interfaceType.Names();

        return MappedTypes.Get(ns, name) is { HasCustomMembersOutput: true };
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

        /// <summary>
        /// Whether the declaring interface is <c>[Overridable]</c> or <c>[Protected]</c>, so the property is
        /// declared <c>protected abstract</c> with an explicit interface implementation.
        /// </summary>
        public bool IsProtectedSurface { get; init; }

        /// <summary>
        /// The declaring interface, for the explicit interface implementation (only when <see cref="IsProtectedSurface"/>).
        /// </summary>
        public string InterfaceName { get; init; } = string.Empty;

        public bool HasGetter { get; set; }

        public bool HasSetter { get; set; }
    }
}
