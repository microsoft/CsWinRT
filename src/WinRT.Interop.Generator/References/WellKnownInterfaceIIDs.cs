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

    /// <summary>
    /// Attempts to resolve a well-known Windows Runtime interface GUID for the specified type signature.
    /// </summary>
    /// <param name="interfaceType"> The <see cref="ITypeDescriptor"/> representing the managed type to inspect</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="guid">Out parameter for the resolved <see cref="Guid"/> of the type. </param>
    /// <returns><c>true</c> if a matching GUID was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetGUID(
        ITypeDescriptor interfaceType,
        InteropReferences interopReferences,
        out Guid guid)
    {
        guid = interfaceType switch
        {
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler1)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler2)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.KeyValuePair2)
                => new Guid("02B51929-C1C4-4A7E-8940-0312B5C18500"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable)
                => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable1)
                => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator)
                => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator1)
                => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationWithProgressCompletedHandler2)
                => new Guid("C2D078D8-AC47-55AB-83E8-123B2BE5BC5A"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationCompletedHandler1)
                => new Guid("9D534225-231F-55E7-A6D0-6C938E2D9160"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.MapChangedEventHandler2)
                => new Guid("19046F0B-CF81-5DEC-BBB2-7CC250DA8B8B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList)
                => new Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList1)
                => new Guid("0E3F106F-A266-50A1-8043-C90FCF3844F6"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IReadOnlyList1)
                => new Guid("5F07498B-8E14-556E-9D2E-2E98D5615DA9"), // Unsure of this one
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IReadOnlyDictionary2)
                => new Guid("B78F0653-FA89-59CF-BA95-726938AAE666"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IDictionary2)
                => new Guid("9962CD50-09D5-5C46-B1E1-3C679C1C8FAE"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.Nullable1)
                => new Guid("61C17706-2D65-11E0-9AE8-D48564015472"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncActionWithProgress1)
                => new Guid("DD725452-2DA3-5103-9C7D-22EE9BB14AD3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncOperationWithProgress2)
                => new Guid("94645425-B9E5-5B91-B509-8DA4DF6A8916"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationProgressHandler1)
                => new Guid("264F1E0C-ABE4-590B-9D37-E1CC118ECC75"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationProgressHandler2)
                => new Guid("264F1E0C-ABE4-590B-9D37-E1CC118ECC75"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncActionWithProgressCompletedHandler1)
                => new Guid("9A0D211C-0374-5D23-9E15-EAA3570FAE63"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IObservableVector1)
                => new Guid("D24C289F-2341-5128-AAA1-292DD0DC1950"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IObservableMap2)
                => new Guid("75F99E2A-137E-537E-A5B1-0B5A6245FC02"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncOperation1)
                => new Guid("2BD35EE6-72D9-5C5D-9827-05EBB81487AB"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.VectorChangedEventHandler1)
                => new Guid("A1E9ACD7-E4DF-5A79-AEFA-DE07934AB0FB"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncActionProgressHandler1)
                => new Guid("C261D8D0-71BA-5F38-A239-872342253A18"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IVectorChangedEventArgs)
                => new Guid("575933DF-34FE-4480-AF15-07691F3D5D9B"),
            _ => Guid.Empty
        };

        return guid != Guid.Empty;
    }
}
