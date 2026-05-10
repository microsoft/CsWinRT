// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class AbiMethodBodyFactory
{
    /// <summary>
    /// Emits the [UnsafeAccessor] declaration for the default interface IID inside a file-scoped
    /// ComWrappers class. Only emits if the default interface is a generic instantiation.
    /// behavior of inserting <c>write_unsafe_accessor_for_iid</c> at the top of the class body.
    /// </summary>
    internal static void EmitUnsafeAccessorForDefaultIfaceIfGeneric(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef? defaultIface)
    {
        if (defaultIface is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ObjRefNameGenerator.EmitUnsafeAccessorForIid(writer, context, gi);
        }
    }

    /// <summary>
    /// Emits the per-interface members (methods, properties, events) into an already-open Methods
    /// static class. Used both for the standalone case and for the fast-abi merged emission.
    /// </summary>
    internal static void EmitMethodsClassMembersFor(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, int startSlot, bool skipExclusiveEvents)
    {
        // Build a map from each MethodDefinition to its WinMD vtable slot.
        // In AsmResolver, type.Methods is iterated in MethodDef row order, so the position of each
        // method in type.Methods (relative to the first method of the type) gives us the same value.
        Dictionary<MethodDefinition, int> methodSlot = [];
        {
            int idx = 0;
            foreach (MethodDefinition m in type.Methods)
            {
                methodSlot[m] = idx + startSlot;
                idx++;
            }
        }

        // Emit non-special methods first (output order is unchanged from before; only the slot lookup changes).
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            string mname = method.Name?.Value ?? string.Empty;
            MethodSignatureInfo sig = new(method);

            writer.Write("""
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public static unsafe 
                """, isMultiline: true);
            MethodFactory.WriteProjectionReturnType(writer, context, sig);
            writer.Write($" {mname}(WindowsRuntimeObjectReference thisReference");
            if (sig.Params.Count > 0) { writer.Write(", "); }
            MethodFactory.WriteParameterList(writer, context, sig);
            writer.Write(")");

            // Emit the body if we can handle this case. Slot comes from the method's WinMD index.
            EmitAbiMethodBodyIfSimple(writer, context, sig, methodSlot[method], isNoExcept: method.IsNoExcept());
        }

        // Emit property accessors. Each getter / setter consumes one vtable slot — looked up from the underlying method.
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            string propType = InterfaceFactory.WritePropType(context, prop);
            (MethodDefinition? gMethod, MethodDefinition? sMethod) = (getter, setter);
            // accessors of the property (the attribute is on the property itself, not on the
            // individual accessors).
            bool propIsNoExcept = prop.IsNoExcept();
            if (gMethod is not null)
            {
                MethodSignatureInfo getSig = new(gMethod);
                writer.Write($$"""
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        public static unsafe {{propType}} {{pname}}(WindowsRuntimeObjectReference thisReference)
                    """, isMultiline: true);
                EmitAbiMethodBodyIfSimple(writer, context, getSig, methodSlot[gMethod], isNoExcept: propIsNoExcept);
            }
            if (sMethod is not null)
            {
                MethodSignatureInfo setSig = new(sMethod);
                writer.Write($$"""
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        public static unsafe void {{pname}}(WindowsRuntimeObjectReference thisReference, {{InterfaceFactory.WritePropType(context, prop, isSetProperty: true)}} value)
                    """, isMultiline: true);
                EmitAbiMethodBodyIfSimple(writer, context, setSig, methodSlot[sMethod], paramNameOverride: "value", isNoExcept: propIsNoExcept);
            }
        }

        // Emit event member methods (returns an event source, takes thisObject + thisReference).
        // Skip events on exclusive interfaces used by their class — they're inlined directly in
        // the RCW class.
        foreach (EventDefinition evt in type.Events)
        {
            if (skipExclusiveEvents) { continue; }
            string evtName = evt.Name?.Value ?? string.Empty;
            TypeSignature evtSig = evt.EventType!.ToTypeSignature(false);
            bool isGenericEvent = evtSig is GenericInstanceTypeSignature;

            // Use the add method's WinMD slot (events project as the add_X method's vmethod_index).
            (MethodDefinition? addMethod, MethodDefinition? _) = evt.GetEventMethods();
            int eventSlot = addMethod is not null && methodSlot.TryGetValue(addMethod, out int es) ? es : 0;

            // Build the projected event source type name. For non-generic delegate handlers, the
            // EventSource subclass lives in the ABI namespace alongside this Methods class, so
            // we need to use the ABI-qualified name. For generic handlers (Windows.Foundation.*EventHandler),
            // it's mapped to global::WindowsRuntime.InteropServices.EventHandlerEventSource<...>.
            string eventSourceProjectedFull;
            if (isGenericEvent)
            {
                IndentedTextWriter scratchEvSrcGeneric = new();
                TypedefNameWriter.WriteTypeName(scratchEvSrcGeneric, context, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, true);
                eventSourceProjectedFull = scratchEvSrcGeneric.ToString();
                if (!eventSourceProjectedFull.StartsWith(GlobalPrefix, StringComparison.Ordinal))
                {
                    eventSourceProjectedFull = GlobalPrefix + eventSourceProjectedFull;
                }
            }
            else
            {
                // Non-generic delegate handler: the EventSource lives in the same ABI namespace
                // as this Methods class, so we use just the short name
                string delegateName = string.Empty;
                if (evtSig is TypeDefOrRefSignature td)
                {
                    delegateName = td.Type?.Name?.Value ?? string.Empty;
                    delegateName = IdentifierEscaping.StripBackticks(delegateName);
                }
                eventSourceProjectedFull = delegateName + "EventSource";
            }
            string eventSourceInteropType = isGenericEvent
                ? InteropTypeNameWriter.EncodeInteropTypeName(evtSig, TypedefNameType.EventSource) + ", WinRT.Interop"
                : string.Empty;

            // Emit the per-event ConditionalWeakTable static field.
            writer.WriteLine();
            writer.Write($$"""
                    private static ConditionalWeakTable<object, {{eventSourceProjectedFull}}> _{{evtName}}
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            [MethodImpl(MethodImplOptions.NoInlining)]
                            static ConditionalWeakTable<object, {{eventSourceProjectedFull}}> MakeTable()
                            {
                                _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);
                
                                return global::System.Threading.Volatile.Read(in field);
                            }
                
                            return global::System.Threading.Volatile.Read(in field) ?? MakeTable();
                        }
                    }
                
                    public static {{eventSourceProjectedFull}} {{evtName}}(object thisObject, WindowsRuntimeObjectReference thisReference)
                    {
                """, isMultiline: true);
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                writer.Write($$"""
                            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
                            [return: UnsafeAccessorType("{{eventSourceInteropType}}")]
                            static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);
                    
                            return _{{evtName}}.GetOrAdd(
                                key: thisObject,
                                valueFactory: static (_, thisReference) => Unsafe.As<{{eventSourceProjectedFull}}>(ctor(thisReference, {{eventSlot.ToString(CultureInfo.InvariantCulture)}})),
                                factoryArgument: thisReference);
                    """, isMultiline: true);
            }
            else
            {
                // Non-generic delegate: directly construct.
                writer.Write($$"""
                            return _{{evtName}}.GetOrAdd(
                                key: thisObject,
                                valueFactory: static (_, thisReference) => new {{eventSourceProjectedFull}}(thisReference, {{eventSlot.ToString(CultureInfo.InvariantCulture)}}),
                                factoryArgument: thisReference);
                    """, isMultiline: true);
            }
            writer.WriteLine("    }");
        }
    }
}
