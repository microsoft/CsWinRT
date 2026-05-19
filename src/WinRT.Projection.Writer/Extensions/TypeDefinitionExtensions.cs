// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="TypeDefinition"/>.
/// </summary>
internal static class TypeDefinitionExtensions
{
    extension(TypeDefinition type)
    {
        /// <summary>
        /// Returns the type's methods filtered to exclude special-name methods (property accessors,
        /// event accessors, and runtime-special methods like <c>.ctor</c>).
        /// </summary>
        /// <returns>The non-special methods in declaration order.</returns>
        public IEnumerable<MethodDefinition> GetNonSpecialMethods()
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.IsSpecial())
                {
                    yield return method;
                }
            }
        }

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
        /// Returns whether the type has a base type that is not <see cref="System.Object"/>
        /// (i.e. the type derives from a real WinRT/.NET class).
        /// </summary>
        public bool HasNonObjectBaseType()
        {
            return type.BaseType is { } bt && !bt.MatchesName("System", "Object");
        }

        /// <summary>
        /// Returns whether the type has a base type that is neither <see cref="System.Object"/>
        /// nor the projection's <c>WindowsRuntime.WindowsRuntimeObject</c> root.
        /// </summary>
        public bool HasNonProjectionBaseClass()
        {
            return type.BaseType is { } bt
                && !bt.MatchesName("System", "Object")
                && !bt.MatchesName("WindowsRuntime", "WindowsRuntimeObject");
        }

        /// <summary>
        /// Returns the second positional argument (a <see cref="uint"/>) of
        /// <c>[Windows.Foundation.Metadata.ContractVersionAttribute]</c> on the type, or
        /// <see langword="null"/> if the attribute is missing or the argument cannot be read.
        /// </summary>
        /// <returns>The contract version, or <see langword="null"/>.</returns>
        public int? GetContractVersion()
        {
            CustomAttribute? attr = type.GetWindowsFoundationMetadataAttribute(ContractVersionAttribute);
            return attr is not null && attr.TryGetFixedArgument(1, out int v) ? v : null;
        }

        /// <summary>
        /// Returns the first positional argument (a <see cref="uint"/>) of
        /// <c>[Windows.Foundation.Metadata.VersionAttribute]</c> on the type, or
        /// <see langword="null"/> if the attribute is missing or the argument cannot be read.
        /// </summary>
        /// <returns>The version, or <see langword="null"/>.</returns>
        public int? GetVersion()
        {
            CustomAttribute? attr = type.GetWindowsFoundationMetadataAttribute(VersionAttribute);
            return attr is not null && attr.TryGetFixedArgument(0, out int v) ? v : null;
        }
    }
}