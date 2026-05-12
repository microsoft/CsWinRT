// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the per-instance event-source storage field and the ABI add/remove handlers for runtime events on projected interfaces.
/// </summary>
internal static class EventTableFactory
{
    /// <summary>
    /// Emits the per-event <c>ConditionalWeakTable&lt;TInterface, EventRegistrationTokenTable&lt;THandler&gt;&gt;</c>
    /// backing field property. The <paramref name="ifaceFullName"/> is the dispatch target type
    /// for the CCW (computed by the caller in EmitDoAbiBodyIfSimple) — for instance events on
    /// authored classes this is the runtime class type, NOT the ABI.Impl interface.
    /// </summary>
    internal static void EmitEventTableField(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        string evtType = TypedefNameWriter.WriteEventType(context, evt);

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            private static ConditionalWeakTable<{{ifaceFullName}}, EventRegistrationTokenTable<{{evtType}}>> _{{evName}}
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static ConditionalWeakTable<{{ifaceFullName}}, EventRegistrationTokenTable<{{evtType}}>> MakeTable()
                    {
                        _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);
            
                        return global::System.Threading.Volatile.Read(in field);
                    }
            
                    return global::System.Threading.Volatile.Read(in field) ?? MakeTable();
                }
            }
            """);
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_add_&lt;EventName&gt;_N</c> method.
    /// </summary>
    internal static void EmitDoAbiAddEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, MethodSignatureInfo sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        // Handler is the (last) input parameter of the add method. The emitted parameter name in the
        // signature comes from WriteAbiParameterTypesPointer which uses the metadata name verbatim.
        string handlerRawName = sig.Parameters.Count > 0 ? (sig.Parameters[^1].Parameter.Name ?? "handler") : "handler";
        string handlerRef = CSharpKeywords.IsKeyword(handlerRawName) ? "@" + handlerRawName : handlerRawName;

        // The cookie/token return parameter takes the metadata return param name (matches truth).
        string cookieName = AbiTypeHelpers.GetReturnParamName(sig);

        TypeSignature evtTypeSig = evt.EventType!.ToTypeSignature(false);
        bool isGeneric = evtTypeSig is GenericInstanceTypeSignature;

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            {
                *{{cookieName}} = default;
                try
                {
                    var __this = ComInterfaceDispatch.GetInstance<{{ifaceFullName}}>((ComInterfaceDispatch*)thisPtr);
            """);

        if (isGeneric)
        {
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(evtTypeSig, TypedefNameType.ABI) + ", WinRT.Interop";
            string projectedTypeName = MethodFactory.WriteProjectedSignature(context, evtTypeSig, false);
            writer.Write(isMultiline: true, $$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                        static extern {{projectedTypeName}} ConvertToManaged([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                        var __handler = ConvertToManaged(null, {{handlerRef}});
                """);
        }
        else
        {
            writer.Write("        var __handler = ");
            TypedefNameWriter.WriteTypeName(writer, context, TypeSemanticsFactory.Get(evtTypeSig), TypedefNameType.ABI, false);
            writer.WriteLine($"Marshaller.ConvertToManaged({handlerRef});");
        }

        writer.Write(isMultiline: true, $$"""
                    *{{cookieName}} = _{{evName}}.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.{{evName}} += __handler;
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
            }
            """);
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_remove_&lt;EventName&gt;_N</c> method.
    /// </summary>
    internal static void EmitDoAbiRemoveEvent(IndentedTextWriter writer, ProjectionEmitContext context, EventDefinition evt, MethodSignatureInfo sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        string tokenRawName = sig.Parameters.Count > 0 ? (sig.Parameters[^1].Parameter.Name ?? "token") : "token";
        string tokenRef = CSharpKeywords.IsKeyword(tokenRawName) ? "@" + tokenRawName : tokenRawName;

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            {
                try
                {
                    var __this = ComInterfaceDispatch.GetInstance<{{ifaceFullName}}>((ComInterfaceDispatch*)thisPtr);
                    if (__this is not null && _{{evName}}.TryGetValue(__this, out var __table) && __table.RemoveEventHandler({{tokenRef}}, out var __handler))
                    {
                        __this.{{evName}} -= __handler;
                    }
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
            }
            """);
    }
}
