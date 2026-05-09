// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Mirrors C++ <c>method_signature</c>: enumerates parameters and return value of a method.
/// </summary>
internal sealed class MethodSig
{
    public MethodDefinition Method { get; }
    public List<ParamInfo> Params { get; }
    public ParameterDefinition? ReturnParam { get; }

    public MethodSig(MethodDefinition method) : this(method, null) { }

    public MethodSig(MethodDefinition method, GenericContext? genCtx)
    {
        Method = method;
        Params = new List<ParamInfo>(method.Parameters.Count);
        // The return parameter is the one with sequence 0 (if any)
        ReturnParam = null;
        foreach (ParameterDefinition p in method.ParameterDefinitions)
        {
            if (p.Sequence == 0)
            {
                ReturnParam = p;
                break;
            }
        }

        // Iterate signature parameters
        if (method.Signature is MethodSignature sig)
        {
            _substitutedReturnType = genCtx is not null && sig.ReturnType is not null
                ? sig.ReturnType.InstantiateGenericTypes(genCtx.Value)
                : sig.ReturnType;
            for (int i = 0; i < sig.ParameterTypes.Count; i++)
            {
                TypeSignature pt = sig.ParameterTypes[i];
                if (genCtx is not null) { pt = pt.InstantiateGenericTypes(genCtx.Value); }
                Params.Add(new ParamInfo(method.Parameters[i], pt));
            }
        }
    }

#pragma warning disable IDE0032 // Use auto property — manual backing field needed for substituted return type
    private readonly TypeSignature? _substitutedReturnType;
#pragma warning restore IDE0032

    public TypeSignature? ReturnType => _substitutedReturnType is TypeSignature t &&
                                        t is not CorLibTypeSignature { ElementType: ElementType.Void }
                                          ? _substitutedReturnType
                                          : null;

    public string ReturnParamName(string defaultName = "__return_value__")
        => ReturnParam?.Name?.Value ?? defaultName;
}
