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
    /// Helpers for <see cref="System.Collections.Generic.IEnumerable{T}"/> types.
    /// </summary>
    public static class IEnumerable1
    {
        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iiterableMethodsType">The resulting methods type.</param>
        public static void IIterableMethods(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition iiterableMethodsType)
        {
            TypeSignature elementType = enumerableType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            iiterableMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "IIterableMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(iiterableMethodsType);

            // Define the 'First' method as follows:
            //
            // public static IEnumerator<<TYPE_ARGUMENT>> First(WindowsRuntimeObjectReference thisReference)
            MethodDefinition firstMethod = new(
                name: "First"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]))
            {
                NoInlining = true
            };

            iiterableMethodsType.Methods.Add(firstMethod);

            // Reference the generated 'ConvertToManaged' method to marshal the 'IEnumerator<T>' instance to managed
            MemberReference convertToManagedMethod = module
                .CreateTypeReference(
                    ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                    name: InteropUtf8NameFactory.TypeName(interopReferences.IEnumerator1.MakeGenericInstanceType(elementType), "Marshaller"))
                .CreateMemberReference("ConvertToManaged", MethodSignature.CreateStatic(
                    returnType: interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module),
                    parameterTypes: [module.CorLibTypeFactory.Void.MakePointerType()]));

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the enumerator pointer that was retrieved)
            //   [3]: 'IEnumerator<<TYPE_ARGUMENT>>' (the marshalled enumerator)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_enumeratorPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_3_enumerator = new(interopReferences.IEnumerator1.MakeGenericInstanceType(elementType).Import(module));

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);
            CilInstruction ldloc_2_tryStart = new(Ldloc_2);
            CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
            CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

            // Create a method body for the 'First' method
            firstMethod.CilMethodBody = new CilMethodBody(firstMethod)
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_enumeratorPtr, loc_3_enumerator },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_enumeratorPtr },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerable1Vftbl.GetField("First"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerable1FirstImpl(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd },

                    // '.try/.finally' code to marshal the enumerator
                    { ldloc_2_tryStart },
                    { Call, convertToManagedMethod.Import(module) },
                    { Stloc_3 },
                    { Leave_S, ldloc_3_finallyEnd.CreateLabel() },
                    { ldloc_2_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectMarshallerFree.Import(module) },
                    { Endfinally },
                    { ldloc_3_finallyEnd },
                    { Ret }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    },
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloc_2_tryStart.CreateLabel(),
                        TryEnd = ldloc_2_finallyStart.CreateLabel(),
                        HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                        HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IIterable&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumerableType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        /// <param name="iidRvaField">The resulting RVA field for the IID data.</param>
        public static void ImplType(
            GenericInstanceTypeSignature enumerableType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition implType,
            out FieldDefinition iidRvaField)
        {
            // We're declaring an 'internal static class' type
            implType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(enumerableType),
                name: InteropUtf8NameFactory.TypeName(enumerableType, "Impl"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(implType);

            // The vtable field looks like this:
            //
            // [FixedAddressValueType]
            // private static readonly <IEnumerable1Vftbl> Vftbl;
            FieldDefinition vftblField = new("Vftbl"u8, FieldAttributes.Private, interopDefinitions.IEnumerable1Vftbl.ToTypeSignature(isValueType: true))
            {
                CustomAttributes = { new CustomAttribute(interopReferences.FixedAddressValueTypeAttribute_ctor.Import(module)) }
            };

            implType.Fields.Add(vftblField);

            // Define the 'First' method
            MethodDefinition firstMethod = InteropMethodDefinitionFactory.IEnumerable1.First(
                enumerableType: enumerableType,
                interopReferences: interopReferences,
                module: module);

            implType.Methods.Add(firstMethod);

            // Create the static constructor to initialize the vtable
            MethodDefinition cctor = implType.GetOrCreateStaticConstructor(module);

            // Initialize the enumerable vtable
            cctor.CilMethodBody = new CilMethodBody(cctor)
            {
                Instructions =
                {
                    { Ldsflda, vftblField },
                    { Conv_U },
                    { Call, interopReferences.IInspectableImplget_Vtable.Import(module) },
                    { Ldobj, interopDefinitions.IInspectableVftbl },
                    { Stobj, interopDefinitions.IInspectableVftbl },
                    { Ldsflda, vftblField },
                    { Ldftn, firstMethod },
                    { Stfld, interopDefinitions.IEnumerable1Vftbl.Fields[6] },
                    { Ret }
                }
            };

            // Create the field for the IID for the enumerable type
            WellKnownMemberDefinitionFactory.IID(
                iidRvaFieldName: InteropUtf8NameFactory.TypeName(enumerableType, "IID"),
                iidRvaDataType: interopDefinitions.IIDRvaDataSize_16,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(),
                out iidRvaField,
                out PropertyDefinition iidProperty,
                out MethodDefinition get_iidMethod);

            interopDefinitions.RvaFields.Fields.Add(iidRvaField);

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
}
