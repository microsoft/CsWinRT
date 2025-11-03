// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for interop type definitions.
/// </summary>
internal static partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Creates an 'IID' property with the specified parameters.
    /// </summary>
    /// <param name="name">The property and field name.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="iid">The <see cref="Guid"/> value to use for the RVA field.</param>
    /// <param name="get_IidMethod">The resulting 'get_IID' method.</param>
    private static void IID(
        Utf8String name,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        in Guid iid,
        out MethodDefinition get_IidMethod)
    {
        WellKnownMemberDefinitionFactory.IID(
            propertyName: name,
            iidRvaFieldName: name,
            iidRvaDataType: interopDefinitions.IIDRvaDataSize_16,
            interopReferences: interopReferences,
            module: module,
            iid: in iid,
            out FieldDefinition iidRvaField,
            out get_IidMethod,
            out PropertyDefinition iidProperty);

        interopDefinitions.RvaFields.Fields.Add(iidRvaField);
        interopDefinitions.InterfaceIIDs.Methods.Add(get_IidMethod);
        interopDefinitions.InterfaceIIDs.Properties.Add(iidProperty);
    }

    /// <summary>
    /// Creates a new type definition for the native object for a generic interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the generic interface type.</param>
    /// <param name="nativeObjectBaseType">The <see cref="TypeSignature"/> for the base native object type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="nativeObjectType">The resulting native object type.</param>
    private static void NativeObject(
        TypeSignature typeSignature,
        TypeSignature nativeObjectBaseType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition nativeObjectType)
    {
        // We're declaring an 'internal sealed class' type
        nativeObjectType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "NativeObject"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: nativeObjectBaseType.Import(module).ToTypeDefOrRef());

        module.TopLevelTypes.Add(nativeObjectType);

        // Define the constructor
        MethodDefinition ctor = MethodDefinition.CreateConstructor(module, interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature());

        nativeObjectType.Methods.Add(ctor);

        _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
        _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
        _ = ctor.CilMethodBody!.Instructions.Insert(2, Call, interopReferences.WindowsRuntimeNativeObjectBaseType_ctor(nativeObjectBaseType).Import(module));
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for a generic interface.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the specialized check.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the generic interface type.</param>
    /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
    /// <param name="get_IidMethod">The 'IID' get method for <paramref name="typeSignature"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="callbackType">The resulting callback type.</param>
    private static void ComWrappersCallback(
        string runtimeClassName,
        TypeSignature typeSignature,
        TypeDefinition nativeObjectType,
        MethodDefinition get_IidMethod,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition callbackType)
    {
        // We're declaring an 'internal abstract class' type
        callbackType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "ComWrappersCallback"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
        {
            Interfaces = { new InterfaceImplementation(interopReferences.IWindowsRuntimeUnsealedObjectComWrappersCallback.Import(module)) }
        };

        module.TopLevelTypes.Add(callbackType);

        // Define the 'TryCreateObject' method as follows:
        //
        // public static bool TryCreateObject(
        //     void* value,
        //     ReadOnlySpan<char> runtimeClassName,
        //     out object? result,
        //     out CreatedWrapperFlags wrapperFlags)
        MethodDefinition tryCreateObjectMethod = new(
            name: "TryCreateObject"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(
                returnType: module.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    module.CorLibTypeFactory.Void.MakePointerType(),
                    interopReferences.ReadOnlySpanChar.Import(module),
                    module.CorLibTypeFactory.Object.MakeByReferenceType(),
                    interopReferences.CreatedWrapperFlags.Import(module).MakeByReferenceType()]))
        { CilOutParameterIndices = [3, 4] };

        // Add and implement 'TryCreateObject'
        callbackType.AddMethodImplementation(
            declaration: interopReferences.IWindowsRuntimeUnsealedObjectComWrappersCallbackTryCreateObject.Import(module),
            method: tryCreateObjectMethod);

        // Declare the local variables:
        //   [0]: 'WindowsRuntimeObjectReference' (for 'result')
        CilLocalVariable loc_0_result = new(interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module));

        // Jump labels
        CilInstruction ldarg_3_failure = new(Ldarg_3);

        // Create a method body for the 'TryCreateObject' method
        tryCreateObjectMethod.CilMethodBody = new CilMethodBody()
        {
            LocalVariables = { loc_0_result },
            Instructions =
            {
                // Compare the runtime class name for the fast path
                { Ldarg_1 },
                { Ldstr, runtimeClassName },
                { Call, interopReferences.MemoryExtensionsAsSpanCharString.Import(module) },
                { Call, interopReferences.MemoryExtensionsSequenceEqualChar.Import(module) },
                { Brfalse_S, ldarg_3_failure.CreateLabel() },

                // Create the 'WindowsRuntimeObjectReference' instance
                { Ldarg_0 },
                { Call, get_IidMethod },
                { Ldarg_3 },
                { Call, interopReferences.WindowsRuntimeComWrappersMarshalCreateObjectReferenceUnsafe.Import(module) },
                { Stloc_0 },

                // Create and assign the 'NativeObject' instance to return
                { Ldarg_2 },
                { Ldloc_0 },
                { Newobj, nativeObjectType.GetMethod(".ctor"u8) },
                { Stind_Ref },
                { Ldc_I4_1 },
                { Ret },

                // Failure path
                { ldarg_3_failure },
                { Ldc_I4_0 },
                { Stind_I4 },
                { Ldarg_2 },
                { Ldnull },
                { Stind_Ref },
                { Ldc_I4_0 },
                { Ret }
            }
        };
    }

    /// <summary>
    /// Creates a new type definition for the marshaller attribute for a generic interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the generic interface type.</param>
    /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
    /// <param name="get_IidMethod">The 'IID' get method for <paramref name="typeSignature"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="marshallerType">The resulting marshaller type.</param>
    private static void ComWrappersMarshallerAttribute(
        TypeSignature typeSignature,
        TypeDefinition nativeObjectType,
        MethodDefinition get_IidMethod,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition marshallerType)
    {
        // We're declaring an 'internal sealed class' type
        marshallerType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "ComWrappersMarshallerAttribute"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: interopReferences.WindowsRuntimeComWrappersMarshallerAttribute.Import(module));

        module.TopLevelTypes.Add(marshallerType);

        // Define the constructor
        MethodDefinition ctor = MethodDefinition.CreateConstructor(module);

        marshallerType.Methods.Add(ctor);

        _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
        _ = ctor.CilMethodBody!.Instructions.Insert(1, Call, interopReferences.WindowsRuntimeComWrappersMarshallerAttribute_ctor.Import(module));

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
                // Create and initialize the 'WindowsRuntimeObjectReference' instance
                { Ldarg_1 },
                { Call, get_IidMethod },
                { Ldarg_2 },
                { Call, interopReferences.WindowsRuntimeComWrappersMarshalCreateObjectReference.Import(module) },
                { Newobj, nativeObjectType.GetMethod(".ctor"u8) },
                { Ret },
            }
        };

        // Add and implement the 'CreateObject' method
        marshallerType.AddMethodImplementation(
            declaration: interopReferences.WindowsRuntimeComWrappersMarshallerAttributeCreateObject.Import(module),
            method: createObjectMethod);
    }

    /// <summary>
    /// Creates a new type definition for the marshaller for a generic interface.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> for the generic interface type.</param>
    /// <param name="interfaceComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallback"/>.</param>
    /// <param name="get_IidMethod">The 'IID' get method for <paramref name="typeSignature"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="marshallerType">The resulting marshaller type.</param>
    private static void Marshaller(
        TypeSignature typeSignature,
        TypeDefinition interfaceComWrappersCallbackType,
        MethodDefinition get_IidMethod,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition marshallerType)
    {
        // We're declaring an 'internal static class' type
        marshallerType = new(
            ns: InteropUtf8NameFactory.TypeNamespace(typeSignature),
            name: InteropUtf8NameFactory.TypeName(typeSignature, "Marshaller"),
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(marshallerType);

        // Prepare the external types we need in the implemented methods
        TypeSignature typeSignature2 = typeSignature.Import(module);
        TypeSignature windowsRuntimeObjectReferenceValueType = interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature();

        // Reference the instantiated 'ConvertToUnmanaged' method for the marshaller
        MemberReference windowsRuntimeInterfaceMarshallerConvertToUnmanaged = interopReferences.WindowsRuntimeInterfaceMarshallerConvertToUnmanaged(typeSignature);

        // Define the 'ConvertToUnmanaged' method as follows:
        //
        // public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<INTERFACE_TYPE> value)
        MethodDefinition convertToUnmanagedMethod = new(
            name: "ConvertToUnmanaged"u8,
            attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            signature: MethodSignature.CreateStatic(
                returnType: windowsRuntimeObjectReferenceValueType,
                parameterTypes: [typeSignature2]))
        {
            CilInstructions =
            {
                { Ldarg_0 },
                { Call, get_IidMethod },
                { Call, windowsRuntimeInterfaceMarshallerConvertToUnmanaged.Import(module) },
                { Ret }
            }
        };

        marshallerType.Methods.Add(convertToUnmanagedMethod);

        // Construct a descriptor for 'WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<<INTERFACE_CALLBACK_TYPE>>(void*)'
        IMethodDescriptor windowsRuntimeUnsealedObjectMarshallerConvertToManaged =
            interopReferences.WindowsRuntimeUnsealedObjectMarshallerConvertToManaged
            .Import(module)
            .MakeGenericInstanceMethod(interfaceComWrappersCallbackType.ToReferenceTypeSignature());

        // Define the 'ConvertToManaged' method as follows:
        //
        // public static <INTERFACE_TYPE> ConvertToManaged(void* value)
        MethodDefinition convertToManagedMethod = new(
            name: "ConvertToManaged"u8,
            attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            signature: MethodSignature.CreateStatic(
                returnType: typeSignature2,
                parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]))
        {
            CilInstructions =
            {
                { Ldarg_0 },
                { Call, windowsRuntimeUnsealedObjectMarshallerConvertToManaged },
                { Ret }
            }
        };

        marshallerType.Methods.Add(convertToManagedMethod);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the vtable for some managed type.
    /// </summary>
    /// <param name="interfaceType">The type of interface being constructed (either <see cref="ComInterfaceType.InterfaceIsIUnknown"/> or <see cref="ComInterfaceType.InterfaceIsIInspectable"/>).</param>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="vftblType">The vtable type to use for the CCW vtable.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="vtableMethods">The set of implementation methods for the implementation type.</param>
    private static void Impl(
        [ConstantExpected] ComInterfaceType interfaceType,
        Utf8String ns,
        Utf8String name,
        TypeDefinition vftblType,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        params ReadOnlySpan<MethodDefinition> vtableMethods)
    {
        // We're declaring an 'internal static class' type
        implType = new TypeDefinition(
            ns: ns,
            name: name,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The vtable field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <VTABLE_TYPE> Vftbl;
        FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, vftblType.ToValueTypeSignature())
        {
            CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
        };

        implType.Fields.Add(vftblField);

        // Create the static constructor to initialize the vtable
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // Initialize the base vtable
        cctor.CilMethodBody = new CilMethodBody()
        {
            Instructions =
            {
                { Ldsflda, vftblField },
                { Conv_U }
            }
        };

        int vtableOffset;

        // Initialize the base interface
        switch (interfaceType)
        {
            case ComInterfaceType.InterfaceIsIUnknown:
                _ = cctor.CilMethodBody.Instructions.Add(Call, interopReferences.IUnknownImplget_Vtable.Import(module));
                _ = cctor.CilMethodBody.Instructions.Add(Ldobj, interopDefinitions.IUnknownVftbl);
                _ = cctor.CilMethodBody.Instructions.Add(Stobj, interopDefinitions.IUnknownVftbl);
                vtableOffset = 3;
                break;
            case ComInterfaceType.InterfaceIsIInspectable:
                _ = cctor.CilMethodBody.Instructions.Add(Call, interopReferences.IInspectableImplget_Vtable.Import(module));
                _ = cctor.CilMethodBody.Instructions.Add(Ldobj, interopDefinitions.IInspectableVftbl);
                _ = cctor.CilMethodBody.Instructions.Add(Stobj, interopDefinitions.IInspectableVftbl);
                vtableOffset = 6;
                break;
            case ComInterfaceType.InterfaceIsDual:
            case ComInterfaceType.InterfaceIsIDispatch:
            default:
                throw new ArgumentOutOfRangeException(nameof(interfaceType));
        }

        // Add all implementation methods, at the right offset
        foreach (MethodDefinition method in vtableMethods)
        {
            implType.Methods.Add(method);

            _ = cctor.CilMethodBody.Instructions.Add(Ldsflda, vftblField);
            _ = cctor.CilMethodBody.Instructions.Add(Ldftn, method);
            _ = cctor.CilMethodBody.Instructions.Add(Stfld, vftblType.Fields[vtableOffset++]);
        }

        // Enforce that we did initialize all vtable entries
        //ArgumentOutOfRangeException.ThrowIfNotEqual(vtableOffset, vftblType.Fields.Count, nameof(vtableMethods)); // TODO

        // Don't forget the 'ret' at the end of the static constructor
        _ = cctor.CilMethodBody.Instructions.Add(Ret);

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
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    private static void InterfaceEntriesImpl(
        Utf8String ns,
        Utf8String name,
        TypeDefinition entriesFieldType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        params ReadOnlySpan<(IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable)> implTypes)
    {
        InterfaceEntriesImpl(
            ns: ns,
            name: name,
            entriesFieldType: entriesFieldType,
            interopReferences: interopReferences,
            module: module,
            get_IID: static (arg, il, module) => il.Add(Call, arg.get_IID.Import(module)),
            get_Vtable: static (arg, il, module) => il.Add(Call, arg.get_Vtable.Import(module)),
            implTypes: implTypes,
            implType: out implType);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    private static void InterfaceEntriesImpl(
        Utf8String ns,
        Utf8String name,
        TypeDefinition entriesFieldType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition implType,
        params ReadOnlySpan<InterfaceEntryInfo> implTypes)
    {
        InterfaceEntriesImpl(
            ns: ns,
            name: name,
            entriesFieldType: entriesFieldType,
            interopReferences: interopReferences,
            module: module,
            get_IID: static (arg, il, module) => arg.LoadIID(il, module),
            get_Vtable: static (arg, il, module) => arg.LoadVtable(il, module),
            implTypes: implTypes,
            implType: out implType);
    }

    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <typeparam name="TArg">The type of arguments to use to populate the interface entries.</typeparam>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    /// <param name="get_IID">The callback to emit code to get the IID.</param>
    /// <param name="get_Vtable">The callback to emit code to get the vtable.</param>
    /// <param name="implType">The resulting implementation type.</param>
    private static void InterfaceEntriesImpl<TArg>(
        Utf8String ns,
        Utf8String name,
        TypeDefinition entriesFieldType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        ReadOnlySpan<TArg> implTypes,
        Action<TArg, CilInstructionCollection, ModuleDefinition> get_IID,
        Action<TArg, CilInstructionCollection, ModuleDefinition> get_Vtable,
        out TypeDefinition implType)
    {
        // We're declaring an 'internal static class' type
        implType = new TypeDefinition(
            ns: ns,
            name: name,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(implType);

        // The interface entries field looks like this:
        //
        // [FixedAddressValueType]
        // private static readonly <DelegateInterfaceEntries> Entries;
        //
        // The '[FixedAddressValueType]' attribute allows ILC to pre-initialize the entire vtable (in .rdata).
        FieldDefinition entriesField = new("Entries"u8, FieldAttributes.Private | FieldAttributes.Static, entriesFieldType.ToValueTypeSignature())
        {
            CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
        };

        implType.Fields.Add(entriesField);

        // Create the static constructor to initialize the interface entries
        MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

        // Import the target fields (they have to be in the module, or the resulting assembly won't be valid):
        //   - [0]: Guid IID
        //   - [1]: nint Vtable
        IFieldDescriptor comInterfaceEntryIIDField = interopReferences.ComInterfaceEntryIID.Import(module);
        IFieldDescriptor comInterfaceEntryVtableField = interopReferences.ComInterfaceEntryVtable.Import(module);

        // We need to create a new method body bound to this constructor
        CilInstructionCollection cctorInstructions = cctor.CilMethodBody!.Instructions;

        // Ensure there's no instructions ('GetOrCreateStaticConstructor' adds a return stub)
        cctorInstructions.Clear();

        // Initialize the COM interface entries, doing this for each entry:
        //
        // Entries.<FIELD#i>.IID = <INTERFACE>Impl.IID;
        // Entries.<FIELD#i>.Vtable = <INTERFACE>Impl.Vtable;
        //
        // Each 'Impl' types is assumed to always have the 'IID' and 'Vtable' properties, in this order.
        for (int i = 0; i < implTypes.Length; i++)
        {
            _ = cctorInstructions.Add(Ldsflda, entriesField);
            _ = cctorInstructions.Add(Ldflda, entriesFieldType.Fields[i]);

            // Invoke the callback to emit code to load 'IID' on the evaluation stack
            get_IID(implTypes[i], cctorInstructions, module);

            _ = cctorInstructions.Add(Ldobj, interopReferences.Guid.Import(module));
            _ = cctorInstructions.Add(Stfld, comInterfaceEntryIIDField);
            _ = cctorInstructions.Add(Ldsflda, entriesField);
            _ = cctorInstructions.Add(Ldflda, entriesFieldType.Fields[i]);

            // Same as above, but to get the vtable pointer on the stack
            get_Vtable(implTypes[i], cctorInstructions, module);

            _ = cctorInstructions.Add(Stfld, comInterfaceEntryVtableField);
        }

        _ = cctorInstructions.Add(Ret);

        // The 'Vtables' property type has the signature being 'ComWrappers.ComInterfaceEntry*'
        PointerTypeSignature vtablesPropertyType = interopReferences.ComInterfaceEntry.Import(module).MakePointerType();

        // Create the 'Vtables' property
        PropertyDefinition vtablesProperty = new(
            name: "Vtables"u8,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.CreateStatic(vtablesPropertyType))
        {
            GetMethod = new MethodDefinition(
                name: "get_Vtables"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: vtablesPropertyType,
                    parameterTypes: []))
            {
                IsAggressiveInlining = true,
                CilInstructions =
                {
                    // Create a method body for the 'Vtables' property (it directly returns the 'Entries' field address)
                    { Ldsflda, entriesField },
                    { Conv_U },
                    { Ret }
                }
            }
        };

        implType.Properties.Add(vtablesProperty);
        implType.Methods.Add(vtablesProperty.GetMethod!);
    }

    /// <summary>
    /// Creates a new type definition for the proxy type of some managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="runtimeClassName">The runtime class name for the managed type.</param>
    /// <param name="comWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance for the marshaller attribute type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="marshallerType">The resulting proxy type.</param>
    public static void Proxy(
        Utf8String ns,
        Utf8String name,
        string runtimeClassName,
        TypeDefinition comWrappersMarshallerAttributeType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out TypeDefinition marshallerType)
    {
        // We're declaring an 'internal static class' type
        marshallerType = new(
            ns: ns,
            name: name,
            attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(marshallerType);

        // Get the constructor for '[WindowsRuntimeClassName]'
        MemberReference windowsRuntimeClassNameAttributeCtor = interopReferences.WindowsRuntimeClassNameAttribute
            .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                returnType: module.CorLibTypeFactory.Void,
                parameterTypes: [module.CorLibTypeFactory.String]))
            .Import(module);

        // Add the attribute with the name of the runtime class
        marshallerType.CustomAttributes.Add(new CustomAttribute(
            constructor: windowsRuntimeClassNameAttributeCtor,
            signature: new CustomAttributeSignature(new CustomAttributeArgument(
                argumentType: module.CorLibTypeFactory.String,
                value: runtimeClassName))));

        // Add the generated marshaller attribute
        marshallerType.CustomAttributes.Add(new CustomAttribute(comWrappersMarshallerAttributeType.GetConstructor()!.Import(module)));
    }

    /// <summary>
    /// Creates the type map attributes for a given type.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the managed type.</param>
    /// <param name="externalTypeMapTargetType">The target type for <see cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)"/>.</param>
    /// <param name="externalTypeMapTrimTargetType">The trim target type for <see cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)"/>.</param>
    /// <param name="proxyTypeMapSourceType">The source type for <see cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)"/>.</param>
    /// <param name="proxyTypeMapProxyType">The proxy type for <see cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)"/>.</param>
    /// <param name="interfaceTypeMapSourceType">The IDIC source type for <see cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)"/>.</param>
    /// <param name="interfaceTypeMapProxyType">The IDIC proxy type for <see cref="TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(Type, Type)"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    private static void TypeMapAttributes(
        string? runtimeClassName,
        [NotNullIfNotNull(nameof(runtimeClassName))] TypeSignature? externalTypeMapTargetType,
        [NotNullIfNotNull(nameof(runtimeClassName))] TypeSignature? externalTypeMapTrimTargetType,
        [NotNullIfNotNull(nameof(proxyTypeMapProxyType))] TypeSignature? proxyTypeMapSourceType,
        [NotNullIfNotNull(nameof(proxyTypeMapSourceType))] TypeSignature? proxyTypeMapProxyType,
        [NotNullIfNotNull(nameof(interfaceTypeMapProxyType))] TypeSignature? interfaceTypeMapSourceType,
        [NotNullIfNotNull(nameof(interfaceTypeMapSourceType))] TypeSignature? interfaceTypeMapProxyType,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Emit the '[TypeMap]' attribute for the external type map.
        // This is optional, only needed for projected types.
        if (runtimeClassName is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
                value: runtimeClassName,
                target: externalTypeMapTargetType!,
                trimTarget: externalTypeMapTrimTargetType!,
                interopReferences: interopReferences,
                module: module));
        }

        // Emit the '[TypeMapAssociation]' attribute for the proxy type map.
        // This is only needed for types that can actually be instantiated.
        if (proxyTypeMapSourceType is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapAssociationWindowsRuntimeComWrappersTypeMapGroup(
                source: proxyTypeMapSourceType,
                proxy: proxyTypeMapProxyType!,
                interopReferences: interopReferences,
                module: module));
        }

        // Emit the '[TypeMapAssociation]' attribute for 'IDynamicInterfaceCastable' scenarios.
        // This is not always needed, as it is specifically only for interface types.
        if (interfaceTypeMapSourceType is not null)
        {
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapAssociationDynamicInterfaceCastableImplementationTypeMapGroup(
                source: interfaceTypeMapSourceType,
                proxy: interfaceTypeMapProxyType!,
                interopReferences: interopReferences,
                module: module));
        }
    }

    /// <summary>
    /// A base type to abstract inserting interface entries information into a static constructor.
    /// </summary>
    private abstract class InterfaceEntryInfo
    {
        /// <summary>
        /// Loads the IID for the interface onto the evaluation stack.
        /// </summary>
        /// <param name="instructions">The target <see cref="CilInstructionCollection"/>.</param>
        /// <param name="module">The <see cref="ModuleDefinition"/> in use.</param>
        public abstract void LoadIID(CilInstructionCollection instructions, ModuleDefinition module);

        /// <summary>
        /// Loads the vtable for the interface onto the evaluation stack.
        /// </summary>
        /// <param name="instructions">The target <see cref="CilInstructionCollection"/>.</param>
        /// <param name="module">The <see cref="ModuleDefinition"/> in use.</param>
        public abstract void LoadVtable(CilInstructionCollection instructions, ModuleDefinition module);
    }
}
