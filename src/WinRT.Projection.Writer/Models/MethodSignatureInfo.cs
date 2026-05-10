// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Resolved view of a method definition's signature: the method itself, the per-parameter info
/// (with any active generic-context substitutions already applied), the optional return parameter,
/// and the substituted return type.
/// </summary>
internal sealed class MethodSig
{
    /// <summary>Gets the underlying method definition.</summary>
    public MethodDefinition Method { get; }

    /// <summary>Gets the per-parameter info for the method, in declaration order.</summary>
    public List<ParamInfo> Params { get; }

    /// <summary>
    /// Gets the parameter definition with sequence 0 (the return parameter), or <see langword="null"/>
    /// if the method does not have one.
    /// </summary>
    public ParameterDefinition? ReturnParam { get; }

    /// <summary>
    /// Initializes a new <see cref="MethodSig"/> with no generic context.
    /// </summary>
    /// <param name="method">The method definition to wrap.</param>
    public MethodSig(MethodDefinition method) : this(method, null) { }

    /// <summary>
    /// Initializes a new <see cref="MethodSig"/>.
    /// </summary>
    /// <param name="method">The method definition to wrap.</param>
    /// <param name="genCtx">An optional generic context used to substitute generic parameters in the parameter and return types.</param>
    public MethodSig(MethodDefinition method, GenericContext? genCtx)
    {
        Method = method;
#pragma warning disable IDE0028 // Use collection expression -- intentional capacity hint
        Params = new List<ParamInfo>(method.Parameters.Count);
#pragma warning restore IDE0028
        ReturnParam = null;
        foreach (ParameterDefinition p in method.ParameterDefinitions)
        {
            if (p.Sequence == 0)
            {
                ReturnParam = p;
                break;
            }
        }

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

    /// <summary>
    /// Gets the (possibly generic-context-substituted) return type of the method, or
    /// <see langword="null"/> when the method returns <see langword="void"/>.
    /// </summary>
    public TypeSignature? ReturnType => _substitutedReturnType is TypeSignature t &&
                                        t is not CorLibTypeSignature { ElementType: ElementType.Void }
                                          ? _substitutedReturnType
                                          : null;

    /// <summary>
    /// Returns the name of the return parameter, or <paramref name="defaultName"/> if there is none.
    /// </summary>
    /// <param name="defaultName">The default name to use when no return parameter is declared.</param>
    /// <returns>The return parameter name (or default).</returns>
    public string ReturnParamName(string defaultName = "__return_value__")
        => ReturnParam?.Name?.Value ?? defaultName;
}