// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the full ABI surface for a projected interface type:
/// the marshaller stub, vtable, impl class, marshaller class, and ABI parameter-list helpers.
/// </summary>
internal static class AbiInterfaceFactory
{
    /// <summary>
    /// Emits the full ABI surface for a projected interface type: marshaller stub, vtable, impl class, marshaller class, and ABI parameter-list helpers.
    /// </summary>
    public static void WriteAbiInterface(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Generic interfaces are handled by interopgen
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        // Emit the per-interface marshaller stub.
        WriteInterfaceMarshallerStub(writer, context, type);

        // For internal projections, just the static ABI methods class is enough.
        if (TypeCategorization.IsProjectionInternal(type))
        {
            return;
        }

        WriteInterfaceVftbl(writer, context, type);
        WriteInterfaceImpl(writer, context, type);
        AbiInterfaceIDicFactory.WriteInterfaceIdicImpl(writer, context, type);
        WriteInterfaceMarshaller(writer, context, type);
    }

    /// <summary>
    /// Writes the ABI parameter types for a vtable function pointer signature.
    /// </summary>
    public static void WriteAbiParameterTypesPointer(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        WriteAbiParameterTypesPointer(writer, context, sig, includeParamNames: false);
    }

    /// <summary>
    /// Writes the ABI parameter types for a vtable function pointer signature, optionally
    /// including parameter names (for method declarations vs. function pointer type lists).
    /// </summary>
    public static void WriteAbiParameterTypesPointer(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, bool includeParamNames)
    {
        // void* thisPtr, then each param's ABI type, then return type pointer
        writer.Write("void*");

        if (includeParamNames)
        {
            writer.Write(" thisPtr");
        }

        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            writer.Write(", ");
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (p.Type is SzArrayTypeSignature)
            {
                // length pointer + value pointer.
                if (includeParamNames)
                {
                    writer.Write($"uint __{p.Parameter.Name ?? "param"}Size, void* ");
                    IdentifierEscaping.WriteEscapedIdentifier(writer, p.Parameter.Name ?? "param");
                }
                else
                {
                    writer.Write("uint, void*");
                }
            }
            else if (p.Type is ByReferenceTypeSignature br)
            {
                // Special case: 'out T[]' is a ReceiveArray ABI signature: (uint* size, T** data).
                if (br.BaseType is SzArrayTypeSignature brSz && cat == ParameterCategory.ReceiveArray)
                {
                    bool isRefElemBr = brSz.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(brSz.BaseType) || brSz.BaseType.IsObject() || brSz.BaseType.IsGenericInstance();

                    if (includeParamNames)
                    {
                        writer.Write($"uint* __{p.Parameter.Name ?? "param"}Size, ");

                        if (isRefElemBr)
                        {
                            writer.Write("void*** ");
                        }
                        else
                        {
                            AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(brSz.BaseType));
                            writer.Write("** ");
                        }

                        IdentifierEscaping.WriteEscapedIdentifier(writer, p.Parameter.Name ?? "param");
                    }
                    else
                    {
                        writer.Write("uint*, ");

                        if (isRefElemBr)
                        {
                            writer.Write("void***");
                        }
                        else
                        {
                            AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(brSz.BaseType));
                            writer.Write("**");
                        }
                    }
                }
                else
                {
                    AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(br.BaseType));
                    writer.Write("*");

