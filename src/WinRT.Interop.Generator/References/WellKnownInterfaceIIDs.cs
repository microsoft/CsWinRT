// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
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
    /// Gets the IID for <c>IStringable</c> (96369F54-8EB6-48F0-ABCE-C1B211E627C3).
    /// </summary>
    public static Guid IID_IStringable { get; } = new("96369F54-8EB6-48F0-ABCE-C1B211E627C3");

    /// <summary>
    /// Gets the IID for <c>IWeakReferenceSource</c> (00000038-0000-0000-C000-000000000046).
    /// </summary>
    public static Guid IID_IWeakReferenceSource { get; } = new("00000038-0000-0000-C000-000000000046");

    /// <summary>
    /// Gets the IID for <c>IMarshal</c> (00000003-0000-0000-C000-000000000046).
    /// </summary>
    public static Guid IID_IMarshal { get; } = new("00000003-0000-0000-C000-000000000046");

    /// <summary>
    /// Gets the IID for <c>IAgileObject</c> (94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90).
    /// </summary>
    public static Guid IID_IAgileObject { get; } = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");

    /// <summary>
    /// Gets the IID for <c>IInspectable</c> (AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90).
    /// </summary>
    public static Guid IID_IInspectable { get; } = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");

    /// <summary>
    /// Gets the IID for <c>IUnknown</c> (00000000-0000-0000-C000-000000000046).
    /// </summary>
    public static Guid IID_IUnknown { get; } = new("00000000-0000-0000-C000-000000000046");

    /// <summary>
    /// Gets the mapping of IIDs to interface names, for reserved COM/WinRT interfaces that cannot be manually implemented.
    /// </summary>
    public static FrozenDictionary<Guid, string> ReservedIIDsMap { get; } = FrozenDictionary.Create(
        new KeyValuePair<Guid, string>(IID_IStringable, nameof(IID_IStringable)[4..]),
        new KeyValuePair<Guid, string>(IID_IWeakReferenceSource, nameof(IID_IWeakReferenceSource)[4..]),
        new KeyValuePair<Guid, string>(IID_IAgileObject, nameof(IID_IAgileObject)[4..]),
        new KeyValuePair<Guid, string>(IID_IInspectable, nameof(IID_IInspectable)[4..]),
        new KeyValuePair<Guid, string>(IID_IUnknown, nameof(IID_IUnknown)[4..]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>get_IID_...</c> method corresponding to the specified <paramref name="interfaceType"/>.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The <see cref="MemberReference"/> for the <c>get_IID_...</c> method for <paramref name="interfaceType"/>.</returns>
    /// <exception cref="System.NullReferenceException"></exception>
    /// <remarks>
    /// The types handled by this method should be kept in sync with
    /// <see cref="WindowsRuntimeExtensions.IsCustomMappedWindowsRuntimeNonGenericInterfaceType"/> and
    /// <see cref="WindowsRuntimeExtensions.IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType"/>.
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
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncInfo)
               => "Windows_Foundation_IAsyncInfo",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncAction)
                => "Windows_Foundation_IAsyncAction",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IVectorChangedEventArgs)
                => "Windows_Foundation_Collections_IVectorChangedEventArgs",

            // XAML types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableIterable",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable)
                => "Microsoft_UI_Xaml_Interop_IBindableIterable",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableIterator",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator)
                => "Microsoft_UI_Xaml_Interop_IBindableIterator",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableVector",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList)
                => "Microsoft_UI_Xaml_Interop_IBindableVector",
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
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IServiceProvider)
                => "Microsoft_UI_Xaml_IXamlServiceProvider",
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
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="guid">Out parameter for the resolved <see cref="Guid"/> of the type. </param>
    /// <returns><c>true</c> if a matching GUID was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetGUID(
        ITypeDescriptor interfaceType,
        bool useWindowsUIXamlProjections,
        InteropReferences interopReferences,
        out Guid guid)
    {
        guid = interfaceType switch
        {
            // Shared types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IDisposable)
                => new Guid("1BFCA4F6-2C4E-5174-9869-B39D35848FCC"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler1)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler2)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.KeyValuePair2)
                => new Guid("02B51929-C1C4-4A7E-8940-0312B5C18500"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable1)
                => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator1)
                => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationWithProgressCompletedHandler2)
                => new Guid("C2D078D8-AC47-55AB-83E8-123B2BE5BC5A"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationCompletedHandler1)
                => new Guid("9D534225-231F-55E7-A6D0-6C938E2D9160"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.MapChangedEventHandler2)
                => new Guid("19046F0B-CF81-5DEC-BBB2-7CC250DA8B8B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList1)
                => new Guid("0E3F106F-A266-50A1-8043-C90FCF3844F6"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IReadOnlyList1)
                => new Guid("5F07498B-8E14-556E-9D2E-2E98D5615DA9"),
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
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IMapChangedEventArgs1)
                => new Guid("9939F4DF-050A-4C0F-AA60-77075F9C4777"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IVectorChangedEventArgs)
                => new Guid("575933DF-34FE-4480-AF15-07691F3D5D9B"),

            // XAML types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable)
                => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator)
                => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList)
                => new Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyCollectionChanged) && useWindowsUIXamlProjections
                => new Guid("28B167D5-1A31-465B-9B25-D5C3AE686C40"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyCollectionChanged)
                => new Guid("530155E1-28A5-5693-87CE-30724D95A06D"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyPropertyChanged) && useWindowsUIXamlProjections
                => new Guid("CF75D69C-F2F4-486B-B302-BB4C09BAEBFA"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyPropertyChanged)
                => new Guid("90B17601-B065-586E-83D9-9ADC3A695284"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.ICommand)
                => new Guid("E5AF3542-CA67-4081-995B-709DD13792DF"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.INotifyDataErrorInfo)
                => new Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IServiceProvider)
                => new Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB"),
            _ => Guid.Empty
        };

        return guid != Guid.Empty;
    }
}
