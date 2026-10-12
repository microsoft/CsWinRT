// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.References;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Finds the classes that authored Windows Runtime components declare to extend the activation factories
/// CsWinRT generates for them, by runtime class name.
/// </summary>
/// <remarks>
/// <para>
/// An author declares one as an <see langword="abstract"/> class annotated with
/// <c>[WindowsRuntimeActivationFactory(typeof(&lt;class&gt;))]</c>. The generated activation factory for that
/// class then derives from it, so any interfaces it implements (e.g. COM interop interfaces) end up on the
/// factory's vtable, alongside the activation and statics interfaces that are still generated as usual.
/// </para>
/// <para>
/// Only abstract classes are considered: a non-abstract one is a complete activation factory instead, which is
/// how runtime classes declared in existing metadata are implemented (see <c>CsWinRTImplementWinMDTypes</c>).
/// </para>
/// </remarks>
internal sealed class ComponentActivationFactoryExtensions
{
    /// <summary>The fully qualified C# names of the extension classes, by runtime class name.</summary>
    private readonly Dictionary<string, string> _extensionsByRuntimeClassName = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new <see cref="ComponentActivationFactoryExtensions"/> instance over the given metadata.
    /// </summary>
    /// <param name="metadata">The resolver for the component's managed implementation types.</param>
    /// <exception cref="WellKnownProjectionWriterException">Thrown if an extension class cannot be derived from, or if a runtime class has more than one.</exception>
    public ComponentActivationFactoryExtensions(ComponentImplementationMetadata metadata)
    {
        foreach (TypeDefinition type in metadata.Types)
        {
            if (!type.IsClass || !type.IsAbstract || type.IsSealed || !TryGetRuntimeClassName(type, out string? runtimeClassName))
            {
                continue;
            }

            // The generated factory is compiled into a separate assembly, so it can only derive from a class it can see
            if (!IsAccessibleFromOtherAssemblies(type) || !HasAccessibleParameterlessConstructor(type))
            {
                throw WellKnownProjectionWriterExceptions.InvalidActivationFactoryExtension(type.FullName, runtimeClassName);
            }

            string typeName = GetGlobalQualifiedName(type);

            if (!_extensionsByRuntimeClassName.TryAdd(runtimeClassName, typeName))
            {
                throw WellKnownProjectionWriterExceptions.DuplicateActivationFactoryExtension(runtimeClassName, _extensionsByRuntimeClassName[runtimeClassName], typeName);
            }
        }
    }

    /// <summary>
    /// Gets the class that the activation factory for a given runtime class extends, if any.
    /// </summary>
    /// <param name="runtimeClassName">The full name of the runtime class.</param>
    /// <returns>The fully qualified C# name of the class to derive from, or <see langword="null"/> if there is none.</returns>
    public string? GetExtensionTypeName(string runtimeClassName)
    {
        return _extensionsByRuntimeClassName.GetValueOrDefault(runtimeClassName);
    }

    /// <summary>
    /// Gets the runtime class a type declares itself the activation factory for, through <c>[WindowsRuntimeActivationFactory]</c>.
    /// </summary>
    private static bool TryGetRuntimeClassName(TypeDefinition type, [NotNullWhen(true)] out string? runtimeClassName)
    {
        foreach (CustomAttribute attribute in type.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is { } attributeType &&
                attributeType.Namespace?.Value == WellKnownNamespaces.WindowsRuntimeInteropServices &&
                attributeType.Name?.Value == WellKnownAttributeNames.WindowsRuntimeActivationFactoryAttribute &&
                attribute.Signature is { FixedArguments: [{ Element: TypeSignature runtimeClassType }] })
            {
                runtimeClassName = runtimeClassType.FullName;

                return true;
            }
        }

        runtimeClassName = null;

        return false;
    }

    /// <summary>
    /// Checks whether a type, and every type containing it, is public and not generic.
    /// </summary>
    private static bool IsAccessibleFromOtherAssemblies(TypeDefinition type)
    {
        for (TypeDefinition? current = type; current is not null; current = current.DeclaringType)
        {
            if (!(current.IsPublic || current.IsNestedPublic) || current.GenericParameters.Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a type has a parameterless constructor a derived class in another assembly can call.
    /// </summary>
    private static bool HasAccessibleParameterlessConstructor(TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0 &&
                (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the fully qualified C# name of a (possibly nested) type.
    /// </summary>
    private static string GetGlobalQualifiedName(TypeDefinition type)
    {
        Stack<string> names = new();
        TypeDefinition current = type;

        while (current.DeclaringType is { } declaringType)
        {
            names.Push(current.Name!.Value);

            current = declaringType;
        }

        StringBuilder builder = new("global::");

        if (current.Namespace?.Value is { Length: > 0 } ns)
        {
            _ = builder.Append(ns).Append('.');
        }

        _ = builder.Append(current.Name!.Value);

        foreach (string name in names)
        {
            _ = builder.Append('.').Append(name);
        }

        return builder.ToString();
    }
}
