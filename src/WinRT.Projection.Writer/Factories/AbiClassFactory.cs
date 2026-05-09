// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the full ABI surface for a projected runtime class type:
/// the marshaller stub, ComWrappers callback, and authoring-metadata wrapper.
/// </summary>
internal static class AbiClassFactory
{
    public static void WriteAbiClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Static classes don't get a *Marshaller (no instances).
        if (TypeCategorization.IsStatic(type)) { return; }
        writer.Write("#nullable enable\n");
        if (context.Settings.Component)
        {
            WriteComponentClassMarshaller(writer, context, type);
            WriteAuthoringMetadataType(writer, context, type);
        }
        else
        {
            // Emit a ComWrappers marshaller class so the attribute reference resolves
            WriteClassMarshallerStub(writer, context, type);
        }
        writer.Write("#nullable disable\n");
    }

    /// <summary>
    /// Emits the simpler component-mode class marshaller. Mirrors C++
    /// <c>write_component_class_marshaller</c>.
    /// </summary>
    internal static void WriteComponentClassMarshaller(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = IdentifierEscaping.StripBackticks(type.Name?.Value ?? string.Empty);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string projectedType = $"global::{typeNs}.{nameStripped}";

        ITypeDefOrRef? defaultIface = type.GetDefaultInterface();
        // instantiation (e.g. IDictionary<K,V>), emit an UnsafeAccessor extern declaration
        // inside ConvertToUnmanaged that fetches the IID via WinRT.Interop's InterfaceIIDs class
        // (since the IID for a generic instantiation is computed at runtime). The IID expression
        // in the call then becomes '<accessor>(null)' instead of a static InterfaceIIDs reference.
        AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature? defaultGenericInst = null;
        if (defaultIface is AsmResolver.DotNet.TypeSpecification spec
            && spec.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
        {
            defaultGenericInst = gi;
        }

        string defaultIfaceIid;
        if (defaultGenericInst is not null)
        {
            // Call the accessor: '<IID_<EscapedName>>(null)'.
            string accessorName = CodeWriters.BuildIidPropertyNameForGenericInterface(context, defaultGenericInst);
            defaultIfaceIid = accessorName + "(null)";
        }
        else
        {
            if (defaultIface is not null)
            {
                IndentedTextWriter __scratchDefaultIid = new();
                CodeWriters.WriteIidExpression(__scratchDefaultIid, context, defaultIface);
                defaultIfaceIid = __scratchDefaultIid.ToString();
            }
            else
            {
                defaultIfaceIid = "default(global::System.Guid)";
            }
        }

        writer.Write("\npublic static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("Marshaller\n{\n");
        writer.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        writer.Write(projectedType);
        writer.Write(" value)\n    {\n");
        if (defaultGenericInst is not null)
        {
            // Emit the UnsafeAccessor declaration (uses 'object?' since component-mode
            // marshallers run inside #nullable enable).
            IndentedTextWriter __scratchAccessor = new();
            CodeWriters.EmitUnsafeAccessorForIid(__scratchAccessor, context, defaultGenericInst, isInNullableContext: true);
            string accessorBlock = __scratchAccessor.ToString();
            // Re-emit each line indented by 8 spaces.
            string[] accessorLines = accessorBlock.TrimEnd('\n').Split('\n');
            foreach (string accessorLine in accessorLines)
            {
                writer.Write("        ");
                writer.Write(accessorLine);
                writer.Write("\n");
            }
        }
        writer.Write("        return WindowsRuntimeInterfaceMarshaller<");
        writer.Write(projectedType);
        writer.Write(">.ConvertToUnmanaged(value, ");
        writer.Write(defaultIfaceIid);
        writer.Write(");\n    }\n\n");
        writer.Write("    public static ");
        writer.Write(projectedType);
        writer.Write("? ConvertToManaged(void* value)\n    {\n");
        writer.Write("        return (");
        writer.Write(projectedType);
        writer.Write("?) WindowsRuntimeObjectMarshaller.ConvertToManaged(value);\n    }\n}\n");
    }

    /// <summary>
    /// Emits the metadata wrapper type <c>file static class &lt;Name&gt; {}</c> with the conditional
    /// set of attributes required for the type's category. Mirrors C++
    /// <c>write_authoring_metadata_type</c>.
    /// </summary>
    internal static void WriteAuthoringMetadataType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = IdentifierEscaping.StripBackticks(type.Name?.Value ?? string.Empty);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string projectedType = string.IsNullOrEmpty(typeNs) ? $"global::{nameStripped}" : $"global::{typeNs}.{nameStripped}";
        string fullName = string.IsNullOrEmpty(typeNs) ? nameStripped : $"{typeNs}.{nameStripped}";
        TypeCategory category = TypeCategorization.GetCategory(type);

        // [WindowsRuntimeReferenceType(typeof(<projected>?))] for non-delegate, non-class types
        // (i.e. enums, structs, interfaces).
        if (category != TypeCategory.Delegate && category != TypeCategory.Class)
        {
            writer.Write("[WindowsRuntimeReferenceType(typeof(");
            writer.Write(projectedType);
            writer.Write("?))]\n");
        }

        // [ABI.<ns>.<name>ComWrappersMarshaller] for non-struct, non-class types
        // (delegates, enums, interfaces).
        if (category != TypeCategory.Struct && category != TypeCategory.Class)
        {
            writer.Write("[ABI.");
            writer.Write(typeNs);
            writer.Write(".");
            writer.Write(nameStripped);
            writer.Write("ComWrappersMarshaller]\n");
        }

        // [WindowsRuntimeClassName("Windows.Foundation.IReference`1<<ns>.<name>>")] for non-class types.
        if (category != TypeCategory.Class)
        {
            writer.Write("[WindowsRuntimeClassName(\"Windows.Foundation.IReference`1<");
            writer.Write(fullName);
            writer.Write(">\")]\n");
        }

        writer.Write("[WindowsRuntimeMetadataTypeName(\"");
        writer.Write(fullName);
        writer.Write("\")]\n");
        writer.Write("[WindowsRuntimeMappedType(typeof(");
        writer.Write(projectedType);
        writer.Write("))]\n");
        writer.Write("file static class ");
        writer.Write(nameStripped);
        writer.Write(" {}\n");
    }

    public static bool EmitImplType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return true; }
        if (TypeCategorization.IsExclusiveTo(type) && !context.Settings.PublicExclusiveTo)
        {
            // one interface impl on the exclusive_to class is marked [Overridable] and matches
            // this interface. Otherwise the Impl wouldn't be reachable as a CCW.
            TypeDefinition? exclusiveToType = CodeWriters.GetExclusiveToType(context.Cache, type);
            if (exclusiveToType is null) { return true; }
            bool hasOverridable = false;
            foreach (InterfaceImplementation impl in exclusiveToType.Interfaces)
            {
                if (impl.Interface is null) { continue; }
                TypeDefinition? ifaceTd = CodeWriters.ResolveInterfaceTypeDef(context.Cache, impl.Interface);
                if (ifaceTd == type && impl.IsOverridable()) { hasOverridable = true; break; }
            }
            return hasOverridable;
        }
        return true;
    }

    /// <summary>
    /// Writes the marshaller infrastructure for a runtime class:
    /// * Public *Marshaller class with real ConvertToUnmanaged/ConvertToManaged bodies
    /// * file-scoped *ComWrappersMarshallerAttribute (CreateObject implementation)
    /// * file-scoped *ComWrappersCallback (IWindowsRuntimeObjectComWrappersCallback for sealed,
    ///   IWindowsRuntimeUnsealedObjectComWrappersCallback for unsealed)
    /// and <c>write_class_comwrappers_callback</c>.
    /// </summary>
    internal static void WriteClassMarshallerStub(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";

        // Get the IID expression for the default interface (used by CreateObject).
        ITypeDefOrRef? defaultIface = type.GetDefaultInterface();
        string defaultIfaceIid;
        if (defaultIface is not null)
        {
            IndentedTextWriter __scratchIid = new();
            CodeWriters.WriteIidExpression(__scratchIid, context, defaultIface);
            defaultIfaceIid = __scratchIid.ToString();
        }
        else
        {
            defaultIfaceIid = "default(global::System.Guid)";
        }

        // Determine the marshalingType expression from the class's [MarshalingBehaviorAttribute]
        // (mirrors C++ get_marshaling_type_name). This is used by both the marshaller attribute and the
        // callback (the C++ code uses the same value for both).
        string marshalingType = CodeWriters.GetMarshalingTypeName(type);

        bool isSealed = type.IsSealed;

        // For unsealed classes, the ConvertToUnmanaged path needs to know whether the default interface is
        // exclusive-to (mirrors C++ logic).
        TypeDefinition? defaultIfaceTd = defaultIface is null ? null : CodeWriters.ResolveInterfaceTypeDef(context.Cache, defaultIface);
        bool defaultIfaceIsExclusive = defaultIfaceTd is not null && TypeCategorization.IsExclusiveTo(defaultIfaceTd);

        // Public *Marshaller class
        writer.Write("public static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("Marshaller\n{\n");
        writer.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        writer.Write(fullProjected);
        writer.Write(" value)\n    {\n");
        if (isSealed)
        {
            // For projected sealed runtime classes, the RCW type is always unwrappable.
            writer.Write("        if (value is not null)\n        {\n");
            writer.Write("            return WindowsRuntimeComWrappersMarshal.UnwrapObjectReferenceUnsafe(value).AsValue();\n");
            writer.Write("        }\n");
        }
        else if (!defaultIfaceIsExclusive && defaultIface is not null)
        {
            IndentedTextWriter __scratchDefIfaceTypeName = new();
            CodeWriters.WriteTypeName(__scratchDefIfaceTypeName, context, TypeSemanticsFactory.Get(defaultIface.ToTypeSignature(false)), TypedefNameType.Projected, false);
            string defIfaceTypeName = __scratchDefIfaceTypeName.ToString();
            writer.Write("        if (value is IWindowsRuntimeInterface<");
            writer.Write(defIfaceTypeName);
            writer.Write("> windowsRuntimeInterface)\n        {\n");
            writer.Write("            return windowsRuntimeInterface.GetInterface();\n");
            writer.Write("        }\n");
        }
        else
        {
            writer.Write("        if (value is not null)\n        {\n");
            writer.Write("            return value.GetDefaultInterface();\n");
            writer.Write("        }\n");
        }
        writer.Write("        return default;\n    }\n\n");
        writer.Write("    public static ");
        writer.Write(fullProjected);
        writer.Write("? ConvertToManaged(void* value)\n    {\n");
        writer.Write("        return (");
        writer.Write(fullProjected);
        writer.Write("?)");
        writer.Write(isSealed ? "WindowsRuntimeObjectMarshaller" : "WindowsRuntimeUnsealedObjectMarshaller");
        writer.Write(".ConvertToManaged<");
        writer.Write(nameStripped);
        writer.Write("ComWrappersCallback>(value);\n    }\n}\n\n");

        // file-scoped *ComWrappersMarshallerAttribute - implements WindowsRuntimeComWrappersMarshallerAttribute.CreateObject
        writer.Write("file sealed unsafe class ");
        writer.Write(nameStripped);
        writer.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
        CodeWriters.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
        writer.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
        writer.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(\n");
        writer.Write("            externalComObject: value,\n");
        writer.Write("            iid: ");
        writer.Write(defaultIfaceIid);
        writer.Write(",\n");
        writer.Write("            marshalingType: ");
        writer.Write(marshalingType);
        writer.Write(",\n");
        writer.Write("            wrapperFlags: out wrapperFlags);\n\n");
        writer.Write("        return new ");
        writer.Write(fullProjected);
        writer.Write("(valueReference);\n    }\n}\n\n");

        if (isSealed)
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeObjectComWrappersCallback
            writer.Write("file sealed unsafe class ");
            writer.Write(nameStripped);
            writer.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
            CodeWriters.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
            writer.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            writer.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            writer.Write("            externalComObject: value,\n");
            writer.Write("            iid: ");
            writer.Write(defaultIfaceIid);
            writer.Write(",\n");
            writer.Write("            marshalingType: ");
            writer.Write(marshalingType);
            writer.Write(",\n");
            writer.Write("            wrapperFlags: out wrapperFlags);\n\n");
            writer.Write("        return new ");
            writer.Write(fullProjected);
            writer.Write("(valueReference);\n    }\n}\n");
        }
        else
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeUnsealedObjectComWrappersCallback
            string nonProjectedRcn = $"{typeNs}.{nameStripped}";
            writer.Write("file sealed unsafe class ");
            writer.Write(nameStripped);
            writer.Write("ComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback\n{\n");
            CodeWriters.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);

            // TryCreateObject (non-projected runtime class name match)
            writer.Write("    public static unsafe bool TryCreateObject(\n");
            writer.Write("        void* value,\n");
            writer.Write("        ReadOnlySpan<char> runtimeClassName,\n");
            writer.Write("        [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? wrapperObject,\n");
            writer.Write("        out CreatedWrapperFlags wrapperFlags)\n    {\n");
            writer.Write("        if (runtimeClassName.SequenceEqual(\"");
            writer.Write(nonProjectedRcn);
            writer.Write("\".AsSpan()))\n        {\n");
            writer.Write("            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            writer.Write("                externalComObject: value,\n");
            writer.Write("                iid: ");
            writer.Write(defaultIfaceIid);
            writer.Write(",\n");
            writer.Write("                marshalingType: ");
            writer.Write(marshalingType);
            writer.Write(",\n");
            writer.Write("                wrapperFlags: out wrapperFlags);\n\n");
            writer.Write("            wrapperObject = new ");
            writer.Write(fullProjected);
            writer.Write("(valueReference);\n            return true;\n        }\n\n");
            writer.Write("        wrapperObject = null;\n        wrapperFlags = CreatedWrapperFlags.None;\n        return false;\n    }\n\n");

            // CreateObject (fallback)
            writer.Write("    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            writer.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            writer.Write("            externalComObject: value,\n");
            writer.Write("            iid: ");
            writer.Write(defaultIfaceIid);
            writer.Write(",\n");
            writer.Write("            marshalingType: ");
            writer.Write(marshalingType);
            writer.Write(",\n");
            writer.Write("            wrapperFlags: out wrapperFlags);\n\n");
            writer.Write("        return new ");
            writer.Write(fullProjected);
            writer.Write("(valueReference);\n    }\n}\n");
        }
    }

}
