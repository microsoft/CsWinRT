// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the per-instance event-source storage field and the ABI add/remove handlers for runtime events on projected interfaces.
/// </summary>
internal static class EventTableFactory
{
    /// <summary>
    /// Emits the per-event <c>ConditionalWeakTable&lt;TInterface, EventRegistrationTokenTable&lt;THandler&gt;&gt;</c>
    /// backing field property. Mirrors the table emission in C++ <c>write_event_abi_invoke</c>.
    /// The <paramref name="ifaceFullName"/> is the dispatch target type for the CCW (computed by
    /// the caller in EmitDoAbiBodyIfSimple) — for instance events on authored classes this is
    /// the runtime class type, NOT the ABI.Impl interface.
    /// </summary>
    internal static void EmitEventTableField(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        IndentedTextWriter __scratchEvtType = new();
        TypedefNameWriter.WriteEventType(__scratchEvtType, context, evt);
        string evtType = __scratchEvtType.ToString();

        writer.Write("""
            
            private static ConditionalWeakTable<
            """, isMultiline: true);
        writer.Write(ifaceFullName);
        writer.Write(", EventRegistrationTokenTable<");
        writer.Write(evtType);
        writer.Write(">> _");
        writer.Write(evName);
        writer.Write("""
            
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static ConditionalWeakTable<
            """, isMultiline: true);
        writer.Write(ifaceFullName);
        writer.Write(", EventRegistrationTokenTable<");
        writer.Write(evtType);
        writer.Write("""
            >> MakeTable()
                    {
                        _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);
            
                        return global::System.Threading.Volatile.Read(in field);
                    }
            
                    return global::System.Threading.Volatile.Read(in field) ?? MakeTable();
                }
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_add_&lt;EventName&gt;_N</c> method. Mirrors the corresponding
    /// branch in C++ <c>write_event_abi_invoke</c>.
    /// </summary>
    internal static void EmitDoAbiAddEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, MethodSig sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        // Handler is the (last) input parameter of the add method. The emitted parameter name in the
        // signature comes from WriteAbiParameterTypesPointer which uses the metadata name verbatim.
        string handlerRawName = sig.Params.Count > 0 ? (sig.Params[^1].Parameter.Name ?? "handler") : "handler";
        string handlerRef = CSharpKeywords.IsKeyword(handlerRawName) ? "@" + handlerRawName : handlerRawName;

        // The cookie/token return parameter takes the metadata return param name (matches truth).
        string cookieName = AbiTypeHelpers.GetReturnParamName(sig);

        AsmResolver.DotNet.Signatures.TypeSignature evtTypeSig = evt.EventType!.ToTypeSignature(false);
        bool isGeneric = evtTypeSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;

        writer.Write("""
            
            {
                *
            """, isMultiline: true);
        writer.Write(cookieName);
        writer.WriteLine(" = default;");
        writer.WriteLine($"    try\n    {{\n        var __this = ComInterfaceDispatch.GetInstance<{ifaceFullName}>((ComInterfaceDispatch*)thisPtr);");

        if (isGeneric)
        {
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(evtTypeSig, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, evtTypeSig, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.Write("""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                        static extern 
                """, isMultiline: true);
            writer.Write(projectedTypeName);
            writer.Write(" ConvertToManaged([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write($$"""
                ")] object _, void* value);
                        var __handler = ConvertToManaged(null, {{handlerRef}});
                """, isMultiline: true);
        }
        else
        {
            writer.Write("        var __handler = ");
            TypedefNameWriter.WriteTypeName(writer, context, TypeSemanticsFactory.Get(evtTypeSig), TypedefNameType.ABI, false);
            writer.WriteLine($"Marshaller.ConvertToManaged({handlerRef});");
        }

        writer.Write("        *");
        writer.Write(cookieName);
        writer.Write(" = _");
        writer.Write(evName);
        writer.Write("""
            .GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.
            """, isMultiline: true);
        writer.Write(evName);
        writer.Write("""
             += __handler;
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_remove_&lt;EventName&gt;_N</c> method. Mirrors the corresponding
    /// branch in C++ <c>write_event_abi_invoke</c>.
    /// </summary>
    internal static void EmitDoAbiRemoveEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, MethodSig sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        string tokenRawName = sig.Params.Count > 0 ? (sig.Params[^1].Parameter.Name ?? "token") : "token";
        string tokenRef = CSharpKeywords.IsKeyword(tokenRawName) ? "@" + tokenRawName : tokenRawName;

        writer.Write("""
            
            {
                try
                {
                    var __this = ComInterfaceDispatch.GetInstance<
            """, isMultiline: true);
        writer.Write(ifaceFullName);
        writer.Write("""
            >((ComInterfaceDispatch*)thisPtr);
                    if(__this is not null && _
            """, isMultiline: true);
        writer.Write(evName);
        writer.Write(".TryGetValue(__this, out var __table) && __table.RemoveEventHandler(");
        writer.Write(tokenRef);
        writer.Write("""
            , out var __handler))
                    {
                        __this.
            """, isMultiline: true);
        writer.Write(evName);
        writer.Write("""
             -= __handler;
                    }
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
            }
            """, isMultiline: true);
    }
}