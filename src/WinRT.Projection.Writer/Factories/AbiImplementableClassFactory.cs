// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// The cached results of <see cref="HasMemberConflicts"/>, for each runtime class.
    /// </summary>
    private static readonly ConditionalWeakTable<TypeDefinition, object> MemberConflictsCache = [];

    /// <summary>
    /// The cached results of <see cref="GetSkipReason"/>, for each runtime class.
    /// </summary>
    private static readonly ConditionalWeakTable<TypeDefinition, StrongBox<string?>> SkipReasonCache = [];

    /// <summary>
    /// The cached result for a runtime class whose members conflict.
    /// </summary>
    private static readonly object ConflictsFound = new();

    /// <summary>
    /// The cached result for a runtime class whose members do not conflict.
    /// </summary>
    private static readonly object NoConflictsFound = new();

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

        // A removed runtime class is not projected, so there would be no projected class for the base to convert to
        if (type.IsRemoved)
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

        // Static runtime classes have no instances, so they only get a factory base, and only if they actually
        // declare any statics. A static class without any has nothing to implement, so it is not worth reporting.
        if (type.IsStatic && !HasFactoryInterfaces(context, type))
        {
            return false;
        }

        if (SkipReasonCache.TryGetValue(type, out StrongBox<string?>? cachedReason))
        {
            return cachedReason.Value is null;
        }

        // The reason is computed outside of the cache, as computing it can recurse into the base class (which
        // 'ConditionalWeakTable.GetValue' does not support). Only the thread that adds it logs it, so a skipped
        // class is reported exactly once even though this is called repeatedly and in parallel.
        StrongBox<string?> reason = new(GetSkipReason(context, type));

        if (!SkipReasonCache.TryAdd(type, reason))
        {
            return !SkipReasonCache.TryGetValue(type, out cachedReason) || cachedReason.Value is null;
        }

        if (reason.Value is not null)
        {
            (context.Settings.Logger ?? Console.Out.WriteLine)(
                $"No implementable base generated for '{type.FullName}': {reason.Value}. It can still be used through its projection, but cannot be implemented in C#.");
        }

        return reason.Value is null;
    }

    /// <summary>
    /// Returns why no abstract implementable base class can be generated for <paramref name="type"/>, if any.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to inspect.</param>
    /// <returns>The reason no base can be generated, or <see langword="null"/> if one can.</returns>
    private static string? GetSkipReason(ProjectionEmitContext context, TypeDefinition type)
    {
        // A custom-mapped interface is projected as its .NET counterpart, whose members the input metadata
        // does not describe. Those are declared from a known .NET shape, so only interfaces without one skip.
        if (FindUnsupportedMappedInterface(context, type) is { } mappedInterface)
        {
            return $"it implements the custom-mapped interface '{mappedInterface.FullName}', whose .NET members cannot be generated";
        }

        // Members of different instantiations of the same generic interface differ only in their types (e.g. the
        // 'GetEnumerator()' of 'IEnumerable<int>' and 'IEnumerable<string>'), so C# cannot declare both 'abstract'.
        if (HasConflictingGenericInstantiations(context, type))
        {
            return "it implements multiple instantiations of the same generic interface";
        }

        // The factory base's name is reserved (see 'GetFactoryClassName'), so a runtime class cannot get one
        // if a real Windows Runtime class in the same namespace already has that name.
        if (IsFactoryClassNameTaken(context, type))
        {
            return $"the name of its factory base, '{GetFactoryClassName(IdentifierEscaping.StripBackticks(type.Names().Name))}', is already used by a runtime class";
        }

        // Two interfaces can require members C# cannot declare side by side, which only shows when the members are
        // resolved. For instance, methods differing only in their return type, or a property an inherited one cannot
        // satisfy (e.g. 'MapControl.Style' of type 'MapStyle' alongside the inherited 'FrameworkElement.Style' of type
        // 'Style'), as redeclaring it would hide the inherited member and leave it impossible to implement.
        if (HasMemberConflicts(context, type))
        {
            return "some of its members (or of its base classes or factory) conflict and cannot be declared together in C#";
        }

        // All other runtime classes always get an instance base: it is separate from the (possibly sealed) projected
        // class and bridges to it via an implicit operator, so it applies to both sealed and unsealed runtime classes.
        if (type.IsStatic)
        {
            return null;
        }

        // The base is chained to by name, so it has to be emitted as well. It is not when it is excluded
        // from this projection (e.g. a base class from another '.winmd'), or when it is itself skipped by
        // one of the checks above. Emitting anyway would reference a class that does not exist.
        if (GetImplementableBaseType(context, type) is not { } baseType)
        {
            return null;
        }

        if (!context.Settings.Filter.Includes(baseType.FullName))
        {
            return $"its base class '{baseType.FullName}' is not part of this projection";
        }

        return ShouldEmit(context, baseType) ? null : $"its base class '{baseType.FullName}' has no implementable base";
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
    /// The suffix is <c>ActivationFactory</c> rather than <c>Factory</c>, which would collide with real
    /// Windows Runtime classes named <c>&lt;Name&gt;Factory</c> (e.g. <c>ActionEntityFactory</c>).
    /// </remarks>
    private static string GetFactoryClassName(string nameStripped)
    {
        return $"{nameStripped}ActivationFactory";
    }

    /// <summary>
    /// Returns whether a type implements, directly or through its base classes, more than one instantiation of the
    /// same generic interface.
    /// </summary>
    private static bool HasConflictingGenericInstantiations(ProjectionEmitContext context, TypeDefinition type)
    {
        Dictionary<string, TypeDefinition> closure = [];

        CollectInterfaceClosure(context, type, closure);

        HashSet<TypeDefinition> genericInterfaces = [];

        foreach (TypeDefinition interfaceType in closure.Values)
        {
            if (interfaceType.GenericParameters.Count > 0 && !genericInterfaces.Add(interfaceType))
            {
                return true;
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
    /// Returns the first interface a type implements, directly or transitively, that is custom-mapped to a
    /// .NET interface that no known abstract member shape exists for, if any.
    /// </summary>
    private static TypeDefinition? FindUnsupportedMappedInterface(ProjectionEmitContext context, TypeDefinition type)
    {
        Dictionary<string, TypeDefinition> closure = [];

        CollectInterfaceClosure(context, type, closure);

        foreach (TypeDefinition interfaceType in closure.Values)
        {
            if (IsCustomMappedInterface(interfaceType) && !MappedInterfaceAbstractMemberFactory.IsSupported(interfaceType.Name ?? ""))
            {
                return interfaceType;
            }
        }

        return null;
    }

    /// <summary>
    /// Emits the abstract bases that let a runtime class be implemented in C#: an instance base (unless the class is
    /// static) and an activation factory base (if the class is activatable or has statics).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to emit the bases for (see <see cref="ShouldEmit"/>).</param>
    /// <param name="implementableClasses">Receives the class, for a reference projection to record in its assembly metadata.</param>
    /// <remarks>
    /// The record is all the CsWinRT build tools need: each base has a well-known name derived from the class, and
    /// the bases are the only types a reference projection declares in the <c>ABI</c> namespace, so they can be
    /// found by name. Recording it as assembly metadata keeps it off the bases' public surface.
    /// </remarks>
    public static void WriteImplementableBases(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, ConcurrentBag<string> implementableClasses)
    {
        if (context.Settings.ReferenceProjection)
        {
            implementableClasses.Add(type.FullName);
        }

        WriteImplementableClass(writer, context, type);
        WriteImplementableFactoryClass(writer, context, type);
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;</c> with one <c>abstract</c> member per projected
    /// instance member, plus an <c>implicit operator</c> to the projected type.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to emit the base for.</param>
    private static void WriteImplementableClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
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

        Dictionary<string, TypeDefinition> baseClosure = [];

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

        CollectImplementedInterfaces(context, type, null, [.. baseClosure.Keys], bases);

        // Mark the base so an implementation deriving from it can be recognized at runtime with a single type
        // test. Marshalling needs that to hand callers the projected type rather than the implementation (see
        // the conversions below). Only the root base declares it; deriving bases inherit it. That test only runs
        // against the implementation projection, so a reference projection leaves the interface out.
        if (baseType is null && !context.Settings.ReferenceProjection)
        {
            bases.Add("global::WindowsRuntime.InteropServices.IWindowsRuntimeImplementableClass");
        }

        string inheritance = bases.Count > 0 ? " : " + string.Join(", ", bases) : string.Empty;

        writer.WriteLine($"public abstract class {nameStripped}{inheritance}");

        using (writer.WriteBlock())
        {
            ImplementableMemberSet members = new();

            SeedInheritedMembers(context, type, members);
            EmitClassMembers(writer, context, type, members);

            // The conversion wraps the authored object: it creates a COM Callable Wrapper for it and then
            // resolves the projected type for that. It has to wrap rather than unwrap, since the authored
            // type derives from this base and not from the projected class, so there is no existing instance
            // of the latter to hand back. A reference assembly carries no implementation, so it only declares
            // the operator; the real body is regenerated at publish time, when this is compiled against the
            // runtime implementation assembly.
            writer.WriteLine();

            if (context.Settings.ReferenceProjection)
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static implicit operator {{projectedType}}({{nameStripped}} value)
                    {
                        throw null;
                    }
                    """);
            }
            else
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static unsafe implicit operator {{projectedType}}({{nameStripped}} value)
                    {
                        if (value is null)
                        {
                            return null;
                        }

                        using WindowsRuntimeObjectReferenceValue objectReferenceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

                        return ({{projectedType}})WindowsRuntimeImplementationMarshal.WrapToManaged(objectReferenceValue.GetThisPtrUnsafe());
                    }
                    """);
            }

            // Every base inherits the conversion to each projected ancestor from the base for that ancestor, which
            // competes with the conversion to this projected class followed by a reference conversion to the
            // ancestor, making a conversion to the ancestor ambiguous. Declaring it here too, from this exact type,
            // makes it the most specific one, and routing it through this class's conversion keeps the result the
            // same either way.
            for (TypeDefinition? ancestor = baseType; ancestor is not null; ancestor = GetImplementableBaseType(context, ancestor))
            {
                string ancestorProjectedType = TypedefNameWriter.BuildGlobalQualifiedName(ancestor.GetRawNamespace(), ancestor.GetStrippedName());
                string ancestorBody = context.Settings.ReferenceProjection ? "throw null;" : $"return ({projectedType})value;";

                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    public static implicit operator {{ancestorProjectedType}}({{nameStripped}} value)
                    {
                        {{ancestorBody}}
                    }
                    """);
            }

            // The reverse conversion, for an author needing their own implementation back from a projected
            // instance. It is explicit because it can fail: the instance may wrap a native implementation, or
            // an implementation of a different type. The conversion only has to reach this base, as the cast to
            // the implementation type itself is then a standard downcast the compiler supplies, which is also
            // what makes casting to the wrong implementation type throw as it should.
            writer.WriteLine();

            if (context.Settings.ReferenceProjection)
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static explicit operator {{nameStripped}}({{projectedType}} value)
                    {
                        throw null;
                    }
                    """);
            }
            else
            {
                writer.WriteLine(isMultiline: true, $$"""
                    public static explicit operator {{nameStripped}}({{projectedType}} value)
                    {
                        if (value is null)
                        {
                            return null;
                        }

                        return ({{nameStripped}})WindowsRuntimeImplementationMarshal.ConvertToImplementation(value, typeof({{nameStripped}}));
                    }
                    """);
            }

            // The same applies in reverse: the conversion from each projected ancestor is inherited from the base for that
            // ancestor, and competes with the conversion from this projected class, making a cast from the ancestor to an
            // implementation ambiguous. The conversion from the ancestor is declared here too, checking for this base.
            for (TypeDefinition? ancestor = baseType; ancestor is not null; ancestor = GetImplementableBaseType(context, ancestor))
            {
                string ancestorProjectedType = TypedefNameWriter.BuildGlobalQualifiedName(ancestor.GetRawNamespace(), ancestor.GetStrippedName());

                writer.WriteLine();

                if (context.Settings.ReferenceProjection)
                {
                    writer.WriteLine(isMultiline: true, $$"""
                        public static explicit operator {{nameStripped}}({{ancestorProjectedType}} value)
                        {
                            throw null;
                        }
                        """);
                }
                else
                {
                    writer.WriteLine(isMultiline: true, $$"""
                        public static explicit operator {{nameStripped}}({{ancestorProjectedType}} value)
                        {
                            if (value is null)
                            {
                                return null;
                            }

                            return ({{nameStripped}})WindowsRuntimeImplementationMarshal.ConvertToImplementation(value, typeof({{nameStripped}}));
                        }
                        """);
                }
            }
        }
    }

    /// <summary>
    /// Records the members declared by the bases of the base classes of <paramref name="type"/>, by replaying their emission.
    /// </summary>
    /// <remarks>
    /// Skipping the interfaces the bases implement is not enough: a new interface can require a member with the same signature
    /// as one a base already declares, and redeclaring it would hide the inherited member. Replaying the emission keys the
    /// members exactly as they are declared (e.g. a custom-mapped <c>IClosable</c> declares <c>Dispose()</c>, not the
    /// <c>Close()</c> its metadata describes), and records whether each one is <see langword="protected"/>.
    /// </remarks>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class whose inherited members to record.</param>
    /// <param name="members">The member set to record them into.</param>
    private static void SeedInheritedMembers(ProjectionEmitContext context, TypeDefinition type, ImplementableMemberSet members)
    {
        List<TypeDefinition> ancestors = [];

        for (TypeDefinition? ancestor = GetImplementableBaseType(context, type); ancestor is not null; ancestor = GetImplementableBaseType(context, ancestor))
        {
            ancestors.Add(ancestor);
        }

        if (ancestors.Count == 0)
        {
            return;
        }

        using IndentedTextWriterOwner discardedWriterOwner = IndentedTextWriterPool.GetOrCreate();

        // Replay from the root, which is the order in which the bases were emitted
        for (int i = ancestors.Count - 1; i >= 0; i--)
        {
            EmitClassMembers(discardedWriterOwner.Writer, context, ancestors[i], members);
        }
    }

    /// <summary>
    /// Emits the <c>abstract</c> members of the instance base of <paramref name="type"/>, for the interfaces it does not inherit.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to emit the members for.</param>
    /// <param name="members">The members declared so far, including inherited ones.</param>
    private static void EmitClassMembers(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, ImplementableMemberSet members)
    {
        Dictionary<string, TypeDefinition> baseClosure = [];

        if (GetImplementableBaseType(context, type) is TypeDefinition baseType)
        {
            CollectInterfaceClosure(context, baseType, baseClosure);
        }

        HashSet<string> writtenInterfaces = [.. baseClosure.Keys];
        Dictionary<string, PropertyInfo> properties = [];

        WriteInterfaceMembersRecursive(writer, context, type, null, members, properties, writtenInterfaces);

        EmitMergedProperties(writer, properties, members);
    }

    /// <summary>
    /// Returns whether the members the bases of <paramref name="type"/> have to declare conflict with each other.
    /// </summary>
    /// <remarks>
    /// The result is cached, as this resolves every member of the class and its base classes.
    /// </remarks>
    private static bool HasMemberConflicts(ProjectionEmitContext context, TypeDefinition type)
    {
        return MemberConflictsCache.GetValue(type, type =>
        {
            using IndentedTextWriterOwner discardedWriterOwner = IndentedTextWriterPool.GetOrCreate();

            ImplementableMemberSet factoryMembers = new();

            EmitFactoryMembers(discardedWriterOwner.Writer, context, type, factoryMembers);

            if (factoryMembers.HasConflict)
            {
                return ConflictsFound;
            }

            if (type.IsStatic)
            {
                return NoConflictsFound;
            }

            ImplementableMemberSet members = new();

            SeedInheritedMembers(context, type, members);
            EmitClassMembers(discardedWriterOwner.Writer, context, type, members);

            return members.HasConflict ? ConflictsFound : NoConflictsFound;
        }) == ConflictsFound;
    }

    /// <summary>
    /// Emits <c>public abstract class &lt;Name&gt;Factory</c> implementing the class's <c>[Activatable]</c>,
    /// <c>[Static]</c> and <c>[Composable]</c> factory interfaces, so the factory (statics, factory methods and
    /// default activation) can be authored in C# by extending it.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to emit the factory base for.</param>
    private static void WriteImplementableFactoryClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        CollectFactoryInterfaces(context, type, out List<TypeDefinition> factoryInterfaces, out _, out _, out _, out bool hasDefaultActivation);

        if (factoryInterfaces.Count == 0 && !hasDefaultActivation)
        {
            return;
        }

        string nameStripped = type.GetStrippedName();

        // Inheritance list: 'IActivationFactory', then the factory/statics interfaces (and any of their base interfaces).
        // Every activation factory implements 'IActivationFactory', even for a class without default activation (e.g. a
        // static class), since it is the interface 'DllGetActivationFactory' hands out the factory as.
        List<string> bases = ["global::WindowsRuntime.InteropServices.IActivationFactory"];
        HashSet<string> declared = [];

        foreach (TypeDefinition iface in factoryInterfaces)
        {
            if (declared.Add(GetInterfaceKey(iface, null)))
            {
                bases.Add(TypedefNameWriter.WriteTypedefName(context, iface, TypedefNameType.CCW, false).Format());
                CollectImplementedInterfaces(context, iface, null, declared, bases);
            }
        }

        writer.WriteLine();

        string factoryName = GetFactoryClassName(nameStripped);

        writer.WriteLine($"public abstract class {factoryName} : {string.Join(", ", bases)}");

        using (writer.WriteBlock())
        {
            EmitFactoryMembers(writer, context, type, new ImplementableMemberSet());

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
                        return (nint)global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<global::WindowsRuntime.InteropServices.IActivationFactory>
                            .ConvertToUnmanaged(value, in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IActivationFactory)
                            .DetachThisPtrUnsafe();
                    }
                    """);
            }
        }
    }

    /// <summary>
    /// Emits the members of the activation factory base of <paramref name="type"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to emit the factory members for.</param>
    /// <param name="members">The member set to record the members into.</param>
    private static void EmitFactoryMembers(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, ImplementableMemberSet members)
    {
        CollectFactoryInterfaces(
            context,
            type,
            out List<TypeDefinition> factoryInterfaces,
            out HashSet<TypeDefinition> composableInterfaces,
            out HashSet<TypeDefinition> publicComposableInterfaces,
            out HashSet<TypeDefinition> activatableInterfaces,
            out bool hasDefaultActivation);

        bool hasComposableDefaultActivation = TryGetComposableDefaultActivation(
            composableInterfaces,
            publicComposableInterfaces,
            out MethodDefinition? parameterlessConstructor);

        string nameStripped = type.GetStrippedName();
        string projectedType = TypedefNameWriter.BuildGlobalQualifiedName(type.GetRawNamespace(), nameStripped);
        HashSet<string> writtenInterfaces = [];
        Dictionary<string, PropertyInfo> properties = [];

        // 'ActivateInstance' returns the implementation's base, so the compiler checks what it constructs, and
        // supplies the 'object' that 'IActivationFactory' expects. A parameterless composable constructor calls
        // it as well (see 'EmitComposableFactoryMembers').
        if (hasDefaultActivation || hasComposableDefaultActivation)
        {
            _ = members.ResolveMethod("ActivateInstance", "", nameStripped, isProtected: false);

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                object global::WindowsRuntime.InteropServices.IActivationFactory.ActivateInstance()
                {
                    return ActivateInstance();
                }

                public abstract {{nameStripped}} ActivateInstance();
                """);
        }
        else
        {
            // Without default activation there is nothing to construct, so this fails with 'E_NOTIMPL'
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, """
                object global::WindowsRuntime.InteropServices.IActivationFactory.ActivateInstance()
                {
                    throw new global::System.NotImplementedException();
                }
                """);
        }

        foreach (TypeDefinition iface in factoryInterfaces)
        {
            if (!writtenInterfaces.Add(GetInterfaceKey(iface, null)))
            {
                continue;
            }

            // A composable factory method carries the raw COM aggregation contract. That is infrastructure,
            // not something an author should implement, so it is generated here and forwards to a hook.
            if (composableInterfaces.Contains(iface))
            {
                EmitComposableFactoryMembers(writer, context, iface, nameStripped, projectedType, hasComposableDefaultActivation ? parameterlessConstructor : null, members);
            }
            else if (activatableInterfaces.Contains(iface))
            {
                EmitActivatableFactoryMembers(writer, context, iface, type, nameStripped, projectedType, members);
            }
            else
            {
                EmitInterfaceMembers(writer, context, iface, null, isProtectedSurface: false, members, properties, writtenInterfaces);
            }
        }

        EmitMergedProperties(writer, properties, members);
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> declares any factory (activation, statics or composition) surface.
    /// </summary>
    private static bool HasFactoryInterfaces(ProjectionEmitContext context, TypeDefinition type)
    {
        CollectFactoryInterfaces(context, type, out List<TypeDefinition> factoryInterfaces, out _, out _, out _, out bool hasDefaultActivation);

        return factoryInterfaces.Count > 0 || hasDefaultActivation;
    }

    /// <summary>
    /// Collects the <c>[Activatable]</c>, <c>[Static]</c> and <c>[Composable]</c> factory interfaces of a runtime class.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The runtime class to inspect.</param>
    /// <param name="factoryInterfaces">The collected factory interfaces.</param>
    /// <param name="composableInterfaces">The subset of <paramref name="factoryInterfaces"/> that are <c>[Composable]</c>.</param>
    /// <param name="publicComposableInterfaces">The subset of <paramref name="composableInterfaces"/> whose constructors are public.</param>
    /// <param name="activatableInterfaces">The subset of <paramref name="factoryInterfaces"/> that are <c>[Activatable]</c>, i.e. constructors of a sealed class.</param>
    /// <param name="hasDefaultActivation">Whether the class supports default (parameterless) activation.</param>
    private static void CollectFactoryInterfaces(
        ProjectionEmitContext context,
        TypeDefinition type,
        out List<TypeDefinition> factoryInterfaces,
        out HashSet<TypeDefinition> composableInterfaces,
        out HashSet<TypeDefinition> publicComposableInterfaces,
        out HashSet<TypeDefinition> activatableInterfaces,
        out bool hasDefaultActivation)
    {
        factoryInterfaces = [];
        composableInterfaces = [];
        publicComposableInterfaces = [];
        activatableInterfaces = [];
        hasDefaultActivation = false;

        foreach (KeyValuePair<string, AttributedType> entry in AttributedTypes.Get(type, context.Cache))
        {
            AttributedType attributedType = entry.Value;

            if (!attributedType.Activatable && !attributedType.Statics && !attributedType.Composable)
            {
                continue;
            }

            // A removed factory interface is not projected, so it is not part of the factory either
            if (attributedType.Type is { IsRemoved: true })
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

                if (attributedType.Activatable)
                {
                    _ = activatableInterfaces.Add(attributedType.Type);
                }

                if (attributedType.Composable)
                {
                    _ = composableInterfaces.Add(attributedType.Type);

                    if (attributedType.Visible)
                    {
                        _ = publicComposableInterfaces.Add(attributedType.Type);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determines whether an unsealed runtime class can also be given default (parameterless) activation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An unsealed class is only activatable through its composable factory, even for a parameterless constructor,
    /// and a class with no constructor at all still gets an empty one. A caller that activates by class name
    /// (<c>RoActivateInstance</c>) goes through <c>IActivationFactory</c> instead, so it cannot activate such a
    /// class at all. Implemented in C#, the class cannot be composed anyway (see <see cref="EmitComposableFactoryMembers"/>),
    /// so it is given the plain default activation those callers rely on.
    /// </para>
    /// <para>
    /// That only needs no constructor, or a public parameterless one to back it: a class whose constructors all
    /// take arguments, or are only visible to derived types, is not meant to be activated without them.
    /// </para>
    /// </remarks>
    /// <param name="composableInterfaces">The class's <c>[Composable]</c> factory interfaces.</param>
    /// <param name="publicComposableInterfaces">The subset of <paramref name="composableInterfaces"/> whose constructors are public.</param>
    /// <param name="parameterlessConstructor">The public parameterless composable constructor, if the class declares one.</param>
    /// <returns>Whether the class should be given default activation.</returns>
    private static bool TryGetComposableDefaultActivation(
        HashSet<TypeDefinition> composableInterfaces,
        HashSet<TypeDefinition> publicComposableInterfaces,
        out MethodDefinition? parameterlessConstructor)
    {
        parameterlessConstructor = null;

        if (composableInterfaces.Count == 0)
        {
            return false;
        }

        int constructorCount = 0;

        foreach (TypeDefinition iface in composableInterfaces)
        {
            foreach (MethodDefinition method in iface.GetNonSpecialMethods())
            {
                constructorCount++;

                // The trailing two parameters are always the outer and the inner, so these are all it takes
                if (parameterlessConstructor is null &&
                    publicComposableInterfaces.Contains(iface) &&
                    method.Signature?.ParameterTypes.Count == 2)
                {
                    parameterlessConstructor = method;
                }
            }
        }

        if (constructorCount > 0 && parameterlessConstructor is null)
        {
            return false;
        }

        return true;
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
    /// <para>
    /// The constructor backing default activation (if any) calls <c>ActivateInstance</c>, so both ways to activate
    /// the class without arguments share one implementation.
    /// </para>
    /// </remarks>
    private static void EmitComposableFactoryMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition ifaceType,
        string nameStripped,
        string projectedType,
        MethodDefinition? defaultActivationConstructor,
        ImplementableMemberSet members)
    {
        string interfaceName = TypedefNameWriter.WriteTypedefName(context, ifaceType, TypedefNameType.CCW, false).Format();

        foreach (MethodDefinition method in ifaceType.GetNonSpecialMethods())
        {
            // A removed constructor is not projected, so it is not part of the factory either
            if (method.IsRemoved)
            {
                continue;
            }

            string name = method.GetRawName();
            MethodSignatureInfo sig = new(method);

            // The trailing two parameters are always the outer and the inner (see the remarks above)
            if (sig.Parameters.Count < 2)
            {
                continue;
            }

            int userParameterCount = sig.Parameters.Count - 2;

            // Name them as the metadata does, rather than assuming the names MIDL happens to produce
            IndentedTextWriterCallback outerParameter = IdentifierEscaping.WriteEscapedIdentifier(sig.Parameters[^2].GetRawName());
            IndentedTextWriterCallback innerParameter = IdentifierEscaping.WriteEscapedIdentifier(sig.Parameters[^1].GetRawName());

            void WriteUserParameters(IndentedTextWriter writer)
            {
                for (int i = 0; i < userParameterCount; i++)
                {
                    IndentedTextWriterCallback parameter = MethodFactory.WriteProjectionParameter(context, sig.Parameters[i]);

                    writer.Write($"{(i > 0 ? ", " : "")}{parameter}");
                }
            }

            // Forwarded with their modifiers (e.g. 'out'), which the hook declares as well
            void WriteUserArguments(IndentedTextWriter writer)
            {
                for (int i = 0; i < userParameterCount; i++)
                {
                    IndentedTextWriterCallback argument = ClassMembersFactory.WriteParameterNameWithModifier(context, sig.Parameters[i]);

                    writer.Write($"{(i > 0 ? ", " : "")}{argument}");
                }
            }

            IndentedTextWriterCallback parameters = MethodFactory.WriteParameterList(context, sig);
            string displayName = projectedType.Replace("global::", "");
            bool isDefaultActivation = method == defaultActivationConstructor;
            string hookName = isDefaultActivation ? "ActivateInstance" : name;

            // The hook (already declared for default activation) may be shared with a constructor of another factory interface
            ImplementableMemberResolution resolution = isDefaultActivation
                ? ImplementableMemberResolution.Implemented
                : members.ResolveMethod(name, IndentedTextWriterCallbackFormat(WriteUserParameters), nameStripped, isProtected: true);

            if (resolution is ImplementableMemberResolution.Conflict ||
                !members.TryAddExplicitImplementation($"{interfaceName}.{name}({parameters.Format()})"))
            {
                continue;
            }

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                {{projectedType}} {{interfaceName}}.{{name}}({{parameters}})
                {
                    if ({{outerParameter}} is not null)
                    {
                        throw new global::System.NotSupportedException(
                            "Composing a Windows Runtime type implemented in C# is not supported. " +
                            "Only activating '{{displayName}}' directly is.");
                    }

                    {{nameStripped}} __instance = {{hookName}}({{WriteUserArguments}});

                    {{innerParameter}} = __instance;

                    return __instance;
                }
                """);

            if (resolution is ImplementableMemberResolution.Declare)
            {
                writer.WriteLine();
                writer.WriteLine($"protected abstract {nameStripped} {name}({WriteUserParameters});");
            }
        }
    }

    /// <summary>
    /// Formats the output of a writer callback as a string.
    /// </summary>
    private static string IndentedTextWriterCallbackFormat(IndentedTextWriterCallback callback)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();

        callback(writerOwner.Writer);

        return writerOwner.Writer.ToString();
    }

    /// <summary>
    /// Emits the members of an <c>[Activatable]</c> factory interface, i.e. the constructors of a sealed class.
    /// </summary>
    /// <remarks>
    /// Each constructor is declared returning the implementation's base rather than the projected class, so the
    /// compiler checks it constructs the implementation, as for composable constructors. The conversion to the
    /// projected class the interface returns is generated here.
    /// </remarks>
    private static void EmitActivatableFactoryMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        TypeDefinition ifaceType,
        TypeDefinition type,
        string nameStripped,
        string projectedType,
        ImplementableMemberSet members)
    {
        string interfaceName = TypedefNameWriter.WriteTypedefName(context, ifaceType, TypedefNameType.CCW, false).Format();

        foreach (MethodDefinition method in ifaceType.GetNonSpecialMethods())
        {
            // A removed constructor is not projected, so it is not part of the factory either
            if (method.IsRemoved)
            {
                continue;
            }

            string name = method.GetRawName();
            MethodSignatureInfo sig = new(method);
            string parameters = MethodFactory.WriteParameterList(context, sig).Format();

            // Constructors always return their class, but anything else is declared as the interface has it
            if (sig.ReturnType?.FullName != type.FullName)
            {
                string returnType = MethodFactory.WriteProjectionReturnType(context, sig).Format();

                if (members.ResolveMethod(name, parameters, returnType, isProtected: false) is ImplementableMemberResolution.Declare)
                {
                    writer.WriteLine();
                    writer.WriteLine($"public abstract {returnType} {name}({parameters});");
                }

                continue;
            }

            // The hook may be shared with a constructor of another factory interface
            ImplementableMemberResolution resolution = members.ResolveMethod(name, parameters, nameStripped, isProtected: false);

            if (resolution is ImplementableMemberResolution.Conflict ||
                !members.TryAddExplicitImplementation($"{interfaceName}.{name}({parameters})"))
            {
                continue;
            }

            IndentedTextWriterCallback arguments = MethodFactory.WriteCallArguments(context, sig, leadingComma: false);

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                {{projectedType}} {{interfaceName}}.{{name}}({{parameters}})
                {
                    return {{name}}({{arguments}});
                }
                """);

            if (resolution is ImplementableMemberResolution.Declare)
            {
                writer.WriteLine();
                writer.WriteLine($"public abstract {nameStripped} {name}({parameters});");
            }
        }
    }

    /// <summary>
    /// Emits the merged <c>abstract</c> properties collected during the interface walk.
    /// </summary>
    /// <remarks>
    /// A property can combine accessors required by several interfaces (e.g. a getter from one and a setter from another), so
    /// it is only resolved once all of them have been walked. An existing property (e.g. from a custom-mapped interface's .NET
    /// shape, or an inherited one) satisfies it as long as it has the same type and every accessor it requires.
    /// </remarks>
    /// <param name="writer">The writer.</param>
    /// <param name="properties">The properties collected during the walk.</param>
    /// <param name="members">The members declared so far.</param>
    private static void EmitMergedProperties(IndentedTextWriter writer, Dictionary<string, PropertyInfo> properties, ImplementableMemberSet members)
    {
        foreach ((string name, PropertyInfo info) in properties)
        {
            // The interfaces disagree on the type of the property, which C# cannot declare
            if (info.HasConflict)
            {
                members.MarkConflict();

                continue;
            }

            ImplementableMemberResolution resolution = members.ResolveProperty(name, info.TypeText, info.HasGetter, info.HasSetter, info.IsProtectedSurface);

            if (resolution is ImplementableMemberResolution.Declare)
            {
                string accessors = (info.HasGetter, info.HasSetter) switch
                {
                    (true, true) => "{ get; set; }",
                    (true, false) => "{ get; }",
                    _ => "{ set; }"
                };

                writer.WriteLine();
                writer.WriteLine($"{(info.IsProtectedSurface ? "protected" : "public")} abstract {info.TypeText} {name} {accessors}");
            }

            // A protected property cannot implement an interface property implicitly, so each interface gets an explicit
            // implementation forwarding to it, with the accessors that interface declares.
            bool isProtected =
                resolution is ImplementableMemberResolution.Forward ||
                (resolution is ImplementableMemberResolution.Declare && info.IsProtectedSurface);

            if (!isProtected)
            {
                continue;
            }

            foreach (PropertySource source in info.Sources)
            {
                if (!members.TryAddExplicitImplementation($"{source.InterfaceName}.{name}"))
                {
                    continue;
                }

                string forwarders = (source.HasGetter, source.HasSetter) switch
                {
                    (true, true) => $"get => {name}; set => {name} = value;",
                    (true, false) => $"get => {name};",
                    _ => $"set => {name} = value;"
                };

                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    {{info.TypeText}} {{source.InterfaceName}}.{{name}}
                    {
                        {{forwarders}}
                    }
                    """);
            }
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
        ImplementableMemberSet members,
        IDictionary<string, PropertyInfo> properties,
        HashSet<string> writtenInterfaces)
    {
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

            GenericInstanceTypeSignature? nextInstance = GetInterfaceInstance(impl, currentInstance);

            if (!writtenInterfaces.Add(GetInterfaceKey(ifaceType, nextInstance)))
            {
                continue;
            }

            // A custom-mapped interface is declared in its .NET form, so its members are declared from that
            // .NET shape rather than from the Windows Runtime metadata shape (which describes different
            // members entirely, and would satisfy nothing).
            if (IsCustomMappedInterface(ifaceType))
            {
                MappedInterfaceAbstractMemberFactory.WriteAbstractMembers(writer, context, nextInstance, ifaceType.Name ?? "", members);

                continue;
            }

            // An '[Overridable]' or '[Protected]' interface is part of a runtime class's derivation contract
            // rather than its public surface, so the projected class declares its members 'protected'. The
            // implementable base has to match, or an authored type would expose them publicly.
            bool isProtectedSurface = impl.IsOverridable() || impl.HasWindowsFoundationMetadataAttribute(References.WellKnownAttributeNames.ProtectedAttribute);

            EmitInterfaceMembers(writer, context, ifaceType, nextInstance, isProtectedSurface, members, properties, writtenInterfaces);
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
        ImplementableMemberSet members,
        IDictionary<string, PropertyInfo> properties,
        HashSet<string> writtenInterfaces)
    {
        GenericContext? memberContext = nextInstance is not null ? new GenericContext(nextInstance, null) : null;

        // Explicit interface implementations need the interface named as it is declared on the base
        string interfaceName = FormatInterfaceName(context, nextInstance?.ToTypeDefOrRef() ?? ifaceType);

        foreach (MethodDefinition method in ifaceType.GetNonSpecialMethods())
        {
            // A removed member is not part of the projected interface, so there is nothing to implement
            if (method.IsRemoved)
            {
                continue;
            }

            string name = method.GetRawName();
            MethodSignatureInfo sig = new(method, memberContext);
            string ret = MethodFactory.WriteProjectionReturnType(context, sig).Format();
            string parms = MethodFactory.WriteParameterList(context, sig).Format();

            ImplementableMemberResolution resolution = members.ResolveMethod(name, parms, ret, isProtectedSurface);

            if (resolution is ImplementableMemberResolution.Declare)
            {
                writer.WriteLine();
                writer.WriteLine($"{(isProtectedSurface ? "protected" : "public")} abstract {ret} {name}({parms});");
            }

            // A protected method cannot implement an interface method implicitly, so it needs an explicit implementation
            bool needsForwarder =
                resolution is ImplementableMemberResolution.Forward ||
                (resolution is ImplementableMemberResolution.Declare && isProtectedSurface);

            if (needsForwarder && members.TryAddExplicitImplementation($"{interfaceName}.{ImplementableMemberSet.GetMethodKey(name, parms)}"))
            {
                IndentedTextWriterCallback args = MethodFactory.WriteCallArguments(context, sig, leadingComma: false);

                writer.WriteLine();
                writer.WriteLine($"{ret} {interfaceName}.{name}({parms}) => {name}({args});");
            }
        }

        // Properties (merged after the walk)
        foreach (PropertyDefinition prop in ifaceType.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetMethods();

            // MIDL places '[Deprecated]' on the accessor (the getter for read/write properties), not on the property
            if ((getter ?? setter) is { IsRemoved: true })
            {
                continue;
            }

            string name = prop.GetRawName();
            string typeText = InterfaceFactory.WritePropType(context, prop, memberContext);

            if (!properties.TryGetValue(name, out PropertyInfo? info))
            {
                info = new PropertyInfo
                {
                    TypeText = typeText,
                    IsProtectedSurface = isProtectedSurface
                };
                properties[name] = info;
            }

            info.HasConflict |= info.TypeText != typeText;
            info.HasGetter |= getter is not null;
            info.HasSetter |= setter is not null;
            info.Sources.Add(new PropertySource(interfaceName, getter is not null, setter is not null));
        }

        foreach (EventDefinition evt in ifaceType.Events)
        {
            // MIDL places '[Deprecated]' on the 'add' accessor, not on the event
            if (evt.AddMethod is { IsRemoved: true })
            {
                continue;
            }

            string name = evt.GetRawName();
            string eventType = TypedefNameWriter.WriteEventType(context, evt, nextInstance).Format();

            ImplementableMemberResolution resolution = members.ResolveEvent(name, eventType, isProtectedSurface);

            if (resolution is ImplementableMemberResolution.Declare)
            {
                writer.WriteLine();
                writer.WriteLine($"{(isProtectedSurface ? "protected" : "public")} abstract event {eventType} {name};");
            }

            // A protected event cannot implement an interface event implicitly, so it needs an explicit implementation
            bool needsForwarder =
                resolution is ImplementableMemberResolution.Forward ||
                (resolution is ImplementableMemberResolution.Declare && isProtectedSurface);

            if (needsForwarder && members.TryAddExplicitImplementation($"{interfaceName}.{name}"))
            {
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    event {{eventType}} {{interfaceName}}.{{name}}
                    {
                        add => {{name}} += value;
                        remove => {{name}} -= value;
                    }
                    """);
            }
        }

        WriteInterfaceMembersRecursive(writer, context, ifaceType, nextInstance, members, properties, writtenInterfaces);
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
        HashSet<string> visited,
        List<string> collected)
    {
        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (impl.Interface is null || !impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceType))
            {
                continue;
            }

            // Declare the interface, constructed with its type arguments when it is generic. Custom-mapped
            // interfaces are declared in their .NET form (e.g. 'IMap<K, V>' as 'IDictionary<K, V>'), which is
            // what an author implements; the runtime's collection adapters service the Windows Runtime side.
            GenericInstanceTypeSignature? nextInstance = GetInterfaceInstance(impl, currentInstance);

            if (!visited.Add(GetInterfaceKey(ifaceType, nextInstance)))
            {
                continue;
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
    /// <remarks>
    /// Interfaces are keyed by their closed signature (see <see cref="GetInterfaceKey"/>), so different
    /// instantiations of the same generic interface are all included.
    /// </remarks>
    private static void CollectInterfaceClosure(ProjectionEmitContext context, TypeDefinition type, Dictionary<string, TypeDefinition> interfaces)
    {
        for (TypeDefinition? current = type; current is not null;)
        {
            CollectInterfaceClosure(context, current, null, interfaces);

            current = current.BaseType is { } baseRef ? baseRef.ResolveAsTypeDefinition(context.Cache) : null;
        }
    }

    /// <summary>
    /// Adds the interfaces implemented by <paramref name="declaringType"/> (transitively) into <paramref name="interfaces"/>.
    /// </summary>
    private static void CollectInterfaceClosure(
        ProjectionEmitContext context,
        TypeDefinition declaringType,
        GenericInstanceTypeSignature? currentInstance,
        Dictionary<string, TypeDefinition> interfaces)
    {
        foreach (InterfaceImplementation impl in declaringType.Interfaces)
        {
            if (!impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceType))
            {
                continue;
            }

            GenericInstanceTypeSignature? nextInstance = GetInterfaceInstance(impl, currentInstance);

            if (interfaces.TryAdd(GetInterfaceKey(ifaceType, nextInstance), ifaceType))
            {
                CollectInterfaceClosure(context, ifaceType, nextInstance, interfaces);
            }
        }
    }

    /// <summary>
    /// Gets the closed signature of a generic interface implementation, with the type arguments of the type declaring it substituted.
    /// </summary>
    /// <param name="impl">The interface implementation.</param>
    /// <param name="currentInstance">The instantiation of the type declaring <paramref name="impl"/>, if it is a generic interface.</param>
    /// <returns>The closed signature, or <see langword="null"/> if the interface is not generic.</returns>
    private static GenericInstanceTypeSignature? GetInterfaceInstance(InterfaceImplementation impl, GenericInstanceTypeSignature? currentInstance)
    {
        if (impl.Interface is null || !impl.Interface.TryGetGenericInstance(out GenericInstanceTypeSignature? instance))
        {
            return null;
        }

        return currentInstance is not null && instance.InstantiateGenericTypes(new GenericContext(currentInstance, null)) is GenericInstanceTypeSignature substitutedInstance
            ? substitutedInstance
            : instance;
    }

    /// <summary>
    /// Gets the key identifying an implemented interface: its closed signature if it is generic, so different
    /// instantiations of the same generic interface are told apart, and its name otherwise.
    /// </summary>
    private static string GetInterfaceKey(TypeDefinition ifaceType, GenericInstanceTypeSignature? instance)
    {
        return instance?.FullName ?? ifaceType.FullName;
    }

    /// <summary>
    /// Accumulated getter/setter presence and projected type text for a merged property.
    /// </summary>
    private sealed class PropertyInfo
    {
        public required string TypeText { get; init; }

        /// <summary>
        /// Whether the first interface declaring the property is <c>[Overridable]</c> or <c>[Protected]</c>, so the property is
        /// declared <c>protected abstract</c>, with explicit interface implementations forwarding to it.
        /// </summary>
        public bool IsProtectedSurface { get; init; }

        public bool HasGetter { get; set; }

        public bool HasSetter { get; set; }

        /// <summary>
        /// Whether the interfaces declaring the property disagree on its type.
        /// </summary>
        public bool HasConflict { get; set; }

        /// <summary>
        /// The interfaces declaring the property, with the accessors each one declares.
        /// </summary>
        public List<PropertySource> Sources { get; } = [];
    }

    /// <summary>
    /// An interface declaring a merged property, with the accessors it declares.
    /// </summary>
    /// <param name="InterfaceName">The interface, named as it is declared on the base.</param>
    /// <param name="HasGetter">Whether the interface declares a getter.</param>
    /// <param name="HasSetter">Whether the interface declares a setter.</param>
    private sealed record PropertySource(string InterfaceName, bool HasGetter, bool HasSetter);
}
