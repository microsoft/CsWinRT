// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

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
        /// Determines whether a type has or inherits an attribute that matches a particular type.
        /// </summary>
        /// <param name="attributeType">The attribute type to look for.</param>
        /// <param name="corLibTypeFactory">The <see cref="CorLibTypeFactory"/> instance to use.</param>
        /// <returns>Whether the type has or inherits an attribute with the specified type.</returns>
        public bool HasOrInheritsAttribute(TypeReference attributeType, CorLibTypeFactory corLibTypeFactory)
        {
            for (
                TypeDefinition? currentType = type;
                currentType is not null && !SignatureComparer.IgnoreVersion.Equals(currentType.BaseType, corLibTypeFactory.Object);
                currentType = currentType.BaseType?.Resolve())
            {
                if (currentType.HasCustomAttribute(attributeType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the first method with a given name from the specified type.
        /// </summary>
        /// <param name="name">The name of the method to get.</param>
        /// <returns>The resulting method.</returns>
        /// <exception cref="ArgumentException">Thrown if the method couldn't be found.</exception>
        public MethodDefinition GetMethod(Utf8String name)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Name == name)
                {
                    return method;
                }
            }

            throw new ArgumentException($"Method with name '{name}' not found.", nameof(name));
        }

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
        /// Gets all methods with a given name from the specified type.
        /// </summary>
        /// <param name="name">The name of the methods to get.</param>
        /// <returns>The resulting methods.</returns>
        public MethodDefinition[] GetMethods(ReadOnlySpan<byte> name)
        {
            List<MethodDefinition> methods = [];

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    methods.Add(method);
                }
            }

            return [.. methods];
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

        /// <summary>
        /// Adds a method implementation to the specified type, and marks it as implementing a given declaration.
        /// </summary>
        /// <param name="declaration">The interface method that is implemented.</param>
        /// <param name="method">The method implementing the base method.</param>
        public void AddMethodImplementation(IMethodDefOrRef declaration, MethodDefinition method)
        {
            type.Methods.Add(method);
            type.MethodImplementations.Add(new MethodImplementation(declaration, method));
        }

        /// <summary>
        /// Enumerates all generic instance type signatures for base interfaces, from a given starting interface.
        /// </summary>
        /// <param name="typeSignature">The constructed signature for the interface type.</param>
        /// <returns>All (unique) generic type signatures for base interfaces.</returns>
        /// <remarks>
        /// This method can only be called when <paramref name="typeSignature"/> is an interface type.
        /// Additionally, <paramref name="typeSignature"/> must be constructed over that type.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="typeSignature"/> is not an interface type, or <paramref name="typeSignature"/> is not a match for it.</exception>
        public IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceInterfaceSignatures(GenericInstanceTypeSignature typeSignature)
        {
            // This method is only valid when called on interface types (it's meant to enumerate the base interfaces)
            if (!type.IsInterface)
            {
                throw new ArgumentException($"The target type must be an interface type.", nameof(type));
            }

            // Verify the provided signature is valid
            if (!SignatureComparer.IgnoreVersion.Equals(type, typeSignature.GenericType))
            {
                throw new ArgumentException("The input type signature does not match the type definition.", nameof(typeSignature));
            }

            GenericContext genericContext = new(typeSignature, null);

            foreach (InterfaceImplementation interfaceImplementation in type.Interfaces)
            {
                // Ignore the interface, if we couldn't construct it. Note that we only need to care about generic
                // interfaces. If an interface were e.g. 'IA : IB<int>', then 'IB<int>' would be present in the
                // 'TypeSpec' table of the declaring module for 'IA', meaning we'd have already seen it.
                if (interfaceImplementation.Interface?.ToReferenceTypeSignature().InstantiateGenericTypes(genericContext) is not GenericInstanceTypeSignature interfaceSignature)
                {
                    continue;
                }

                yield return interfaceSignature;

                // Also recurse on the base interfaces
                if (interfaceSignature.IsFullyResolvable)
                {
                    foreach (GenericInstanceTypeSignature baseInterfaceImplementation in interfaceSignature.Resolve()!.EnumerateGenericInstanceInterfaceSignatures(interfaceSignature))
                    {
                        yield return baseInterfaceImplementation;
                    }
                }
            }
        }
    }
}
