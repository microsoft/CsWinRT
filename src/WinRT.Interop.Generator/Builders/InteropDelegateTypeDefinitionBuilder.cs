// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for interop delegate type definitions.
/// </summary>
internal static class InteropDelegateTypeDefinitionBuilder
{
    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for an 'IDelegate' interface.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    public static void ImplType(
        TypeSignature delegateType,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        out FieldDefinition iidRvaField)
    {
        // We're declaring an 'internal static class' type
        implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "Impl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateVftbl> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, wellKnownInteropDefinitions.DelegateVftbl.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(vftblField);

        // Define the 'Invoke' methods as follows:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        // private static int Invoke(void* thisPtr, void* sender, void* e)
        MethodDefinition invokeMethod = new(
            name: "Invoke"u8,
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Int32,
                parameterTypes: [
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType()]))
        {
            CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(module) }
        };

        implType.Methods.Add(invokeMethod);

        // Create a method body for the 'Invoke' method
        CilInstructionCollection invokeInstructions = invokeMethod.CreateAndBindCilMethodBody().Instructions;

        _ = invokeInstructions.Add(CilOpCodes.Ldc_I4_0);
        _ = invokeInstructions.Add(CilOpCodes.Ret);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the delegate vtable
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Conv_U);
        _ = cctorInstructions.Add(CilOpCodes.Call, wellKnownInteropReferences.IUnknownImplget_Vtable.ImportWith(module.DefaultImporter));
        _ = cctorInstructions.Add(CilOpCodes.Ldobj, wellKnownInteropDefinitions.IUnknownVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Stobj, wellKnownInteropDefinitions.IUnknownVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Ldftn, invokeMethod);
        _ = cctorInstructions.Add(CilOpCodes.Stfld, wellKnownInteropDefinitions.DelegateVftbl.Fields[3]);
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // Create the field for the IID for the delegate type
        WellKnownMemberDefinitionFactory.IID(
            iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "IID"),
            iidRvaDataType: wellKnownInteropDefinitions.IIDRvaDataSize_16,
            module: module,
            iid: Guid.NewGuid(),
            out iidRvaField,
            out PropertyDefinition iidProperty,
            out MethodDefinition get_iidMethod);

        wellKnownInteropDefinitions.RvaFields.Fields.Add(iidRvaField);

        implType.Properties.Add(iidProperty);
        implType.Methods.Add(get_iidMethod);

        // Create the 'Vtable' property
        WellKnownMemberDefinitionFactory.Vtable(
            vftblField: vftblField,
            corLibTypeFactory: module.CorLibTypeFactory,
            out PropertyDefinition vtableProperty,
            out MethodDefinition get_VtableMethod);

        implType.Properties.Add(vtableProperty);
        implType.Methods.Add(get_VtableMethod);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for the 'IReference`1&lt;T&gt;' instantiation for some <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    public static void ReferenceImplType(
        TypeSignature delegateType,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        out FieldDefinition iidRvaField)
    {
        // We're declaring an 'internal static class' type
        implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceImpl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateReferenceVftbl> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, wellKnownInteropDefinitions.DelegateReferenceVftbl.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(vftblField);

        // Define the 'Value' methods as follows:
        //
        // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        // private static int Value(void* thisPtr, void** result)
        MethodDefinition valueMethod = new(
            name: "Value"u8,
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Int32,
                parameterTypes: [
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
        {
            CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(module) }
        };

        implType.Methods.Add(valueMethod);

        // Create a method body for the 'Value' method
        CilInstructionCollection invokeInstructions = valueMethod.CreateAndBindCilMethodBody().Instructions;

        _ = invokeInstructions.Add(CilOpCodes.Ldc_I4_0);
        _ = invokeInstructions.Add(CilOpCodes.Ret);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the delegate vtable
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Conv_U);
        _ = cctorInstructions.Add(CilOpCodes.Call, wellKnownInteropReferences.IInspectableImplget_Vtable.ImportWith(module.DefaultImporter));
        _ = cctorInstructions.Add(CilOpCodes.Ldobj, wellKnownInteropDefinitions.IInspectableVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Stobj, wellKnownInteropDefinitions.IInspectableVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Ldftn, valueMethod);
        _ = cctorInstructions.Add(CilOpCodes.Stfld, wellKnownInteropDefinitions.DelegateReferenceVftbl.Fields[6]);
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // Create the field for the IID for the boxed delegate type
        WellKnownMemberDefinitionFactory.IID(
            iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceIID"),
            iidRvaDataType: wellKnownInteropDefinitions.IIDRvaDataSize_16,
            module: module,
            iid: Guid.NewGuid(),
            out iidRvaField,
            out PropertyDefinition iidProperty,
            out MethodDefinition get_iidMethod);

        wellKnownInteropDefinitions.RvaFields.Fields.Add(iidRvaField);

        implType.Properties.Add(iidProperty);
        implType.Methods.Add(get_iidMethod);

        // Create the 'Vtable' property
        WellKnownMemberDefinitionFactory.Vtable(
            vftblField: vftblField,
            corLibTypeFactory: module.CorLibTypeFactory,
            out PropertyDefinition vtableProperty,
            out MethodDefinition get_VtableMethod);

        implType.Properties.Add(vtableProperty);
        implType.Methods.Add(get_VtableMethod);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for an 'IDelegate' interface.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ImplType"/>.</param>
    /// <param name="delegateReferenceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ReferenceImplType"/>.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    public static void InterfaceEntriesImplType(
        TypeSignature delegateType,
        TypeDefinition delegateImplType,
        TypeDefinition delegateReferenceImplType,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition implType)
    {
        // We're declaring an 'internal static class' type
        implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "InterfaceEntriesImpl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The interface entries field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateInterfaceEntries> Entries;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Entries"u8, FieldAttributes.Private, wellKnownInteropDefinitions.DelegateInterfaceEntries.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        InteropMethodBodyFactory.InterfaceEntriesImpl(
            cctor: implType.GetOrCreateStaticConstructor(module),
            entriesField: entriesField,
            entriesFieldType: wellKnownInteropDefinitions.DelegateInterfaceEntries,
            module: module,
            implTypes: [
                delegateImplType,
                delegateReferenceImplType,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IStringableImpl)!,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IWeakReferenceSourceImpl)!,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IMarshalImpl)!,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IAgileObjectImpl)!,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IInspectableImpl)!,
                module.MetadataResolver.ResolveType(wellKnownInteropReferences.IUnknownImpl)!]);

        // The 'Vtables' property type has the signature being 'ComWrappers.ComInterfaceEntry*'
        PointerTypeSignature vtablesPropertyType = module.DefaultImporter
            .ImportType(typeof(ComWrappers.ComInterfaceEntry))
            .MakePointerType();

        // The 'Vtables' property doesn't have a special signature
        PropertySignature vtablePropertySignature = new(CallingConventionAttributes.Property, vtablesPropertyType, []);

        // Create the 'Vtables' property
        PropertyDefinition vtablesProperty = new("Vtables"u8, PropertyAttributes.None, vtablePropertySignature)
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: new MethodSignature(
                    attributes: CallingConventionAttributes.Default,
                    returnType: vtablesPropertyType,
                    parameterTypes: []))
            { IsAggressiveInlining = true }
        };

        implType.Properties.Add(vtablesProperty);
        implType.Methods.Add(vtablesProperty.GetMethod!);

        // Create a method body for the 'Vtables' property
        CilInstructionCollection get_VtablesInstructions = vtablesProperty.GetMethod!.CreateAndBindCilMethodBody().Instructions;

        // The 'get_Vtables' method directly returns the 'Entries' field address
        _ = get_VtablesInstructions.Add(CilOpCodes.Ldsflda, entriesField);
        _ = get_VtablesInstructions.Add(CilOpCodes.Conv_U);
        _ = get_VtablesInstructions.Add(CilOpCodes.Ret);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the <c>IComWrappersCallback</c> interface for some <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="delegateImplType">The type returned by <see cref="ImplType"/>.</param>
    /// <param name="nativeDelegateType">The type returned by <see cref="NativeDelegateType"/>.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="callbackType">The resulting callback type.</param>
    public static void ComWrappersCallbackType(
        TypeSignature delegateType,
        TypeDefinition delegateImplType,
        TypeDefinition nativeDelegateType,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition callbackType)
    {
        // We're declaring an 'internal abstract class' type
        callbackType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "ComWrappersCallback"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
        {
            Interfaces = { new InterfaceImplementation(wellKnownInteropReferences.IWindowsRuntimeComWrappersCallback.ImportWith(module.DefaultImporter)) }
        };

        module.TopLevelTypes.Add(callbackType);

        // Define the 'CreateObject' methods as follows:
        //
        // public static object CreateObject(void* value)
        MethodDefinition createObjectMethod = new(
            name: "CreateObject"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Object,
                parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

        callbackType.Methods.Add(createObjectMethod);

        // Mark the 'CreateObject' method as implementing the interface method
        callbackType.MethodImplementations.Add(new MethodImplementation(
            declaration: wellKnownInteropReferences.IWindowsRuntimeComWrappersCallbackCreateObject.ImportWith(module.DefaultImporter),
            body: createObjectMethod));

        // Create a method body for the 'CreateObject' method
        CilInstructionCollection createObjectInstructions = createObjectMethod.CreateAndBindCilMethodBody().Instructions;

        // Get the special delegate constructor taking the target and function pointer. We leverage this to create
        // a delegate instance that directly wraps our 'WindowsRuntimeObjectReference' object and 'Invoke' method.
        IMethodDefOrRef ctor = delegateType
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor", new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: delegateType,
                parameterTypes: [module.CorLibTypeFactory.Object, module.CorLibTypeFactory.IntPtr]))
            .ImportWith(module.DefaultImporter);

        _ = createObjectInstructions.Add(CilOpCodes.Ldarg_0);
        _ = createObjectInstructions.Add(CilOpCodes.Call, delegateImplType.GetMethod("get_IID"u8));
        _ = createObjectInstructions.Add(CilOpCodes.Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceCreateUnsafe.ImportWith(module.DefaultImporter));
        _ = createObjectInstructions.Add(CilOpCodes.Ldftn, nativeDelegateType.GetMethod("Invoke"u8));
        _ = createObjectInstructions.Add(CilOpCodes.Newobj, ctor);
        _ = createObjectInstructions.Add(CilOpCodes.Ret);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the native delegate for some <see cref="Delegate"/> type.
    /// </summary>
    /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="nativeDelegateType">The resulting callback type.</param>
    public static void NativeDelegateType(
        TypeSignature delegateType,
        ModuleDefinition module,
        out TypeDefinition nativeDelegateType)
    {
        // We're declaring an 'internal static class' type
        nativeDelegateType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
            name: InteropUtf8NameFactory.TypeName(delegateType, "NativeDelegate"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(nativeDelegateType);

        // Define the 'Invoke' methods as follows:
        //
        // public static void Invoke(WindowsRuntimeObjectReference objectReference, <PARAMETER#0> arg0, <PARAMETER#1> arg1)
        MethodDefinition invokeMethod = new(
            name: "Invoke"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: new MethodSignature(
                attributes: CallingConventionAttributes.Default,
                returnType: module.CorLibTypeFactory.Void,
                parameterTypes: [
                    module.CorLibTypeFactory.Object,
                    module.CorLibTypeFactory.Object]));

        nativeDelegateType.Methods.Add(invokeMethod);

        // Create a method body for the 'Invoke' method
        CilInstructionCollection invokeInstructions = invokeMethod.CreateAndBindCilMethodBody().Instructions;

        _ = invokeInstructions.Add(CilOpCodes.Ret);
    }
}
