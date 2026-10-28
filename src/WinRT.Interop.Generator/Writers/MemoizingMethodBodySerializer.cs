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
/// AsmResolver creates one context in <c>DotNetDirectoryBuffer.FinalizeMethods</c> and shares it across all
/// methods in a metadata build. The interop writer creates a new serializer for each write, so this instance
/// accepts only that context and its diagnostic listener. Tokens are shared across method bodies within the
/// write, never across builds. Instances are not thread-safe.
/// </remarks>
internal sealed class MemoizingMethodBodySerializer : IMethodBodySerializer
{
    /// <summary>The wrapped serializer.</summary>
    private readonly IMethodBodySerializer _inner;

    /// <summary>The error listener of the build using this serializer.</summary>
    private readonly DiagnosticBag _diagnostics;

    /// <summary>The single context received from the metadata builder.</summary>
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

        if (_context is null)
        {
            // Without observing errors, failed requests can't be told apart from successful ones
            if (!ReferenceEquals(context.ErrorListener, _diagnostics))
            {
                throw new InvalidOperationException("The serialization context must use this build's diagnostic listener.");
            }

            MemoizingMetadataTokenProvider tokenProvider = new(context.TokenProvider, _diagnostics);

            _memoizingContext = new MethodBodySerializationContext(tokenProvider, context.SymbolsProvider, context.ErrorListener);
            _context = context;
        }
        else if (!ReferenceEquals(context, _context))
        {
            throw new InvalidOperationException("The serializer can only be used with one metadata build context.");
        }

        return _inner.SerializeMethodBody(_memoizingContext!, method);
    }
}
