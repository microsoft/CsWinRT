// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="Delegate"/> types.
    /// </summary>
    public static class Delegate
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
            GenericInstanceTypeSignature delegateType,
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
                CustomAttributes = { new CustomAttribute(wellKnownInteropReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // Define the 'Invoke' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Invoke(void* thisPtr, void* sender, void* e)
            MethodDefinition invokeMethod = new(
                name: "Invoke"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(wellKnownInteropReferences, module) }
            };

            implType.Methods.Add(invokeMethod);

            // Labels for jumps
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction ldarg_o_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, wellKnownInteropReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'Invoke' method
            invokeMethod.CilMethodBody = new CilMethodBody(invokeMethod)
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
                Instructions =
                {
                    // '.try' code
                    { ldarg_o_tryStart },
                    { Call, wellKnownInteropReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(delegateType).Import(module) },
                    { Ldarg_1 },
                    { Call, wellKnownInteropReferences.WindowsRuntimeObjectMarshallerConvertToManaged.Import(module) },
                    { Ldarg_2 },
                    { Call, wellKnownInteropReferences.WindowsRuntimeObjectMarshallerConvertToManaged.Import(module) },
                    { Callvirt, wellKnownInteropReferences.DelegateInvoke(delegateType).Import(module) },
                    { Ldc_I4_0 },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // '.catch' code
                    { call_catchStartMarshalException },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [0]
                    { ldloc_0_returnHResult  },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_o_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = wellKnownInteropReferences.Exception.Import(module)
                    }
                }
            };

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the delegate vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, wellKnownInteropReferences.IUnknownImplget_Vtable.Import(module) },
                    { Ldobj, wellKnownInteropDefinitions.IUnknownVftbl },
                    { Stobj, wellKnownInteropDefinitions.IUnknownVftbl },
                    { Ldsflda, vftblField },
                    { Ldftn, invokeMethod },
                    { Stfld, wellKnownInteropDefinitions.DelegateVftbl.Fields[3] },
                    { Ret }
                }
            };

            // Create the field for the IID for the delegate type
            WellKnownMemberDefinitionFactory.IID(
                iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "IID"),
                iidRvaDataType: wellKnownInteropDefinitions.IIDRvaDataSize_16,
                wellKnownInteropReferences: wellKnownInteropReferences,
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
            GenericInstanceTypeSignature delegateType,
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
                CustomAttributes = { new CustomAttribute(wellKnownInteropReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // Define the 'get_Value' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Value(void* thisPtr, void** result)
            MethodDefinition valueMethod = new(
                name: "get_Value"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(wellKnownInteropReferences, module) }
            };

            implType.Methods.Add(valueMethod);

            // Reference the generated 'ConvertToUnmanaged' method
            MemberReference convertToUnmanagedMethod = module
                .CreateTypeReference(InteropUtf8NameFactory.TypeNamespace(delegateType), InteropUtf8NameFactory.TypeName(delegateType, "Marshaller"))
                .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateStatic(
                    returnType: wellKnownInteropReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
                    parameterTypes: [delegateType]))
                .Import(module);

            // Jump labels
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
            CilInstruction call_catchStartMarshalException = new(Call, wellKnownInteropReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);

            // Declare 2 local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' to use to marshal the delegate
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_referenceValue = new(wellKnownInteropReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));

            // Create a method body for the 'get_Value' method
            valueMethod.CilMethodBody = new CilMethodBody(valueMethod)
            {
                LocalVariables = { loc_0_hresult, loc_1_referenceValue },
                Instructions =
                {
                    // Return 'E_POINTER' if the argument is 'null'
                    { Ldarg_1 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { Ldc_I4, unchecked((int)0x80004003) },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_1_tryStart },
                    { Ldarg_0 },
                    { Call, wellKnownInteropReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(delegateType).Import(module) },
                    { Call, convertToUnmanagedMethod },
                    { Stloc_1 },
                    { Ldloca_S, loc_1_referenceValue },
                    { Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe.Import(module) },
                    { Stind_I },
                    { Ldc_I4_0 },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // 'catch' code
                    { call_catchStartMarshalException },
                    { Stloc_0 },
                    { Leave_S, ldloc_0_returnHResult.CreateLabel() },

                    // Return the 'HRESULT' from location [0]
                    { ldloc_0_returnHResult },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Exception,
                        TryStart = ldarg_1_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = wellKnownInteropReferences.Exception.Import(module)
                    }
                }
            };

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the delegate vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, wellKnownInteropReferences.IInspectableImplget_Vtable.Import(module) },
                    { Ldobj, wellKnownInteropDefinitions.IInspectableVftbl },
                    { Stobj, wellKnownInteropDefinitions.IInspectableVftbl },
                    { Ldsflda, vftblField },
                    { Ldftn, valueMethod },
                    { Stfld, wellKnownInteropDefinitions.DelegateReferenceVftbl.Fields[6] },
                    { Ret }
                }
            };

            // Create the field for the IID for the boxed delegate type
            WellKnownMemberDefinitionFactory.IID(
                iidRvaFieldName: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceIID"),
                iidRvaDataType: wellKnownInteropDefinitions.IIDRvaDataSize_16,
                wellKnownInteropReferences: wellKnownInteropReferences,
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
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateImplType,
            TypeDefinition delegateReferenceImplType,
            WellKnownInteropDefinitions wellKnownInteropDefinitions,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            InteropTypeDefinitionBuilder.InterfaceEntriesImplType(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "InterfaceEntriesImpl"),
                entriesFieldType: wellKnownInteropDefinitions.DelegateInterfaceEntries,
                wellKnownInteropReferences: wellKnownInteropReferences,
                module: module,
                implType: out implType,
                implTypes: [
                    (delegateImplType.GetMethod("get_IID"u8), delegateImplType.GetMethod("get_Vtable"u8)),
                    (delegateReferenceImplType.GetMethod("get_IID"u8), delegateReferenceImplType.GetMethod("get_Vtable"u8)),
                    (wellKnownInteropReferences.IPropertyValueImplget_IID, wellKnownInteropReferences.IPropertyValueImplget_OtherTypeVtable),
                    (wellKnownInteropReferences.IStringableImplget_IID, wellKnownInteropReferences.IStringableImplget_Vtable),
                    (wellKnownInteropReferences.IWeakReferenceSourceImplget_IID, wellKnownInteropReferences.IWeakReferenceSourceImplget_Vtable),
                    (wellKnownInteropReferences.IMarshalImplget_IID, wellKnownInteropReferences.IMarshalImplget_Vtable),
                    (wellKnownInteropReferences.IAgileObjectImplget_IID, wellKnownInteropReferences.IAgileObjectImplget_Vtable),
                    (wellKnownInteropReferences.IInspectableImplget_IID, wellKnownInteropReferences.IInspectableImplget_Vtable),
                    (wellKnownInteropReferences.IUnknownImplget_IID, wellKnownInteropReferences.IUnknownImplget_Vtable)]);
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
                Interfaces = { new InterfaceImplementation(wellKnownInteropReferences.IWindowsRuntimeComWrappersCallback.Import(module)) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'CreateObject' method as follows:
            //
            // public static object CreateObject(void* value)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            callbackType.Methods.Add(createObjectMethod);

            // Mark the 'CreateObject' method as implementing the interface method
            callbackType.MethodImplementations.Add(new MethodImplementation(
                declaration: wellKnownInteropReferences.IWindowsRuntimeComWrappersCallbackCreateObject.Import(module),
                body: createObjectMethod));

            // Create a method body for the 'CreateObject' method
            createObjectMethod.CilMethodBody = new CilMethodBody(createObjectMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, delegateImplType.GetMethod("get_IID"u8) },
                    { Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceCreateUnsafe.Import(module) },
                    { Ldftn, nativeDelegateType.GetMethod("Invoke"u8) },
                    { Newobj, wellKnownInteropReferences.Delegate_ctor(delegateType).Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the native delegate for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeDelegateType">The resulting callback type.</param>
        public static void NativeDelegateType(
            GenericInstanceTypeSignature delegateType,
            WellKnownInteropDefinitions wellKnownInteropDefinitions,
            WellKnownInteropReferences wellKnownInteropReferences,
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

            // Construct the 'Invoke' method on the delegate type, so we can get the constructed parameter types
            MethodSignature invokeSignature = delegateType.Resolve()!.GetMethod("Invoke"u8).Signature!.InstantiateGenericTypes(GenericContext.FromType(delegateType));

            // Define the 'Invoke' method as follows:
            //
            // public static void Invoke(WindowsRuntimeObjectReference objectReference, <PARAMETER#0> arg0, <PARAMETER#1> arg1)
            MethodDefinition invokeMethod = new(
                name: "Invoke"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        wellKnownInteropReferences.WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false).Import(module),
                        invokeSignature.ParameterTypes[0].Import(module),
                        invokeSignature.ParameterTypes[1].Import(module)]));

            nativeDelegateType.Methods.Add(invokeMethod);

            // Create a method body for the 'Invoke' method
            CilMethodBody invokeBody = invokeMethod.CreateAndBindCilMethodBody();
            CilInstructionCollection invokeInstructions = invokeBody.Instructions;

            // Import 'WindowsRuntimeObjectReferenceValue', compute it just once
            TypeSignature windowsRuntimeObjectReferenceValueType = wellKnownInteropReferences.WindowsRuntimeObjectReferenceValue
                .Import(module)
                .ToTypeSignature(isValueType: true);

            // Declare 3 variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'WindowsRuntimeObjectReferenceValue' (for 'senderValue')
            //   [2]: 'WindowsRuntimeObjectReferenceValue' (for 'eValue')
            //   [3]: 'void*' (for 'thisPtr')
            invokeBody.LocalVariables.Add(new CilLocalVariable(windowsRuntimeObjectReferenceValueType));
            invokeBody.LocalVariables.Add(new CilLocalVariable(windowsRuntimeObjectReferenceValueType));
            invokeBody.LocalVariables.Add(new CilLocalVariable(windowsRuntimeObjectReferenceValueType));
            invokeBody.LocalVariables.Add(new CilLocalVariable(module.CorLibTypeFactory.Void.MakePointerType()));

            CilInstruction ret = new(Ret);

            // Load the local [0]
            _ = invokeInstructions.Add(Ldarg_0);
            _ = invokeInstructions.Add(Callvirt, wellKnownInteropReferences.WindowsRuntimeObjectReferenceAsValue.Import(module));
            _ = invokeInstructions.Add(Stloc_0);

            // '.try' for local [0]
            CilInstruction try_0 = invokeInstructions.Add(Ldarg_1);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectMarshallerConvertToUnmanaged.Import(module));
            _ = invokeInstructions.Add(Stloc_1);

            // '.try' for local [1]
            CilInstruction try_1 = invokeInstructions.Add(Ldarg_2);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectMarshallerConvertToUnmanaged.Import(module));
            _ = invokeInstructions.Add(Stloc_2);

            // 'Invoke' call for the native delegate (and 'try' for local [2])
            CilInstruction try_2 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[0]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Stloc_3);
            _ = invokeInstructions.Add(Ldloc_3);
            _ = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[1]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[2]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Ldloc_3);
            _ = invokeInstructions.Add(Ldind_I);
            _ = invokeInstructions.Add(Ldfld, wellKnownInteropDefinitions.DelegateVftbl.Fields[3]);
            _ = invokeInstructions.Add(Calli, WellKnownTypeSignatureFactory.InvokeImpl(module.CorLibTypeFactory, wellKnownInteropReferences).Import(module).MakeStandAloneSignature());
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module));
            _ = invokeInstructions.Add(Leave_S, ret.CreateLabel());

            // 'finally' for local [2]
            CilInstruction finally_2 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[2]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
            _ = invokeInstructions.Add(Endfinally);

            // 'finally' for local [1]
            CilInstruction finally_1 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[1]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
            _ = invokeInstructions.Add(Endfinally);

            // 'finally' for local [0]
            CilInstruction finally_0 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[0]);
            _ = invokeInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
            _ = invokeInstructions.Add(Endfinally);

            invokeInstructions.Add(ret);

            // Setup 'try/finally' for local [0]
            invokeBody.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Finally,
                TryStart = try_0.CreateLabel(),
                TryEnd = finally_0.CreateLabel(),
                HandlerStart = finally_0.CreateLabel(),
                HandlerEnd = ret.CreateLabel()
            });

            // Setup 'try/finally' for local [1]
            invokeBody.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Finally,
                TryStart = try_1.CreateLabel(),
                TryEnd = finally_1.CreateLabel(),
                HandlerStart = finally_1.CreateLabel(),
                HandlerEnd = finally_0.CreateLabel()
            });

            // Setup 'try/finally' for local [2]
            invokeBody.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Finally,
                TryStart = try_2.CreateLabel(),
                TryEnd = finally_2.CreateLabel(),
                HandlerStart = finally_2.CreateLabel(),
                HandlerEnd = finally_1.CreateLabel()
            });
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateReferenceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ReferenceImplType"/>.</param>
        /// <param name="delegateInterfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImplType"/>.</param>
        /// <param name="delegateComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateReferenceImplType,
            TypeDefinition delegateInterfaceEntriesImplType,
            TypeDefinition delegateComWrappersCallbackType,
            WellKnownInteropDefinitions wellKnownInteropDefinitions,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: wellKnownInteropReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            marshallerType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, wellKnownInteropReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

            // The 'ComputeVtables' method returns the 'ComWrappers.ComInterfaceEntry*' type
            PointerTypeSignature computeVtablesReturnType = wellKnownInteropReferences.ComInterfaceEntry.Import(module).MakePointerType();

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: computeVtablesReturnType,
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                // The parameter is '[out]'
                ParameterDefinitions = { new ParameterDefinition(sequence: 1, name: null, attributes: ParameterAttributes.Out) }
            };

            marshallerType.Methods.Add(computeVtablesMethod);

            // Mark the 'ComputeVtables' method as overriding the base method
            marshallerType.MethodImplementations.Add(new MethodImplementation(
                declaration: wellKnownInteropReferences.WindowsRuntimeComWrappersMarshallerAttributeComputeVtables.Import(module),
                body: computeVtablesMethod));

            // Create a method body for the 'ComputeVtables' method
            CilInstructionCollection computeVtablesInstructions = computeVtablesMethod.CreateAndBindCilMethodBody().Instructions;

            _ = computeVtablesInstructions.Add(Ldarg_1);
            computeVtablesInstructions.Add(CilInstruction.CreateLdcI4(wellKnownInteropDefinitions.DelegateInterfaceEntries.Fields.Count));
            _ = computeVtablesInstructions.Add(Stind_I4);
            _ = computeVtablesInstructions.Add(Call, delegateInterfaceEntriesImplType.GetMethod("get_Vtables"u8));
            _ = computeVtablesInstructions.Add(Ret);

            // Define the 'CreateObject' method as follows:
            //
            // public static object CreateObject(void* value)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(createObjectMethod);

            // Mark the 'CreateObject' method as overriding the base method
            marshallerType.MethodImplementations.Add(new MethodImplementation(
                declaration: wellKnownInteropReferences.WindowsRuntimeComWrappersMarshallerAttributeCreateObject.Import(module),
                body: createObjectMethod));

            // Create a method body for the 'CreateObject' method
            CilInstructionCollection createObjectInstructions = createObjectMethod.CreateAndBindCilMethodBody().Instructions;

            // Import the 'UnboxToManaged<TCallback>' method for the delegate
            IMethodDescriptor windowsRuntimeDelegateMarshallerUnboxToManaged2Descriptor = wellKnownInteropReferences.WindowsRuntimeDelegateMarshallerUnboxToManaged2
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToTypeSignature(isValueType: false));

            _ = createObjectInstructions.Add(Ldarg_1);
            _ = createObjectInstructions.Add(Call, delegateReferenceImplType.GetMethod("get_IID"u8));
            _ = createObjectInstructions.Add(Call, windowsRuntimeDelegateMarshallerUnboxToManaged2Descriptor);
            _ = createObjectInstructions.Add(Ret);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ImplType"/>.</param>
        /// <param name="delegateReferenceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ReferenceImplType"/>.</param>
        /// <param name="delegateComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateImplType,
            TypeDefinition delegateReferenceImplType,
            TypeDefinition delegateComWrappersCallbackType,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Prepare the external types we need in the implemented methods
            TypeSignature delegateType2 = delegateType.Import(module);
            TypeSignature windowsRuntimeObjectReferenceValueType = wellKnownInteropReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: false);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [delegateType2]));

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Create a method body for the 'ConvertToUnmanaged' method
            CilInstructionCollection convertToUnmanagedMethodInstructions = convertToUnmanagedMethod.CreateAndBindCilMethodBody().Instructions;

            _ = convertToUnmanagedMethodInstructions.Add(Ldarg_0);
            _ = convertToUnmanagedMethodInstructions.Add(Call, delegateImplType.GetMethod("get_IID"u8));
            _ = convertToUnmanagedMethodInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeDelegateMarshallerConvertToUnmanaged.Import(module));
            _ = convertToUnmanagedMethodInstructions.Add(Ret);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <DELEGATE_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: delegateType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(convertToManagedMethod);

            // Construct a descriptor for 'WindowsRuntimeDelegateMarshaller.ConvertToManaged<<DELEGATE_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeDelegateMarshallerConvertToManagedDescriptor =
                wellKnownInteropReferences.WindowsRuntimeDelegateMarshallerConvertToManaged
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToTypeSignature(isValueType: false));

            // Create a method body for the 'ConvertToManaged' method
            CilInstructionCollection convertToManagedMethodInstructions = convertToManagedMethod.CreateAndBindCilMethodBody().Instructions;

            _ = convertToManagedMethodInstructions.Add(Ldarg_0);
            _ = convertToManagedMethodInstructions.Add(Call, windowsRuntimeDelegateMarshallerConvertToManagedDescriptor);
            _ = convertToManagedMethodInstructions.Add(Ret);

            // Define the 'BoxToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition boxToUnmanagedMethod = new(
                name: "BoxToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [delegateType2]));

            marshallerType.Methods.Add(boxToUnmanagedMethod);

            // Create a method body for the 'ConvertToUnmanaged' method
            CilInstructionCollection boxToUnmanagedMethodInstructions = boxToUnmanagedMethod.CreateAndBindCilMethodBody().Instructions;

            _ = boxToUnmanagedMethodInstructions.Add(Ldarg_0);
            _ = boxToUnmanagedMethodInstructions.Add(Call, delegateReferenceImplType.GetMethod("get_IID"u8));
            _ = boxToUnmanagedMethodInstructions.Add(Call, wellKnownInteropReferences.WindowsRuntimeDelegateMarshallerBoxToUnmanaged.Import(module));
            _ = boxToUnmanagedMethodInstructions.Add(Ret);

            // Define the 'UnboxToManaged' method as follows:
            //
            // public static <DELEGATE_TYPE> UnboxToManaged(void* value)
            MethodDefinition unboxToUnmanagedMethod = new(
                name: "UnboxToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateInstance(
                    returnType: delegateType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            marshallerType.Methods.Add(unboxToUnmanagedMethod);

            // Construct a descriptor for 'WindowsRuntimeDelegateMarshaller.UnboxToManaged<<DELEGATE_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeDelegateMarshallerUnboxToManagedDescriptor =
                wellKnownInteropReferences.WindowsRuntimeDelegateMarshallerUnboxToManaged
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToTypeSignature(isValueType: false));

            // Create a method body for the 'UnboxToManaged' method
            CilInstructionCollection unboxToUnmanagedMethodInstructions = unboxToUnmanagedMethod.CreateAndBindCilMethodBody().Instructions;

            _ = unboxToUnmanagedMethodInstructions.Add(Ldarg_0);
            _ = unboxToUnmanagedMethodInstructions.Add(Call, windowsRuntimeDelegateMarshallerUnboxToManagedDescriptor);
            _ = unboxToUnmanagedMethodInstructions.Add(Ret);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateComWrappersMarshallerAttributeType,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Get the constructor for '[WindowsRuntimeClassName]'
            MemberReference windowsRuntimeClassNameAttributeCtor = wellKnownInteropReferences.WindowsRuntimeClassNameAttribute
                .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [module.CorLibTypeFactory.String]))
                .Import(module);

            // Add the attribute with the name of the runtime class
            marshallerType.CustomAttributes.Add(new CustomAttribute(
                constructor: windowsRuntimeClassNameAttributeCtor,
                signature: new CustomAttributeSignature(new CustomAttributeArgument(
                    argumentType: module.CorLibTypeFactory.String,
                    value: delegateType.FullName))));

            // Add the generated marshaller attribute
            marshallerType.CustomAttributes.Add(new CustomAttribute(delegateComWrappersMarshallerAttributeType.GetConstructor()!));
        }
    }
}
