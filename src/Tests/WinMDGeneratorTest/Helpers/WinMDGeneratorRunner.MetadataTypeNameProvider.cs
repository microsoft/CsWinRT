// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace WinMDGeneratorTest.Helpers;

internal static partial class WinMDGeneratorRunner
{
    /// <summary>
    /// Formats signature types with their metadata assembly scopes for API assertions.
    /// </summary>
    private sealed class MetadataTypeNameProvider : ISignatureTypeProvider<string, object?>
    {
        /// <summary>
        /// Formats the metadata type identified by a definition, reference, or specification handle.
        /// </summary>
        public string GetTypeName(
            MetadataReader reader,
            EntityHandle handle)
        {
            return handle.Kind switch
            {
                HandleKind.TypeDefinition => GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
                HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
                HandleKind.TypeSpecification => GetTypeFromSpecification(reader, null, (TypeSpecificationHandle)handle, 0),
                _ => throw new NotSupportedException($"Unexpected metadata type handle: {handle.Kind}.")
            };
        }

        /// <inheritdoc/>
        public string GetTypeFromDefinition(
            MetadataReader reader,
            TypeDefinitionHandle handle,
            byte rawTypeKind)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string assembly = reader.GetString(reader.GetAssemblyDefinition().Name);

            return $"{assembly}::{Format(reader.GetString(type.Namespace), reader.GetString(type.Name))}";
        }

        /// <inheritdoc/>
        public string GetTypeFromReference(
            MetadataReader reader,
            TypeReferenceHandle handle,
            byte rawTypeKind)
        {
            TypeReference type = reader.GetTypeReference(handle);
            string assembly = type.ResolutionScope.Kind switch
            {
                HandleKind.AssemblyReference => reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name),
                HandleKind.ModuleDefinition => reader.GetString(reader.GetAssemblyDefinition().Name),
                _ => throw new NotSupportedException($"Unexpected type reference scope: {type.ResolutionScope.Kind}.")
            };

            return $"{assembly}::{Format(reader.GetString(type.Namespace), reader.GetString(type.Name))}";
        }

        /// <inheritdoc/>
        public string GetTypeFromSpecification(
            MetadataReader reader,
            object? genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }

        /// <inheritdoc/>
        public string GetGenericInstantiation(
            string genericType,
            ImmutableArray<string> typeArguments)
        {
            return $"{genericType}<{string.Join(", ", typeArguments)}>";
        }

        /// <inheritdoc/>
        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode.ToString();
        }

        /// <inheritdoc/>
        public string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        /// <inheritdoc/>
        public string GetArrayType(
            string elementType,
            ArrayShape shape)
        {
            return $"{elementType}[{new string(',', shape.Rank - 1)}]";
        }

        /// <inheritdoc/>
        public string GetByReferenceType(string elementType)
        {
            return elementType + "&";
        }

        /// <inheritdoc/>
        public string GetPointerType(string elementType)
        {
            return elementType + "*";
        }

        /// <inheritdoc/>
        public string GetGenericTypeParameter(
            object? genericContext,
            int index)
        {
            return $"!{index}";
        }

        /// <inheritdoc/>
        public string GetGenericMethodParameter(
            object? genericContext,
            int index)
        {
            return $"!!{index}";
        }

        /// <inheritdoc/>
        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            throw new NotSupportedException("WinMD signatures cannot contain function pointers.");
        }

        /// <inheritdoc/>
        public string GetModifiedType(
            string modifier,
            string unmodifiedType,
            bool isRequired)
        {
            throw new NotSupportedException("WinMD signatures cannot contain custom modifiers.");
        }

        /// <inheritdoc/>
        public string GetPinnedType(string elementType)
        {
            throw new NotSupportedException("WinMD signatures cannot contain pinned types.");
        }
    }
}