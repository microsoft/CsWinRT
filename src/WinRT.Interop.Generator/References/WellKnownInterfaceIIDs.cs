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
        ITypeDescriptor interfaceType,
        bool useWindowsUIXamlProjections,
        InteropReferences interopReferences)
    {
        // Get the name for the right IID property from 'WinRT.Runtime.dll'
        string nameSuffix = interfaceType switch
        {
            // Shared types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IDisposable)
                => "Windows_Foundation_IClosable",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IServiceProvider)
                => "Microsoft_UI_Xaml_IXamlServiceProvider",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncInfo)
               => "Windows_Foundation_IAsyncInfo",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncAction)
                => "Windows_Foundation_IAsyncAction",

            // XAML types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyCollectionChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyCollectionChanged)
                => "Microsoft_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyPropertyChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Data_INotifyPropertyChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyPropertyChanged)
                => "Microsoft_UI_Xaml_Data_INotifyPropertyChanged",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.ICommand) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Input_ICommand",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.ICommand)
                => "Microsoft_UI_Xaml_Input_ICommand",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyDataErrorInfo)
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
        if (signature is SzArrayTypeSignature)
        {
            // TODO: SzArrayTypeSignature case
            return Guid.Empty;
        }
        else if (signature is GenericInstanceTypeSignature genericSignature)
        {
            return genericSignature switch
            {
                // Shared types
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler)
                    => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler1)
                    => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler2)
                    => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.KeyValuePair)
                    => new Guid("02B51929-C1C4-4A7E-8940-0312B5C18500"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IEnumerable)
                    => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IEnumerable1)
                    => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IEnumerator)
                    => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IEnumerator1)
                    => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.AsyncOperationWithProgressCompletedHandler2)
                    => new Guid("c2d078d8-ac47-55ab-83e8-123b2be5bc5a"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.AsyncOperationCompletedHandler1)
                    => new Guid("9d534225-231f-55e7-a6d0-6c938e2d9160"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.MapChangedEventHandler2)
                    => new Guid("19046f0b-cf81-5dec-bbb2-7cc250da8b8b"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IList1)
                    => new Guid("0e3f106f-a266-50a1-8043-c90fcf3844f6"), // Unsure of this one
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IReadOnlyList1)
                    => new Guid("5f07498b-8e14-556e-9d2e-2e98d5615da9"), // Unsure of this one
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IReadOnlyDictionary2)
                    => new Guid("b78f0653-fa89-59cf-ba95-726938aae666"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.IDictionary2)
                    => new Guid("9962cd50-09d5-5c46-b1e1-3c679c1c8fae"),
                _ when SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.Nullable)
                    => new Guid("61c17706-2d65-11e0-9ae8-d48564015472"),
                _ => Guid.Empty
            };
        }
        return Guid.Empty;
    }
}
