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
    /// Helpers for <see cref="Delegate"/> types.
    /// </summary>
    public static class Delegate
    {
        /// <summary>
        /// Creates the 'IID' properties for an 'IDelegate' interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for the 'IDelegate' interface.</param>
        /// <param name="get_ReferenceIidMethod">The resulting 'IID' get method for the boxed 'IDelegate' interface.</param>
        public static void IIDs(
            GenericInstanceTypeSignature delegateType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out MethodDefinition get_IidMethod,
            out MethodDefinition get_ReferenceIidMethod)
        {
            // 'IDelegate' IID
            IID(
                name: InteropUtf8NameFactory.TypeName(delegateType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: GuidGenerator.CreateIID(delegateType, interopReferences, useWindowsUIXamlProjections),
                out get_IidMethod);

            // 'IReference<T>' IID
            IID(
                name: InteropUtf8NameFactory.TypeName(delegateType, "Reference"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: GuidGenerator.CreateIID(delegateType, interopReferences, useWindowsUIXamlProjections),
                out get_ReferenceIidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for an 'IDelegate' interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature delegateType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
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
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));

            // Create a method body for the 'Invoke' method
            invokeMethod.CilMethodBody = new CilMethodBody()
            {
                // Declare 1 variable:
                //   [0]: 'int' (the 'HRESULT' to return)
                LocalVariables = { new CilLocalVariable(module.CorLibTypeFactory.Int32) },
                Instructions =
                {
                    // '.try' code
                    { ldarg_0_tryStart },
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(delegateType).Import(module) },
                    { Ldarg_1 },
                    { Call, interopReferences.WindowsRuntimeObjectMarshallerConvertToManaged.Import(module) },
                    { Ldarg_2 },
                    { Call, interopReferences.WindowsRuntimeObjectMarshallerConvertToManaged.Import(module) },
                    { Callvirt, interopReferences.DelegateInvoke(delegateType, module).Import(module) },
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
                        TryStart = ldarg_0_tryStart.CreateLabel(),
                        TryEnd = call_catchStartMarshalException.CreateLabel(),
                        HandlerStart = call_catchStartMarshalException.CreateLabel(),
                        HandlerEnd = ldloc_0_returnHResult.CreateLabel(),
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIUnknown,
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "Impl"),
                vftblType: interopDefinitions.DelegateVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [invokeMethod]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for the 'IReference`1&lt;T&gt;' instantiation for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Marshaller"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ReferenceImplType(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
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
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Jump labels
            CilInstruction nop_beforeTry = new(Nop);
            CilInstruction ldarg_1_tryStart = new(Ldarg_1);
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
                    { Call, interopReferences.ComInterfaceDispatchGetInstance.MakeGenericInstanceMethod(delegateType).Import(module) },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
                    { Stloc_1 },
                    { Ldloca_S, loc_1_referenceValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe.Import(module) },
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
                        ExceptionType = interopReferences.Exception.Import(module)
                    }
                }
            };

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "ReferenceImpl"),
                vftblType: interopDefinitions.DelegateReferenceVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [valueMethod]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the COM interface entries for an 'IDelegate' interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ImplType"/>.</param>
        /// <param name="delegateReferenceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="ReferenceImplType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IDelegate' interface.</param>
        /// <param name="get_ReferenceIidMethod">The 'IID' get method for the boxed 'IDelegate' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceEntriesImplType">The resulting implementation type.</param>
        public static void InterfaceEntriesImpl(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateImplType,
            TypeDefinition delegateReferenceImplType,
            MethodDefinition get_IidMethod,
            MethodDefinition get_ReferenceIidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceEntriesImplType)
        {
            InteropTypeDefinitionBuilder.InterfaceEntriesImpl(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "InterfaceEntriesImpl"),
                entriesFieldType: interopDefinitions.DelegateInterfaceEntries,
                interopReferences: interopReferences,
                module: module,
                implType: out interfaceEntriesImplType,
                implTypes: [
                    (get_IidMethod, delegateImplType.GetMethod("get_Vtable"u8)),
                    (get_ReferenceIidMethod, delegateReferenceImplType.GetMethod("get_Vtable"u8)),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IPropertyValue, interopReferences.IPropertyValueImplget_OtherTypeVtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IStringable, interopReferences.IStringableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IWeakReferenceSource, interopReferences.IWeakReferenceSourceImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IMarshal, interopReferences.IMarshalImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IAgileObject, interopReferences.IAgileObjectImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IInspectable, interopReferences.IInspectableImplget_Vtable),
                    (interopReferences.WellKnownInterfaceIIDsget_IID_IUnknown, interopReferences.IUnknownImplget_Vtable)]);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IComWrappersCallback</c> interface for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="nativeDelegateType">The type returned by <see cref="NativeDelegateType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IDelegate' interface.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature delegateType,
            TypeDefinition nativeDelegateType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
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
                Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeObjectComWrappersCallback.Import(module)) }
            };

            module.TopLevelTypes.Add(callbackType);

            // Define the 'CreateObject' method as follows:
            //
            // public static object CreateObject(void* value, CreatedWrapperFlags wrapperFlags)
            MethodDefinition createObjectMethod = new(
                name: "CreateObject"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Object,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        interopReferences.CreatedWrapperFlags.Import(module).MakeByReferenceType()]))
            {
                CilOutParameterIndices = [2],
                CilMethodBody = new CilMethodBody
                {
                    // Create a new delegate targeting the 'Invoke' extension, with the 'WindowsRuntimeObjectReference' object
                    // as target. This allows us to not need an additional type (and allocation) for the delegate target.
                    Instructions =
                    {
                        { Ldarg_0 },
                        { Call, get_IidMethod },
                        { Ldarg_1 },
                        { Call, interopReferences.WindowsRuntimeComWrappersMarshalCreateObjectReferenceUnsafe.Import(module) },
                        { Ldftn, nativeDelegateType.GetMethod("Invoke"u8) },
                        { Newobj, interopReferences.Delegate_ctor(delegateType).Import(module) },
                        { Ret }
                    }
                }
            };

            // Add and implement the 'CreateObject' method
            callbackType.AddMethodImplementation(
                declaration: interopReferences.IWindowsRuntimeObjectComWrappersCallbackCreateObject.Import(module),
                method: createObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the native delegate for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeDelegateType">The resulting callback type.</param>
        public static void NativeDelegateType(
            GenericInstanceTypeSignature delegateType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
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
            MethodSignature invokeSignature = delegateType.Import(module).Resolve()!.GetMethod("Invoke"u8).Signature!.InstantiateGenericTypes(GenericContext.FromType(delegateType));

            // Define the 'Invoke' method as follows:
            //
            // public static void Invoke(WindowsRuntimeObjectReference objectReference, <PARAMETER#0> arg0, <PARAMETER#1> arg1)
            MethodDefinition invokeMethod = new(
                name: "Invoke"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module),
                        invokeSignature.ParameterTypes[0].Import(module),
                        invokeSignature.ParameterTypes[1].Import(module)]))
            { CilMethodBody = new CilMethodBody() };

            nativeDelegateType.Methods.Add(invokeMethod);

            // Get the method body for the 'Invoke' method
            CilMethodBody invokeBody = invokeMethod.CilMethodBody;
            CilInstructionCollection invokeInstructions = invokeBody.Instructions;

            // Import 'WindowsRuntimeObjectReferenceValue', compute it just once
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue
                .Import(module)
                .ToValueTypeSignature();

            // Declare the local variables:
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
            _ = invokeInstructions.Add(Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module));
            _ = invokeInstructions.Add(Stloc_0);

            // '.try' for local [0]
            CilInstruction try_0 = invokeInstructions.Add(Ldarg_1);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectMarshallerConvertToUnmanaged.Import(module));
            _ = invokeInstructions.Add(Stloc_1);

            // '.try' for local [1]
            CilInstruction try_1 = invokeInstructions.Add(Ldarg_2);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectMarshallerConvertToUnmanaged.Import(module));
            _ = invokeInstructions.Add(Stloc_2);

            // 'Invoke' call for the native delegate (and 'try' for local [2])
            CilInstruction try_2 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[0]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Stloc_3);
            _ = invokeInstructions.Add(Ldloc_3);
            _ = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[1]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[2]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module));
            _ = invokeInstructions.Add(Ldloc_3);
            _ = invokeInstructions.Add(Ldind_I);
            _ = invokeInstructions.Add(Ldfld, interopDefinitions.DelegateVftbl.Fields[3]);
            _ = invokeInstructions.Add(Calli, WellKnownTypeSignatureFactory.InvokeImpl(interopReferences).Import(module).MakeStandAloneSignature());
            _ = invokeInstructions.Add(Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module));
            _ = invokeInstructions.Add(Leave_S, ret.CreateLabel());

            // 'finally' for local [2]
            CilInstruction finally_2 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[2]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
            _ = invokeInstructions.Add(Endfinally);

            // 'finally' for local [1]
            CilInstruction finally_1 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[1]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
            _ = invokeInstructions.Add(Endfinally);

            // 'finally' for local [0]
            CilInstruction finally_0 = invokeInstructions.Add(Ldloca_S, invokeBody.LocalVariables[0]);
            _ = invokeInstructions.Add(Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module));
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
        /// <param name="delegateInterfaceEntriesImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceEntriesImpl"/>.</param>
        /// <param name="delegateComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_ReferenceIidMethod">The resulting 'IID' get method for the boxed 'IDelegate' interface.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateInterfaceEntriesImplType,
            TypeDefinition delegateComWrappersCallbackType,
            MethodDefinition get_ReferenceIidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            // We're declaring an 'internal sealed class' type
            marshallerType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "ComWrappersMarshallerAttribute"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

            module.TopLevelTypes.Add(marshallerType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

            marshallerType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

            // The 'ComputeVtables' method returns the 'ComWrappers.ComInterfaceEntry*' type
            PointerTypeSignature computeVtablesReturnType = interopReferences.ComInterfaceEntry.Import(module).MakePointerType();

            // Define the 'ComputeVtables' method as follows:
            //
            // public override ComInterfaceEntry* ComputeVtables(out int count)
            MethodDefinition computeVtablesMethod = new(
                name: "ComputeVtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: computeVtablesReturnType,
                    parameterTypes: [module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
            {
                CilOutParameterIndices = [1],
                CilInstructions =
                {
                    { Ldarg_1 },
                    { CilInstruction.CreateLdcI4(interopDefinitions.DelegateInterfaceEntries.Fields.Count) },
                    { Stind_I4 },
                    { Call, delegateInterfaceEntriesImplType.GetMethod("get_Vtables"u8) },
                    { Ret }
                }
            };

            // Add and implement the 'ComputeVtables' method
            marshallerType.AddMethodImplementation(
                declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeComputeVtables.Import(module),
                method: computeVtablesMethod);

            // Import the 'UnboxToManaged<TCallback>' method for the delegate
            IMethodDescriptor windowsRuntimeDelegateMarshallerUnboxToManaged2Descriptor = interopReferences.WindowsRuntimeDelegateMarshallerUnboxToManaged2
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToReferenceTypeSignature());

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
                    { Ldarg_2 },
                    { Ldc_I4_1 },
                    { Stind_I4 },
                    { Ldarg_1 },
                    { Call, get_ReferenceIidMethod },
                    { Call, windowsRuntimeDelegateMarshallerUnboxToManaged2Descriptor },
                    { Ret }
                }
            };

            // Add and implement the 'CreateObject' method
            marshallerType.AddMethodImplementation(
                declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeCreateObject.Import(module),
                method: createObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IDelegate' interface.</param>
        /// <param name="get_ReferenceIidMethod">The resulting 'IID' get method for the boxed 'IDelegate' interface.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            MethodDefinition get_ReferenceIidMethod,
            InteropReferences interopReferences,
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
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature();

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [delegateType2]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, get_IidMethod },
                    { Call, interopReferences.WindowsRuntimeDelegateMarshallerConvertToUnmanaged.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToUnmanagedMethod);

            // Construct a descriptor for 'WindowsRuntimeDelegateMarshaller.ConvertToManaged<<DELEGATE_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeDelegateMarshallerConvertToManaged =
                interopReferences.WindowsRuntimeDelegateMarshallerConvertToManaged
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToReferenceTypeSignature());

            // Define the 'ConvertToManaged' method as follows:
            //
            // public static <DELEGATE_TYPE> ConvertToManaged(void* value)
            MethodDefinition convertToManagedMethod = new(
                name: "ConvertToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: delegateType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, windowsRuntimeDelegateMarshallerConvertToManaged },
                    { Castclass, delegateType2.ToTypeDefOrRef() },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(convertToManagedMethod);

            // Define the 'BoxToUnmanaged' method as follows:
            //
            // public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition boxToUnmanagedMethod = new(
                name: "BoxToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: windowsRuntimeObjectReferenceValueType,
                    parameterTypes: [delegateType2]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, get_ReferenceIidMethod },
                    { Call, interopReferences.WindowsRuntimeDelegateMarshallerBoxToUnmanaged.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(boxToUnmanagedMethod);

            // Construct a descriptor for 'WindowsRuntimeDelegateMarshaller.UnboxToManaged<<DELEGATE_CALLBACK_TYPE>>(void*)'
            IMethodDescriptor windowsRuntimeDelegateMarshallerUnboxToManaged =
                interopReferences.WindowsRuntimeDelegateMarshallerUnboxToManaged
                .Import(module)
                .MakeGenericInstanceMethod(delegateComWrappersCallbackType.ToReferenceTypeSignature());

            // Define the 'UnboxToManaged' method as follows:
            //
            // public static <DELEGATE_TYPE> UnboxToManaged(void* value)
            MethodDefinition unboxToUnmanagedMethod = new(
                name: "UnboxToManaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: delegateType2,
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, windowsRuntimeDelegateMarshallerUnboxToManaged },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(unboxToUnmanagedMethod);
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType),
                runtimeClassName: delegateType.FullName, // TODO
                comWrappersMarshallerAttributeType: delegateComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition proxyType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: delegateType.FullName, // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: delegateType,
                proxyTypeMapSourceType: delegateType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}