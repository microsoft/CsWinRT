// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Helpers to interact with the <c>WellKnownInterfaceIIDs</c> type from <c>WinRT.Runtime.dll</c>.
/// </summary>
internal static class WellKnownInterfaceIIDs
{
    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>get_IID_...</c> method corresponding to the specified <paramref name="interfaceType"/>.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The <see cref="MemberReference"/> for the <c>get_IID_...</c> method for <paramref name="interfaceType"/>.</returns>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <remarks>
    /// The types handled by this method should be kept in sync with <see cref="WindowsRuntimeExtensions.IsCustomMappedWindowsRuntimeNonGenericInterfaceType"/>.
    /// </remarks>
    public static MemberReference get_IID(
        TypeSignature interfaceType,
        bool useWindowsUIXamlProjections,
        InteropReferences interopReferences)
    {
        // TODO: remove this once comparisons work fine without it
        ITypeDefOrRef interfaceTypeRef = interfaceType.ToTypeDefOrRef();

        // Get the name for the right IID property from 'WinRT.Runtime.dll'
        string nameSuffix = interfaceType switch
        {
            // Shared types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.IDisposable)
                => "Windows_Foundation_IClosable",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.IServiceProvider)
                => "Microsoft_UI_Xaml_IXamlServiceProvider",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.IAsyncInfo)
               => "Windows_Foundation_IAsyncInfo",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.IAsyncAction)
                => "Windows_Foundation_IAsyncAction",

            // XAML types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.INotifyCollectionChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.INotifyCollectionChanged)
                => "Microsoft_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.INotifyPropertyChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Data_INotifyPropertyChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.INotifyPropertyChanged)
                => "Microsoft_UI_Xaml_Data_INotifyPropertyChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.ICommand) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Input_ICommand",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.ICommand)
                => "Microsoft_UI_Xaml_Input_ICommand",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.INotifyDataErrorInfo)
                => "Microsoft_UI_Xaml_Data_INotifyDataErrorInfo",
            _ => throw WellKnownInteropExceptions.InvalidCustomMappedTypeForWellKnownInterfaceIIDs(interfaceType)
        };

        // Create the member reference from 'WellKnownInterfaceIIDs'
        return interopReferences.WellKnownInterfaceIIDs.CreateMemberReference(
            memberName: $"get_IID_{nameSuffix}",
            signature: MethodSignature.CreateStatic(WellKnownTypeSignatureFactory.InGuid(interopReferences)));
    }

    public static Guid get_GUID(
        TypeSignature signature,
        bool useWindowsUIXamlProjections,
        InteropReferences interopReferences)
    {
        if (signature is GenericInstanceTypeSignature genericSignature)
        {
            return genericSignature switch
            {
                // Shared types
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler)
                    => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler1)
                    => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler2)
                    => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
                _ => Guid.Empty
            };
        }
        // TODO: remove this once comparisons work fine without it
        ITypeDefOrRef interfaceTypeRef = signature.ToTypeDefOrRef();

        // Get the name for the right IID property from 'WinRT.Runtime.dll'
        return signature switch
        {
            // Shared types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.EventHandler)
                => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.EventHandler1)
                => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceTypeRef, interopReferences.EventHandler2)
                => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
            _ => Guid.Empty
        };
    }
}
