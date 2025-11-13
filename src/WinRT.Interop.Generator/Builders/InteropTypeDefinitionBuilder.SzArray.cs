// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Helpers;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for SZ array types.
    /// </summary>
    public static class SzArray
    {
        /// <summary>
        /// Creates the 'IID' property for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="arrayType"/>.</param>
        public static void IID(
            SzArrayTypeSignature arrayType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(arrayType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: GuidGenerator.CreateIID(arrayType, interopReferences), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            SzArrayTypeSignature arrayType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal static class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "Marshaller"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(marshallerType);

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static void ConvertToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>>, out uint size, out <ABI_ELEMENT_TYPE>* array)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(arrayType.BaseType).Import(module),
                        module.CorLibTypeFactory.UInt32.MakeByReferenceType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()])) // TODO
            {
                CilOutParameterIndices = [2, 3],
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <ELEMENT_TYPE>[] ConvertToManaged(uint size, <ABI_ELEMENT_TYPE>* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: arrayType.Import(module),
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()])) // TODO
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(convertToManagedMethod);

            // Define the 'CopyToManaged' method as follows:
            //
            // public static void CopyToManaged(uint size, <ABI_ELEMENT_TYPE>* value, Span<<ELEMENT_TYPE>> destination)
            MethodDefinition copyToManagedMethod = new(
                name: "CopyToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType(), // TODO
                        interopReferences.Span1.MakeGenericValueType(arrayType.BaseType).Import(module)]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(copyToManagedMethod);

            // Define the 'CopyToUnmanaged' method as follows:
            //
            // public static void CopyToUnmanaged(ReadOnlySpan<<ELEMENT_TYPE>> value, uint size, <ABI_ELEMENT_TYPE>* destination)
            MethodDefinition copyToUnmanagedMethod = new(
                name: "CopyToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.ReadOnlySpan1.MakeGenericValueType(arrayType.BaseType).Import(module),
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };

            marshallerType.Methods.Add(copyToUnmanagedMethod);

            // Define the 'Free' method
            MethodDefinition freeMethod = InteropMethodDefinitionFactory.SzArrayMarshaller.Free(
                arrayType,
                interopReferences,
                module);

            marshallerType.Methods.Add(freeMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeArrayComWrappersCallback</c> interface for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="marshallerType">The type returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallback(
            SzArrayTypeSignature arrayType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            // We're declaring an 'internal abstract class' type
            callbackType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ComWrappersCallback"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeArrayComWrappersCallback.Import(module)) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'CreateArray' method as follows:
            //
            // public static Array CreateArray(uint count, void* value)
            MethodDefinition createArrayMethod = new(
                name: "CreateArray"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.Array.ToReferenceTypeSignature().Import(module),
                    parameterTypes: [
                        module.CorLibTypeFactory.UInt32,
                        module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Ldarg_1 },
                    { Call, marshallerType.GetMethod("ConvertToManaged"u8) },
                    { Ret }
                }
            };

            // Add and implement 'CreateArray'
            callbackType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeArrayComWrappersCallbackCreateArray.Import(module),
                method: createArrayMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for the 'IReferenceArray`1&lt;T&gt;' instantiation for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ArrayImpl(
            SzArrayTypeSignature arrayType,
            TypeDefinition marshallerType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'get_Value' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int get_Value(void* thisPtr, uint* size, <ABI_ELEMENT_TYPE>** result)
            MethodDefinition valueMethod = new(
                name: "get_Value"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        module.CorLibTypeFactory.UInt32.MakePointerType(),
                        module.CorLibTypeFactory.Void.MakePointerType().MakePointerType()])) // TODO
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Jump labels
            CilInstruction ldc_i4_e_pointer = new(Ldc_I4, unchecked((int)0x80004003));
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);

            // Declare 2 local variables:
            //   [0]: 'int' (the 'HRESULT' to return)
            //   [1]: 'WindowsRuntimeObjectReferenceValue' to use to marshal the delegate
            CilLocalVariable loc_0_hresult = new(module.CorLibTypeFactory.Int32);
            CilLocalVariable loc_1_referenceValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature().Import(module));

            // Create a method body for the 'get_Value' method
            valueMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_hresult, loc_1_referenceValue },
                Instructions =
                {
                    // Return 'E_POINTER' if either argument is 'null'
                    { Ldarg_1 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Beq_S, ldc_i4_e_pointer.CreateLabel() },
                    { Ldarg_2 },
                    { Ldc_I4_0 },
                    { Conv_U },
                    { Bne_Un_S, nop_beforeTry.CreateLabel() },
                    { ldc_i4_e_pointer },
                    { Ret },
                    { nop_beforeTry },

                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(arrayType).Import(module) },
                    { Newobj, interopReferences.ReadOnlySpan1_ctor(arrayType).Import(module) },
                    { Ldarg_1 },
                    { Ldarg_2 },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
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
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "Impl"),
                vftblType: interopDefinitions.IReferenceArrayVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [valueMethod]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="implType">The <see cref="TypeDefinition"/> instance returned by <see cref="Impl"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            SzArrayTypeSignature arrayType,
            TypeDefinition implType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceEntriesImplType)
        {
            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "InterfaceEntriesImpl"),
                entriesFieldType: interopDefinitions.IReferenceArrayInterfaceEntries,
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: [
                    (get_IidMethod, implType.GetMethod("get_Vtable"u8)),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IPropertyValue, interopReferences.IPropertyValueImplget_OtherTypeVtable), // TODO
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable)]);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="arrayInterfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="arrayComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallback"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IReferenceArray`1&lt;T&gt;' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            SzArrayTypeSignature arrayType,
            TypeDefinition arrayInterfaceEntriesImplType,
            TypeDefinition arrayComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            marshallerType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

            // Define the 'GetOrCreateComInterfaceForObject' method as follows:
            //
            // public static void* GetOrCreateComInterfaceForObject(object value)
            MethodDefinition getOrCreateComInterfaceForObjectMethod = new(
                name: "GetOrCreateComInterfaceForObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void.MakePointerType(),
                    parameterTypes: [module.CorLibTypeFactory.Object]))
            {
                CilInstructions =
                {
                    { Ldarg_1 },
                    { Ldc_I4_2 }, // TODO
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject.Import(module) },
                    { Ret }
                }
            };

            // Add and implement the 'GetOrCreateComInterfaceForObject' method
            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);

            // Define the 'ComputeVtables' method as follows:
            //
            // public static ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.ComInterfaceEntry.Import(module).MakePointerType(),
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [1],
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4(interopDefinitions.IReferenceArrayInterfaceEntries.Fields.Count) },
                    { Stind_I4 },
                    { Call, arrayInterfaceEntriesImplType.GetMethod("get_Vtables"u8) },
                    { Ret }
                }
            };

            // Add and implement the 'ComputeVtables' method
            marshallerType.Methods.Add(computeVtablesMethod);

            // Import the 'UnboxToManaged<TCallback>' method for the array
            IMethodDescriptor windowsRuntimeArrayMarshallerUnboxToManagedDescriptor = interopReferences.WindowsRuntimeArrayMarshallerUnboxToManaged
                .Import(module)
                .MakeGenericInstanceMethod(arrayComWrappersCallbackType.ToReferenceTypeSignature());

            // Define the 'CreateObject' method as follows:
            //
            // public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.CreatedWrapperFlags.Import(module).MakeByReferenceType()]))
            {
                CilOutParameterIndices = [2],
                CilInstructions =
                {
                    // Set the 'wrapperFlags' to 'CreatedWrapperFlags.NonWrapping'
                    { Ldarg_2 },
                    { Ldc_I4_2 },
                    { Stind_I4 },

                    // Forward to 'WindowsRuntimeArrayMarshaller'
                    { Ldarg_1 },
                    { Call, get_IidMethod },
                    { Call, windowsRuntimeArrayMarshallerUnboxToManagedDescriptor },
                    { Ret }
                }
            };

            // Add and implement the 'CreateObject' method
            marshallerType.Methods.Add(createObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="arrayComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            SzArrayTypeSignature arrayType,
            TypeDefinition arrayComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(arrayType),
                name: InteropUtf8NameFactory.TypeName(arrayType), // TODO
                runtimeClassName: $"Windows.Foundation.IReferenceArray`1<{arrayType.BaseType}>", // TODO
                comWrappersMarshallerAttributeType: arrayComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some SZ array type.
        /// </summary>
        /// <param name="arrayType">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            SzArrayTypeSignature arrayType,
            TypeDefinition proxyType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: arrayType.FullName, // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: arrayType,
                proxyTypeMapSourceType: arrayType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}
