// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for "Impl" types for managed types that can be exposed to Windows Runtime.
/// </summary>
internal static class InteropImplTypeResolver
{
    /// <summary>
    /// Gets the "Impl" methods for a given generic instance type.
    /// </summary>
    /// <param name="type">The type to get the "Impl" method for.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <returns>The "Impl" methods for <paramref name="type"/>.</returns>
    public static (IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) GetGenericInstanceTypeImpl(
        GenericInstanceTypeSignature type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState)
    {
        // For generic types (i.e. generic interfaces), their marshalling code will be in 'WinRT.Interop.dll',
        // and produced at build time by this same executable. This also covers generic delegates, etc.
        TypeDefinition implTypeDefinition = emitState.LookupTypeDefinition(type, "Impl");
        MethodDefinition get_VtableMethod = implTypeDefinition.GetMethod("get_Vtable"u8);

        // The IID will be in the generated '<InterfaceIIDs>' type in 'WinRT.Interop.dll'
        Utf8String get_IIDMethodName = $"get_IID_{InteropUtf8NameFactory.TypeName(type)}";
        MethodDefinition get_IIDMethod = interopDefinitions.InterfaceIIDs.GetMethod(get_IIDMethodName);

        // Return the pair of methods from the ABI type in 'WinRT.Interop.dll'
        return (get_IIDMethod, get_VtableMethod);
    }

    /// <summary>
    /// Gets the "Impl" methods for a given custom-mapped or manually projected type.
    /// </summary>
    /// <param name="type">The type to get the "Impl" method for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The "Impl" methods for <paramref name="type"/>.</returns>
    public static (IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) GetCustomMappedOrManuallyProjectedTypeImpl(
        TypeSignature type,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        // For (non-generic) custom-mapped types, their ABI types are in 'WinRT.Runtime.dll', so we use those directly.
        // This also applies to all manually-projected interface types (e.g. 'IAsyncAction'), they have the same location.
        TypeReference typeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference($"ABI.{type.Namespace}", $"{type.Name}Impl");
        MemberReference get_VtableMethod = typeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr));

        // For custom-mapped types, the IID is in 'WellKnownInterfaceIIDs' in 'WinRT.Runtime.dll'
        MemberReference get_IIDMethod = WellKnownInterfaceIIDs.get_IID(
            interfaceType: type,
            interopReferences: interopReferences,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections);

        // Return the pair of methods from the ABI type in 'WinRT.Runtime.dll'
        return (get_IIDMethod, get_VtableMethod);
    }

    /// <summary>
    /// Gets the "Impl" methods for a (non-generic) projected type.
    /// </summary>
    /// <param name="type">The type to get the "Impl" method for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The "Impl" methods for <paramref name="type"/>.</returns>
    public static (IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) GetProjectedTypeImpl(
        TypeDefinition type,
        InteropReferences interopReferences)
    {
        // Finally, we have the base scenario of simple non-generic projected Windows Runtime interface types. In this
        // case, the marshalling code will just be in the declaring assembly of each of these projected interface types.
        TypeReference ImplTypeReference = interopReferences.WinRTProjection.CreateTypeReference($"ABI.{type.Namespace}", $"{type.Name}Impl");
        MemberReference get_VtableMethod = ImplTypeReference.CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr));

        // For normal projected types, the IID is in the generated 'InterfaceIIDs' type in the containing assembly
        string get_IIDMethodName = $"get_IID_{type.FullName.Replace('.', '_')}";
        TypeSignature get_IIDMethodReturnType = WellKnownTypeSignatureFactory.InGuid(interopReferences);
        TypeReference interfaceIIDsTypeReference = interopReferences.WinRTProjection.CreateTypeReference("ABI"u8, "InterfaceIIDs"u8);
        MemberReference get_IIDMethod = interfaceIIDsTypeReference.CreateMemberReference(get_IIDMethodName, MethodSignature.CreateStatic(get_IIDMethodReturnType));

        // Return the pair of methods from the ABI type in the declaring assembly for the type
        return (get_IIDMethod, get_VtableMethod);
    }

    /// <summary>
    /// Gets the "Impl" methods for a given SZ array type.
    /// </summary>
    /// <param name="type">The <see cref="SzArrayTypeSignature"/> for the SZ array type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The "Impl" methods for <paramref name="type"/>.</returns>
    public static (IMethodDefOrRef get_IID, IMethodDefOrRef get_Vtable) GetSzArrayTypeImpl(SzArrayTypeSignature type, InteropReferences interopReferences)
    {
        // Get the type name that matches the one used in the 'PropertyType' enum type
        string typeName = type.BaseType switch
        {
            { ElementType: ElementType.U1 } => "UInt8",
            { ElementType: ElementType.I2 } => "Int16",
            { ElementType: ElementType.U2 } => "UInt16",
            { ElementType: ElementType.I4 } => "Int32",
            { ElementType: ElementType.U4 } => "UInt32",
            { ElementType: ElementType.I8 } => "Int64",
            { ElementType: ElementType.U8 } => "UInt64",
            { ElementType: ElementType.R4 } => "Single",
            { ElementType: ElementType.R8 } => "Double",
            { ElementType: ElementType.Boolean } => "Boolean",
            { ElementType: ElementType.Char } => "Char16",
            { ElementType: ElementType.Object } => "Inspectable",
            { ElementType: ElementType.String } => "String",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.DateTimeOffset) => "DateTime",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.TimeSpan) => "TimeSpan",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.Guid) => "Guid",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.Point) => "Point",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.Size) => "Size",
            _ when SignatureComparer.IgnoreVersion.Equals(type.BaseType, interopReferences.Rect) => "Rect",
            _ => "OtherType"
        };

        // Prepare the method to get the IID and the one for the "Impl" vtable. These are all defined
        // on the 'IPropertyValueImpl' type in 'WinRT.Runtime.dll', with this exact naming pattern.
        IMethodDefOrRef get_IIDMethod = interopReferences.WellKnownInterfaceIIDsget_IID_IPropertyValue;
        IMethodDefOrRef get_VtableMethod = interopReferences.IPropertyValueImpl.CreateMemberReference(
            memberName: $"get_{typeName}ArrayVtable",
            signature: MethodSignature.CreateStatic(interopReferences.CorLibTypeFactory.IntPtr));

        // Return the pair of methods from the ABI type in 'WinRT.Runtime.dll'
        return (get_IIDMethod, get_VtableMethod);
    }
}
