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

        writer.Write("\n");
        writer.Write(visibility);
        writer.Write(" static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("ReferenceImpl\n{\n");
        writer.WriteLine("    [FixedAddressValueType]");
        writer.Write("    private static readonly ReferenceVftbl Vftbl;\n\n");
        writer.Write("    static ");
        writer.Write(nameStripped);
        writer.Write("ReferenceImpl()\n    {\n");
        writer.WriteLine("        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;");
        writer.WriteLine("        Vftbl.get_Value = &get_Value;");
        writer.Write("    }\n\n");
        writer.Write("    public static nint Vtable\n    {\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => (nint)Unsafe.AsPointer(in Vftbl);\n    }\n\n");
        writer.WriteLine("    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]");
        bool isBlittableStructType = blittable && TypeCategorization.GetCategory(type) == TypeCategory.Struct;
        bool isNonBlittableStructType = !blittable && TypeCategorization.GetCategory(type) == TypeCategory.Struct;
        if ((blittable && TypeCategorization.GetCategory(type) != TypeCategory.Struct)
            || isBlittableStructType)
        {
            // For blittable types and blittable structs: direct memcpy via C# struct assignment.
            // Even bool/char fields work because their managed layout matches the WinRT ABI.
            writer.Write("    public static int get_Value(void* thisPtr, void* result)\n    {\n");
            writer.Write("        if (result is null)\n        {\n");
            writer.Write("            return unchecked((int)0x80004003);\n        }\n\n");
            writer.Write("        try\n        {\n");
            writer.Write("            var value = (");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(")(ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr));");
            writer.Write("            *(");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine("*)result = value;");
            writer.Write("            return 0;\n        }\n");
            writer.Write("        catch (Exception e)\n        {\n");
            writer.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);\n        }\n");
            writer.WriteLine("    }");
        }
        else if (isNonBlittableStructType)
        {
            // Non-blittable struct: marshal via <Name>Marshaller.ConvertToUnmanaged then write the
            // (ABI) struct value into the result pointer.
            writer.Write("    public static int get_Value(void* thisPtr, void* result)\n    {\n");
            writer.Write("        if (result is null)\n        {\n");
            writer.Write("            return unchecked((int)0x80004003);\n        }\n\n");
            writer.Write("        try\n        {\n");
            writer.Write("            ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" unboxedValue = (");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(")ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);");
            writer.Write("            ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(" value = ");
            writer.Write(nameStripped);
            writer.WriteLine("Marshaller.ConvertToUnmanaged(unboxedValue);");
            writer.Write("            *(");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.WriteLine("*)result = value;");
            writer.Write("            return 0;\n        }\n");
            writer.Write("        catch (Exception e)\n        {\n");
            writer.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);\n        }\n");
            writer.WriteLine("    }");
        }
        else if (TypeCategorization.GetCategory(type) is TypeCategory.Class or TypeCategory.Delegate)
        {
            // Non-blittable runtime class / delegate: marshal via <Name>Marshaller and detach.
            writer.Write("    public static int get_Value(void* thisPtr, void* result)\n    {\n");
            writer.Write("        if (result is null)\n        {\n");
            writer.Write("            return unchecked((int)0x80004003);\n        }\n\n");
            writer.Write("        try\n        {\n");
            writer.Write("            ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" unboxedValue = (");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(")ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);");
            writer.Write("            void* value = ");
            // Use the same-namespace short marshaller name (we're in the ABI namespace).
            writer.Write(nameStripped);
            writer.WriteLine("Marshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();");
            writer.WriteLine("            *(void**)result = value;");
            writer.Write("            return 0;\n        }\n");
            writer.Write("        catch (Exception e)\n        {\n");
            writer.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);\n        }\n");
            writer.WriteLine("    }");
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
        writer.Write("\n    public static ref readonly Guid IID\n    {\n");
        writer.WriteLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        writer.Write("        get => ref global::ABI.InterfaceIIDs.");
        IIDExpressionWriter.WriteIidReferenceGuidPropertyName(writer, context, type);
        writer.Write(";\n    }\n");
        writer.Write("}\n\n");
    }
}
