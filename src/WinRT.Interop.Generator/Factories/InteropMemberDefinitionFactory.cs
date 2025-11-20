// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop member definitions.
/// </summary>
internal class InteropMemberDefinitionFactory
{
    /// <summary>
    /// Creates a lazy, volatile, reference type, default constructor, read-only property with a backing field, factory method, and get accessor method.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="index">The index of the property (used for the factory method name).</param>
    /// <param name="propertyType">The type of the property.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="backingField">The resulting backing field.</param>
    /// <param name="factoryMethod">The resulting factory method.</param>
    /// <param name="getAccessorMethod">The resulting get accessor method.</param>
    /// <param name="propertyDefinition">The resulting property definition.</param>
    public static void LazyVolatileReferenceDefaultConstructorReadOnlyProperty(
        Utf8String propertyName,
        int index,
        TypeSignature propertyType,
        InteropReferences interopReferences,
        ModuleDefinition module,
        out FieldDefinition backingField,
        out MethodDefinition factoryMethod,
        out MethodDefinition getAccessorMethod,
        out PropertyDefinition propertyDefinition)
    {
        // Define the backing field
        backingField = new FieldDefinition(
            name: $"<{propertyName}>k__BackingField",
            attributes: FieldAttributes.Private | FieldAttributes.Static,
            fieldType: propertyType.Import(module));

        // Define the factory method as follows:
        //
        // [MethodImpl(MethodImplOptions.NoInlining)]
        // public <PROPERTY_TYPE> <get_<PROPERTY_NAME>>g__CreateValue|<INDEX>_0()
        factoryMethod = new MethodDefinition(
            name: $"<get_{propertyName}>g__CreateValue|{index}_0",
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(propertyType.Import(module)))
        {
            NoInlining = true,
            CilInstructions =
            {
                // _ = Interlocked.CompareExchange(ref <BACKING_FIELD>, value: new(), comparand: null);
                { Ldsflda, backingField },
                { Newobj, propertyType.ToTypeDefOrRef().CreateConstructorReference(module.CorLibTypeFactory).Import(module) },
                { Ldnull },
                { Call, interopReferences.InterlockedCompareExchange1.MakeGenericInstanceMethod(propertyType).Import(module) },
                { Pop },

                // return <BACKING_FIELD>;
                { Ldsfld, backingField },
                { Ret }
            }
        };

        // Label for the 'ret' (we are doing lazy-init for the backing field)
        CilInstruction ret = new(Ret);

        // Create the property getter method
        getAccessorMethod = new MethodDefinition(
            name: $"get_{propertyName}",
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(propertyType.Import(module)))
        {
            IsAggressiveInlining = true,
            CilInstructions =
            {
                { Ldsfld, backingField },
                { Dup },
                { Brtrue_S, ret.CreateLabel() },
                { Pop },
                { Call, factoryMethod },
                { ret }
            }
        };

        // Create the property
        propertyDefinition = new PropertyDefinition(
            name: propertyName,
            attributes: PropertyAttributes.None,
            signature: PropertySignature.FromGetMethod(getAccessorMethod))
        {
            GetMethod = getAccessorMethod
        };
    }
}