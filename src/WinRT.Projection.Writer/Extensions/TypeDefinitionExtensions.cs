// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="TypeDefinition"/>.
/// </summary>
internal static class TypeDefinitionExtensions
{
    extension(TypeDefinition type)
    {
        /// <summary>
        /// Returns the <c>[Default]</c> interface of the type (the interface whose vtable backs the
        /// type's <c>IInspectable</c> identity), or <see langword="null"/> if the type does not declare one.
        /// </summary>
        /// <returns>The default interface, or <see langword="null"/>.</returns>
        public ITypeDefOrRef? GetDefaultInterface()
        {
            foreach (InterfaceImplementation impl in type.Interfaces)
            {
                if (impl.IsDefaultInterface() && impl.Interface is not null)
                {
                    return impl.Interface;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the <c>Invoke</c> method of a delegate type definition, or <see langword="null"/>
        /// if no such method exists.
        /// </summary>
        /// <returns>The delegate's <c>Invoke</c> method, or <see langword="null"/>.</returns>
        public MethodDefinition? GetDelegateInvoke()
        {
            foreach (MethodDefinition m in type.Methods)
            {
                if (m.IsSpecialName && m.Name == "Invoke")
                {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns whether the type declares a parameterless instance constructor.
        /// </summary>
        /// <returns><see langword="true"/> if the type has a default constructor; otherwise <see langword="false"/>.</returns>
        public bool HasDefaultConstructor()
        {
            foreach (MethodDefinition m in type.Methods)
            {
                if (m.IsRuntimeSpecialName && m.Name == ".ctor" && m.Parameters.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the second positional argument (a <see cref="uint"/>) of
        /// <c>[Windows.Foundation.Metadata.ContractVersionAttribute]</c> on the type, or
        /// <see langword="null"/> if the attribute is missing or the argument cannot be read.
        /// </summary>
        /// <returns>The contract version, or <see langword="null"/>.</returns>
        public int? GetContractVersion()
        {
            CustomAttribute? attr = type.GetAttribute(WindowsFoundationMetadata, ContractVersionAttribute);

            if (attr is null)
            {
                return null;
            }

            if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 1)
            {
                object? v = attr.Signature.FixedArguments[1].Element;

                if (v is uint u)
                {
                    return (int)u;
                }

                if (v is int i)
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first positional argument (a <see cref="uint"/>) of
        /// <c>[Windows.Foundation.Metadata.VersionAttribute]</c> on the type, or
        /// <see langword="null"/> if the attribute is missing or the argument cannot be read.
        /// </summary>
        /// <returns>The version, or <see langword="null"/>.</returns>
        public int? GetVersion()
        {
            CustomAttribute? attr = type.GetAttribute(WindowsFoundationMetadata, VersionAttribute);

            if (attr is null)
            {
                return null;
            }

            if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 0)
            {
                object? v = attr.Signature.FixedArguments[0].Element;

                if (v is uint u)
                {
                    return (int)u;
                }

                if (v is int i)
                {
                    return i;
                }
            }

            return null;
        }
    }
}