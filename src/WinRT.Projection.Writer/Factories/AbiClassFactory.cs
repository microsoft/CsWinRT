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
        writer.WriteLine("#nullable enable");
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
        writer.WriteLine("#nullable disable");
    }

    /// <summary>
    /// Emits the simpler component-mode class marshaller.
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
            string accessorName = ObjRefNameGenerator.BuildIidPropertyNameForGenericInterface(context, defaultGenericInst);
            defaultIfaceIid = accessorName + "(null)";
        }
        else
        {
            if (defaultIface is not null)
            {
                IndentedTextWriter __scratchDefaultIid = new();
                ObjRefNameGenerator.WriteIidExpression(__scratchDefaultIid, context, defaultIface);
                defaultIfaceIid = __scratchDefaultIid.ToString();
            }
            else
            {
                defaultIfaceIid = "default(global::System.Guid)";
            }
        }

        writer.WriteLine("");
        writer.Write($$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{projectedType}} value)
                {
            """, isMultiline: true);
        if (defaultGenericInst is not null)
        {
            // Emit the UnsafeAccessor declaration (uses 'object?' since component-mode
            // marshallers run inside #nullable enable).
            IndentedTextWriter __scratchAccessor = new();
            ObjRefNameGenerator.EmitUnsafeAccessorForIid(__scratchAccessor, context, defaultGenericInst, isInNullableContext: true);
            string accessorBlock = __scratchAccessor.ToString();
            // Re-emit each line indented by 8 spaces.
            string[] accessorLines = accessorBlock.TrimEnd('\n').Split('\n');
            foreach (string accessorLine in accessorLines)
            {
                writer.WriteLine($"        {accessorLine}");
            }
        }
        writer.Write($$"""
                    return WindowsRuntimeInterfaceMarshaller<{{projectedType}}>.ConvertToUnmanaged(value, {{defaultIfaceIid}});
                }
            
                public static {{projectedType}}? ConvertToManaged(void* value)
                {
                    return ({{projectedType}}?) WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
                }
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits the metadata wrapper type <c>file static class &lt;Name&gt; {}</c> with the conditional
    /// set of attributes required for the type's category.
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
        if (category is not (TypeCategory.Delegate or TypeCategory.Class))
        {
            writer.WriteLine($"[WindowsRuntimeReferenceType(typeof({projectedType}?))]");
        }

        // [ABI.<ns>.<name>ComWrappersMarshaller] for non-struct, non-class types
        // (delegates, enums, interfaces).
        if (category is not (TypeCategory.Struct or TypeCategory.Class))
        {
            writer.WriteLine($"[ABI.{typeNs}.{nameStripped}ComWrappersMarshaller]");
        }

        // [WindowsRuntimeClassName("Windows.Foundation.IReference`1<<ns>.<name>>")] for non-class types.
        if (category != TypeCategory.Class)
        {
            writer.WriteLine($"[WindowsRuntimeClassName(\"Windows.Foundation.IReference`1<{fullName}>\")]");
        }

        writer.Write($$"""
            [WindowsRuntimeMetadataTypeName("{{fullName}}")]
            [WindowsRuntimeMappedType(typeof({{projectedType}}))]
            file static class {{nameStripped}} {}
            """, isMultiline: true);
    }

    public static bool EmitImplType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return true; }
        if (TypeCategorization.IsExclusiveTo(type) && !context.Settings.PublicExclusiveTo)
        {
            // one interface impl on the exclusive_to class is marked [Overridable] and matches
            // this interface. Otherwise the Impl wouldn't be reachable as a CCW.
            TypeDefinition? exclusiveToType = AbiTypeHelpers.GetExclusiveToType(context.Cache, type);
            if (exclusiveToType is null) { return true; }
            bool hasOverridable = false;
            foreach (InterfaceImplementation impl in exclusiveToType.Interfaces)
            {
                if (impl.Interface is null) { continue; }
                TypeDefinition? ifaceTd = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl.Interface);
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
            ObjRefNameGenerator.WriteIidExpression(__scratchIid, context, defaultIface);
            defaultIfaceIid = __scratchIid.ToString();
        }
        else
        {
            defaultIfaceIid = "default(global::System.Guid)";
        }

        // Determine the marshalingType expression from the class's [MarshalingBehaviorAttribute]
        //. This is used by both the marshaller attribute and the
        // callback (the C++ code uses the same value for both).
        string marshalingType = ConstructorFactory.GetMarshalingTypeName(type);

        bool isSealed = type.IsSealed;

        // For unsealed classes, the ConvertToUnmanaged path needs to know whether the default interface is
        // exclusive-to.
        TypeDefinition? defaultIfaceTd = defaultIface is null ? null : AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, defaultIface);
        bool defaultIfaceIsExclusive = defaultIfaceTd is not null && TypeCategorization.IsExclusiveTo(defaultIfaceTd);

        // Public *Marshaller class
        writer.Write($$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{fullProjected}} value)
                {
            """, isMultiline: true);
        if (isSealed)
        {
            // For projected sealed runtime classes, the RCW type is always unwrappable.
            writer.Write("""
                        if (value is not null)
                        {
                            return WindowsRuntimeComWrappersMarshal.UnwrapObjectReferenceUnsafe(value).AsValue();
                        }
                """, isMultiline: true);
        }
        else if (!defaultIfaceIsExclusive && defaultIface is not null)
        {
            IndentedTextWriter __scratchDefIfaceTypeName = new();
            TypedefNameWriter.WriteTypeName(__scratchDefIfaceTypeName, context, TypeSemanticsFactory.Get(defaultIface.ToTypeSignature(false)), TypedefNameType.Projected, false);
            string defIfaceTypeName = __scratchDefIfaceTypeName.ToString();
            writer.Write($$"""
                        if (value is IWindowsRuntimeInterface<{{defIfaceTypeName}}> windowsRuntimeInterface)
                        {
                            return windowsRuntimeInterface.GetInterface();
                        }
                """, isMultiline: true);
        }
        else
        {
            writer.Write("""
                        if (value is not null)
                        {
                            return value.GetDefaultInterface();
                        }
                """, isMultiline: true);
        }
        writer.Write($$"""
                    return default;
                }
            
                public static {{fullProjected}}? ConvertToManaged(void* value)
                {
                    return ({{fullProjected}}?){{(isSealed ? "WindowsRuntimeObjectMarshaller" : "WindowsRuntimeUnsealedObjectMarshaller")}}.ConvertToManaged<{{nameStripped}}ComWrappersCallback>(value);
                }
            }
            
            file sealed unsafe class {{nameStripped}}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
            {
            """, isMultiline: true);
        AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
        writer.Write($$"""
                public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                {
                    WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
                        externalComObject: value,
                        iid: {{defaultIfaceIid}},
                        marshalingType: {{marshalingType}},
                        wrapperFlags: out wrapperFlags);
            
                    return new {{fullProjected}}(valueReference);
                }
            }
            """, isMultiline: true);
        writer.WriteLine("");

        if (isSealed)
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeObjectComWrappersCallback
            writer.Write($$"""
                file sealed unsafe class {{nameStripped}}ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
                {
                """, isMultiline: true);
            AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
            writer.Write($$"""
                    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                    {
                        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                            externalComObject: value,
                            iid: {{defaultIfaceIid}},
                            marshalingType: {{marshalingType}},
                            wrapperFlags: out wrapperFlags);
                
                        return new {{fullProjected}}(valueReference);
                    }
                }
                """, isMultiline: true);
        }
        else
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeUnsealedObjectComWrappersCallback
            string nonProjectedRcn = $"{typeNs}.{nameStripped}";
            writer.Write($$"""
                file sealed unsafe class {{nameStripped}}ComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
                {
                """, isMultiline: true);
            AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);

            // TryCreateObject (non-projected runtime class name match)
            writer.Write($$"""
                    public static unsafe bool TryCreateObject(
                        void* value,
                        ReadOnlySpan<char> runtimeClassName,
                        [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? wrapperObject,
                        out CreatedWrapperFlags wrapperFlags)
                    {
                        if (runtimeClassName.SequenceEqual("{{nonProjectedRcn}}".AsSpan()))
                        {
                            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                                externalComObject: value,
                                iid: {{defaultIfaceIid}},
                                marshalingType: {{marshalingType}},
                                wrapperFlags: out wrapperFlags);
                
                            wrapperObject = new {{fullProjected}}(valueReference);
                            return true;
                        }
                
                        wrapperObject = null;
                        wrapperFlags = CreatedWrapperFlags.None;
                        return false;
                    }
                
                    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                    {
                        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                            externalComObject: value,
                            iid: {{defaultIfaceIid}},
                            marshalingType: {{marshalingType}},
                            wrapperFlags: out wrapperFlags);
                
                        return new {{fullProjected}}(valueReference);
                    }
                }
                """, isMultiline: true);
        }
    }

}