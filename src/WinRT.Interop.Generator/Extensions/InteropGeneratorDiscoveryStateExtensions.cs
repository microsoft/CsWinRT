// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="InteropGeneratorDiscoveryState"/>.
/// </summary>
internal static class InteropGeneratorDiscoveryStateExtensions
{
    /// <summary>
    /// Tracks a generic interface type of any projected or custom-mapped type.
    /// </summary>
    /// <param name="discoveryState">The current <see cref="InteropGeneratorDiscoveryState"/> instance.</param>
    /// <param name="typeSignature">The generic interface type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    public static void TrackGenericInterfaceType(
        this InteropGeneratorDiscoveryState discoveryState,
        GenericInstanceTypeSignature typeSignature,
        InteropReferences interopReferences)
    {
        if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerator1))
        {
            discoveryState.TrackIEnumerator1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerable1))
        {
            discoveryState.TrackIEnumerable1Type(typeSignature);

            // We need special handling for 'IEnumerator<T>' types whenever we discover any constructed 'IEnumerable<T>'
            // type. This ensures that we're never missing any 'IEnumerator<T>' instantiation, which we might depend on
            // from other generated code, or projections. This special handling is needed because unlike with the other
            // interfaces, 'IEnumerator<T>' will not show up as a base interface for other collection interface types.
            discoveryState.TrackIEnumerator1Type(interopReferences.IEnumerator1.MakeGenericReferenceType(typeSignature.TypeArguments[0]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IList1))
        {
            discoveryState.TrackIList1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyList1))
        {
            discoveryState.TrackIReadOnlyList1Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IDictionary2))
        {
            discoveryState.TrackIDictionary2Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyDictionary2))
        {
            discoveryState.TrackIReadOnlyDictionary2Type(typeSignature);
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IObservableVector1))
        {
            discoveryState.TrackIObservableVector1Type(typeSignature);

            // We need special handling for constructed 'VectorChangedEventHandler<T>' types, as those are required for each
            // discovered 'IObservableVector<T>' type. These are not necessarily discovered in the same way, as while we are
            // recursively constructing interfaces, we don't have the same logic for delegate types (or for types used in
            // any signature of interface members). Because we only need this delegate type and the one below, we can just
            // special case it. That is, we manually construct it every time we discover a constructed 'IObservableVector<T>'.
            discoveryState.TrackGenericDelegateType(interopReferences.VectorChangedEventHandler1.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IObservableMap2))
        {
            discoveryState.TrackIObservableMap2Type(typeSignature);

            // Same handling as below for 'MapChangedEventHandler<K,V>' types
            discoveryState.TrackGenericDelegateType(interopReferences.MapChangedEventHandler2.MakeGenericReferenceType([.. typeSignature.TypeArguments]));
        }
        else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IMapChangedEventArgs1))
        {
            discoveryState.TrackIMapChangedEventArgs1Type(typeSignature);
        }
    }
}
