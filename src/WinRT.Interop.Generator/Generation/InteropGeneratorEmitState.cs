// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>, specifically for the emit phase.
/// </summary>
internal sealed class InteropGeneratorEmitState
{
    /// <summary>
    /// A map to provide fast lookup for generated types that need to be referenced in different parts of the emit phase.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<TypeSignature, TypeDefinition>> _typeDefinitionLookup = [];

    /// <summary>
    /// A map to provide fast lookup for generated methods that need to be referenced in different parts of the emit phase.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<TypeSignature, MethodDefinition>> _methodDefinitionLookup = [];

    /// <summary>
    /// The collection of method rewrite infos to process for two-pass code generation.
    /// </summary>
    private readonly ConcurrentBag<MethodRewriteInfo> _methodRewriteInfos = [];

    /// <summary>
    /// A map to allow reusing vtable types for applicable <c>IMapView&lt;K, V&gt;</c> interfaces.
    /// </summary>
    private readonly ConcurrentDictionary<TypeSignature, TypeDefinition> _mapViewVftblTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// A map to allow reusing vtable types for applicable <c>IMap&lt;K, V&gt;</c> interfaces.
    /// </summary>
    private readonly ConcurrentDictionary<(TypeSignature Key, TypeSignature Value), TypeDefinition> _mapVftblTypes = new(SignatureComparer.IgnoreVersion.MakeValueTupleComparer());

    /// <summary>
    /// A map to allow reusing vtable types for applicable <c>IDelegate</c> interfaces.
    /// </summary>
    private readonly ConcurrentDictionary<(TypeSignature Sender, TypeSignature Args), TypeDefinition> _delegateVftblTypes = new(SignatureComparer.IgnoreVersion.MakeValueTupleComparer());

    /// <summary>
    /// Indicates whether the current state is readonly.
    /// </summary>
    private volatile bool _isReadOnly;

    /// <summary>
    /// Tracks a new type generation for a given signature and key, for fast lookup.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to track.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> associated with <paramref name="typeDefinition"/>.</param>
    /// <param name="key">The key for <paramref name="typeDefinition"/>.</param>
    public void TrackTypeDefinition(TypeDefinition typeDefinition, TypeSignature typeSignature, string key)
    {
        ThrowIfReadOnly();

        ConcurrentDictionary<TypeSignature, TypeDefinition> innerLookup = _typeDefinitionLookup.GetOrAdd(
            key: key,
            valueFactory: static _ => new ConcurrentDictionary<TypeSignature, TypeDefinition>(SignatureComparer.IgnoreVersion));

        if (!innerLookup.TryAdd(typeSignature, typeDefinition))
        {
            throw WellKnownInteropExceptions.AddingDuplicateTrackedTypeDefinition(typeSignature, key);
        }
    }

    /// <summary>
    /// Looks up a type definition previously registered with <see cref="TrackTypeDefinition"/>.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> to use for lookup.</param>
    /// <param name="key">The key to use for lookup.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public TypeDefinition LookupTypeDefinition(TypeSignature typeSignature, string key)
    {
        if (_typeDefinitionLookup.TryGetValue(key, out ConcurrentDictionary<TypeSignature, TypeDefinition>? innerLookup) &&
            innerLookup.TryGetValue(typeSignature, out TypeDefinition? typeDefinition))
        {
            return typeDefinition;
        }

        throw WellKnownInteropExceptions.TrackedTypeDefinitionLookupError(typeSignature, key);
    }

    /// <summary>
    /// Tracks a new method generation for a given signature and key, for fast lookup.
    /// </summary>
    /// <param name="methodDefinition">The <see cref="MethodDefinition"/> to track.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> associated with <paramref name="methodDefinition"/>.</param>
    /// <param name="key">The key for <paramref name="methodDefinition"/>.</param>
    public void TrackMethodDefinition(MethodDefinition methodDefinition, TypeSignature typeSignature, string key)
    {
        ThrowIfReadOnly();

        ConcurrentDictionary<TypeSignature, MethodDefinition> innerLookup = _methodDefinitionLookup.GetOrAdd(
            key: key,
            valueFactory: static _ => new ConcurrentDictionary<TypeSignature, MethodDefinition>(SignatureComparer.IgnoreVersion));

        if (!innerLookup.TryAdd(typeSignature, methodDefinition))
        {
            throw WellKnownInteropExceptions.AddingDuplicateTrackedMethodDefinition(typeSignature, key);
        }
    }

