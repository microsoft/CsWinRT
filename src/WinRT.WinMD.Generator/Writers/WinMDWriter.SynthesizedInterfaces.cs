// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Models;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// The type of synthesized Windows Runtime interface to generate for a runtime class.
    /// </summary>
    private enum SynthesizedInterfaceType
    {
        /// <summary>Contains static methods, properties, and events from the class.</summary>
        Static,

        /// <summary>Contains activation or composition factory methods projected from constructors.</summary>
        Factory,

        /// <summary>Contains instance methods, properties, and events not from implemented interfaces.</summary>
        Default,

        /// <summary>Contains the instance members a composable class only exposes to derived types.</summary>
        Protected,

        /// <summary>Contains the instance members a derived type is allowed to override.</summary>
        Overridable
    }

    /// <summary>
    /// Gets the synthesized interface name for a class, following the Windows Runtime naming convention.
    /// </summary>
    /// <remarks>
    /// The convention is: <c>I{ClassName}Class</c> for default, <c>I{ClassName}Factory</c>
    /// for factory, <c>I{ClassName}Static</c> for static, <c>I{ClassName}Protected</c> for protected,
    /// and <c>I{ClassName}Overrides</c> for overridable interfaces.
    /// </remarks>
    /// <param name="className">The simple name of the runtime class.</param>
    /// <param name="type">The type of synthesized interface.</param>
    /// <returns>The synthesized interface name (e.g., <c>"IFooClass"</c>).</returns>
    private static string GetSynthesizedInterfaceName(string className, SynthesizedInterfaceType type)
    {
        return "I" + className + type switch
        {
            SynthesizedInterfaceType.Default => "Class",
            SynthesizedInterfaceType.Factory => "Factory",
            SynthesizedInterfaceType.Static => "Static",
            SynthesizedInterfaceType.Protected => "Protected",
            SynthesizedInterfaceType.Overridable => "Overrides",
            _ => "",
        };
    }

    /// <summary>
    /// Adds all synthesized interfaces (<c>IFooClass</c>, <c>IFooFactory</c>, <c>IFooStatic</c>)
    /// for a runtime class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows Runtime requires runtime classes to express their public API surface through interfaces.
    /// This method creates up to three synthesized interfaces containing the class's own members
    /// (not those inherited from explicitly implemented interfaces):
    /// </para>
    /// <list type="bullet">
    ///   <item><c>IFooStatic</c>: static methods, properties, and events.</item>
    ///   <item><c>IFooFactory</c>: parameterized constructors projected as factory methods.</item>
    ///   <item><c>IFooClass</c>: instance members not already provided by implemented interfaces.</item>
    /// </list>
    /// <para>
    /// Members are excluded from synthesized interfaces if they come from implemented interfaces
    /// (including custom-mapped interfaces, explicit implementations, and <c>MethodImpl</c> entries).
    /// </para>
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="classOutputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="classDeclaration">The <see cref="TypeDeclaration"/> tracking the class.</param>
    private void AddSynthesizedInterfaces(TypeDefinition inputType, TypeDefinition classOutputType, TypeDeclaration classDeclaration)
    {
        // Static vs non-static member filtering is handled below per-member

        // Collect members that come from interface implementations
        HashSet<string> membersFromInterfaces = [];

        // Use all interfaces including inherited ones from the input type
        List<InterfaceImplementation> allInterfaces = GatherAllInterfaces(inputType);
        foreach (InterfaceImplementation interfaceImplementation in allInterfaces)
        {
            TypeDefinition? interfaceDef = interfaceImplementation.Interface is TypeSpecification typeSpecification
                ? SafeResolve((typeSpecification.Signature as GenericInstanceTypeSignature)?.GenericType)
                : SafeResolve(interfaceImplementation.Interface);

            if (interfaceDef is not null)
            {
                foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
                {
                    _ = membersFromInterfaces.Add(interfaceMethod.Name?.Value ?? "");
                }

                foreach (PropertyDefinition prop in interfaceDef.Properties)
                {
                    _ = membersFromInterfaces.Add(prop.Name?.Value ?? "");
                }

                foreach (EventDefinition @event in interfaceDef.Events)
                {
                    _ = membersFromInterfaces.Add(@event.Name?.Value ?? "");
                }
            }
        }

        // Also include members from custom mapped interfaces (already excluded from the class)
        HashSet<string> customMappedNames = CollectCustomMappedMemberNames(inputType);
        membersFromInterfaces.UnionWith(customMappedNames);

        // Also detect explicit interface implementations from the compiled IL
        // (private methods with dots in their names like "AuthoringTest.IDouble.GetDouble")
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic && method.Name?.Value?.Contains('.') == true)
            {
                // Extract the method name after the last dot
                string fullName = method.Name.Value;
                int lastDot = fullName.LastIndexOf('.');
                if (lastDot > 0)
                {
                    _ = membersFromInterfaces.Add(fullName[(lastDot + 1)..]);
                }
            }
        }

        // Also use 'MethodImplementations' from the input type's IL to detect implicit interface
        // implementations. This handles cases where a public class method implicitly implements
        // an external interface method (e.g., 'IWwwFormUrlDecoderEntry.get_Name') — the compiler
        // generates 'MethodImpl' entries that tell us which methods come from interfaces.
        foreach (MethodImplementation methodImpl in inputType.MethodImplementations)
        {
            if (methodImpl.Body is MethodDefinition bodyMethod && bodyMethod.IsPublic)
            {
                _ = membersFromInterfaces.Add(bodyMethod.Name?.Value ?? "");
            }
        }

        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Static, membersFromInterfaces);
        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Factory, membersFromInterfaces);
        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Default, membersFromInterfaces);

        // Only a composable class can be derived from outside the component, so it is the only shape for
        // which the protected and overridable surfaces are meaningful. For every other runtime class those
        // members stay entirely internal to the component, exactly as they were before.
        if (HasPublicCompositionFactory(inputType))
        {
            AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Protected, membersFromInterfaces);
            AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Overridable, membersFromInterfaces);
        }
    }

    /// <summary>
    /// Adds a single synthesized interface of the specified type for a runtime class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interface is only emitted if it has at least one member, or if it is the default interface
    /// and the class has no other interface implementations. When emitted, the interface receives
    /// <c>[Version]</c>, <c>[Guid]</c>, and <c>[ExclusiveTo]</c> attributes, and the appropriate
    /// metadata attribute is added to the class (<c>[Activatable]</c> or <c>[Composable]</c>
    /// for factory, <c>[Static]</c> for static, <c>[Default]</c> for default).
    /// </para>
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="classOutputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="classDeclaration">The <see cref="TypeDeclaration"/> tracking the class.</param>
    /// <param name="interfaceType">The type of synthesized interface to create.</param>
    /// <param name="membersFromInterfaces">Set of member names already provided by implemented interfaces.</param>
    private void AddSynthesizedInterface(
        TypeDefinition inputType,
        TypeDefinition classOutputType,
        TypeDeclaration classDeclaration,
        SynthesizedInterfaceType interfaceType,
        HashSet<string> membersFromInterfaces)
    {
        bool hasMembers = false;
        string @namespace = inputType.Namespace?.Value ?? "";
        string className = inputType.Name!.Value;
        string interfaceName = GetSynthesizedInterfaceName(className, interfaceType);

        TypeAttributes typeAttributes =
            TypeAttributes.NotPublic |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Interface |
            TypeAttributes.Abstract;

        TypeDefinition synthesizedInterface = new(@namespace, interfaceName, typeAttributes);

        bool isComposable = HasPublicCompositionFactory(inputType);

        // Add members to the synthesized interface
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (interfaceType == SynthesizedInterfaceType.Protected)
            {
                if (!method.IsSpecialName && IsComposableProtectedMember(method))
                {
                    hasMembers = true;
                    AddMethodToInterface(synthesizedInterface, method);
                }

                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Overridable)
            {
                if (!method.IsSpecialName && IsComposableOverridableMember(inputType, method))
                {
                    hasMembers = true;
                    AddMethodToInterface(synthesizedInterface, method);
                }

                continue;
            }

            if (!method.IsPublic)
            {
                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Factory &&
                method.IsConstructor &&
                (isComposable ||
                 (!inputType.IsAbstract && method.Parameters.Count > 0)))
            {
                hasMembers = true;
                AddFactoryMethod(synthesizedInterface, inputType, method, isComposable);
            }
            else if (interfaceType == SynthesizedInterfaceType.Static && method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
                hasMembers = true;
                AddMethodToInterface(synthesizedInterface, method);
            }
            else if (interfaceType == SynthesizedInterfaceType.Default && !method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
                // Overridable members of a composable class belong on its '[Overridable]' interface
                if (isComposable && IsComposableOverridableMember(inputType, method))
                {
                    continue;
                }

                // An override of an authored base class member is already declared by that base class
                if (method.IsVirtual && !method.IsNewSlot && OverridesAuthoredBaseMember(inputType, method.Name?.Value ?? ""))
                {
                    continue;
                }

                // Only include members not already from an interface
                if (!membersFromInterfaces.Contains(method.Name?.Value ?? ""))
                {
                    hasMembers = true;
                    AddMethodToInterface(synthesizedInterface, method);
                }
            }
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            if (GetPrimaryAccessor(property) is { IsVirtual: true, IsNewSlot: false } overrideAccessor &&
                OverridesAuthoredBaseMember(inputType, overrideAccessor.Name?.Value ?? ""))
            {
                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Protected)
            {
                if (IsComposableProtectedProperty(property))
                {
                    hasMembers = true;
                    AddPropertyToType(synthesizedInterface, property, isInterfaceParent: true, allowNonPublicSetter: true);
                }

                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Overridable)
            {
                if (IsComposableOverridableProperty(inputType, property))
                {
                    hasMembers = true;
                    AddPropertyToType(synthesizedInterface, property, isInterfaceParent: true, allowNonPublicSetter: true);
                }

                continue;
            }

            bool isStatic = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true;
            bool isPublic = property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true;

            if (!isPublic)
            {
                continue;
            }

            // Overridable properties of a composable class belong on its '[Overridable]' interface
            if (isComposable && IsComposableOverridableProperty(inputType, property))
            {
                continue;
            }

            if ((interfaceType == SynthesizedInterfaceType.Static && isStatic) ||
                (interfaceType == SynthesizedInterfaceType.Default && !isStatic))
            {
                // For default interface, skip properties already fully provided by an implemented interface.
                // If the interface only has a getter but the class also has a setter, emit the setter
                // on the exclusive interface so it's accessible from native consumers.
                if (interfaceType == SynthesizedInterfaceType.Default)
                {
                    string getterName = "get_" + property.Name!.Value;
                    string setterName = "set_" + property.Name!.Value;
                    bool getterFromInterface = membersFromInterfaces.Contains(getterName);
                    bool setterFromInterface = membersFromInterfaces.Contains(setterName);

                    if (getterFromInterface && setterFromInterface)
                    {
                        // Both getter and setter are from an interface, skip entirely
                        continue;
                    }

                    if (getterFromInterface && !setterFromInterface && property.SetMethod?.IsPublic == true)
                    {
                        // Getter is from an interface but class adds a public setter - emit setter only
                        hasMembers = true;
                        AddSetterOnlyPropertyToType(synthesizedInterface, property);
                        continue;
                    }

                    if (getterFromInterface)
                    {
                        // Getter is from interface, no setter to add
                        continue;
                    }
                }

                hasMembers = true;
                AddPropertyToType(synthesizedInterface, property, isInterfaceParent: true);
            }
        }

        // Add events
        foreach (EventDefinition @event in inputType.Events)
        {
            // Protected and overridable events are not projected: an event accessor pair has no
            // '[UnsafeAccessor]'-based CCW dispatch in the generated projection, so exposing them
            // would produce a vtable that cannot be implemented. They stay internal to the component,
            // exactly as they did before composable classes were supported.
            if (interfaceType is SynthesizedInterfaceType.Protected or SynthesizedInterfaceType.Overridable)
            {
                break;
            }

            bool isStatic = @event.AddMethod?.IsStatic == true;
            bool isPublic = @event.AddMethod?.IsPublic == true || @event.RemoveMethod?.IsPublic == true;

            if (!isPublic)
            {
                continue;
            }

            if ((interfaceType == SynthesizedInterfaceType.Static && isStatic) ||
                (interfaceType == SynthesizedInterfaceType.Default && !isStatic))
            {
                // For default interface, skip events already provided by an implemented interface
                if (interfaceType == SynthesizedInterfaceType.Default)
                {
                    string adderName = "add_" + @event.Name!.Value;
                    if (membersFromInterfaces.Contains(adderName))
                    {
                        continue;
                    }
                }

                hasMembers = true;
                AddEventToType(synthesizedInterface, @event, isInterfaceParent: true);
            }
        }

        // Only emit the interface if it has members, or if it's the default and the class has no other interfaces
        if (hasMembers ||
            (interfaceType == SynthesizedInterfaceType.Default &&
             (inputType.Interfaces.Count == 0 ||
              (isComposable && !HasUsableDefaultInterface(inputType)))))
        {
            _outputModule.TopLevelTypes.Add(synthesizedInterface);

            string qualifiedInterfaceName = string.IsNullOrEmpty(@namespace) ? interfaceName : $"{@namespace}.{interfaceName}";

            TypeDeclaration interfaceDeclaration = new(null, synthesizedInterface, isComponentType: false);
            _typeDefinitionMapping[qualifiedInterfaceName] = interfaceDeclaration;

            int version = GetVersion(inputType);

            if (interfaceType == SynthesizedInterfaceType.Default)
            {
                classDeclaration.DefaultInterface = qualifiedInterfaceName;

                // Add interface implementation on the class (use 'TypeRef' per WinMD convention)
                InterfaceImplementation interfaceImpl = new(EnsureTypeReference(synthesizedInterface));
                classOutputType.Interfaces.Add(interfaceImpl);

                // Add '[Default]' attribute on the interface implementation
                AddDefaultAttribute(interfaceImpl);
            }
            else if (interfaceType is SynthesizedInterfaceType.Protected or SynthesizedInterfaceType.Overridable)
            {
                // The class implements its protected and overridable interfaces, with the marker attribute
                // on the interface implementation itself (this is where MIDL places them too), so that both
                // the CsWinRT and the C++/WinRT projections can tell them apart from the public surface.
                InterfaceImplementation interfaceImpl = new(EnsureTypeReference(synthesizedInterface));
                classOutputType.Interfaces.Add(interfaceImpl);

                if (interfaceType == SynthesizedInterfaceType.Protected)
                {
                    AddProtectedAttribute(interfaceImpl);
                }
                else
                {
                    AddOverridableAttribute(interfaceImpl);
                }
            }

            // Add version attribute
            AddVersionAttribute(synthesizedInterface, version);

            // Add GUID attribute
            AddGuidAttributeFromName(synthesizedInterface, interfaceName);

            // Add ExclusiveTo attribute
            AddExclusiveToAttribute(synthesizedInterface, inputType.FullName);

            if (interfaceType == SynthesizedInterfaceType.Factory)
            {
                if (isComposable)
                {
                    AddComposableAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
                }
                else
                {
                    AddActivatableAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
                }
            }
            else if (interfaceType == SynthesizedInterfaceType.Static)
            {
                classDeclaration.StaticInterface = qualifiedInterfaceName;
                AddStaticAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
            }
        }
    }

    /// <summary>
    /// Adds a factory method to a synthesized factory interface.
    /// </summary>
    /// <remarks>
    /// Constructors are projected as <c>Create{ClassName}</c> factory methods. Composable
    /// factories also receive the controlling outer and return the non-delegating inner.
    /// </remarks>
    /// <param name="synthesizedInterface">The factory interface to add the method to.</param>
    /// <param name="classType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="constructor">The constructor <see cref="MethodDefinition"/>.</param>
    /// <param name="isComposable">Whether to append the controlling outer and non-delegating inner parameters.</param>
    private void AddFactoryMethod(
        TypeDefinition synthesizedInterface,
        TypeDefinition classType,
        MethodDefinition constructor,
        bool isComposable)
    {
        // Look up the output class TypeDefinition to use as the return type
        string classFullName = classType.FullName;
        TypeDefinition outputClassType = _typeDefinitionMapping[classFullName].OutputType!;
        TypeSignature returnType = new TypeDefOrRefSignature(outputClassType, isValueType: false);

        List<TypeSignature> parameterTypes = [.. constructor.Signature!.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        if (isComposable)
        {
            parameterTypes.Add(_outputModule.CorLibTypeFactory.Object);
            parameterTypes.Add(_outputModule.CorLibTypeFactory.Object.MakeByReferenceType());
        }

        MethodDefinition factoryMethod = new(
            name: "Create" + classType.Name!.Value,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            signature: MethodSignature.CreateInstance(returnType, parameterTypes));

        AddParameterDefinitions(factoryMethod, constructor);

        if (isComposable)
        {
            factoryMethod.ParameterDefinitions.Add(new ParameterDefinition(
                sequence: (ushort)(constructor.Parameters.Count + 1),
                name: "baseInterface",
                attributes: ParameterAttributes.In));
            factoryMethod.ParameterDefinitions.Add(new ParameterDefinition(
                sequence: (ushort)(constructor.Parameters.Count + 2),
                name: "innerInterface",
                attributes: ParameterAttributes.Out));
        }

        synthesizedInterface.Methods.Add(factoryMethod);
    }

    /// <summary>
    /// Gets the fully name of an interface, stripping generic type arguments for generic interfaces.
    /// </summary>
    /// <remarks>
    /// For a generic interface like <c>IList&lt;string&gt;</c>, returns the open generic name
    /// <c>"System.Collections.Generic.IList`1"</c> rather than the closed form.
    /// </remarks>
    /// <param name="type">The interface type reference.</param>
    /// <returns>The full name of the interface type.</returns>
    private static string GetInterfaceFullName(ITypeDefOrRef type)
    {
        return type is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst
            ? genericInst.GenericType.FullName
            : type.FullName;
    }
}