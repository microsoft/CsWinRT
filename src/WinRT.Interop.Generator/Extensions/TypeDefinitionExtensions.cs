// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="TypeDefinition"/> type.
/// </summary>
internal static class TypeDefinitionExtensions
{
    extension(TypeDefinition type)
    {
        /// <summary>
        /// Gets whether a given type is static.
        /// </summary>
        public bool IsStatic => type.IsAbstract && type.IsSealed;

        /// <summary>
        /// Gets the first method with a given name from the specified type.
        /// </summary>
        /// <param name="name">The name of the method to get.</param>
        /// <returns>The resulting method.</returns>
        /// <exception cref="ArgumentException">Thrown if the method couldn't be found.</exception>
        public MethodDefinition GetMethod(ReadOnlySpan<byte> name)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    return method;
                }
            }

            throw new ArgumentException($"Method with name '{new Utf8String(name)}' not found.", nameof(name));
        }

        /// <summary>
        /// Gets the first property with a given name from the specified type.
        /// </summary>
        /// <param name="name">The name of the property to get.</param>
        /// <returns>The resulting property.</returns>
        /// <exception cref="ArgumentException">Thrown if the property couldn't be found.</exception>
        public PropertyDefinition GetProperty(ReadOnlySpan<byte> name)
        {
            foreach (PropertyDefinition property in type.Properties)
            {
                if (property.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    return property;
                }
            }

            throw new ArgumentException($"Property with name '{new Utf8String(name)}' not found.", nameof(name));
        }

        /// <summary>
        /// Gets the first field with a given name from the specified type.
        /// </summary>
        /// <param name="name">The name of the field to get.</param>
        /// <returns>The resulting field.</returns>
        /// <exception cref="ArgumentException">Thrown if the field couldn't be found.</exception>
        public FieldDefinition GetField(ReadOnlySpan<byte> name)
        {
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    return field;
                }
            }

            throw new ArgumentException($"Field with name '{new Utf8String(name)}' not found.", nameof(name));
        }
    }
}
