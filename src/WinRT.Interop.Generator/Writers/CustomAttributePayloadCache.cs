// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.IO;

namespace WindowsRuntime.InteropGenerator.Writers;

/// <summary>
/// Serializes custom attribute payloads once, for both deterministic ordering and writing the module.
/// </summary>
/// <remarks>
/// <para>
/// Payloads are serialized exactly like the metadata builder serializes them for <see cref="Module"/>, including
/// any <see cref="ExtendableBlobSignature.ExtraData"/>. A payload is recomputed if the signature of its attribute
/// (or the extra data of that signature) was replaced, but the arguments of a signature must not be modified
/// between serializing it and writing the module: verifying them would cost as much as serializing them again.
/// </para>
/// <para>
/// <see cref="GetOrAdd"/> can be called concurrently. <see cref="SubstituteSignatures"/> consumes all payloads,
/// so a later write only reuses payloads that were serialized again after that.
/// </para>
/// </remarks>
internal sealed class CustomAttributePayloadCache
{
    /// <summary>The serialized payloads, by attribute identity.</summary>
    private readonly ConcurrentDictionary<CustomAttribute, Payload> _payloads = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Creates a new <see cref="CustomAttributePayloadCache"/> instance.
    /// </summary>
    /// <param name="module">The module that the attributes are written to.</param>
    public CustomAttributePayloadCache(ModuleDefinition module)
    {
        ArgumentNullException.ThrowIfNull(module);

        Module = module;
    }

    /// <summary>
    /// Gets the module that the attributes are written to.
    /// </summary>
    public ModuleDefinition Module { get; }

    /// <summary>
    /// Gets the serialized payload of an attribute, serializing it if needed.
    /// </summary>
    /// <param name="attribute">The attribute to serialize.</param>
    /// <returns>The serialized payload of <paramref name="attribute"/> (empty if it has no signature).</returns>
    /// <remarks>
    /// Errors are thrown rather than collected, and payloads cannot refer to metadata tokens, as these
    /// are not assigned yet. Neither can happen for valid attributes, which encode types by name.
    /// </remarks>
    public ReadOnlyMemory<byte> GetOrAdd(CustomAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        // The metadata builder writes attributes without a signature with an empty blob
        if (attribute.Signature is not { } signature)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        if (_payloads.TryGetValue(attribute, out Payload? payload) && payload.IsCurrent(signature))
        {
            return payload.Bytes;
        }

        using MemoryStream stream = new();

        BlobSerializationContext context = new(
            writer: new BinaryStreamWriter(stream),
            contextModule: Module,
            indexProvider: ThrowingIndexProvider.Instance,
            errorListener: ThrowErrorListener.Instance,
            diagnosticSource: attribute);

        // Capture the extra data first, as it is appended to the payload by the call below
        byte[]? extraData = signature.ExtraData;

        signature.Write(context);

        payload = new Payload(signature, extraData, stream.ToArray());

        _payloads[attribute] = payload;

        return payload.Bytes;
    }

    /// <summary>
    /// Temporarily replaces the signatures of all serialized attributes, so that writing them reuses their payloads.
    /// </summary>
    /// <param name="module">The module being written, which must be <see cref="Module"/>.</param>
    /// <returns>An object that restores the original signatures and discards all payloads when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="module"/> is not <see cref="Module"/>.</exception>
    public IDisposable SubstituteSignatures(ModuleDefinition module)
    {
        ArgumentNullException.ThrowIfNull(module);

        // Serialized type names depend on the module they are written to
        if (!ReferenceEquals(module, Module))
        {
            throw new ArgumentException("The payloads were serialized for a different module.", nameof(module));
        }

        Substitution substitution = new(this);

        try
        {
            foreach ((CustomAttribute attribute, Payload payload) in _payloads)
            {
                if (attribute.Signature is { } signature && payload.IsCurrent(signature))
                {
                    substitution.Replace(attribute, new SerializedPayloadSignature(payload));
                }
            }
        }
        catch
        {
            substitution.Dispose();

            throw;
        }

        return substitution;
    }

    /// <summary>
    /// A serialized attribute payload.
    /// </summary>
    /// <param name="signature">The serialized signature.</param>
    /// <param name="extraData">The extra data of <paramref name="signature"/> when it was serialized.</param>
    /// <param name="bytes">The serialized payload, ending with <paramref name="extraData"/>.</param>
    private sealed class Payload(CustomAttributeSignature signature, byte[]? extraData, byte[] bytes)
    {
        /// <summary>Gets the serialized signature.</summary>
        public CustomAttributeSignature Signature { get; } = signature;

