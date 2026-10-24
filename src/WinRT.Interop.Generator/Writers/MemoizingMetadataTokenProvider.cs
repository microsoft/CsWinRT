// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Writers;

/// <summary>
/// An <see cref="IMetadataTokenProvider"/> that remembers the tokens assigned to reference and specification members.
/// </summary>
/// <remarks>
/// <para>
/// The metadata builder deduplicates rows only after building them, so every operand naming the same member
/// again recomputes its parent tokens and serializes its signature. The first request for a member always goes
/// to the wrapped provider (so tokens and heap entries are assigned in the original order), and later requests
/// for the same object return that same token, exactly like the wrapped provider would.
/// </para>
/// <para>
/// A request is only remembered if it registered no error and did not mark the build as fatal, so failing requests
/// keep reporting their errors (with their own diagnostic source) every time. This requires the diagnostics to be
/// the error listener of the wrapped provider. Members must not be modified while the provider is in use, and each
/// instance must only be used for a single build, from a single thread.
/// </para>
/// </remarks>
internal sealed class MemoizingMetadataTokenProvider : IMetadataTokenProvider
{
    /// <summary>The tokens of all members that were successfully added, by object identity.</summary>
    private readonly Dictionary<IMetadataMember, MetadataToken> _tokens = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Creates a new <see cref="MemoizingMetadataTokenProvider"/> instance.
    /// </summary>
    /// <param name="inner">The wrapped token provider.</param>
    /// <param name="diagnostics">The error listener used by <paramref name="inner"/>.</param>
    public MemoizingMetadataTokenProvider(IMetadataTokenProvider inner, DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Inner = inner;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the wrapped token provider.
    /// </summary>
    public IMetadataTokenProvider Inner { get; }

    /// <summary>
    /// Gets the error listener used by <see cref="Inner"/>.
    /// </summary>
    public DiagnosticBag Diagnostics { get; }

    /// <inheritdoc/>
    public MetadataToken GetTypeReferenceToken(TypeReference type, object? diagnosticSource = null)
    {
        return GetOrAddToken(type, diagnosticSource, static (provider, type, source) => provider.GetTypeReferenceToken(type, source));
    }

    /// <inheritdoc/>
    public MetadataToken GetMemberReferenceToken(MemberReference member, object? diagnosticSource = null)
    {
        return GetOrAddToken(member, diagnosticSource, static (provider, member, source) => provider.GetMemberReferenceToken(member, source));
    }

    /// <inheritdoc/>
    public MetadataToken GetTypeSpecificationToken(TypeSpecification type, object? diagnosticSource = null)
    {
        return GetOrAddToken(type, diagnosticSource, static (provider, type, source) => provider.GetTypeSpecificationToken(type, source));
    }

    /// <inheritdoc/>
    public MetadataToken GetMethodSpecificationToken(MethodSpecification method, object? diagnosticSource = null)
    {
        return GetOrAddToken(method, diagnosticSource, static (provider, method, source) => provider.GetMethodSpecificationToken(method, source));
    }

    /// <inheritdoc/>
    public MetadataToken GetTypeDefinitionToken(TypeDefinition type, object? diagnosticSource = null)
    {
        return Inner.GetTypeDefinitionToken(type, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetOrImportTypeDefinitionToken(TypeDefinition type, object? diagnosticSource = null)
    {
        return Inner.GetOrImportTypeDefinitionToken(type, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetFieldDefinitionToken(FieldDefinition field, object? diagnosticSource = null)
    {
        return Inner.GetFieldDefinitionToken(field, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetOrImportFieldDefinitionToken(FieldDefinition field, object? diagnosticSource = null)
    {
        return Inner.GetOrImportFieldDefinitionToken(field, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetMethodDefinitionToken(MethodDefinition method, object? diagnosticSource = null)
    {
        return Inner.GetMethodDefinitionToken(method, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetOrImportMethodDefinitionToken(MethodDefinition method, object? diagnosticSource = null)
    {
        return Inner.GetOrImportMethodDefinitionToken(method, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetStandAloneSignatureToken(StandAloneSignature signature, object? diagnosticSource = null)
    {
        return Inner.GetStandAloneSignatureToken(signature, diagnosticSource);
    }

    /// <inheritdoc/>
    public MetadataToken GetAssemblyReferenceToken(AssemblyReference assembly, object? diagnosticSource = null)
    {
        return Inner.GetAssemblyReferenceToken(assembly, diagnosticSource);
    }

    /// <inheritdoc/>
    public uint GetUserStringIndex(string value)
    {
        return Inner.GetUserStringIndex(value);
    }

    /// <summary>
    /// Gets the token of a member, using the wrapped provider until a request for it succeeds.
    /// </summary>
    /// <typeparam name="T">The type of member.</typeparam>
    /// <param name="member">The member to get the token for.</param>
    /// <param name="diagnosticSource">The object to report errors for.</param>
    /// <param name="getToken">The callback to get the token from the wrapped provider.</param>
    /// <returns>The token of <paramref name="member"/>.</returns>
    private MetadataToken GetOrAddToken<T>(T member, object? diagnosticSource, Func<IMetadataTokenProvider, T, object?, MetadataToken> getToken)
        where T : class, IMetadataMember
    {
        // Leave invalid requests to the wrapped provider, which decides how to handle them
        if (member is null)
        {
            return getToken(Inner, member!, diagnosticSource);
        }

        if (_tokens.TryGetValue(member, out MetadataToken token))
        {
            return token;
        }

        int errorCount = Diagnostics.Exceptions.Count;
        bool wasFatal = Diagnostics.IsFatal;

        token = getToken(Inner, member, diagnosticSource);

        // Don't remember failures, so that each request reports its own errors like the wrapped provider does
        if (Diagnostics.Exceptions.Count == errorCount && Diagnostics.IsFatal == wasFatal)
        {
            _ = _tokens.TryAdd(member, token);
        }

        return token;
    }
}
