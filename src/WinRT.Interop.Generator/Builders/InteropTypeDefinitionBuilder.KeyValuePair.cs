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
    /// Helpers for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class KeyValuePair
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
                int typeArgumentIndex = name == "get_Key" ? 0 : 1;
                TypeSignature typeArgument = keyValuePairType.TypeArguments[typeArgumentIndex];

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

                // Import 'ComWrappers.ComInterfaceDispatch.GetInstance'
                MethodSpecification getInstanceMethod = wellKnownInteropReferences.ComInterfaceDispatchGetInstance
                    .MakeGenericInstanceMethod(module.CorLibTypeFactory.Object)
                    .ImportWith(module.DefaultImporter);

                // Reference the 'KeyValuePair<,>' type
                ITypeDefOrRef keyValuePairTypeRef = module.DefaultImporter.ImportTypeSignature(keyValuePairType).ToTypeDefOrRef();

                // Reference the 'KeyValuePair<,>' accessor
                MemberReference get_MethodRef = keyValuePairTypeRef.CreateMemberReference(
                    memberName: name,
                    signature: MethodSignature.CreateInstance(new GenericParameterSignature(
                        parameterType: GenericParameterType.Type,
                        index: typeArgumentIndex)));

                // Jump labels
                CilInstruction nop_beforeTry = new(Nop);
                CilInstruction ldarg_1_tryStart = new(Ldarg_1);
                CilInstruction call_catchStartMarshalException = new(Call, wellKnownInteropReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
                CilInstruction ldloc_0_returnHResult = new(Ldloc_0);

                // Declare 2 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                //   [1]: 'KeyValuePair<,>' (the boxed object to get values from)
                CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
                CilLocalVariable loc_1_keyValuePair = new(keyValuePairType.Import(module));

                // Create a method body for the method
                method.CilMethodBody = new CilMethodBody(method)
                {
                    LocalVariables = { loc_0_hresult, loc_1_keyValuePair },
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

                // This method can dynamically change (eg. different marshallers), so use explicit syntax
                CilInstructionCollection instructions = method.CilMethodBody.Instructions;

                // Return 'E_POINTER' if the argument is 'null'
                _ = instructions.Add(Ldarg_1);
                _ = instructions.Add(Ldc_I4_0);
                _ = instructions.Add(Conv_U);
                _ = instructions.Add(Bne_Un_S, nop_beforeTry.CreateLabel());
                _ = instructions.Add(Ldc_I4, unchecked((int)0x80004003));
                _ = instructions.Add(Ret);
                instructions.Add(nop_beforeTry);

                // '.try' code
                instructions.Add(ldarg_1_tryStart);
                _ = instructions.Add(Ldarg_0);
                _ = instructions.Add(Call, getInstanceMethod);
                _ = instructions.Add(Unbox_Any, keyValuePairTypeRef);
                _ = instructions.Add(Stloc_1);
                _ = instructions.Add(Ldarg_1);
                _ = instructions.Add(Ldloca_S, loc_1_keyValuePair);
                _ = instructions.Add(Call, get_MethodRef);
                _ = instructions.Add(Stind_I);
                _ = instructions.Add(Ldc_I4_0);
                _ = instructions.Add(Stloc_0);
                _ = instructions.Add(Leave_S, ldloc_0_returnHResult.CreateLabel());

                // 'catch' code
                instructions.Add(call_catchStartMarshalException);
                _ = instructions.Add(Stloc_0);
                _ = instructions.Add(Leave_S, ldloc_0_returnHResult.CreateLabel());

                // Return the 'HRESULT' from location [0]
                instructions.Add(ldloc_0_returnHResult);
                _ = instructions.Add(Ret);

                return method;
            }

            // Define the accessor exports
            MethodDefinition get_KeyMethod = GetKeyOrValuePropertyAccessorMethod(keyValuePairType, wellKnownInteropReferences, module, "get_Key");
            MethodDefinition get_ValueMethod = GetKeyOrValuePropertyAccessorMethod(keyValuePairType, wellKnownInteropReferences, module, "get_Value");

            implType.Methods.Add(get_KeyMethod);
            implType.Methods.Add(get_ValueMethod);

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the 'KeyValuePair<,>' vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, wellKnownInteropReferences.IInspectableImplget_Vtable.ImportWith(module.DefaultImporter) },
                    { Ldobj, wellKnownInteropDefinitions.IInspectableVftbl },
                    { Stobj, wellKnownInteropDefinitions.IInspectableVftbl },
                    { Ldsflda, vftblField },
                    { Ldftn, get_KeyMethod },
                    { Stfld, wellKnownInteropDefinitions.IKeyValuePairVftbl.Fields[6] },
                    { Ldsflda, vftblField },
                    { Ldftn, get_ValueMethod },
                    { Stfld, wellKnownInteropDefinitions.IKeyValuePairVftbl.Fields[7] },
                    { Ret }
                }
            };

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

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> interface.
        /// </summary>
        /// <param name="keyValuePairType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="keyValuePairTypeImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ImplType"/>.</param>
        /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void InterfaceEntriesImplType(
            GenericInstanceTypeSignature keyValuePairType,
            TypeDefinition keyValuePairTypeImplType,
            WellKnownInteropDefinitions wellKnownInteropDefinitions,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            InteropTypeDefinitionBuilder.InterfaceEntriesImplType(
                ns: InteropUtf8NameFactory.TypeNamespace(keyValuePairType),
                name: InteropUtf8NameFactory.TypeName(keyValuePairType, "InterfaceEntriesImpl"),
                entriesFieldType: wellKnownInteropDefinitions.IKeyValuePairInterfaceEntries,
                module: module,
                implType: out implType,
                implTypes: [
                    (keyValuePairTypeImplType.GetMethod("get_IID"u8), keyValuePairTypeImplType.GetMethod("get_Vtable"u8)),
                    (wellKnownInteropReferences.IStringableImplget_IID, wellKnownInteropReferences.IStringableImplget_Vtable),
                    (wellKnownInteropReferences.IWeakReferenceSourceImplget_IID, wellKnownInteropReferences.IWeakReferenceSourceImplget_Vtable),
                    (wellKnownInteropReferences.IMarshalImplget_IID, wellKnownInteropReferences.IMarshalImplget_Vtable),
                    (wellKnownInteropReferences.IAgileObjectImplget_IID, wellKnownInteropReferences.IAgileObjectImplget_Vtable),
                    (wellKnownInteropReferences.IInspectableImplget_IID, wellKnownInteropReferences.IInspectableImplget_Vtable),
                    (wellKnownInteropReferences.IUnknownImplget_IID, wellKnownInteropReferences.IUnknownImplget_Vtable)]);
        }
    }
}
