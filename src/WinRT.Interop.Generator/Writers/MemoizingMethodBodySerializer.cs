// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code;

namespace WindowsRuntime.InteropGenerator.Writers;

/// <summary>
/// An <see cref="IMethodBodySerializer"/> that resolves operand tokens through a <see cref="MemoizingMetadataTokenProvider"/>.
/// </summary>
/// <remarks>
/// Tokens are only remembered for contexts reporting errors to the given diagnostics, which must also be the
/// error listener of their token provider (as is the case for the metadata builder). Other contexts are passed
/// to the wrapped serializer unchanged. Remembered tokens are discarded as soon as a different token provider
/// is used, so each instance only ever serves a single build. Instances are not thread-safe.
/// </remarks>
internal sealed class MemoizingMethodBodySerializer : IMethodBodySerializer
{
    /// <summary>The wrapped serializer.</summary>
    private readonly IMethodBodySerializer _inner;

    /// <summary>The error listener of the build using this serializer.</summary>
    private readonly DiagnosticBag _diagnostics;

    /// <summary>The last context received from the metadata builder.</summary>
    private MethodBodySerializationContext? _context;

    /// <summary>The context passed to <see cref="_inner"/> in place of <see cref="_context"/>.</summary>
    private MethodBodySerializationContext? _memoizingContext;

    /// <summary>
    /// Creates a new <see cref="MemoizingMethodBodySerializer"/> instance.
    /// </summary>
    /// <param name="inner">The wrapped serializer.</param>
    /// <param name="diagnostics">The error listener of the build using this serializer.</param>
    public MemoizingMethodBodySerializer(IMethodBodySerializer inner, DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _inner = inner;
        _diagnostics = diagnostics;
    }

    /// <inheritdoc/>
    public ISegmentReference SerializeMethodBody(MethodBodySerializationContext context, MethodDefinition method)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Without observing errors, failed requests can't be told apart from successful ones
        if (!ReferenceEquals(context.ErrorListener, _diagnostics))
        {
            return _inner.SerializeMethodBody(context, method);
        }

        if (!ReferenceEquals(context, _context))
        {
            MemoizingMetadataTokenProvider tokenProvider = _memoizingContext?.TokenProvider is MemoizingMetadataTokenProvider current &&
                ReferenceEquals(current.Inner, context.TokenProvider)
                ? current
                : new MemoizingMetadataTokenProvider(context.TokenProvider, _diagnostics);

            // Never reuse the symbols provider or error listener of a previous context
            _memoizingContext = new MethodBodySerializationContext(tokenProvider, context.SymbolsProvider, context.ErrorListener);
            _context = context;
        }

        return _inner.SerializeMethodBody(_memoizingContext!, method);
    }
}