    /// <summary>
    /// Looks up a method definition previously registered with <see cref="TrackMethodDefinition"/>.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> to use for lookup.</param>
    /// <param name="key">The key to use for lookup.</param>
    /// <returns>The resulting <see cref="MethodDefinition"/> instance.</returns>
    public MethodDefinition LookupMethodDefinition(TypeSignature typeSignature, string key)
    {
        if (_methodDefinitionLookup.TryGetValue(key, out ConcurrentDictionary<TypeSignature, MethodDefinition>? innerLookup) &&
            innerLookup.TryGetValue(typeSignature, out MethodDefinition? methodDefinition))
        {
            return methodDefinition;
        }

        throw WellKnownInteropExceptions.TrackedMethodDefinitionLookupError(typeSignature, key);
    }

    /// <summary>
    /// Tracks a method rewrite that involves returning a value from the specified method at a given marker instruction.
    /// </summary>
    /// <param name="returnType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    /// <param name="source"><inheritdoc cref="MethodRewriteInfo.ReturnValue.Source" path="/node()"/></param>
    public void TrackReturnValueMethodRewrite(
        TypeSignature returnType,
        MethodDefinition method,
        CilInstruction marker,
        CilLocalVariable source)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.ReturnValue
        {
            Type = returnType,
            Method = method,
            Marker = marker,
            Source = source
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves returning a native value from the specified method.
    /// </summary>
    /// <param name="retValType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    public void TrackRetValValueMethodRewrite(
        TypeSignature retValType,
        MethodDefinition method,
        CilInstruction marker)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.RetVal
        {
            Type = retValType,
            Method = method,
            Marker = marker
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves emitting direct calls to <c>ConvertToUnmanaged</c> in the specified method.
    /// </summary>
    /// <param name="parameterType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    public void TrackRawRetValMethodRewrite(
        TypeSignature parameterType,
        MethodDefinition method,
        CilInstruction marker)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.RawRetVal
        {
            Type = parameterType,
            Method = method,
            Marker = marker
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves loading a managed parameter in the specified method.
    /// </summary>
    /// <param name="parameterType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    /// <param name="parameterIndex"><inheritdoc cref="MethodRewriteInfo.ManagedParameter.ParameterIndex" path="/node()"/></param>
    public void TrackManagedParameterMethodRewrite(
        TypeSignature parameterType,
        MethodDefinition method,
        CilInstruction marker,
        int parameterIndex)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.ManagedParameter
        {
            Type = parameterType,
            Method = method,
            Marker = marker,
            ParameterIndex = parameterIndex
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves marshalling a managed value in the specified method.
    /// </summary>
    /// <param name="parameterType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    public void TrackManagedValueMethodRewrite(
        TypeSignature parameterType,
        MethodDefinition method,
        CilInstruction marker)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.ManagedValue
        {
            Type = parameterType,
            Method = method,
            Marker = marker
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves loading a native parameter in the specified method.
    /// </summary>
    /// <param name="parameterType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="tryMarker"><inheritdoc cref="MethodRewriteInfo.NativeParameter.TryMarker" path="/node()"/></param>
    /// <param name="loadMarker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    /// <param name="finallyMarker"><inheritdoc cref="MethodRewriteInfo.NativeParameter.FinallyMarker" path="/node()"/></param>
    /// <param name="parameterIndex"><inheritdoc cref="MethodRewriteInfo.NativeParameter.ParameterIndex" path="/node()"/></param>
    public void TrackNativeParameterMethodRewrite(
        TypeSignature parameterType,
        MethodDefinition method,
        CilInstruction tryMarker,
        CilInstruction loadMarker,
        CilInstruction finallyMarker,
        int parameterIndex)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.NativeParameter
        {
            Type = parameterType,
            Method = method,
            TryMarker = tryMarker,
            Marker = loadMarker,
            FinallyMarker = finallyMarker,
            ParameterIndex = parameterIndex
        });
    }

    /// <summary>
    /// Tracks a method rewrite that involves emitting calls to dispose (or release) a given value in the specified method.
    /// </summary>
    /// <param name="parameterType"><inheritdoc cref="MethodRewriteInfo.Type" path="/node()"/></param>
    /// <param name="method"><inheritdoc cref="MethodRewriteInfo.Method" path="/node()"/></param>
    /// <param name="marker"><inheritdoc cref="MethodRewriteInfo.Marker" path="/node()"/></param>
    public void TrackDisposeRewrite(
        TypeSignature parameterType,
        MethodDefinition method,
        CilInstruction marker)
    {
        ThrowIfReadOnly();

        _methodRewriteInfos.Add(new MethodRewriteInfo.Dispose
        {
            Type = parameterType,
            Method = method,
            Marker = marker
        });
    }

