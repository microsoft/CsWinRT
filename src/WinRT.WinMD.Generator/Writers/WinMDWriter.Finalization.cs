// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Finalizes the WinMD generation by adding MethodImpls, version attributes, and custom attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method runs after all types have been processed and performs four finalization phases:
    /// </para>
    /// <list type="number">
    ///   <item>Add <c>MethodImpl</c> fixups for classes (wiring interface methods to class implementations).</item>
    ///   <item>Add a default <c>[Version]</c> attribute for types that don't have one.</item>
    ///   <item>Copy custom attributes from input types to output types.</item>
    ///   <item>Add <c>[Overload]</c> attributes for methods with the same name.</item>
    /// </list>
    /// <para>
    /// The <c>MethodImpl</c> phase is the most complex: it resolves interfaces from both the output
    /// module and the input (reference) assemblies, handles generic type arguments, and handles
    /// explicit vs. implicit interface implementations.
    /// </para>
    /// </remarks>
    public void FinalizeGeneration()
    {
        // Phase 1: Add 'MethodImpl' fixups for classes.
        // Snapshot the mapping to avoid modification during iteration ('ProcessType' may add entries via 'MapTypeSignatureToOutput').
        List<KeyValuePair<string, TypeDeclaration>> typeDeclarations = [.. _typeDefinitionMapping];

        foreach ((_, TypeDeclaration declaration) in typeDeclarations)
        {
            if (declaration.OutputType is null || declaration.InputType is null || !declaration.IsComponentType)
            {
                continue;
            }

            AddMethodImplFixups(declaration);
        }

        // Phase 2: Add default version attributes for types that don't have one
        int defaultVersion = Version.Parse(_version).Major;

        foreach ((string _, TypeDeclaration declaration) in typeDeclarations)
        {
            if (declaration.OutputType is null)
            {
                continue;
            }

            // Skip adding '[Version]' attribute if the input type has '[ContractVersion]'
            // attribute (it will be copied in Phase 3 via 'CopyCustomAttributes').
            if (!declaration.OutputType.HasVersionAttribute &&
                declaration.InputType is not { HasContractVersionAttribute: true })
            {
                // Use the version from the input type if available, otherwise use the default
                int version = declaration.InputType is not null ? GetVersion(declaration.InputType) : defaultVersion;

                AddVersionAttribute(declaration.OutputType, version);
            }
        }

        // Phase 3: Add custom attributes from input types to output types
        foreach ((string _, TypeDeclaration declaration) in typeDeclarations)
        {
            if (declaration.OutputType is null || declaration.InputType is null || !declaration.IsComponentType)
            {
                continue;
            }

            CopyCustomAttributes(declaration.InputType, declaration.OutputType);
        }

        // Phase 4: Add overload attributes for overloaded methods. Only interfaces (authored and
        // synthesized) carry '[Overload]' attributes, since a runtime class exposes its members
        // through interfaces (where the Windows Runtime ABI method names live). Emitting them on a
        // class as well would be redundant and could conflict with the names on its interfaces.
        // Activation and composition factory interfaces are excluded: Windows Runtime metadata does
        // not allow '[Overload]' on a factory method (MIDL5130), so their overloads are given unique
        // metadata names when they are emitted instead (see 'AddFactoryMethod').
        foreach ((string _, TypeDeclaration declaration) in typeDeclarations)
        {
            if (declaration.OutputType is not { IsInterface: true } outputInterface ||
                _factoryInterfaces.Contains(outputInterface))
            {
                continue;
            }

            AddOverloadAttributesForType(declaration.OutputType);
        }

        // Phase 5: Add the '[ExclusiveTo]' attribute to the authored '[Overridable]' interfaces. Windows
        // Runtime metadata requires every overridable (and protected) interface to be exclusive to the
        // class exposing it, or MIDL rejects that class as soon as another component derives from it.
        foreach (string interfaceName in _authoredOverridableInterfacesRequiringExclusiveTo)
        {
            if (_authoredOverridableInterfaceOwners.TryGetValue(interfaceName, out string? className) &&
                _typeDefinitionMapping.TryGetValue(interfaceName, out TypeDeclaration? interfaceDeclaration) &&
                interfaceDeclaration.OutputType is { } overridableInterface &&
                !overridableInterface.CustomAttributes.Any(static attribute =>
                    attribute.Constructor?.DeclaringType?.FullName == "Windows.Foundation.Metadata.ExclusiveToAttribute"))
            {
                AddExclusiveToAttribute(overridableInterface, className);
            }
        }
    }

    /// <summary>
    /// Adds <c>MethodImpl</c> fixups for all implemented interfaces on a class type declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method wires each interface method to its corresponding class implementation by creating
    /// <c>MethodImpl</c> entries. It handles three scenarios:
    /// </para>
    /// <list type="bullet">
    ///   <item>Output-resolvable interfaces: directly resolve from the output module.</item>
    ///   <item>Input-resolvable interfaces: fall back to the input (reference) assembly, mapping
    ///     .NET projection types to Windows Runtime equivalents.</item>
    ///   <item>Default synthesized interfaces: handled separately since they are generated by the tool.</item>
    /// </list>
    /// </remarks>
    /// <param name="declaration">The type declaration to add MethodImpl fixups for.</param>
    private void AddMethodImplFixups(TypeDeclaration declaration)
    {
        TypeDefinition classOutputType = declaration.OutputType!;
        TypeDefinition classInputType = declaration.InputType!;

        // Add MethodImpls for implemented interfaces (excluding the default synthesized interface, handled below).
        // Snapshot the interfaces list to avoid modification during iteration.
        List<InterfaceImplementation> outputInterfaces = [.. classOutputType.Interfaces];

        foreach (InterfaceImplementation classInterfaceImpl in outputInterfaces)
        {
            // Resolve the interface — handle TypeSpecification (generic instances) by resolving the GenericType
            bool resolvedFromInput = false;
            TypeSignature[]? interfaceGenericArgs = null;
            TypeDefinition? interfaceDef = null;

            if (classInterfaceImpl.Interface is TypeSpecification outputTypeSpecification
                && outputTypeSpecification.Signature is GenericInstanceTypeSignature genericInstanceSignature)
            {
                interfaceDef = SafeResolve(genericInstanceSignature.GenericType);
                interfaceGenericArgs = [.. genericInstanceSignature.TypeArguments];
            }
            else
            {
                interfaceDef = SafeResolve(classInterfaceImpl.Interface);

                // For same-module 'TypeRef's (created by 'EnsureTypeReference'), 'Resolve()' may fail
                // since the output module isn't in the resolver. Look up in our type mapping instead.
                if (interfaceDef is null && classInterfaceImpl.Interface is not null)
                {
                    string ifaceFullName = classInterfaceImpl.Interface.FullName ?? "";

                    if (_typeDefinitionMapping.TryGetValue(ifaceFullName, out TypeDeclaration? ifaceDecl) && ifaceDecl.OutputType is not null)
                    {
                        interfaceDef = ifaceDecl.OutputType;
                    }
                }
            }

            // If the output interface can't be resolved (Windows Runtime contract assemblies),
            // find the matching interface from the INPUT type which points to resolvable projection assemblies
            if (interfaceDef is null)
            {
                string outputIfaceName = GetInterfaceFullName(classInterfaceImpl.Interface!);

                foreach (InterfaceImplementation inputImpl in classInputType.Interfaces)
                {
                    if (inputImpl.Interface is not null && GetInterfaceFullName(inputImpl.Interface) == outputIfaceName)
                    {
                        interfaceDef = inputImpl.Interface is TypeSpecification inputTypeSpecification
                            && inputTypeSpecification.Signature is GenericInstanceTypeSignature inputGenericInstanceSignature
                            ? SafeResolve(inputGenericInstanceSignature.GenericType)
                            : SafeResolve(inputImpl.Interface);
                        resolvedFromInput = interfaceDef is not null;
                        break;
                    }
                }
            }

            if (interfaceDef is null)
            {
                // Still unresolvable — MethodImpls for mapped interfaces are already
                // created by 'AddCustomMappedTypeMembers', so this is expected for those.
                continue;
            }

            // Skip the default synthesized interface — it's handled separately below
            string interfaceFullName = interfaceDef.FullName;

            if (interfaceFullName == declaration.DefaultInterface)
            {
                continue;
            }

            AddMethodImplsForInterface(classOutputType, classInterfaceImpl, interfaceDef, resolvedFromInput, interfaceGenericArgs);
        }

        // Add MethodImpls for default synthesized interface
        if (declaration.DefaultInterface is not null &&
            _typeDefinitionMapping.TryGetValue(declaration.DefaultInterface, out TypeDeclaration? defaultInterfaceDecl) &&
            defaultInterfaceDecl.OutputType is not null)
        {
            TypeDefinition defaultInterface = defaultInterfaceDecl.OutputType;

            foreach (MethodDefinition interfaceMethod in defaultInterface.Methods)
            {
                MethodDefinition? classMethod = FindMatchingMethod(classOutputType, interfaceMethod);

                if (classMethod is not null)
                {
                    MemberReference interfaceMethodRef = new(defaultInterface, interfaceMethod.Name!.Value, interfaceMethod.Signature);
                    classOutputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, classMethod));
                }
            }
        }
    }

    /// <summary>
    /// Adds <c>MethodImpl</c> entries for all methods in a specific interface on a class.
    /// </summary>
    /// <param name="classOutputType">The class type in the output WinMD.</param>
    /// <param name="classInterfaceImpl">The interface implementation entry on the class.</param>
    /// <param name="interfaceDef">The resolved interface <see cref="TypeDefinition"/>.</param>
    /// <param name="resolvedFromInput">Whether the interface was resolved from the input assembly.</param>
    /// <param name="interfaceGenericArgs">The generic type arguments if the interface is a generic instantiation.</param>
    private void AddMethodImplsForInterface(
        TypeDefinition classOutputType,
        InterfaceImplementation classInterfaceImpl,
        TypeDefinition interfaceDef,
        bool resolvedFromInput,
        TypeSignature[]? interfaceGenericArgs)
    {
        string interfaceFullName = interfaceDef.FullName;
        List<MethodDefinition> interfaceMethods = [.. interfaceDef.Methods];

        foreach (MethodDefinition interfaceMethod in interfaceMethods)
        {
            // Check if an explicit implementation already exists for this interface method.
            // If so, prefer it — don't create a 'MethodImpl' for the public method.
            string explicitName = $"{interfaceFullName}.{interfaceMethod.Name?.Value}";
            int paramCount = interfaceMethod.Signature?.ParameterTypes.Count ?? 0;

            bool hasExplicitImpl = classOutputType.Methods.Any(m =>
                m.Name?.Value == explicitName &&
                (m.Signature?.ParameterTypes.Count ?? 0) == paramCount);

            MethodDefinition? classMethod;

            if (hasExplicitImpl)
            {
                classMethod = FindExplicitMethodImpl(classOutputType, explicitName, interfaceMethod, paramCount, resolvedFromInput);
            }
            else
            {
                // Find the corresponding method on the class by name.
                // When resolved from input ref assemblies, map .NET projection types to Windows Runtime equivalents.
                classMethod = FindMatchingMethod(classOutputType, interfaceMethod, resolvedFromInput, interfaceGenericArgs);

                // Fallback for event methods from ref assemblies: CsWinRT projections change
                // event accessor signatures (e.g., remove_ takes delegate instead of EventRegistrationToken).
                // Match by name only since Windows Runtime event accessors are unique by name.
                if (classMethod is null && resolvedFromInput && interfaceMethod.IsSpecialName)
                {
                    string methodName = interfaceMethod.Name?.Value ?? "";
                    classMethod = classOutputType.Methods.FirstOrDefault(m => m.Name?.Value == methodName);
                }
            }

            if (classMethod is not null)
            {
                // Use the class method's signature for the 'MethodImpl' declaration when resolved
                // from input ref assemblies — the ref assembly uses .NET projection types
                // (e.g., 'System.Type') but the WinMD needs Windows Runtime types (e.g., 'TypeName')
                MethodSignature implSignature = resolvedFromInput ? classMethod.Signature! : interfaceMethod.Signature!;
                MemberReference interfaceMethodRef = new(classInterfaceImpl.Interface, interfaceMethod.Name!.Value, implSignature);
                classOutputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, classMethod));
            }
        }
    }

    /// <summary>
    /// Finds an explicit interface implementation method on the class by matching the explicit name
    /// and parameter types.
    /// </summary>
    /// <param name="classOutputType">The class to search.</param>
    /// <param name="explicitName">The fully-qualified explicit method name.</param>
    /// <param name="interfaceMethod">The interface method to match against.</param>
    /// <param name="paramCount">The number of parameters to match.</param>
    /// <param name="resolvedFromInput">Whether to use projection-equivalent type matching.</param>
    /// <returns>The matching method, or <see langword="null"/> if not found.</returns>
    private MethodDefinition? FindExplicitMethodImpl(
        TypeDefinition classOutputType,
        string explicitName,
        MethodDefinition interfaceMethod,
        int paramCount,
        bool resolvedFromInput)
    {
        return classOutputType.Methods.FirstOrDefault(m =>
        {
            if (m.Name?.Value != explicitName)
            {
                return false;
            }

            if ((m.Signature?.ParameterTypes.Count ?? 0) != paramCount)
            {
                return false;
            }

            for (int i = 0; i < paramCount; i++)
            {
                string classParamName = m.Signature!.ParameterTypes[i].FullName;
                string ifaceParamName = interfaceMethod.Signature!.ParameterTypes[i].FullName;

                if (classParamName != ifaceParamName &&
                    !(resolvedFromInput && IsProjectionEquivalent(ifaceParamName, classParamName)))
                {
                    return false;
                }
            }

            return true;
        });
    }

    /// <summary>
    /// Finds a method on a class that matches an interface method by name and parameter types.
    /// </summary>
    /// <param name="classType">The class to search.</param>
    /// <param name="interfaceMethod">The interface method to match against.</param>
    /// <param name="mapInterfaceTypes">Whether to map .NET projection types to Windows Runtime equivalents when comparing.</param>
    /// <param name="interfaceGenericArgs">Generic type arguments to resolve generic parameters in the interface method.</param>
    /// <returns>The matching method, or <see langword="null"/> if not found.</returns>
    private MethodDefinition? FindMatchingMethod(
        TypeDefinition classType,
        MethodDefinition interfaceMethod,
        bool mapInterfaceTypes = false,
        TypeSignature[]? interfaceGenericArgs = null)
    {
        string methodName = interfaceMethod.Name?.Value ?? "";

        foreach (MethodDefinition classMethod in classType.Methods)
        {
            if (classMethod.Name?.Value != methodName)
            {
                continue;
            }

            // Match parameter count
            if (classMethod.Signature?.ParameterTypes.Count != interfaceMethod.Signature?.ParameterTypes.Count)
            {
                continue;
            }

            // Match parameter types
            bool parametersMatch = true;

            for (int i = 0; i < (classMethod.Signature?.ParameterTypes.Count ?? 0); i++)
            {
                string classParamName = classMethod.Signature!.ParameterTypes[i].FullName;
                TypeSignature ifaceParamType = interfaceMethod.Signature!.ParameterTypes[i];

                // Resolve generic parameters (!0, !1) using the interface's generic arguments
                if (interfaceGenericArgs is not null)
                {
                    ifaceParamType = ResolveGenericArg(ifaceParamType, interfaceGenericArgs);
                }

                string ifaceParamName = ifaceParamType.FullName;

                if (classParamName != ifaceParamName)
                {
                    // When comparing against externally-resolved interface methods (from ref assemblies),
                    // check if the .NET projection type maps to the Windows Runtime type via 'TypeMapper'
                    if (!mapInterfaceTypes || !IsProjectionEquivalent(ifaceParamName, classParamName))
                    {
                        parametersMatch = false;
                        break;
                    }
                }
            }

            if (!parametersMatch)
            {
                continue;
            }

            return classMethod;
        }

        return null;
    }

    /// <summary>
    /// Checks if a .NET projection type name maps to a Windows Runtime type name via the <see cref="Helpers.TypeMapper"/>.
    /// </summary>
    /// <param name="dotNetTypeName">The .NET projection type name (e.g. <c>"System.Collections.Generic.IEnumerable`1"</c>).</param>
    /// <param name="winrtTypeName">The Windows Runtime type name to compare against (e.g. <c>"Windows.Foundation.Collections.IIterable`1"</c>).</param>
    /// <returns><see langword="true"/> if the .NET type maps to the Windows Runtime type; otherwise, <see langword="false"/>.</returns>
    private bool IsProjectionEquivalent(string dotNetTypeName, string winrtTypeName)
    {
        // Strip generic type arguments for mapper lookup.
        // E.g., "System.Collections.Generic.IEnumerable`1<System.String>" → "System.Collections.Generic.IEnumerable`1"
        // The mapper uses open generic names as keys.
        string lookupName = dotNetTypeName;
        int angleBracket = dotNetTypeName.IndexOf('<');

        if (angleBracket > 0)
        {
            lookupName = dotNetTypeName[..angleBracket];
        }

        if (_mapper.HasMappingForType(lookupName))
        {
            MappedTypeInfo mappedTypeInfo = _mapper.GetMappedType(lookupName).GetMappedTypeInfo();
            string mappedName = mappedTypeInfo.FullName;

            // For generic types, compare the open generic name portion of both
            if (angleBracket > 0)
            {
                int winrtAngle = winrtTypeName.IndexOf('<');
                string winrtOpenName = winrtAngle > 0 ? winrtTypeName[..winrtAngle] : winrtTypeName;

                return mappedName == winrtOpenName;
            }

            return mappedName == winrtTypeName;
        }

        return false;
    }

    /// <summary>
    /// Adds <c>[Overload]</c> attributes to overloaded methods within a type.
    /// </summary>
    /// <remarks>
    /// Windows Runtime requires that overloaded methods have unique names. This method finds method groups
    /// with the same name and assigns a unique name to every overload except the default one (the method
    /// marked with <c>[DefaultOverload]</c>, or the first in metadata order when none is marked), which keeps
    /// the original name. When the author has applied <c>[Overload("...")]</c> on a method, that name is honored
    /// as-is; otherwise a unique sequential name (<c>[Overload("MethodName2")]</c>, <c>[Overload("MethodName3")]</c>,
    /// etc.) is generated, skipping any name already used by another member or a previously assigned overload.
    /// </remarks>
    /// <param name="type">The type to add overload attributes to.</param>
    private void AddOverloadAttributesForType(TypeDefinition type)
    {
        List<MethodDefinition> methods = [.. type.Methods.Where(m => !m.IsConstructor && !m.IsSpecialName)];

        // Collect the names already in use within the type, so auto-generated overload names can avoid
        // collisions: every member name (methods including accessors, properties and events) and any
        // author-specified overload name. Auto-generated names are added to the set as they are produced,
        // so they cannot collide with each other across groups (e.g. 'M1' + '2' and 'M' + '12').
        HashSet<string> reservedNames = new(StringComparer.Ordinal);

        foreach (MethodDefinition method in type.Methods)
        {
            _ = reservedNames.Add(method.Name?.Value ?? "");

            if (_userSpecifiedOverloadNames.TryGetValue(method, out string? userOverloadName))
            {
                _ = reservedNames.Add(userOverloadName);
            }
        }

        foreach (PropertyDefinition property in type.Properties)
        {
            _ = reservedNames.Add(property.Name?.Value ?? "");
        }

        foreach (EventDefinition @event in type.Events)
        {
            _ = reservedNames.Add(@event.Name?.Value ?? "");
        }

        // Group methods by name to find overloaded methods
        foreach (IGrouping<string, MethodDefinition> group in methods.GroupBy(m => m.Name?.Value ?? "").Where(g => g.Count() > 1))
        {
            // The default overload keeps the original (non-overloaded) name: the one marked with
            // '[DefaultOverload]', or the first in metadata order when none is marked. Every other
            // overload needs a unique name (author-specified when present, otherwise auto-generated).
            MethodDefinition defaultMethod = group.FirstOrDefault(HasDefaultOverloadAttribute) ?? group.First();

            int lastSuffix = 1;

            foreach (MethodDefinition method in group)
            {
                if (method == defaultMethod)
                {
                    continue;
                }

                // Honor an author-applied '[Overload("...")]' name when present (see 'RecordUserSpecifiedOverloadName')
                if (_userSpecifiedOverloadNames.TryGetValue(method, out string? overloadName))
                {
                    AddOverloadAttribute(method, overloadName);

                    continue;
                }

                // Otherwise auto-generate the next sequential name that is not already in use (and reserve it)
                do
                {
                    overloadName = $"{group.Key}{++lastSuffix}";
                }
                while (!reservedNames.Add(overloadName));

                AddOverloadAttribute(method, overloadName);
            }
        }
    }

    /// <summary>
    /// Checks whether a method is marked with <c>[Windows.Foundation.Metadata.DefaultOverload]</c>.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns><see langword="true"/> if the method has the attribute; otherwise, <see langword="false"/>.</returns>
    private static bool HasDefaultOverloadAttribute(MethodDefinition method)
    {
        return method.FindCustomAttributes("Windows.Foundation.Metadata", "DefaultOverloadAttribute").Any();
    }

    /// <summary>
    /// Records the overload name explicitly specified by the author via
    /// <c>[Windows.Foundation.Metadata.Overload("...")]</c> on an input method, so it can be honored
    /// by <see cref="AddOverloadAttributesForType"/> during finalization.
    /// </summary>
    /// <remarks>
    /// The author-applied <c>[Overload]</c> attribute is intentionally not copied verbatim to the output method
    /// (see <c>ShouldCopyAttribute</c>); it is re-emitted by <see cref="AddOverloadAttribute"/> as the single
    /// source of truth, so the overload name is applied only to genuinely overloaded methods and always
    /// references the Windows Runtime contract assembly.
    /// </remarks>
    /// <param name="inputMethod">The input <see cref="MethodDefinition"/> to read the attribute from.</param>
    /// <param name="outputMethod">The output <see cref="MethodDefinition"/> to associate the name with.</param>
    private void RecordUserSpecifiedOverloadName(MethodDefinition inputMethod, MethodDefinition outputMethod)
    {
        if (inputMethod.FindCustomAttributes("Windows.Foundation.Metadata", "OverloadAttribute").FirstOrDefault() is not CustomAttribute attribute)
        {
            return;
        }

        // The single fixed argument is the overload name. AsmResolver stores attribute string arguments
        // as 'Utf8String' (not 'System.String'), so it is matched as a non-null element and converted.
        if (attribute.Signature is { FixedArguments: [{ Element: { } overloadName }] })
        {
            _userSpecifiedOverloadNames[outputMethod] = overloadName.ToString()!;
        }
    }

    /// <summary>
    /// Adds an <c>[Overload]</c> attribute to a method.
    /// </summary>
    /// <param name="method">The method to add the attribute to.</param>
    /// <param name="overloadName">The unique overload name to assign.</param>
    private void AddOverloadAttribute(MethodDefinition method, string overloadName)
    {
        TypeReference overloadAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "OverloadAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(
            parent: overloadAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [_outputModule.CorLibTypeFactory.String]));

        CustomAttributeSignature signature = new();
        signature.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.String, overloadName));

        method.CustomAttributes.Add(new CustomAttribute(ctor, signature));
    }
}
