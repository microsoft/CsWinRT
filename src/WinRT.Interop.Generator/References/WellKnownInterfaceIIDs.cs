// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
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

    public static Boolean get_GUID(
        TypeSignature signature,
        bool useWindowsUIXamlProjections,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out Guid guid)
    {
        ITypeDefOrRef type = signature switch
        {
            SzArrayTypeSignature szArrayTypeSignature => szArrayTypeSignature.GetUnderlyingType().ToTypeDefOrRef(),
            GenericInstanceTypeSignature genericSignature => genericSignature.GenericType,
            _ => signature.ToTypeDefOrRef(),
        };

        guid = type switch
        {
            // Shared types
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler)
                => new Guid("9de1c535-6ae1-11e0-84e1-18a905bcc53f"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler1)
                => new Guid("9de1c535-6ae1-11e0-84e1-18a905bcc53f"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler2)
                => new Guid("9de1c535-6ae1-11e0-84e1-18a905bcc53f"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.KeyValuePair2)
                => new Guid("02b51929-c1c4-4a7e-8940-0312b5c18500"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerable)
                => new Guid("faa585ea-6214-4217-afda-7f46de5869b3"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerable1)
                => new Guid("faa585ea-6214-4217-afda-7f46de5869b3"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerator)
                => new Guid("6a79e863-4300-459a-9966-cbb660963ee1"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerator1)
                => new Guid("6a79e863-4300-459a-9966-cbb660963ee1"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationWithProgressCompletedHandler2)
                => new Guid("c2d078d8-ac47-55ab-83e8-123b2be5bc5a"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationCompletedHandler1)
                => new Guid("9d534225-231f-55e7-a6d0-6c938e2d9160"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.MapChangedEventHandler2)
                => new Guid("19046f0b-cf81-5dec-bbb2-7cc250da8b8b"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IList)
                => new Guid("393de7de-6fd0-4c0d-bb71-47244a113e93"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IList1)
                => new Guid("0e3f106f-a266-50a1-8043-c90fcf3844f6"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IReadOnlyList1)
                => new Guid("5f07498b-8e14-556e-9d2e-2e98d5615da9"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IReadOnlyDictionary2)
                => new Guid("b78f0653-fa89-59cf-ba95-726938aae666"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IDictionary2)
                => new Guid("9962cd50-09d5-5c46-b1e1-3c679c1c8fae"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Nullable)
                => new Guid("61c17706-2d65-11e0-9ae8-d48564015472"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncActionWithProgress1)
                => new Guid("dd725452-2da3-5103-9c7d-22ee9bb14ad3"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncOperationWithProgress2)
                => new Guid("94645425-b9e5-5b91-b509-8da4df6a8916"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationProgressHandler1)
                => new Guid("264f1e0c-abe4-590b-9d37-e1cc118ecc75"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationProgressHandler2)
                => new Guid("264f1e0c-abe4-590b-9d37-e1cc118ecc75"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncActionWithProgressCompletedHandler1)
                => new Guid("9a0d211c-0374-5d23-9e15-eaa3570fae63"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IObservableVector1)
                => new Guid("d24c289f-2341-5128-aaa1-292dd0dc1950"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IObservableMap2)
                => new Guid("75f99e2a-137e-537e-a5b1-0b5a6245fc02"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncOperation1)
                => new Guid("2bd35ee6-72d9-5c5d-9827-05ebb81487ab"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.VectorChangedEventHandler1)
                => new Guid("a1e9acd7-e4df-5a79-aefa-de07934ab0fb"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncActionProgressHandler1)
                => new Guid("c261d8d0-71ba-5f38-a239-872342253a18"),
            _ when SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IVectorChangedEventArgs)
                => new Guid("575933df-34fe-4480-af15-07691f3d5d9b"),
            _ => Guid.Empty
        };

        return guid != Guid.Empty;
    }
}



