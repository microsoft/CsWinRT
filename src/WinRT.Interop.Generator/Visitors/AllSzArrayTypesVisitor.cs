// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Visitors;

/// <summary>
/// An <see cref="ITypeSignatureVisitor{TResult}"/> that recursively visits all SZ array types in a signature.
/// </summary>
internal sealed class AllSzArrayTypesVisitor : ITypeSignatureVisitor<IEnumerable<SzArrayTypeSignature>>
{
    /// <summary>
    /// Creates a new <see cref="AllSzArrayTypesVisitor"/> instance.
    /// </summary>
    private AllSzArrayTypesVisitor()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="AllSzArrayTypesVisitor"/> instance.
    /// </summary>
    public static AllSzArrayTypesVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitArrayType(ArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitBoxedType(BoxedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitByReferenceType(ByReferenceTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitCorLibType(CorLibTypeSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitCustomModifierType(CustomModifierTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitGenericInstanceType(GenericInstanceTypeSignature signature)
    {
        // This will recursively visit all substituted arguments in an instantiated generic type signature. For
        // instance, if it finds 'List<(string[], string, List<int[]>)>', it will give give back 'string[]' and
        // also recurse on all other arguments, such that the 'int[]' type signature can also be discovered.
        return signature.TypeArguments.SelectMany(static arg => arg.AcceptVisitor(Instance));
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitGenericParameter(GenericParameterSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitPinnedType(PinnedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitPointerType(PointerTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitSentinelType(SentinelTypeSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitSzArrayType(SzArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this).Append(signature);
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitTypeDefOrRef(TypeDefOrRefSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<SzArrayTypeSignature> VisitFunctionPointerType(FunctionPointerTypeSignature signature)
    {
        // Visit both the return type, and all parameter types for the function pointer
        return signature.Signature.ReturnType.AcceptVisitor(this).Concat(
            signature.Signature.ParameterTypes.SelectMany(static param => param.AcceptVisitor(Instance)));
    }
}