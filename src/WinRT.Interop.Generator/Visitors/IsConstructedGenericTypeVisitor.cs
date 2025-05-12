// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Visitors;

/// <summary>
/// An <see cref="ITypeSignatureVisitor{TResult}"/> that checks if a type signature represents a fully constructed generic type.
/// </summary>
internal sealed class IsConstructedGenericTypeVisitor : ITypeSignatureVisitor<bool>
{
    /// <summary>
    /// Creates a new <see cref="IsConstructedGenericTypeVisitor"/> instance.
    /// </summary>
    private IsConstructedGenericTypeVisitor()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="IsConstructedGenericTypeVisitor"/> instance.
    /// </summary>
    public static IsConstructedGenericTypeVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public bool VisitArrayType(ArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitBoxedType(BoxedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitByReferenceType(ByReferenceTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitCorLibType(CorLibTypeSignature signature)
    {
        return false;
    }

    /// <inheritdoc/>
    public bool VisitCustomModifierType(CustomModifierTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitGenericInstanceType(GenericInstanceTypeSignature signature)
    {
        // Check that all type arguments are constructed
        foreach (TypeSignature typeArgument in signature.TypeArguments)
        {
            if (!typeArgument.AcceptVisitor(this))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool VisitGenericParameter(GenericParameterSignature signature)
    {
        // By definition, generic parameter are not constructed
        return false;
    }

    /// <inheritdoc/>
    public bool VisitPinnedType(PinnedTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitPointerType(PointerTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitSentinelType(SentinelTypeSignature signature)
    {
        return true;
    }

    /// <inheritdoc/>
    public bool VisitSzArrayType(SzArrayTypeSignature signature)
    {
        return signature.BaseType.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public bool VisitTypeDefOrRef(TypeDefOrRefSignature signature)
    {
        // Type definitions and references are valid constructed type arguments
        return true;
    }

    /// <inheritdoc/>
    public bool VisitFunctionPointerType(FunctionPointerTypeSignature signature)
    {
        // Check that the return type is a constructed type
        if (!signature.Signature.ReturnType.AcceptVisitor(this))
        {
            return false;
        }

        // Check that all parameter types are also constructed
        foreach (TypeSignature parameterType in signature.Signature.ParameterTypes)
        {
            if (!parameterType.AcceptVisitor(this))
            {
                return false;
            }
        }

        return true;
    }
}
