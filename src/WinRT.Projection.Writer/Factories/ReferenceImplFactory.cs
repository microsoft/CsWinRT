// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the IReference&lt;T&gt; implementation class for a struct/enum/delegate type
/// (the boxed-value adapter that exposes the value through the WinRT IReference COM interface).
/// </summary>
internal static class ReferenceImplFactory
{
    /// <summary>Writes the IReference impl class for a struct/enum/delegate type.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type definition.</param>
    public static void Write(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        string visibility = context.Settings.Component ? "public" : "file";
        bool blittable = AbiTypeHelpers.IsTypeBlittable(context.Cache, type);

        writer.WriteLine("");
        writer.Write(visibility);
        writer.Write(" static unsafe class ");
        writer.Write(nameStripped);
        writer.WriteLine("ReferenceImpl");
        writer.WriteLine("{");
        writer.Write("""
                [FixedAddressValueType]
                private static readonly ReferenceVftbl Vftbl;
            
                static 
            """, isMultiline: true);
        writer.Write(nameStripped);
        writer.Write("""
            ReferenceImpl()
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
            """, isMultiline: true);
        bool isBlittableStructType = blittable && TypeCategorization.GetCategory(type) == TypeCategory.Struct;
        bool isNonBlittableStructType = !blittable && TypeCategorization.GetCategory(type) == TypeCategory.Struct;
        if ((blittable && TypeCategorization.GetCategory(type) != TypeCategory.Struct)
            || isBlittableStructType)
        {
            // For blittable types and blittable structs: direct memcpy via C# struct assignment.
            // Even bool/char fields work because their managed layout matches the WinRT ABI.
            writer.Write("""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }
                
                        try
                        {
                            var value = (
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write("""
                )(ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr));
                            *(
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write("""
                *)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                """, isMultiline: true);
        }
        else if (isNonBlittableStructType)
        {
            // Non-blittable struct: marshal via <Name>Marshaller.ConvertToUnmanaged then write the
            // (ABI) struct value into the result pointer.
            writer.Write("""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }
                
                        try
                        {
                            
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" unboxedValue = (");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write("""
                )ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);
                            
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(" value = ");
            writer.Write(nameStripped);
            writer.Write("""
                Marshaller.ConvertToUnmanaged(unboxedValue);
                            *(
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write("""
                *)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                """, isMultiline: true);
        }
        else if (TypeCategorization.GetCategory(type) is TypeCategory.Class or TypeCategory.Delegate)
        {
            // Non-blittable runtime class / delegate: marshal via <Name>Marshaller and detach.
            writer.Write("""
                    public static int get_Value(void* thisPtr, void* result)
                    {
                        if (result is null)
                        {
                            return unchecked((int)0x80004003);
                        }
                
                        try
                        {
                            
                """, isMultiline: true);
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" unboxedValue = (");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write("""
                )ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);
                            void* value = 
                """, isMultiline: true);
            // Use the same-namespace short marshaller name (we're in the ABI namespace).
            writer.Write(nameStripped);
            writer.Write("""
                Marshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();
                            *(void**)result = value;
                            return 0;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }
                    }
                """, isMultiline: true);
        }
        else
        {
            // Defensive: should be unreachable. WriteReferenceImpl is only called for enum/struct/delegate
            // types (WriteAbiEnum / WriteAbiStruct / WriteAbiDelegate dispatchers).
            throw new System.InvalidOperationException(
                $"WriteReferenceImpl: unsupported type category {TypeCategorization.GetCategory(type)} " +
                $"for type '{type.FullName}'. Expected enum/struct/delegate.");
        }
        // IID property: 'public static ref readonly Guid IID' pointing at the reference type's IID.
        writer.WriteLine("");
        writer.Write("""
                public static ref readonly Guid IID
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref global::ABI.InterfaceIIDs.
            """, isMultiline: true);
        IIDExpressionWriter.WriteIidReferenceGuidPropertyName(writer, context, type);
        writer.WriteLine(";");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine("");
    }
}