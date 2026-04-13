// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for marshaller types for Windows Runtime types.
/// </summary>
internal readonly ref struct InteropMarshallerType
{
    /// <summary>
    /// The managed type being marshalled.
    /// </summary>
    private readonly TypeSignature _type;

    /// <summary>
    /// The <see cref="InteropReferences"/> instance to use.
    /// </summary>
    private readonly InteropReferences _interopReferences;

    /// <summary>
    /// The <see cref="ITypeDefOrRef"/> for the marshaller type for <see cref="_type"/>.
    /// </summary>
    private readonly ITypeDefOrRef _marshallerType;

    /// <summary>
    /// Creates a new <see cref="InteropMarshallerType"/> instance with the specified parameters.
    /// </summary>
    /// <param name="type"><inheritdoc cref="_type" path="/summary/node()"/></param>
    /// <param name="interopReferences"><inheritdoc cref="_interopReferences" path="/summary/node()"/></param>
    /// <param name="marshallerType"><inheritdoc cref="_marshallerType" path="/summary/node()"/></param>
    public InteropMarshallerType(
        TypeSignature type,
        InteropReferences interopReferences,
        ITypeDefOrRef marshallerType)
    {
        _type = type;
        _interopReferences = interopReferences;
        _marshallerType = marshallerType;
    }

    /// <summary>
    /// Gets the <see cref="IMethodDefOrRef"/> for the <c>ConvertToManaged</c> method for a specified type.
    /// </summary>
    /// <returns>The resulting <see cref="IMethodDefOrRef"/> value.</returns>
    public IMethodDefOrRef ConvertToManaged()
    {
        return _marshallerType.GetMethodDefOrRef(
            name: "ConvertToManaged"u8,
            signature: MethodSignature.CreateStatic(
                returnType: _type,
                parameterTypes: [_type.GetAbiType(_interopReferences)]));
    }

    /// <summary>
    /// Gets the <see cref="IMethodDefOrRef"/> for the <c>ConvertToUnmanaged</c> method for a specified type.
    /// </summary>
    /// <returns>The resulting <see cref="IMethodDefOrRef"/> value.</returns>
    public IMethodDefOrRef ConvertToUnmanaged()
    {
        TypeSignature abiType = _type.GetAbiType(_interopReferences);

        // If the ABI type is 'void*' and the type is not 'string', then we use 'WindowsRuntimeObjectReferenceValue'.
        // This is because that's always used to return Windows Runtime native objects. Just 'HSTRING' is special.
        TypeSignature returnType = abiType.IsTypeOfVoidPointer() && !_type.IsTypeOfString()
            ? _interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature()
            : abiType;

        return _marshallerType.GetMethodDefOrRef(
            name: "ConvertToUnmanaged"u8,
            signature: MethodSignature.CreateStatic(
                returnType: returnType,
                parameterTypes: [_type]));
    }

    /// <summary>
    /// Gets the <see cref="IMethodDefOrRef"/> for the <c>BoxToUnmanaged</c> method for a specified type.
    /// </summary>
    /// <returns>The resulting <see cref="IMethodDefOrRef"/> value.</returns>
    public IMethodDefOrRef BoxToUnmanaged()
    {
        // When boxing, the parameter is either 'Nullable<T>' for value types, or just the same type
        TypeSignature parameterType = _type.IsValueType
            ? _interopReferences.Nullable1.MakeGenericValueType([_type])
            : _type;

        return _marshallerType.GetMethodDefOrRef(
            name: "BoxToUnmanaged"u8,
            signature: MethodSignature.CreateStatic(
                returnType: _interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [parameterType]));
    }

    /// <summary>
    /// Gets the <see cref="IMethodDefOrRef"/> for the <c>UnboxToManaged</c> method for a specified type.
    /// </summary>
    /// <returns>The resulting <see cref="IMethodDefOrRef"/> value.</returns>
    public IMethodDefOrRef UnboxToManaged()
    {
        // When unboxing, the return type is either 'Nullable<T>' for value types, or just the same type
        TypeSignature returnType = _type.IsValueType
            ? _interopReferences.Nullable1.MakeGenericValueType([_type])
            : _type;

        return _marshallerType.GetMethodDefOrRef(
            name: "UnboxToManaged"u8,
            signature: MethodSignature.CreateStatic(
                returnType: returnType,
                parameterTypes: [_interopReferences.Void.MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="IMethodDefOrRef"/> for the <c>Dispose</c> method for a specified type.
    /// </summary>
    /// <returns>The resulting <see cref="IMethodDefOrRef"/> value.</returns>
    public IMethodDefOrRef Dispose()
    {
        return _marshallerType.GetMethodDefOrRef(
            name: "Dispose"u8,
            signature: MethodSignature.CreateStatic(
                returnType: _interopReferences.Void,
                parameterTypes: [_type.GetAbiType(_interopReferences)]));
    }
}
