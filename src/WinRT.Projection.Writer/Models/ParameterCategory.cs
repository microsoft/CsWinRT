// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>Param category mirroring C++ <c>param_category</c>.</summary>
internal enum ParamCategory
{
    In,
    Ref,
    Out,
    PassArray,
    FillArray,
    ReceiveArray,
}

/// <summary>Helpers for parameter analysis.</summary>
internal static class ParamHelpers
{
    public static ParamCategory GetParamCategory(ParamInfo p)
    {
        bool isArray = p.Type is SzArrayTypeSignature;
        bool isOut = p.Parameter.Definition?.IsOut == true;
        bool isIn = p.Parameter.Definition?.IsIn == true;
        // Check both the captured signature type and the parameter's own type (handles cases where
        // the signature is wrapped in a ByReferenceTypeSignature only on one side after substitution).
        // Also peel custom modifiers (e.g. modreq[InAttribute]) which can hide a ByRef beneath.
        bool isByRef = IsByRefType(p.Type) || IsByRefType(p.Parameter.ParameterType);
        // If byref and underlying is an array, treat as array param (PassArray/ReceiveArray/FillArray)
        // based on in/out flags. WinRT metadata represents 'out byte[]' as 'byte[]&' with [out].
        bool isByRefArray = isByRef && PeelByRefAndCustomModifiers(p.Type) is SzArrayTypeSignature;
        if (isArray || isByRefArray)
        {
            if (isIn) { return ParamCategory.PassArray; }
            if (isByRef && isOut) { return ParamCategory.ReceiveArray; }
            return ParamCategory.FillArray;
        }
        if (isOut) { return ParamCategory.Out; }
        if (isByRef) { return ParamCategory.Ref; }
        return ParamCategory.In;
    }

    private static TypeSignature? PeelByRefAndCustomModifiers(TypeSignature? sig)
    {
        TypeSignature? cur = sig;
        while (true)
        {
            if (cur is CustomModifierTypeSignature cm) { cur = cm.BaseType; continue; }
            if (cur is ByReferenceTypeSignature br) { cur = br.BaseType; continue; }
            break;
        }
        return cur;
    }

    private static bool IsByRefType(TypeSignature? sig)
    {
        // Strip custom modifiers (e.g. modreq[InAttribute] or modopt[IsExternalInit]) before checking byref.
        TypeSignature? cur = sig;
        while (cur is CustomModifierTypeSignature cm)
        {
            cur = cm.BaseType;
        }
        return cur is ByReferenceTypeSignature;
    }
}
