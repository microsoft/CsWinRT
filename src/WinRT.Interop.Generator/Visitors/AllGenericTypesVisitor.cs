// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Visitors;

/// <summary>
/// An <see cref="ITypeSignatureVisitor{TResult}"/> that recursively visits all generic types in a signature.
/// </summary>
internal sealed class AllGenericTypesVisitor : ITypeSignatureVisitor<IEnumerable<GenericInstanceTypeSignature>>
{
    /// <summary>
    /// Creates a new <see cref="AllGenericTypesVisitor"/> instance.
    /// </summary>
    private AllGenericTypesVisitor()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="AllGenericTypesVisitor"/> instance.
    /// </summary>
    public static AllGenericTypesVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitArrayType(ArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitBoxedType(BoxedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitByReferenceType(ByReferenceTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitCorLibType(CorLibTypeSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitCustomModifierType(CustomModifierTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitGenericInstanceType(GenericInstanceTypeSignature signature)
    {
        // This (and the method visiting function pointers below) are the reason this visitor exists. It recursively
        // returns all signatures of combined generics. For instance, if it finds 'List<(int, string, List<bool>)>',
        // it will give back 'List<(int, string, List<bool>)>', '(int, string, List<bool>>), and 'List<bool>' separately.
        return signature.TypeArguments.SelectMany(static arg => arg.AcceptVisitor(Instance)).Append(signature);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitGenericParameter(GenericParameterSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitPinnedType(PinnedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitPointerType(PointerTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitSentinelType(SentinelTypeSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitSzArrayType(SzArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitTypeDefOrRef(TypeDefOrRefSignature signature)
    {
        return [];
    }

    /// <inheritdoc/>
    public IEnumerable<GenericInstanceTypeSignature> VisitFunctionPointerType(FunctionPointerTypeSignature signature)
    {
        // Visit both the return type, and all parameter types for the function pointer
        return signature.Signature.ReturnType.AcceptVisitor(this).Concat(
            signature.Signature.ParameterTypes.SelectMany(static param => param.AcceptVisitor(Instance)));
    }
}