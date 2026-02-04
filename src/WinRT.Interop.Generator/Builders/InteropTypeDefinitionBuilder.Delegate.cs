// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
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
        /// Creates the 'IID' properties for an 'IDelegate' interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
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

            // 'IReference<T>' IID, which uses a boxed type signature to represent it.
            // This is not technically a valid type signature (since you can't have a
            // boxed reference type), however we're only using this to signal to the
            // IID generation logic that the delegate type is being used in the boxing
            // scenario. This is different than boxed value type, which instead are
            // just always projected as and using 'Nullable<T>' to represent this.
            IID(
                name: InteropUtf8NameFactory.TypeName(delegateType, "Reference"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: GuidGenerator.CreateIID(delegateType.MakeBoxedType(), interopReferences, useWindowsUIXamlProjections),
                out get_ReferenceIidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the vtable for an <c>IDelegate</c> interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="vftblType">The resulting vtable type.</param>
        public static void Vftbl(
            GenericInstanceTypeSignature delegateType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition vftblType)
        {
            MethodSignature invokeSignature = delegateType.GetDelegateInvokeMethodSignature(module);

            // Prepare the sender and arguments types (same as for the 'Impl' type below). Note that
            // we are relying on the fact that all Windows Runtime generic delegate types have in
            // common that they all return 'void' and have exactly two parameters on 'Invoke'.
            TypeSignature senderType = invokeSignature.ParameterTypes[0];
            TypeSignature argsType = invokeSignature.ParameterTypes[1];

            bool isSenderReferenceType = senderType.HasReferenceAbiType(interopReferences);
            bool isArgsReferenceType = argsType.HasReferenceAbiType(interopReferences);

            // We can share the vtable type for 'void*' when both sender and args types are reference types
            if (isSenderReferenceType && isArgsReferenceType)
            {
                vftblType = interopDefinitions.DelegateVftbl;

                return;
            }

            // If both the sender and the args types are not reference types, we can't possibly share
            // the vtable type. So in this case, we just always construct a specialized new type.
            if (!isSenderReferenceType && !isArgsReferenceType)
            {
                vftblType = WellKnownTypeDefinitionFactory.DelegateVftbl(
                    ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                    name: InteropUtf8NameFactory.TypeName(delegateType, "Vftbl"),
                    senderType: senderType.GetAbiType(interopReferences),
                    argsType: argsType.GetAbiType(interopReferences),
                    interopReferences: interopReferences,
                    module: module);

                module.TopLevelTypes.Add(vftblType);

                return;
            }

            // Helper to create vtable types that can be shared between multiple delegate types
            static void GetOrCreateVftbl(
                TypeSignature senderType,
                TypeSignature argsType,
                TypeSignature displaySenderType,
                TypeSignature displayArgsType,
                InteropReferences interopReferences,
                InteropGeneratorEmitState emitState,
                ModuleDefinition module,
                out TypeDefinition vftblType)
            {
                // If we already have a vtable type for this pair, reuse that
                if (emitState.TryGetDelegateVftblType(senderType, argsType, out vftblType!))
                {
                    return;
                }

                // Create a dummy signature just to generate the mangled name for the vtable type
                TypeSignature sharedEventHandlerType = interopReferences.EventHandler2.MakeGenericReferenceType(
                    displaySenderType,
                    displayArgsType);

                // Construct a new specialized vtable type
                TypeDefinition newVftblType = WellKnownTypeDefinitionFactory.DelegateVftbl(
                    ns: InteropUtf8NameFactory.TypeNamespace(sharedEventHandlerType),
                    name: InteropUtf8NameFactory.TypeName(sharedEventHandlerType, "Vftbl"),
                    senderType: senderType,
                    argsType: argsType,
                    interopReferences: interopReferences,
                    module: module);

                // Go through the lookup so that we can reuse the vtable later
                vftblType = emitState.GetOrAddDelegateVftblType(senderType, argsType, newVftblType);

                // If we won the race and this is the vtable type that was just created, we can add it to the module
                if (vftblType == newVftblType)
                {
                    module.TopLevelTypes.Add(newVftblType);
                }
            }

            // Get or create a shared vtable where the reference type is replaced with just 'void*'
            if (isSenderReferenceType)
            {
                GetOrCreateVftbl(
                    senderType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                    argsType: argsType.GetAbiType(interopReferences),
                    displaySenderType: interopReferences.CorLibTypeFactory.Object,
                    displayArgsType: argsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out vftblType);
            }
            else
            {
                GetOrCreateVftbl(
                    senderType: senderType.GetAbiType(interopReferences),
                    argsType: interopReferences.CorLibTypeFactory.Void.MakePointerType(),
                    displaySenderType: senderType,
                    displayArgsType: interopReferences.CorLibTypeFactory.Object,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out vftblType);
            }
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for an 'IDelegate' interface.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="vftblType">The type returned by <see cref="Vftbl"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition vftblType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            MethodSignature invokeSignature = delegateType.GetDelegateInvokeMethodSignature(module);

            // Prepare the sender and arguments types. This path is only ever reached for valid generic
            // Windows Runtime delegate types, and they all have exactly two type arguments (see above).
            TypeSignature senderType = invokeSignature.ParameterTypes[0];
            TypeSignature argsType = invokeSignature.ParameterTypes[1];

            // Define the 'Invoke' method as follows:
            //
            // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            // private static int Invoke(void* thisPtr, <ABI_SENDER_TYPE> sender, <ABI_ARGS_TYPE> e)
            MethodDefinition invokeMethod = new(
                name: "Invoke"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Int32,
                    parameterTypes: [
                        module.CorLibTypeFactory.Void.MakePointerType(),
                        senderType.GetAbiType(interopReferences).Import(module),
                        argsType.GetAbiType(interopReferences).Import(module)]))
            {
                CustomAttributes = { InteropCustomAttributeFactory.UnmanagedCallersOnly(interopReferences, module) }
            };

            // Labels for jumps
            CilInstruction ldloc_0_returnHResult = new(Ldloc_0);
            CilInstruction ldarg_0_tryStart = new(Ldarg_0);
            CilInstruction call_catchStartMarshalException = new(Call, interopReferences.RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged.Import(module));
            CilInstruction nop_parameter1Rewrite = new(Nop);
            CilInstruction nop_parameter2Rewrite = new(Nop);

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
                    { nop_parameter1Rewrite },
                    { nop_parameter2Rewrite },
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

            // Track rewriting the two parameters for this method
            emitState.TrackManagedParameterMethodRewrite(
                parameterType: senderType,
                method: invokeMethod,
                marker: nop_parameter1Rewrite,
                parameterIndex: 1);

            emitState.TrackManagedParameterMethodRewrite(
                parameterType: argsType,
                method: invokeMethod,
                marker: nop_parameter2Rewrite,
                parameterIndex: 2);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIUnknown,
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "Impl"),
                vftblType: vftblType,
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
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeDelegateType">The resulting callback type.</param>
        public static void NativeDelegateType(
            GenericInstanceTypeSignature delegateType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition nativeDelegateType)
        {
            MethodSignature invokeSignature = delegateType.GetDelegateInvokeMethodSignature(module);

            // Prepare the sender and arguments types (same as for the 'Impl' type above)
            TypeSignature senderType = invokeSignature.ParameterTypes[0];
            TypeSignature argsType = invokeSignature.ParameterTypes[1];

            // We're declaring an 'internal static class' type
            nativeDelegateType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType, "NativeDelegate"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(nativeDelegateType);

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
                        senderType.Import(module),
                        argsType.Import(module)]));

            nativeDelegateType.Methods.Add(invokeMethod);

            // Prepare the 'Invoke' signature for the '[UnmanagedCallersOnly]' export method.
            // This is derived from the managed one, but with ABI types for both parameters.
            MethodSignature invokeAbiSignature = WellKnownTypeSignatureFactory.InvokeImpl(
                senderType: senderType.GetAbiType(interopReferences),
                argsType: argsType.GetAbiType(interopReferences),
                interopReferences: interopReferences);

            // Import 'WindowsRuntimeObjectReferenceValue', compute it just once
            TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue
                .Import(module)
                .ToValueTypeSignature();

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            CilLocalVariable loc_0_thisValue = new(windowsRuntimeObjectReferenceValueType);
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());

            // Jump labels
            CilInstruction nop_try_this = new(Nop);
            CilInstruction nop_try_sender = new(Nop);
            CilInstruction nop_try_args = new(Nop);
            CilInstruction nop_ld_sender = new(Nop);
            CilInstruction nop_ld_args = new(Nop);
            CilInstruction nop_finally_sender = new(Nop);
            CilInstruction nop_finally_args = new(Nop);
            CilInstruction ldloca_0_finally_0 = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ret = new(Ret);

            // Create a method body for the 'Invoke' method
            invokeMethod.CilMethodBody = new CilMethodBody()
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr },
                Instructions =
                {
                    // Load the local [0]
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },
                    { nop_try_this },

                    // Arguments loading inside outer 'try/finally' block
                    { nop_try_sender },
                    { nop_try_args },

                    // 'Invoke' call for the native delegate (and 'try' for local [2])
                    { Ldloca_S, loc_0_thisValue },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { nop_ld_sender },
                    { nop_ld_args },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.DelegateVftbl.Fields[3] },
                    { Calli, invokeAbiSignature.Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, ret.CreateLabel() },

                    // Optional 'finally' blocks for the marshalled parameters. These are intentionally
                    // in reverse order, as the inner-most parameter should be released first.
                    { nop_finally_args },
                    { nop_finally_sender },

                    // 'finally' for local [0]
                    { ldloca_0_finally_0 },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },

                    // return;
                    { ret }
                },
                ExceptionHandlers =
                {
                    // Setup 'try/finally' for local [0]
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = nop_try_this.CreateLabel(),
                        TryEnd = ldloca_0_finally_0.CreateLabel(),
                        HandlerStart = ldloca_0_finally_0.CreateLabel(),
                        HandlerEnd = ret.CreateLabel()
                    }
                }
            };

            // Track rewriting the two parameters for this method
            emitState.TrackNativeParameterMethodRewrite(
                parameterType: senderType,
                method: invokeMethod,
                tryMarker: nop_try_sender,
                loadMarker: nop_ld_sender,
                finallyMarker: nop_finally_sender,
                parameterIndex: 1);

            emitState.TrackNativeParameterMethodRewrite(
                parameterType: argsType,
                method: invokeMethod,
                tryMarker: nop_try_args,
                loadMarker: nop_ld_args,
                finallyMarker: nop_finally_args,
                parameterIndex: 2);
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
            MethodDefinition ctor = MethodDefinition.CreateDefaultConstructor(module, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor);

            marshallerType.Methods.Add(ctor);

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

            marshallerType.Methods.Add(computeVtablesMethod);

            // Define the 'GetOrCreateComInterfaceForObject' method as follows:
            //
            // public override void* GetOrCreateComInterfaceForObject(object value)
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
                    { CilInstruction.CreateLdcI4((int)CreateComInterfaceFlags.TrackerSupport) },
                    { Call, interopReferences.WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject.Import(module) },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(getOrCreateComInterfaceForObjectMethod);

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
                    { CilInstruction.CreateLdcI4((int)CreatedWrapperFlags.TrackerObject) },
                    { Stind_I4 },
                    { Ldarg_1 },
                    { Call, get_ReferenceIidMethod },
                    { Call, windowsRuntimeDelegateMarshallerUnboxToManaged2Descriptor },
                    { Ret }
                }
            };

            marshallerType.Methods.Add(createObjectMethod);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="delegateComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for the 'IDelegate' interface.</param>
        /// <param name="get_ReferenceIidMethod">The resulting 'IID' get method for the boxed 'IDelegate' interface.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition delegateComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            MethodDefinition get_ReferenceIidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
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

            // Track the type (it may be needed to marshal parameters or return values)
            emitState.TrackTypeDefinition(marshallerType, delegateType, "Marshaller");

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
        /// Creates a new type definition for the proxy type for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            TypeSignature delegateType,
            TypeDefinition comWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition proxyType)
        {
            // For delegate types, we need to both specify the mapped metadata name, so that when marshalling 'Type' instances to
            // native we can correctly detect the mapped type to be a metadata type, and also annotate the proxy types with the
            // '[WindowsRuntimeMetadataTypeName]', as that's different than the runtime class name (which uses 'IReference<T>').
            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(delegateType),
                name: InteropUtf8NameFactory.TypeName(delegateType),
                mappedMetadata: "Windows.Foundation.FoundationContract",
                runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(delegateType, useWindowsUIXamlProjections),
                metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(delegateType, useWindowsUIXamlProjections),
                mappedType: delegateType,
                comWrappersMarshallerAttributeType: comWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="Proxy(TypeSignature, TypeDefinition, InteropReferences, ModuleDefinition, bool, out TypeDefinition)"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        public static void TypeMapAttributes(
            TypeSignature delegateType,
            TypeDefinition proxyType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections)
        {
            // For delegate types, we also need to pass the metadata type name when setting up the type map
            // attributes, as we need '[TypeMap<TTypeMapGroup>]' entries in the metadata type map as well.
            // This allows marshalling a 'TypeName' representing a Windows Runtime delegate type correctly.
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(delegateType, useWindowsUIXamlProjections),
                metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(delegateType, useWindowsUIXamlProjections),
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: delegateType,
                marshallingTypeMapSourceType: delegateType,
                marshallingTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                metadataTypeMapSourceType: null,
                metadataTypeMapProxyType: null,
                interfaceTypeMapSourceType: null,
                interfaceTypeMapProxyType: null,
                interopReferences: interopReferences,
                module: module);
        }
    }
}