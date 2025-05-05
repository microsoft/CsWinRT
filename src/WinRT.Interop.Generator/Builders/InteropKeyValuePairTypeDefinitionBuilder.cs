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

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for interop <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type definitions.
/// </summary>
internal static class InteropKeyValuePairTypeDefinitionBuilder
{
    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> interface.
    /// </summary>
    /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
    public static void ImplType(
        GenericInstanceTypeSignature keyValuePairType,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        out FieldDefinition iidRvaField)
    {
        // We're declaring an 'internal static class' type
        implType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(keyValuePairType),
            name: InteropUtf8NameFactory.TypeName(keyValuePairType, "Impl"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <KeyValuePairVftbl> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, wellKnownInteropDefinitions.IKeyValuePairVftbl.ToTypeSignature(isValueType: true))
        {
            CustomAttributes = { InteropCustomAttributeFactory.FixedAddressValueType(module) }
        };

        implType.Fields.Add(vftblField);

        // Helper to define an accessor method
        static MethodDefinition GetKeyOrValuePropertyAccessorMethod(
            GenericInstanceTypeSignature keyValuePairType,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            string name)
        {
            TypeSignature typeArgument = keyValuePairType.TypeArguments[name == "get_Key" ? 0 : 1];

            // Define the method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int <METHOD_NAME>(void* thisPtr, <ABI_TYPE>* key)
            MethodDefinition method = new(
                name: name,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.DefaultImporter.ImportTypeSignature(typeArgument).MakePointerType()]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(module) }
            };

            // Create a method body for the method
            CilMethodBody methodBody = method.CreateAndBindCilMethodBody();
            CilInstructionCollection methodInstructions = methodBody.Instructions;

            // Declare 2 variable:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'KeyValuePair<,>' (the boxed object to get values from)
            methodBody.LocalVariables.Add(new CilLocalVariable(module.CorLibTypeFactory.Int32));
            methodBody.LocalVariables.Add(new CilLocalVariable(module.DefaultImporter.ImportTypeSignature(keyValuePairType)));

            CilInstruction nop = new(CilOpCodes.Nop);
            CilInstruction ldloc_0 = new(CilOpCodes.Ldloc_0);

            // Return 'E_POINTER' if the argument is 'null'
            _ = methodInstructions.Add(CilOpCodes.Ldarg_1);
            _ = methodInstructions.Add(CilOpCodes.Ldc_I4_0);
            _ = methodInstructions.Add(CilOpCodes.Conv_U);
            _ = methodInstructions.Add(CilOpCodes.Bne_Un_S, nop.CreateLabel());
            _ = methodInstructions.Add(CilOpCodes.Ldc_I4, unchecked((int)0x80004003));
            _ = methodInstructions.Add(CilOpCodes.Ret);
            methodInstructions.Add(nop);

            // Import 'ComWrappers.ComInterfaceDispatch.GetInstance'
            MethodSpecification getInstanceMethod = wellKnownInteropReferences.ComInterfaceDispatchGetInstance
                .MakeGenericInstanceMethod(module.CorLibTypeFactory.Object)
                .ImportWith(module.DefaultImporter);

            // Reference the 'KeyValuePair<,>' type
            ITypeDefOrRef keyValuePairTypeRef = module.DefaultImporter.ImportTypeSignature(keyValuePairType).ToTypeDefOrRef();

            // Reference the 'KeyValuePair<,>' accessor
            MemberReference get_MethodRef = keyValuePairTypeRef.CreateMemberReference(
                memberName: name,
                signature: MethodSignature.CreateInstance(module.DefaultImporter.ImportTypeSignature(typeArgument)));

