// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.References;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Resolved view of a method definition's signature: the method itself, the per-parameter info
/// (with any active generic-context substitutions already applied), the optional return parameter,
/// and the substituted return type.
/// </summary>
internal sealed class MethodSignatureInfo
{
    /// <summary>
    /// Gets the underlying method definition.
    /// </summary>
    public MethodDefinition Method { get; }

    /// <summary>
    /// Gets the per-parameter info for the method, in declaration order.
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters => _params;

    private readonly List<ParameterInfo> _params;

    /// <summary>
    /// Gets the parameter definition with sequence 0 (the return parameter), or <see langword="null"/>
    /// if the method does not have one.
    /// </summary>
    public ParameterDefinition? ReturnParameter { get; }

    /// <summary>
    /// Gets the (possibly generic-context-substituted) return type of the method, or
    /// <see langword="null"/> when the method returns <see langword="void"/>.
    /// </summary>
    public TypeSignature? ReturnType { get; }

    /// <summary>
    /// Initializes a new <see cref="MethodSignatureInfo"/> with no generic context.
    /// </summary>
    /// <param name="method">The method definition to wrap.</param>
    public MethodSignatureInfo(MethodDefinition method) : this(method, null) { }

    /// <summary>
    /// Initializes a new <see cref="MethodSignatureInfo"/>.
    /// </summary>
    /// <param name="method">The method definition to wrap.</param>
    /// <param name="genericContext">An optional generic context used to substitute generic parameters in the parameter and return types.</param>
    [SuppressMessage("Style", "IDE0028:Use collection expression",
        Justification = "List<ParameterInfo>(capacity) cannot be expressed as a collection expression.")]
    public MethodSignatureInfo(MethodDefinition method, GenericContext? genericContext)
    {
        Method = method;
        _params = new List<ParameterInfo>(method.Parameters.Count);
        ReturnParameter = null;
        foreach (ParameterDefinition p in method.ParameterDefinitions)
        {
            if (p.Sequence == 0)
            {
                ReturnParameter = p;
                break;
            }
        }

        if (method.Signature is MethodSignature sig)
        {
            TypeSignature? rt = sig.ReturnType;

            if (rt is not null && genericContext is not null)
            {
                rt = rt.InstantiateGenericTypes(genericContext.Value);
            }

            ReturnType = rt is CorLibTypeSignature { ElementType: ElementType.Void } ? null : rt;

            for (int i = 0; i < sig.ParameterTypes.Count; i++)
            {
                TypeSignature pt = sig.ParameterTypes[i];

                if (genericContext is not null)
                {
                    pt = pt.InstantiateGenericTypes(genericContext.Value);
                }

                _params.Add(new ParameterInfo(method.Parameters[i], pt));
            }
        }
    }

    /// <summary>
    /// Returns the name of the return parameter, or <paramref name="defaultName"/> if there is none.
    /// </summary>
    /// <param name="defaultName">The default name to use when no return parameter is declared.</param>
    /// <returns>The return parameter name (or default).</returns>
    public string ReturnParameterName(string defaultName = ProjectionNames.DefaultReturnParameterName)
        => ReturnParameter?.Name?.Value ?? defaultName;
}
