// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the full ABI surface for a projected runtime class type:
/// the marshaller stub, ComWrappers callback, and authoring-metadata wrapper.
/// </summary>
internal static class AbiClassFactory
{
    /// <summary>
    /// Emits the full ABI surface for a projected runtime class: marshaller stub, ComWrappers callback, and authoring-metadata wrapper.
    /// </summary>
    public static void WriteAbiClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Static classes don't get a *Marshaller (no instances).
        if (type.IsStatic)
        {
            return;
        }

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
    }

    /// <summary>
    /// Emits the simpler component-mode class marshaller.
    /// </summary>
    internal static void WriteComponentClassMarshaller(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string projectedType = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);

        ITypeDefOrRef? defaultIface = type.GetDefaultInterface();

        // If the default interface is a generic instantiation (e.g. IDictionary<K,V>), emit an
        // UnsafeAccessor extern declaration inside ConvertToUnmanaged that fetches the IID via
        // WinRT.Interop's InterfaceIIDs class (since the IID for a generic instantiation is computed
        // at runtime). The IID expression in the call then becomes '<accessor>(null)' instead of a
        // static InterfaceIIDs reference.
        if (defaultIface?.TryGetGenericInstance(out GenericInstanceTypeSignature? defaultGenericInst) is not true)
        {
            defaultGenericInst = null;
        }

        string defaultIfaceIid;

        // If the type is generic, we need to invoke the unsafe accessor (because the IID will be generated
        // in the 'WinRT.Interop.dll' assembly, whereas if not we can directly use the local IID property.
        if (defaultGenericInst is not null)
        {
            // Call the accessor: '<IID_<EscapedName>>(null)'
            string accessorName = ObjRefNameGenerator.BuildIidPropertyNameForGenericInterface(context, defaultGenericInst);
            defaultIfaceIid = accessorName + "(null)";
        }
        else
        {
            defaultIfaceIid = defaultIface is not null
                ? ObjRefNameGenerator.WriteIidExpression(context, defaultIface).Format()
                : "default(global::System.Guid)";
        }

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{projectedType}} value)
                {
            """);

        // Emit the '[UnsafeAccessor]' declaration if the type is a generic type.
        if (defaultGenericInst is not null)
        {
            UnsafeAccessorFactory.EmitIidAccessor(writer, context, defaultGenericInst);
        }

        writer.WriteLine(isMultiline: true, $$"""
                    return WindowsRuntimeInterfaceMarshaller<{{projectedType}}>.ConvertToUnmanaged(value, {{defaultIfaceIid}});
                }
            
                public static {{projectedType}} ConvertToManaged(void* value)
                {
                    return ({{projectedType}}) WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
                }
            }
            """);
    }

    /// <summary>
    /// Emits the metadata wrapper type <c>file static class &lt;Name&gt; {}</c> with the conditional
    /// set of attributes required for the type's category.
    /// </summary>
    internal static void WriteAuthoringMetadataType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string projectedType = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);
        string fullName = string.IsNullOrEmpty(typeNs) ? nameStripped : $"{typeNs}.{nameStripped}";
        TypeKind kind = TypeKindResolver.Resolve(type);

        // [WindowsRuntimeReferenceType(typeof(<projected>?))] for non-delegate, non-class types
        // (i.e. enums, structs, interfaces).
        if (kind is not (TypeKind.Delegate or TypeKind.Class))
        {
            writer.WriteLine($"[WindowsRuntimeReferenceType(typeof({projectedType}?))]");
        }

        // [ABI.<ns>.<name>ComWrappersMarshaller] for non-struct, non-class types
        // (delegates, enums, interfaces).
        if (kind is not (TypeKind.Struct or TypeKind.Class))
        {
            writer.WriteLine($"[ABI.{typeNs}.{nameStripped}ComWrappersMarshaller]");
        }

        // [WindowsRuntimeClassName("Windows.Foundation.IReference`1<<ns>.<name>>")] for non-class types.
        if (kind != TypeKind.Class)
        {
            writer.WriteLine($"[WindowsRuntimeClassName(\"Windows.Foundation.IReference`1<{fullName}>\")]");
        }

        writer.WriteLine(isMultiline: true, $$"""
            [WindowsRuntimeMetadataTypeName("{{fullName}}")]
            [WindowsRuntimeMappedType(typeof({{projectedType}}))]
            file static class {{nameStripped}};
            """);
    }

    /// <summary>
    /// Returns whether the ABI impl type for <paramref name="type"/> should be emitted in the current settings (component vs reference vs exclusive-to scope).
    /// </summary>
    public static bool EmitImplType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return true;
        }

        if (type.IsExclusiveTo && !context.Settings.PublicExclusiveTo)
        {
            // one interface impl on the exclusive_to class is marked [Overridable] and matches
            // this interface. Otherwise the Impl wouldn't be reachable as a CCW.
            TypeDefinition? exclusiveToType = AbiTypeHelpers.GetExclusiveToType(context.Cache, type);

            if (exclusiveToType is null)
            {
                return true;
            }

            bool hasOverridable = false;
            foreach (InterfaceImplementation impl in exclusiveToType.Interfaces)
            {
                if (!impl.TryResolveTypeDef(context.Cache, out TypeDefinition? ifaceTd))
                {
                    continue;
                }

                if (ifaceTd == type && impl.IsOverridable())
                {
                    hasOverridable = true;
                    break;
                }
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
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string fullProjected = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);

        // Get the IID expression for the default interface (used by CreateObject).
        ITypeDefOrRef? defaultIface = type.GetDefaultInterface();
        string defaultIfaceIid = defaultIface is not null
            ? ObjRefNameGenerator.WriteIidExpression(context, defaultIface).Format()
            : "default(global::System.Guid)";

        // Determine the marshalingType expression from the class's [MarshalingBehaviorAttribute].
        // The same value is used for both the marshaller attribute and the callback.
        string marshalingType = ConstructorFactory.GetMarshalingTypeName(type);

        bool isSealed = type.IsSealed;

        // For unsealed classes, the ConvertToUnmanaged path needs to know whether the default interface is
        // exclusive-to.
        TypeDefinition? defaultIfaceTd = defaultIface?.ResolveAsTypeDefinition(context.Cache);
        bool defaultIfaceIsExclusive = defaultIfaceTd is not null && defaultIfaceTd.IsExclusiveTo;

        // Public *Marshaller class
        writer.WriteLine(isMultiline: true, $$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{fullProjected}} value)
                {
            """);
        if (isSealed)
        {
            // For projected sealed runtime classes, the RCW type is always unwrappable.
            writer.WriteLine(isMultiline: true, """
                        if (value is not null)
                        {
                            return WindowsRuntimeComWrappersMarshal.UnwrapObjectReferenceUnsafe(value).AsValue();
                        }
                """);
        }
        else if (!defaultIfaceIsExclusive && defaultIface is not null)
        {
            string defIfaceTypeName = TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.Get(defaultIface.ToTypeSignature(false)), TypedefNameType.Projected, false).Format();
            writer.WriteLine(isMultiline: true, $$"""
                        if (value is IWindowsRuntimeInterface<{{defIfaceTypeName}}> windowsRuntimeInterface)
                        {
                            return windowsRuntimeInterface.GetInterface();
                        }
                """);
        }
        else
        {
            writer.WriteLine(isMultiline: true, """
                        if (value is not null)
                        {
                            return value.GetDefaultInterface();
                        }
                """);
        }
        writer.WriteLine(isMultiline: true, $$"""
                    return default;
                }

                public static {{fullProjected}} ConvertToManaged(void* value)
                {
                    return ({{fullProjected}}){{(isSealed ? "WindowsRuntimeObjectMarshaller" : "WindowsRuntimeUnsealedObjectMarshaller")}}.ConvertToManaged<{{nameStripped}}ComWrappersCallback>(value);
                }
            }

            file sealed unsafe class {{nameStripped}}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
            """);
        using (writer.WriteBlock())
        {
            AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
            writer.WriteLine(isMultiline: true, $$"""
                public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                {
                    WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
                        externalComObject: value,
                        iid: {{defaultIfaceIid}},
                        marshalingType: {{marshalingType}},
                        wrapperFlags: out wrapperFlags);

                    return new {{fullProjected}}(valueReference);
                }
                """);
        }

        writer.WriteLine();

        if (isSealed)
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeObjectComWrappersCallback
            writer.WriteLine(isMultiline: true, $$"""
                file sealed unsafe class {{nameStripped}}ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
                {
                """);
            AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);
            writer.WriteLine(isMultiline: true, $$"""
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
                """);
        }
        else
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeUnsealedObjectComWrappersCallback
            string nonProjectedRcn = $"{typeNs}.{nameStripped}";
            writer.WriteLine(isMultiline: true, $$"""
                file sealed unsafe class {{nameStripped}}ComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
                {
                """);
            AbiMethodBodyFactory.EmitUnsafeAccessorForDefaultIfaceIfGeneric(writer, context, defaultIface);

            // TryCreateObject (non-projected runtime class name match)
            writer.WriteLine(isMultiline: true, $$"""
                    public static unsafe bool TryCreateObject(
                        void* value,
                        ReadOnlySpan<char> runtimeClassName,
                        out object wrapperObject,
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
                """);
        }
    }

}
