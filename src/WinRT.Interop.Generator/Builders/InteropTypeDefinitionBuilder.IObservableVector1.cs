// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> types.
    /// </summary>
    public static class IObservableVector1
    {
        /// <summary>
        /// Creates the cached factory type for the property for the event args for the vector.
        /// </summary>
        /// <param name="vectorType">The <see cref="GenericInstanceTypeSignature"/> for the vector type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="factoryType">The resulting factory type.</param>
        public static void EventSourceFactory(
            GenericInstanceTypeSignature vectorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition factoryType)
        {
            TypeSignature elementType = vectorType.TypeArguments[0];

            // We're declaring an 'internal sealed class' type
            factoryType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(vectorType),
                name: InteropUtf8NameFactory.TypeName(vectorType, "EventSourceFactory"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(factoryType);

            // 'Instance' field with the cached factory instance
            factoryType.Fields.Add(new FieldDefinition("Instance"u8, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, factoryType.ToReferenceTypeSignature()));

            // The actual factory is of type 'Func<WindowsRuntimeObject, WindowsRuntimeObjectReference, VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>>>'
            TypeSignature funcType = interopReferences.Func3.MakeGenericReferenceType(
                interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature(),
                interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType(elementType));

            // 'Value' field with the cached factory delegate
            factoryType.Fields.Add(new FieldDefinition("Value"u8, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, funcType.Import(module)));

            // Add the parameterless constructor
            factoryType.Methods.Add(MethodDefinition.CreateDefaultConstructor(module));

            // Get the constructor for the generic event source type
            MethodDefinition eventSourceConstructor = emitState.LookupTypeDefinition(vectorType, "EventSource").GetConstructor(
                comparer: SignatureComparer.IgnoreVersion,
                parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature(), interopReferences.CorLibTypeFactory.Int32])!;

            // Define the 'Callback' method as follows:
            //
            // public static VectorChangedEventHandlerEventSource<<ELEMENT_TYPE>> Callback(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
            MethodDefinition callbackMethod = new(
                name: "Callback"u8,
                attributes: MethodAttributes.Private | MethodAttributes.HideBySig,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.VectorChangedEventHandler1EventSource.MakeGenericReferenceType(elementType).Import(module),
                    parameterTypes: [
                        interopReferences.WindowsRuntimeObject.ToReferenceTypeSignature().Import(module),
                        interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature().Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_2 },
                    { Ldc_I4_6 },
                    { Newobj, eventSourceConstructor },
                    { Ret }
                }
            };

            factoryType.Methods.Add(callbackMethod);

            // We need the static constructor to initialize the static fields
            MethodDefinition cctor = factoryType.GetOrCreateStaticConstructor(module);

            cctor.CilInstructions.Clear();

            // Create a new instance of the factory type and store it in the 'Instance' field
            _ = cctor.CilInstructions.Add(Newobj, factoryType.GetConstructor()!);
            _ = cctor.CilInstructions.Add(Stsfld, factoryType.Fields[0]);

            // Create the delegate type and store it in the 'Value' field
            _ = cctor.CilInstructions.Add(Ldsfld, factoryType.Fields[0]);
            _ = cctor.CilInstructions.Add(Ldftn, callbackMethod);
            _ = cctor.CilInstructions.Add(Newobj, interopReferences.Delegate_ctor(funcType));
            _ = cctor.CilInstructions.Add(Stsfld, factoryType.Fields[1]);

            _ = cctor.CilInstructions.Add(Ret);
        }
    }
}
