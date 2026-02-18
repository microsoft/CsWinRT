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
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IBuffer)
                => "Windows_Storage_Streams_IBuffer",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IInputStream)
                => "Windows_Storage_Streams_IInputStream",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IOutputStream)
                => "Windows_Storage_Streams_IOutputStream",
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IRandomAccessStream)
                => "Windows_Storage_Streams_IRandomAccessStream",

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
                => new Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler)
                => new Guid("C50898F6-C536-5F47-8583-8B2C2438A13B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler1)
                => new Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.EventHandler2)
                => new Guid("9DE1C534-6AE1-11E0-84E1-18A905BCC53F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.KeyValuePair2)
                => new Guid("02B51929-C1C4-4A7E-8940-0312B5C18500"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable1)
                => new Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator1)
                => new Guid("6A79E863-4300-459A-9966-CBB660963EE1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationWithProgressCompletedHandler2)
                => new Guid("E85DF41D-6AA7-46E3-A8E2-F009D840C627"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationCompletedHandler1)
                => new Guid("FCDCF02C-E5D8-4478-915A-4D90B74B83A5"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.MapChangedEventHandler2)
                => new Guid("179517F3-94EE-41F8-BDDC-768A895544F3"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IList1)
                => new Guid("913337E9-11A1-4345-A3A2-4E7F956E222D"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IReadOnlyList1)
                => new Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IReadOnlyDictionary2)
                => new Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IDictionary2)
                => new Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.Nullable1)
                => new Guid("61C17706-2D65-11E0-9AE8-D48564015472"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncActionWithProgress1)
                => new Guid("1F6DB258-E803-48A1-9546-EB7353398884"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncOperationWithProgress2)
                => new Guid("B5D036D7-E297-498F-BA60-0289E76E23DD"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncOperationProgressHandler2)
                => new Guid("55690902-0AAB-421A-8778-F8CE5026D758"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncActionWithProgressCompletedHandler1)
                => new Guid("9C029F91-CC84-44FD-AC26-0A6C4E555281"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IObservableVector1)
                => new Guid("5917EB53-50B4-4A0D-B309-65862B3F1DBC"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IObservableMap2)
                => new Guid("65DF2BF5-BF39-41B5-AEBC-5A9D865E472B"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IAsyncOperation1)
                => new Guid("9FC2B0BB-E446-44E2-AA61-9CAB8F636AF2"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.VectorChangedEventHandler1)
                => new Guid("0C051752-9FBF-4C70-AA0C-0E4C82D9A761"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.AsyncActionProgressHandler1)
                => new Guid("6D844858-0CFF-4590-AE89-95A5A5C8B4B8"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IMapChangedEventArgs1)
                => new Guid("9939F4DF-050A-4C0F-AA60-77075F9C4777"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IVectorChangedEventArgs)
                => new Guid("575933DF-34FE-4480-AF15-07691F3D5D9B"),

            // XAML types
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerable)
                => new Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F"),
            _ when SignatureComparer.IgnoreVersion.Equals(interfaceType, interopReferences.IEnumerator)
                => new Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331"),
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