        /// <summary>Gets the extra data of <see cref="Signature"/> when it was serialized.</summary>
        public byte[]? ExtraData { get; } = extraData;

        /// <summary>Gets the serialized payload, ending with <see cref="ExtraData"/>.</summary>
        public byte[] Bytes { get; } = bytes;

        /// <summary>
        /// Checks whether the payload was serialized from a given signature, as it currently is.
        /// </summary>
        /// <param name="signature">The current signature of the attribute.</param>
        /// <returns>Whether the payload was serialized from <paramref name="signature"/>.</returns>
        public bool IsCurrent(CustomAttributeSignature signature)
        {
            return ReferenceEquals(signature, Signature) && ReferenceEquals(signature.ExtraData, ExtraData);
        }
    }

    /// <summary>
    /// A signature writing a previously serialized payload, in place of the signature it was serialized from.
    /// </summary>
    private sealed class SerializedPayloadSignature : CustomAttributeSignature
    {
        /// <summary>The payload to write.</summary>
        private readonly Payload _payload;

        /// <summary>
        /// Creates a new <see cref="SerializedPayloadSignature"/> instance.
        /// </summary>
        /// <param name="payload">The payload to write.</param>
        public SerializedPayloadSignature(Payload payload)
        {
            _payload = payload;

            // The extra data is appended by the base 'Write' method, so it must not be part of the contents too
            ExtraData = payload.ExtraData;
        }

        /// <inheritdoc/>
        public override bool IsCompatibleWith(ICustomAttributeType constructor, IErrorListener listener)
        {
            // Keep validating the original arguments (and reporting the original errors) against the constructor
            return _payload.Signature.IsCompatibleWith(constructor, listener);
        }

        /// <inheritdoc/>
        protected override void Initialize(IList<CustomAttributeArgument> fixedArguments, IList<CustomAttributeNamedArgument> namedArguments)
        {
            foreach (CustomAttributeArgument argument in _payload.Signature.FixedArguments)
            {
                fixedArguments.Add(argument);
            }

            foreach (CustomAttributeNamedArgument argument in _payload.Signature.NamedArguments)
            {
                namedArguments.Add(argument);
            }
        }

        /// <inheritdoc/>
        protected override void WriteContents(in BlobSerializationContext context)
        {
            int extraDataLength = _payload.ExtraData?.Length ?? 0;

            context.Writer.WriteBytes(_payload.Bytes.AsSpan(0, _payload.Bytes.Length - extraDataLength));
        }
    }

    /// <summary>
    /// Restores the signatures replaced by <see cref="SubstituteSignatures"/>.
    /// </summary>
    /// <param name="cache">The cache that the payloads come from.</param>
    private sealed class Substitution(CustomAttributePayloadCache cache) : IDisposable
    {
        /// <summary>The attributes with a replaced signature, along with both signatures.</summary>
        private readonly List<(CustomAttribute Attribute, CustomAttributeSignature Original, SerializedPayloadSignature Replacement)> _replacements = [];

        /// <summary>
        /// Replaces the signature of an attribute.
        /// </summary>
        /// <param name="attribute">The attribute to update.</param>
        /// <param name="replacement">The signature to use while writing.</param>
        public void Replace(CustomAttribute attribute, SerializedPayloadSignature replacement)
        {
            _replacements.Add((attribute, attribute.Signature!, replacement));

            attribute.Signature = replacement;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach ((CustomAttribute attribute, CustomAttributeSignature original, SerializedPayloadSignature replacement) in _replacements)
            {
                // Don't overwrite a signature that was replaced again in the meantime
                if (ReferenceEquals(attribute.Signature, replacement))
                {
                    attribute.Signature = original;
                }
            }

            _replacements.Clear();

            // The arguments may be modified after this write, so payloads must be serialized again for any later one
            cache._payloads.Clear();
        }
    }

    /// <summary>
    /// An index provider for payloads, which cannot refer to metadata tokens as they are not assigned yet.
    /// </summary>
    private sealed class ThrowingIndexProvider : ITypeCodedIndexProvider
    {
        /// <summary>Gets the shared stateless provider.</summary>
        public static ThrowingIndexProvider Instance { get; } = new();

        /// <inheritdoc/>
        public uint GetTypeDefOrRefIndex(ITypeDefOrRef type, object? diagnosticSource)
        {
            throw new InvalidOperationException("Custom attribute ordering must not allocate metadata tokens.");
        }
    }
}
