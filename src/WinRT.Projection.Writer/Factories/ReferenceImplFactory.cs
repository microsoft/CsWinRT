// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the IReference&lt;T&gt; implementation class for a struct/enum/delegate type
/// (the boxed-value adapter that exposes the value through the WinRT IReference COM interface).
/// </summary>
internal static class ReferenceImplFactory
{
    /// <summary>
    /// Writes the IReference impl class for a struct/enum/delegate type.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type definition.</param>
    public static void WriteReferenceImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        string visibility = context.Settings.Component ? "public" : "file";
        bool blittable = AbiTypeHelpers.IsTypeBlittable(context.Cache, type);
        bool isBlittableStructType = blittable && type.IsStruct;
        bool isNonBlittableStructType = !blittable && type.IsStruct;
        IndentedTextWriterCallback iidPropName = IidExpressionGenerator.WriteIidReferenceGuidPropertyName(context, type);

        // Emit the 'get_Value' method body. Branches by type category: blittable/blittable-struct
        // do a direct struct-assignment copy, non-blittable struct routes through the per-type
        // '*Marshaller.ConvertToUnmanaged', and runtime-class / delegate route through their
        // marshaller and detach the resulting WindowsRuntimeObjectReferenceValue. The 'else'
        // fallback is unreachable: only enum/struct/delegate dispatch into this helper from
        // 'WriteAbiEnum' / 'WriteAbiStruct' / 'WriteAbiDelegate'.
        void WriteGetValueBody(IndentedTextWriter writer)
        {
            if ((blittable && !type.IsStruct) || isBlittableStructType)
            {
                IndentedTextWriterCallback projected = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, true);

                writer.Write(isMultiline: true, $$"""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }
                    
                        try
                        {
                            var value = ({{projected}})(ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr));
                            *({{projected}}*)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                    """);
            }
            else if (isNonBlittableStructType)
            {
                IndentedTextWriterCallback projectedName = MethodFactory.WriteProjectedSignature(context, type.ToTypeSignature(), false);
                string abiName = AbiTypeHelpers.GetAbiStructTypeName(context, type.ToTypeSignature());

                writer.Write(isMultiline: true, $$"""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }

                        try
                        {
                            {{projectedName}} unboxedValue = ({{projectedName}})ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);
                            {{abiName}} value = {{nameStripped}}Marshaller.ConvertToUnmanaged(unboxedValue);
                            *({{abiName}}*)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                    """);
            }
            else if (TypeKindResolver.Resolve(type) is TypeKind.Class or TypeKind.Delegate)
            {
                IndentedTextWriterCallback projectedName = MethodFactory.WriteProjectedSignature(context, type.ToTypeSignature(), false);

                writer.Write(isMultiline: true, $$"""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }

                        try
                        {
                            {{projectedName}} unboxedValue = ({{projectedName}})ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);
                            void* value = {{nameStripped}}Marshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();
                            *(void**)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                    """);
            }
            else
            {
                // Defensive: should be unreachable. WriteReferenceImpl is only called for enum/struct/delegate
                // types (WriteAbiEnum / WriteAbiStruct / WriteAbiDelegate dispatchers).
                throw WellKnownProjectionWriterExceptions.UnreachableEmissionState(
                    $"WriteReferenceImpl: unsupported type category {TypeKindResolver.Resolve(type)} " +
                    $"for type '{type.FullName}'. Expected enum/struct/delegate.");
            }
        }

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            {{visibility}} static unsafe class {{nameStripped}}ReferenceImpl
            {
                [FixedAddressValueType]
                private static readonly ReferenceVftbl Vftbl;
            
                static {{nameStripped}}ReferenceImpl()
                {
                    *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;
                    Vftbl.get_Value = &get_Value;
                }
            
                public static nint Vtable
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => (nint)Unsafe.AsPointer(in Vftbl);
                }
            
                [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
                {{WriteGetValueBody}}
            
                public static ref readonly Guid IID
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref global::ABI.InterfaceIIDs.{{iidPropName}};
                }
            }
            """);
        writer.WriteLine();
    }
}
