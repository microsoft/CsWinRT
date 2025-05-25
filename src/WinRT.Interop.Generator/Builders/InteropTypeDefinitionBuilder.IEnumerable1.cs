// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
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
            implType = new(
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

            // Initialize the enumerator vtable
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
                    { Stfld, interopDefinitions.IEnumerator1Vftbl.Fields[6] },
                    { Ret }
                }
            };

            // Create the field for the IID for the enumerator type
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