    /// <summary>
    /// Enumerates all <see cref="MethodRewriteInfo"/> instances with info on two-pass code generation steps to perform.
    /// </summary>
    /// <returns>The <see cref="MethodRewriteInfo"/> instances to process.</returns>
    public IEnumerable<MethodRewriteInfo> EnumerateMethodRewriteInfos()
    {
        return _methodRewriteInfos;
    }

    /// <summary>
    /// Tries to get a previously registered vtable type for an <c>IMapView&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="vftblType">The resulting vtable type, if present.</param>
    /// <returns>Whether <paramref name="vftblType"/> was successfully retrieved.</returns>
    public bool TryGetIMapView2VftblType(TypeSignature keyType, [NotNullWhen(true)] out TypeDefinition? vftblType)
    {
        return _mapViewVftblTypes.TryGetValue(keyType, out vftblType);
    }

    /// <summary>
    /// Gets or adds a vtable type for an <c>IMapView&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="vftblType">The created vtable type for <paramref name="keyType"/>.</param>
    /// <returns>The vtable type that should be used.</returns>
    public TypeDefinition GetOrAddIMapView2VftblType(TypeSignature keyType, TypeDefinition vftblType)
    {
        ThrowIfReadOnly();

        return _mapViewVftblTypes.GetOrAdd(keyType, vftblType);
    }

    /// <summary>
    /// Tries to get a previously registered vtable type for an <c>IMap&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="valueType">The value type.</param>
    /// <param name="vftblType">The resulting vtable type, if present.</param>
    /// <returns>Whether <paramref name="vftblType"/> was successfully retrieved.</returns>
    public bool TryGetIMap2VftblType(TypeSignature keyType, TypeSignature valueType, [NotNullWhen(true)] out TypeDefinition? vftblType)
    {
        return _mapVftblTypes.TryGetValue((keyType, valueType), out vftblType);
    }

    /// <summary>
    /// Gets or adds a vtable type for an <c>IMap&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="valueType">The value type.</param>
    /// <param name="vftblType">The created vtable type for <paramref name="keyType"/>.</param>
    /// <returns>The vtable type that should be used.</returns>
    public TypeDefinition GetOrAddIMap2VftblType(TypeSignature keyType, TypeSignature valueType, TypeDefinition vftblType)
    {
        ThrowIfReadOnly();

        return _mapVftblTypes.GetOrAdd((keyType, valueType), vftblType);
    }

    /// <summary>
    /// Tries to get a previously registered vtable type for an <c>IDelegate</c> interface.
    /// </summary>
    /// <param name="senderType">The sender type.</param>
    /// <param name="argsType">The args type.</param>
    /// <param name="vftblType">The resulting vtable type, if present.</param>
    /// <returns>Whether <paramref name="vftblType"/> was successfully retrieved.</returns>
    public bool TryGetDelegateVftblType(TypeSignature senderType, TypeSignature argsType, [NotNullWhen(true)] out TypeDefinition? vftblType)
    {
        return _delegateVftblTypes.TryGetValue((senderType, argsType), out vftblType);
    }

    /// <summary>
    /// Gets or adds a vtable type for an <c>IDelegate</c> interface.
    /// </summary>
    /// <param name="senderType">The key type.</param>
    /// <param name="argsType">The value type.</param>
    /// <param name="vftblType">The created vtable type for <paramref name="senderType"/>.</param>
    /// <returns>The vtable type that should be used.</returns>
    public TypeDefinition GetOrAddDelegateVftblType(TypeSignature senderType, TypeSignature argsType, TypeDefinition vftblType)
    {
        ThrowIfReadOnly();

        return _delegateVftblTypes.GetOrAdd((senderType, argsType), vftblType);
    }

    /// <summary>
    /// Marks the current state as readonly.
    /// </summary>
    public void MakeReadOnly()
    {
        _isReadOnly = true;
    }

    /// <summary>
    /// Throws if <see cref="MakeReadOnly"/> was called.
    /// </summary>
    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
        {
            throw WellKnownInteropExceptions.EmitStateChangeAfterMakeReadOnlyError();
        }
    }
}