            // '.try' code
            CilInstruction tryStart = methodInstructions.Add(CilOpCodes.Ldarg_1);
            _ = methodInstructions.Add(CilOpCodes.Ldarg_0);
            _ = methodInstructions.Add(CilOpCodes.Call, getInstanceMethod);
            _ = methodInstructions.Add(CilOpCodes.Unbox_Any, keyValuePairTypeRef);
            _ = methodInstructions.Add(CilOpCodes.Stloc_1);
            _ = methodInstructions.Add(CilOpCodes.Ldarg_1);
            _ = methodInstructions.Add(CilOpCodes.Ldloca_S, methodBody.LocalVariables[1]);
            _ = methodInstructions.Add(CilOpCodes.Call, get_MethodRef);
            _ = methodInstructions.Add(CilOpCodes.Stind_I);
            _ = methodInstructions.Add(CilOpCodes.Ldc_I4_0);
            _ = methodInstructions.Add(CilOpCodes.Stloc_0);
            _ = methodInstructions.Add(CilOpCodes.Leave_S, ldloc_0.CreateLabel());

            // 'catch' code
            CilInstruction catchStart = methodInstructions.Add(CilOpCodes.Ldarg_1);
            _ = methodInstructions.Add(CilOpCodes.Ldc_I4_0);
            _ = methodInstructions.Add(CilOpCodes.Conv_U);
            _ = methodInstructions.Add(CilOpCodes.Stind_I);
            _ = methodInstructions.Add(CilOpCodes.Call, wellKnownInteropReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.ImportWith(module.DefaultImporter));
            _ = methodInstructions.Add(CilOpCodes.Stloc_0);
            _ = methodInstructions.Add(CilOpCodes.Leave_S, ldloc_0.CreateLabel());

            // Return the 'HRESULT' from location [0]
            methodInstructions.Add(ldloc_0);
            _ = methodInstructions.Add(CilOpCodes.Ret);

            // Setup the exception handler
            methodBody.ExceptionHandlers.Add(new CilExceptionHandler
            {
                HandlerType = CilExceptionHandlerType.Exception,
                TryStart = tryStart.CreateLabel(),
                TryEnd = catchStart.CreateLabel(),
                HandlerStart = catchStart.CreateLabel(),
                HandlerEnd = ldloc_0.CreateLabel(),
                ExceptionType = module.CorLibTypeFactory.CorLibScope
                    .CreateTypeReference("System", "Exception")
                    .ImportWith(module.DefaultImporter)
            });

            return method;
        }

        // Define the accessor exports
        MethodDefinition get_KeyMethod = GetKeyOrValuePropertyAccessorMethod(keyValuePairType, wellKnownInteropReferences, module, "get_Key");
        MethodDefinition get_ValueMethod = GetKeyOrValuePropertyAccessorMethod(keyValuePairType, wellKnownInteropReferences, module, "get_Value");

        implType.Methods.Add(get_KeyMethod);
        implType.Methods.Add(get_ValueMethod);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CreateAndBindCilMethodBody().Instructions;

        // Initialize the 'KeyValuePair<,>' vtable
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Conv_U);
        _ = cctorInstructions.Add(CilOpCodes.Call, wellKnownInteropReferences.IInspectableImplget_Vtable.ImportWith(module.DefaultImporter));
        _ = cctorInstructions.Add(CilOpCodes.Ldobj, wellKnownInteropDefinitions.IInspectableVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Stobj, wellKnownInteropDefinitions.IInspectableVftbl);
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Ldftn, get_KeyMethod);
        _ = cctorInstructions.Add(CilOpCodes.Stfld, wellKnownInteropDefinitions.IKeyValuePairVftbl.Fields[6]);
        _ = cctorInstructions.Add(CilOpCodes.Ldsflda, vftblField);
        _ = cctorInstructions.Add(CilOpCodes.Ldftn, get_ValueMethod);
        _ = cctorInstructions.Add(CilOpCodes.Stfld, wellKnownInteropDefinitions.IKeyValuePairVftbl.Fields[7]);
        _ = cctorInstructions.Add(CilOpCodes.Ret);

        // Create the field for the IID for the 'KeyValuePair<,>' type
        WellKnownMemberDefinitionFactory.IID(
            iidRvaFieldName: InteropUtf8NameFactory.TypeName(keyValuePairType, "IID"),
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
}
