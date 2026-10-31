// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator.References;
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
    /// Gets the IID shared by the UWP XAML and WinUI <c>ICustomPropertyProvider</c> interfaces.
    /// </summary>
    public static Guid IID_ICustomPropertyProvider { get; } = new("7C925755-3E48-42B4-8677-76372267033F");

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
    /// <exception cref="NullReferenceException"></exception>
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
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IActivationFactory)
                => "IActivationFactory",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IDisposable)
                => "Windows_Foundation_IClosable",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IAsyncInfo)
               => "Windows_Foundation_IAsyncInfo",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IAsyncAction)
                => "Windows_Foundation_IAsyncAction",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IVectorChangedEventArgs)
                => "Windows_Foundation_Collections_IVectorChangedEventArgs",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IStringable)
                => "IStringable",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IMemoryBufferReference)
                => "Windows_Foundation_IMemoryBufferReference",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IBuffer)
                => "Windows_Storage_Streams_IBuffer",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IInputStream)
                => "Windows_Storage_Streams_IInputStream",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IOutputStream)
                => "Windows_Storage_Streams_IOutputStream",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IRandomAccessStream)
                => "Windows_Storage_Streams_IRandomAccessStream",

            // XAML types
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerable) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableIterable",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerable)
                => "Microsoft_UI_Xaml_Interop_IBindableIterable",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerator) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableIterator",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerator)
                => "Microsoft_UI_Xaml_Interop_IBindableIterator",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IList) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_IBindableVector",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IList)
                => "Microsoft_UI_Xaml_Interop_IBindableVector",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyCollectionChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyCollectionChanged)
                => "Microsoft_UI_Xaml_Interop_INotifyCollectionChanged",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyPropertyChanged) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Data_INotifyPropertyChanged",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyPropertyChanged)
                => "Microsoft_UI_Xaml_Data_INotifyPropertyChanged",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.ICommand) && useWindowsUIXamlProjections
                => "Windows_UI_Xaml_Input_ICommand",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.ICommand)
                => "Microsoft_UI_Xaml_Input_ICommand",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyDataErrorInfo)
                => "Microsoft_UI_Xaml_Data_INotifyDataErrorInfo",
            _ when interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IServiceProvider)
                => "Microsoft_UI_Xaml_IXamlServiceProvider",
            _ => throw WellKnownInteropExceptions.InvalidCustomMappedTypeForWellKnownInterfaceIIDs(interfaceType)
        };

        // Create the member reference from 'WellKnownInterfaceIIDs'
        return interopReferences.WellKnownInterfaceIIDs.CreateMemberReference(
            name: $"get_IID_{nameSuffix}",
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
        // Validate the actual assembly identity before consulting the shared name-based constants
        if (interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IDisposable) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.Nullable1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.EventHandler) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.EventHandler1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.EventHandler2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IAsyncOperation1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IAsyncOperationWithProgress2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IAsyncActionWithProgress1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncActionCompletedHandler) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncActionProgressHandler1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncActionWithProgressCompletedHandler1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncOperationCompletedHandler1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncOperationProgressHandler2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.AsyncOperationWithProgressCompletedHandler2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.KeyValuePair2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerable1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerator1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IList1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IReadOnlyList1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IDictionary2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IReadOnlyDictionary2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IObservableVector1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IObservableMap2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IVectorChangedEventArgs) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IMapChangedEventArgs1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.VectorChangedEventHandler1) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.MapChangedEventHandler2) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerable) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IEnumerator) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IList) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyPropertyChanged) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyDataErrorInfo) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.INotifyCollectionChanged) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.ICommand) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.IServiceProvider) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.PropertyChangedEventHandler) ||
            interopReferences.SignatureComparer.Equals(interfaceType, interopReferences.NotifyCollectionChangedEventHandler))
        {
            return WellKnownWindowsRuntimeIids.TryGetIid(interfaceType.FullName, useWindowsUIXamlProjections, out guid);
        }

        guid = default;

        return false;
    }
}
