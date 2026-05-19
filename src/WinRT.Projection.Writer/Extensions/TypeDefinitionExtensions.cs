// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Metadata;
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
                if (!method.IsSpecial)
                {
                    yield return method;
                }
            }
        }

        /// <summary>
        /// Returns the property accessor methods (get and set) declared by the type's
        /// properties. Used to filter "regular" methods (non-property, non-event) when emitting
        /// per-method code in interface bodies.
        /// </summary>
        public IEnumerable<MethodDefinition> GetPropertyAccessors()
        {
            foreach (PropertyDefinition prop in type.Properties)
            {
                if (prop.GetMethod is MethodDefinition g)
                {
                    yield return g;
                }

                if (prop.SetMethod is MethodDefinition s)
                {
                    yield return s;
                }
            }
        }

        /// <summary>
        /// Adds all directly-required interfaces of <paramref name="type"/> to
        /// <paramref name="visited"/> so they aren't re-emitted as forwarders. Used by the DIC
        /// shim emitters that already cover their inherited interface members transitively
        /// (e.g. <c>IBindableVector</c> already includes <c>IBindableIterable</c>'s members).
        /// </summary>
        /// <param name="cache">The metadata cache used to resolve interface references.</param>
        /// <param name="visited">The accumulator set to which resolved required interface
        /// definitions are added.</param>
        public void MarkRequiredInterfacesVisited(MetadataCache cache, HashSet<TypeDefinition> visited)
        {
            foreach (InterfaceImplementation impl in type.Interfaces)
            {
                if (impl.TryResolveTypeDef(cache, out TypeDefinition? required))
                {
                    _ = visited.Add(required);
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
                if (m.IsInvoke)
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
                if (m.IsDefaultConstructor)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the type has a base type that is not <see cref="object"/>
        /// (i.e. the type derives from a real WinRT/.NET class).
        /// </summary>
        public bool HasNonObjectBaseType()
        {
            return type.BaseType is not (null or CorLibTypeSignature { ElementType: ElementType.Object });
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