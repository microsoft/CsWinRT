// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Resolvers;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Pre-computed marshalling flags for a <see cref="MethodSignatureInfo"/> used by the
/// RCW-caller and Do_Abi body emitters. Bundles the return-shape classification and the
/// per-parameter-shape probes that drive try/finally emission, so the calling code only
/// needs to walk the parameter list once instead of re-deriving each flag inline.
/// </summary>
/// <param name="ReturnShape">The resolved <see cref="AbiTypeKind"/> of the return type, or <see cref="AbiTypeKind.Unknown"/> when the method returns void.</param>
/// <param name="ReturnIsString">True when the return type is <see cref="System.String"/>.</param>
/// <param name="ReturnIsRefType">True when the return type's shape is reference-typed (per <see cref="AbiTypeKindExtensions"/>).</param>
/// <param name="ReturnIsBlittableStruct">True when the return type is a blittable struct.</param>
/// <param name="ReturnIsComplexStruct">True when the return type is a complex struct.</param>
/// <param name="ReturnIsReceiveArray">True when the return type is an SZ-array whose element is a known ABI element shape (blittable, ref-like, complex struct, HResult-exception, or mapped value type).</param>
/// <param name="ReturnIsHResultException">True when the return type is <see cref="System.Exception"/> (mapped from WinRT <c>HResult</c>).</param>
/// <param name="ReturnIsSystemTypeForCleanup">True when the return type is <see cref="System.Type"/>; its ABI form holds an HSTRING that must be disposed.</param>
/// <param name="HasOutNeedsCleanup">True when at least one Out parameter's underlying type carries a resource that requires post-call cleanup (ref-like array element, System.Type, complex struct, or generic instance).</param>
/// <param name="HasReceiveArray">True when at least one parameter has <c>ReceiveArray</c> category.</param>
/// <param name="HasNonBlittablePassArray">True when at least one parameter has Pass/Fill-array category and its element type is neither blittable nor mapped value type.</param>
/// <param name="HasComplexStructInput">True when at least one In/Ref parameter's underlying type is a complex struct (needs marshaller initialisation).</param>
internal readonly record struct MethodSignatureMarshallingFacts(
    AbiTypeKind ReturnShape,
    bool ReturnIsString,
    bool ReturnIsRefType,
    bool ReturnIsBlittableStruct,
    bool ReturnIsComplexStruct,
    bool ReturnIsReceiveArray,
    bool ReturnIsHResultException,
    bool ReturnIsSystemTypeForCleanup,
    bool HasOutNeedsCleanup,
    bool HasReceiveArray,
    bool HasNonBlittablePassArray,
    bool HasComplexStructInput)
{
    /// <summary>
    /// True when the emitted RCW-caller body must wrap the vtable call in a <c>try</c>/<c>finally</c>
    /// to cover any of the resource-cleanup paths (return-side or parameter-side).
    /// </summary>
    public bool NeedsTryFinally =>
        ReturnIsString || ReturnIsRefType || ReturnIsReceiveArray || HasOutNeedsCleanup
        || HasReceiveArray || ReturnIsComplexStruct || HasNonBlittablePassArray
        || HasComplexStructInput || ReturnIsSystemTypeForCleanup;

    /// <summary>
    /// Computes the marshalling facts for <paramref name="sig"/> using <paramref name="resolver"/>
    /// for shape classification.
    /// </summary>
    public static MethodSignatureMarshallingFacts From(MethodSignatureInfo sig, AbiTypeKindResolver resolver)
    {
        TypeSignature? rt = sig.ReturnType;
        AbiTypeKind returnShape = rt is null ? AbiTypeKind.Unknown : resolver.Resolve(rt);

        bool returnIsString = returnShape == AbiTypeKind.String;
        bool returnIsRefType = returnShape.IsReferenceType();
        bool returnIsBlittableStruct = returnShape == AbiTypeKind.BlittableStruct;
        bool returnIsComplexStruct = returnShape == AbiTypeKind.ComplexStruct;
        bool returnIsReceiveArray = rt is SzArrayTypeSignature retSz
            && resolver.IsRecognizedReceiveArrayElement(retSz.BaseType);
        bool returnIsHResultException = returnShape == AbiTypeKind.HResultException;
        bool returnIsSystemTypeForCleanup = rt is not null && rt.IsSystemType();

        bool hasOutNeedsCleanup = false;
        bool hasReceiveArray = false;
        bool hasNonBlittablePassArray = false;
        bool hasComplexStructInput = false;

        foreach ((_, ParameterInfo p, ParameterCategory cat) in sig.EnumerateWithCategory())
        {
            if (!hasOutNeedsCleanup && cat == ParameterCategory.Out
                && resolver.RequiresOutParameterCleanup(p.Type.StripByRefAndCustomModifiers()))
            {
                hasOutNeedsCleanup = true;
            }

            if (!hasReceiveArray && cat == ParameterCategory.ReceiveArray)
            {
                hasReceiveArray = true;
            }

            if (!hasNonBlittablePassArray && cat.IsArrayInput()
                && p.Type is SzArrayTypeSignature szArr
                && !resolver.IsDirectPassArrayElement(szArr.BaseType))
            {
                hasNonBlittablePassArray = true;
            }

            if (!hasComplexStructInput && cat.IsScalarInput()
                && resolver.IsComplexStruct(p.Type.StripByRefAndCustomModifiers()))
            {
                hasComplexStructInput = true;
            }
        }

        return new MethodSignatureMarshallingFacts(
            ReturnShape: returnShape,
            ReturnIsString: returnIsString,
            ReturnIsRefType: returnIsRefType,
            ReturnIsBlittableStruct: returnIsBlittableStruct,
            ReturnIsComplexStruct: returnIsComplexStruct,
            ReturnIsReceiveArray: returnIsReceiveArray,
            ReturnIsHResultException: returnIsHResultException,
            ReturnIsSystemTypeForCleanup: returnIsSystemTypeForCleanup,
            HasOutNeedsCleanup: hasOutNeedsCleanup,
            HasReceiveArray: hasReceiveArray,
            HasNonBlittablePassArray: hasNonBlittablePassArray,
            HasComplexStructInput: hasComplexStructInput);
    }
}