                    if (includeParamNames)
                    {
                        writer.Write(" ");
                        IdentifierEscaping.WriteEscapedIdentifier(writer, p.Parameter.Name ?? "param");
                    }
                }
            }
            else
            {
                AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(p.Type));

                if (cat is ParameterCategory.Out or ParameterCategory.Ref)
                {
                    writer.Write("*");
                }

                if (includeParamNames)
                {
                    writer.Write(" ");
                    IdentifierEscaping.WriteEscapedIdentifier(writer, p.Parameter.Name ?? "param");
                }
            }
        }

        // Return parameter
        if (sig.ReturnType is not null)
        {
            writer.Write(", ");
            string retName = AbiTypeHelpers.GetReturnParamName(sig);
            string retSizeName = AbiTypeHelpers.GetReturnSizeParamName(sig);
            // Special handling for SzArray return types: WinRT projects them as a (uint*, T**) pair.
            if (sig.ReturnType is SzArrayTypeSignature retSz)
            {
                if (includeParamNames)
                {
                    writer.Write($"uint* {retSizeName}, ");
                    AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(retSz.BaseType));
                    writer.Write($"** {retName}");
                }
                else
                {
                    writer.Write("uint*, ");
                    AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(retSz.BaseType));
                    writer.Write("**");
                }
            }
            else
            {
                AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(sig.ReturnType));
                writer.Write("*");

                if (includeParamNames)
                {
                    writer.Write($" {retName}");
                }
            }
        }
    }

    /// <summary>
    /// Emits the per-interface vtable struct (<c>{Name}Vftbl</c>) with IUnknown/IInspectable function pointer fields followed by one field per interface method.
    /// </summary>
    public static void WriteInterfaceVftbl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (!AbiClassFactory.EmitImplType(writer, context, type))
        {
            return;
        }

        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct {{nameStripped}}Vftbl
            """);
        using (writer.WriteBlock())
        {
            writer.Write(isMultiline: true, """
                public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
                public delegate* unmanaged[MemberFunction]<void*, uint> Release;
                public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;
                public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;
                public delegate* unmanaged[MemberFunction]<void*, int*, int> GetTrustLevel;
                """);

            foreach (MethodDefinition method in type.Methods)
            {
                string vm = AbiTypeHelpers.GetVirtualMethodName(type, method);
                MethodSignatureInfo sig = new(method);
                writer.Write("public delegate* unmanaged[MemberFunction]<");
                WriteAbiParameterTypesPointer(writer, context, sig);
                writer.WriteLine($", int> {vm};");
            }
        }
    }

    /// <summary>
    /// Emits the ABI implementation for a runtime interface type (vtable struct, IUnknown/IInspectable entries, Methods class, and CCW Do_Abi handlers).
    /// </summary>
    public static void WriteInterfaceImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (!AbiClassFactory.EmitImplType(writer, context, type))
        {
            return;
        }

        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {nameStripped}Impl");
        using IndentedTextWriter.Block __implBlock = writer.WriteBlock();
        writer.Write(isMultiline: true, $$"""
            [FixedAddressValueType]
            private static readonly {{nameStripped}}Vftbl Vftbl;
            
            static {{nameStripped}}Impl()
            {
                *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;
            """);
        foreach (MethodDefinition method in type.Methods)
        {
            string vm = AbiTypeHelpers.GetVirtualMethodName(type, method);
            writer.WriteLine($"    Vftbl.{vm} = &Do_Abi_{vm};");
        }
        writer.Write(isMultiline: true, """
            }
            
            public static ref readonly Guid IID
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref 
            """);
        AbiTypeHelpers.WriteIidGuidReference(writer, context, type);
        writer.Write(isMultiline: true, """
            ;
            }
            
            public static nint Vtable
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (nint)Unsafe.AsPointer(in Vftbl);
            }
            """);
        writer.WriteLine();

        // Do_Abi_* implementations: emit real bodies for simple primitive cases,
        // throw null! for everything else (deferred — needs full per-parameter marshalling).
        // type (not the interface) since the authored class IS the implementation. This is what
        // 'write_method_abi_invoke' produces because 'method.Parent()' is treated through
        // 'does_abi_interface_implement_ccw_interface' for authoring scenarios.
        // EXCEPTION: static factory interfaces ([Static] attr on the class) and activation
        // factory interfaces ([Activatable(typeof(IFooFactory))]) are implemented by the
        // generated 'ABI.Impl.<NS>.<IFooStatic>'/<IFooFactory>' types, NOT by the user runtime
        // class. For those, the dispatch target must be 'global::ABI.Impl.<NS>.<InterfaceName>'.
        TypeDefinition? exclusiveToOwner = null;
        bool exclusiveIsFactoryOrStatic = false;

        if (context.Settings.Component)
        {
            MetadataCache cache = context.Cache;
            exclusiveToOwner = AbiTypeHelpers.GetExclusiveToType(cache, type);

            if (exclusiveToOwner is not null)
            {
                foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(exclusiveToOwner, cache))
                {
                    if (kv.Value.Type == type && (kv.Value.Statics || kv.Value.Activatable))
                    {
                        exclusiveIsFactoryOrStatic = true;
                        break;
                    }
                }
            }
        }

        string ifaceFullName;

        if (exclusiveToOwner is not null && !exclusiveIsFactoryOrStatic)
        {
            string ownerNs = exclusiveToOwner.Namespace?.Value ?? string.Empty;
            string ownerNm = IdentifierEscaping.StripBackticks(exclusiveToOwner.Name?.Value ?? string.Empty);
            ifaceFullName = string.IsNullOrEmpty(ownerNs)
                ? GlobalPrefix + ownerNm
                : GlobalPrefix + ownerNs + "." + ownerNm;
        }
        else if (exclusiveToOwner is not null && exclusiveIsFactoryOrStatic)
        {
            // Factory/static interfaces in authoring mode are implemented by the generated
            // 'global::ABI.Impl.<NS>.<InterfaceName>' type that the activation factory CCW exposes.
            string ifaceNs = type.Namespace?.Value ?? string.Empty;
            string ifaceNm = IdentifierEscaping.StripBackticks(type.Name?.Value ?? string.Empty);
            ifaceFullName = string.IsNullOrEmpty(ifaceNs)
                ? "global::ABI.Impl." + ifaceNm
                : "global::ABI.Impl." + ifaceNs + "." + ifaceNm;
        }
        else
        {
            ifaceFullName = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, true);

            if (!ifaceFullName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            {
                ifaceFullName = GlobalPrefix + ifaceFullName;
            }
        }

        // Build a map of event add/remove methods to their event so we can emit the table field
        // and the proper Do_Abi_add_*/Do_Abi_remove_* bodies.
        Dictionary<MethodDefinition, EventDefinition>? eventMap = AbiTypeHelpers.BuildEventMethodMap(type);

        // Build sets of property accessors and event accessors so the first loop below can
        // iterate "regular" methods (non-property, non-event) only. Do_Abi bodies are emitted in
        // this order: methods first, then properties (setter before getter), then events.
        HashSet<MethodDefinition> propertyAccessors = [];
        foreach (PropertyDefinition prop in type.Properties)
        {
            if (prop.GetMethod is MethodDefinition g)
            {
                _ = propertyAccessors.Add(g);
            }

            if (prop.SetMethod is MethodDefinition s)
            {
                _ = propertyAccessors.Add(s);
            }
        }

        // Local helper to emit a single Do_Abi method body for a given MethodDefinition.
        void EmitOneDoAbi(MethodDefinition method)
        {
            string vm = AbiTypeHelpers.GetVirtualMethodName(type, method);
            MethodSignatureInfo sig = new(method);
            string mname = method.Name?.Value ?? string.Empty;

            // If this method is an event add accessor, emit the per-event ConditionalWeakTable
            // before the Do_Abi method.
            if (eventMap is not null && eventMap.TryGetValue(method, out EventDefinition? evt) && evt.AddMethod == method)
            {
                EventTableFactory.EmitEventTableField(writer, context, evt, ifaceFullName);
            }

            writer.Write(isMultiline: true, $$"""
                [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
                private static unsafe int Do_Abi_{{vm}}(
                """);
            WriteAbiParameterTypesPointer(writer, context, sig, includeParamNames: true);
            writer.Write(")");

            if (eventMap is not null && eventMap.TryGetValue(method, out EventDefinition? evt2))
            {
                if (evt2.AddMethod == method)
                {
                    EventTableFactory.EmitDoAbiAddEvent(writer, context, evt2, sig, ifaceFullName);
                }
                else
                {
                    EventTableFactory.EmitDoAbiRemoveEvent(writer, context, evt2, sig, ifaceFullName);
                }
            }
            else
            {
                AbiMethodBodyFactory.EmitDoAbiBodyIfSimple(writer, context, sig, ifaceFullName, mname);
            }
        }

        // 1. Regular methods (non-property, non-event), in metadata order.
        foreach (MethodDefinition method in type.Methods)
        {
            if (propertyAccessors.Contains(method))
            {
                continue;
            }

            if (eventMap is not null && eventMap.ContainsKey(method))
            {
                continue;
            }

            EmitOneDoAbi(method);
        }

        // 2. Properties, in metadata order. Setter before getter per write_property_abi_invoke.
        foreach (PropertyDefinition prop in type.Properties)
        {
            if (prop.SetMethod is MethodDefinition s)
            {
                EmitOneDoAbi(s);
            }

            if (prop.GetMethod is MethodDefinition g)
            {
                EmitOneDoAbi(g);
            }
        }

        // 3. Events, in metadata order. Add then Remove (matches metadata order from BuildEventMethodMap).
        foreach (EventDefinition evt in type.Events)
        {
            if (evt.AddMethod is MethodDefinition a)
            {
                EmitOneDoAbi(a);
            }

            if (evt.RemoveMethod is MethodDefinition r)
            {
                EmitOneDoAbi(r);
            }
        }
    }

    /// <summary>
    /// Emits the per-interface marshaller class (<c>{Name}Marshaller</c>) with the boxing/unboxing helpers used by user code to marshal references across the ABI.
    /// </summary>
    public static void WriteInterfaceMarshaller(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (TypeCategorization.IsExclusiveTo(type))
        {
            return;
        }

        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            #nullable enable
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
            """);
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.Write(isMultiline: true, """
             value)
                {
                    return WindowsRuntimeInterfaceMarshaller<
            """);
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.Write(">.ConvertToUnmanaged(value, ");
        AbiTypeHelpers.WriteIidGuidReference(writer, context, type);
        writer.Write(isMultiline: true, """
            );
                }
            
                public static 
            """);
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.Write(isMultiline: true, """
            ? ConvertToManaged(void* value)
                {
                    return (
            """);
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.Write(isMultiline: true, """
            ?) WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
                }
            }
            #nullable disable
            """);
    }

    /// <summary>
    /// Writes a minimal interface 'Methods' static class with method body emission.
    /// blittable-primitive-return/no-args methods get real implementations; everything else
    /// remains as 'throw null!' stubs (deferred — needs full per-parameter marshalling).
    /// </summary>
    private static void WriteInterfaceMarshallerStub(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        // exclusive to a class (and not opted into PublicExclusiveTo) or if it's marked
        // [ProjectionInternal]; public otherwise.
        bool useInternal = (TypeCategorization.IsExclusiveTo(type) && !context.Settings.PublicExclusiveTo)
            || TypeCategorization.IsProjectionInternal(type);

        // Fast ABI: if this interface is a non-default exclusive-to interface of a fast-abi
        // class, skip emitting it entirely — its members are merged into the default
        // interface's Methods class
        if (ClassFactory.IsFastAbiOtherInterface(context.Cache, type))
        {
            return;
        }

        // If the interface is exclusive-to a class that's been excluded from the projection,
        // skip emitting the entire *Methods class — it would be dead code (the owning class
        // is manually projected in WinRT.Runtime, e.g. IColorHelperStatics for ColorHelper,
        // IColorsStatics for Colors, IFontWeightsStatics for FontWeights). the original code also
        // omits these because their owning class is not projected.
        if (TypeCategorization.IsExclusiveTo(type))
        {
            TypeDefinition? owningClass = AbiTypeHelpers.GetExclusiveToType(context.Cache, type);

            if (owningClass is not null && !context.Settings.Filter.Includes(owningClass))
            {
                return;
            }
        }

        // are inlined in the RCW class, so we skip emitting them in the Methods type.
        bool skipExclusiveEvents = false;

        if (TypeCategorization.IsExclusiveTo(type) && !context.Settings.PublicExclusiveTo)
        {
            TypeDefinition? classType = AbiTypeHelpers.GetExclusiveToType(context.Cache, type);

            if (classType is not null)
            {
                foreach (InterfaceImplementation impl in classType.Interfaces)
                {
                    TypeDefinition? implDef = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl.Interface!);

                    if (implDef is not null && implDef == type)
                    {
                        skipExclusiveEvents = true;
                        break;
                    }
                }
            }
        }

        // Fast ABI: if this interface is the default interface of a fast-abi class, the
        // generated Methods class must include the merged members of the default interface
        // PLUS each [ExclusiveTo] non-default interface in vtable order, with progressively
        // increasing slot indices.
        // For non-fast-abi interfaces, the segment list is just [(type, INSPECTABLE_METHOD_COUNT, skipExclusiveEvents)].
        const int InspectableMethodCount = 6;
        List<(TypeDefinition Iface, int StartSlot, bool SkipEvents)> segments = [];
        (TypeDefinition Class, TypeDefinition? Default, List<TypeDefinition> Others)? fastAbi = ClassFactory.GetFastAbiClassForInterface(context.Cache, type);
        bool isFastAbiDefault = fastAbi is not null && fastAbi.Value.Default is not null
            && AbiTypeHelpers.InterfacesEqualByName(fastAbi.Value.Default, type);

        if (isFastAbiDefault)
        {
            int slot = InspectableMethodCount;
            // Default interface: skip its events (they're inlined in the RCW class).
            segments.Add((type, slot, true));
            slot += AbiTypeHelpers.CountMethods(type) + AbiTypeHelpers.GetClassHierarchyIndex(context.Cache, fastAbi!.Value.Class);
            foreach (TypeDefinition other in fastAbi.Value.Others)
            {
                segments.Add((other, slot, false));
                slot += AbiTypeHelpers.CountMethods(other);
            }
        }
        else
        {
            segments.Add((type, InspectableMethodCount, skipExclusiveEvents));
        }

        // Skip emission if the entire merged class would be empty.
        bool hasAnyMember = false;
        foreach ((TypeDefinition seg, int _, bool segSkipEvents) in segments)
        {
            if (AbiTypeHelpers.HasEmittableMembers(seg, segSkipEvents))
            {
                hasAnyMember = true;
                break;
            }
        }

        if (!hasAnyMember)
        {
            return;
        }

        writer.Write(isMultiline: true, $$"""
            {{(useInternal ? "internal static class " : "public static class ")}}{{nameStripped}}Methods
            {
            """);

        foreach ((TypeDefinition iface, int startSlot, bool segSkipEvents) in segments)
        {
            AbiMethodBodyFactory.EmitMethodsClassMembersFor(writer, context, iface, startSlot, segSkipEvents);
        }

        writer.WriteLine("}");
    }

}
