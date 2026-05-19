// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// Returns the constructors for the current type.
        /// </summary>
        public IEnumerable<MethodDefinition> GetConstructors()
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsConstructor)
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

        /// <summary>
        /// Tries to read the <c>[Windows.Foundation.Metadata.GuidAttribute]</c> on the type and reconstructs
        /// it as a <see cref="Guid"/> value. Returns <see langword="null"/> if the attribute
        /// is missing or its 11 positional fields (<c>uint</c>, <c>ushort</c>, <c>ushort</c>, then
        /// 8 × <c>byte</c>) cannot be read.
        /// </summary>
        /// <param name="guid">The parsed <see cref="Guid"/> value, if successfully retrieved.</param>
        /// <returns><see langword="true"/> if the GUID was successfully retrieved; otherwise <see langword="false"/>.</returns>
        public bool TryGetWindowsRuntimeGuid(out Guid guid)
        {
            CustomAttribute? attr = type.GetWindowsFoundationMetadataAttribute("GuidAttribute");

            if (attr is null || attr.Signature is null)
            {
                guid = default;

                return false;
            }

            IList<CustomAttributeArgument> args = attr.Signature.FixedArguments;

            if (args.Count < 11)
            {
                guid = default;

                return false;
            }

            guid = new Guid(
                a: Convert.ToUInt32(args[0].Element, CultureInfo.InvariantCulture),
                b: Convert.ToUInt16(args[1].Element, CultureInfo.InvariantCulture),
                c: Convert.ToUInt16(args[2].Element, CultureInfo.InvariantCulture),
                d: Convert.ToByte(args[3].Element, CultureInfo.InvariantCulture),
                e: Convert.ToByte(args[4].Element, CultureInfo.InvariantCulture),
                f: Convert.ToByte(args[5].Element, CultureInfo.InvariantCulture),
                g: Convert.ToByte(args[6].Element, CultureInfo.InvariantCulture),
                h: Convert.ToByte(args[7].Element, CultureInfo.InvariantCulture),
                i: Convert.ToByte(args[8].Element, CultureInfo.InvariantCulture),
                j: Convert.ToByte(args[9].Element, CultureInfo.InvariantCulture),
                k: Convert.ToByte(args[10].Element, CultureInfo.InvariantCulture));

            return true;
        }
    }
}