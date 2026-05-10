// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>Param category mirroring C++ <c>param_category</c>.</summary>
internal enum ParameterCategory
{
    In,
    Ref,
    Out,
    PassArray,
    FillArray,
    ReceiveArray,
}

/// <summary>Helpers for parameter analysis.</summary>
internal static class ParameterCategoryResolver
{
    public static ParameterCategory GetParamCategory(ParameterInfo p)
    {
        bool isArray = p.Type is SzArrayTypeSignature;
        bool isOut = p.Parameter.Definition?.IsOut == true;
        bool isIn = p.Parameter.Definition?.IsIn == true;
        // Check both the captured signature type and the parameter's own type (handles cases where
        // the signature is wrapped in a ByReferenceTypeSignature only on one side after substitution).
        // Also peel custom modifiers (e.g. modreq[InAttribute]) which can hide a ByRef beneath.
        bool isByRef = p.Type.IsByRefType() || p.Parameter.ParameterType.IsByRefType();
        // If byref and underlying is an array, treat as array param (PassArray/ReceiveArray/FillArray)
        // based on in/out flags. WinRT metadata represents 'out byte[]' as 'byte[]&' with [out].
        bool isByRefArray = isByRef && p.Type.StripByRefAndCustomModifiers() is SzArrayTypeSignature;
        if (isArray || isByRefArray)
        {
            if (isIn) { return ParameterCategory.PassArray; }
            if (isByRef && isOut) { return ParameterCategory.ReceiveArray; }
            return ParameterCategory.FillArray;
        }
        if (isOut) { return ParameterCategory.Out; }
        if (isByRef) { return ParameterCategory.Ref; }
        return ParameterCategory.In;
    }
}