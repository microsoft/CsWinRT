// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
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
        if (defaultIface is not null && defaultIface.TryGetGenericInstance(out GenericInstanceTypeSignature? gi))
        {
            UnsafeAccessorFactory.EmitIidAccessor(writer, context, gi);
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
        int idx = 0;
        foreach (MethodDefinition m in type.Methods)
        {
            methodSlot[m] = idx + startSlot;
            idx++;
        }

        // Emit non-special methods first (output order is unchanged from before; only the slot lookup changes).
        foreach (MethodDefinition method in type.GetNonSpecialMethods())
        {
            string mname = method.GetRawName();
            MethodSignatureInfo sig = new(method);

            writer.Write(isMultiline: true, """
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public static unsafe 
                """);
            IndentedTextWriterCallback ret = MethodFactory.WriteProjectionReturnType(context, sig);
            IndentedTextWriterCallback parms = MethodFactory.WriteParameterList(context, sig);
            string comma = sig.Parameters.Count > 0 ? ", " : "";
            writer.Write($"{ret} {mname}(WindowsRuntimeObjectReference thisReference{comma}{parms})");

            // Emit the body if we can handle this case. Slot comes from the method's WinMD index.
            EmitAbiMethodBodyIfSimple(writer, context, sig, methodSlot[method], isNoExcept: method.IsNoExcept);
        }

        // Emit property accessors. Each getter / setter consumes one vtable slot — looked up from the underlying method.
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.GetRawName();
            string propType = InterfaceFactory.WritePropType(context, prop);

            if (prop.GetMethod is { } getter)
            {
                writer.Write(isMultiline: true, $$"""
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        public static unsafe {{propType}} {{pname}}(WindowsRuntimeObjectReference thisReference)
                    """);

                EmitAbiMethodBodyIfSimple(writer, context, new MethodSignatureInfo(getter), methodSlot[getter], isNoExcept: prop.IsNoExcept);
            }

            if (prop.SetMethod is { } setter)
            {
                writer.Write(isMultiline: true, $$"""
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        public static unsafe void {{pname}}(WindowsRuntimeObjectReference thisReference, {{InterfaceFactory.WritePropType(context, prop, isSetProperty: true)}} value)
                    """);

                EmitAbiMethodBodyIfSimple(writer, context, new MethodSignatureInfo(setter), methodSlot[setter], paramNameOverride: "value", isNoExcept: prop.IsNoExcept);
            }
        }

        // Emit event member methods (returns an event source, takes thisObject + thisReference).
        // Skip events on exclusive interfaces used by their class — they're inlined directly in
        // the RCW class.
        foreach (EventDefinition evt in type.Events)
        {
            if (skipExclusiveEvents)
            {
                continue;
            }

            string evtName = evt.GetRawName();
            TypeSignature evtSig = evt.EventType!.ToTypeSignature(false);
            bool isGenericEvent = evtSig is GenericInstanceTypeSignature;

            // Use the add method's WinMD slot (events project as the add_X method's vmethod_index).
            MethodDefinition? addMethod = evt.AddMethod;
            int eventSlot = addMethod is not null && methodSlot.TryGetValue(addMethod, out int es) ? es : 0;

            // Build the projected event source type name. For non-generic delegate handlers, the
            // EventSource subclass lives in the ABI namespace alongside this Methods class, so
            // we need to use the ABI-qualified name. For generic handlers (Windows.Foundation.*EventHandler),
            // it's mapped to global::WindowsRuntime.InteropServices.EventHandlerEventSource<...>.
            string eventSourceProjectedFull;

            if (isGenericEvent)
            {
                eventSourceProjectedFull = TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, true).Format();

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
                    delegateName = td.Type.GetRawName();
                    delegateName = IdentifierEscaping.StripBackticks(delegateName);
                }

                eventSourceProjectedFull = delegateName + "EventSource";
            }

            string eventSourceInteropType = isGenericEvent
                ? InteropTypeNameWriter.GetInteropAssemblyQualifiedName(evtSig, TypedefNameType.EventSource)
                : string.Empty;

            // Emit the per-event ConditionalWeakTable static field.
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
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
                """);
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                writer.IncreaseIndent();
                writer.IncreaseIndent();
                UnsafeAccessorFactory.EmitConstructorReturningObject(
                    writer,
                    interopType: eventSourceInteropType,
                    functionName: "ctor",
                    parameterList: "WindowsRuntimeObjectReference nativeObjectReference, int index");
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    return _{{evtName}}.GetOrAdd(
                        key: thisObject,
                        valueFactory: static (_, thisReference) => Unsafe.As<{{eventSourceProjectedFull}}>(ctor(thisReference, {{eventSlot.ToString(CultureInfo.InvariantCulture)}})),
                        factoryArgument: thisReference);
                    """);
                writer.DecreaseIndent();
                writer.DecreaseIndent();
            }
            else
            {
                // Non-generic delegate: directly construct.
                writer.WriteLine(isMultiline: true, $$"""
                            return _{{evtName}}.GetOrAdd(
                                key: thisObject,
                                valueFactory: static (_, thisReference) => new {{eventSourceProjectedFull}}(thisReference, {{eventSlot.ToString(CultureInfo.InvariantCulture)}}),
                                factoryArgument: thisReference);
                    """);
            }
            writer.WriteLine("    }");
        }
    }
}
