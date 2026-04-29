// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Models;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
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

        /// <summary>Contains factory methods (parameterized constructors projected as <c>Create</c> methods).</summary>
        Factory,

        /// <summary>Contains instance methods, properties, and events not from implemented interfaces.</summary>
        Default
    }

    /// <summary>
    /// Gets the synthesized interface name for a class, following the Windows Runtime naming convention.
    /// </summary>
    /// <remarks>
    /// The convention is: <c>I{ClassName}Class</c> for default, <c>I{ClassName}Factory</c>
    /// for factory, and <c>I{ClassName}Static</c> for static interfaces.
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
    }

    /// <summary>
    /// Adds a single synthesized interface of the specified type for a runtime class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interface is only emitted if it has at least one member, or if it is the default interface
    /// and the class has no other interface implementations. When emitted, the interface receives
    /// <c>[Version]</c>, <c>[Guid]</c>, and <c>[ExclusiveTo]</c> attributes, and the appropriate
    /// metadata attribute is added to the class (<c>[Activatable]</c> for factory, <c>[Static]</c>
    /// for static, <c>[Default]</c> for default).
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

        // Add members to the synthesized interface
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic)
            {
                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Factory &&
                method.IsConstructor &&
                method.Parameters.Count > 0)
            {
                // Factory methods: parameterized constructors become Create methods
                hasMembers = true;
                AddFactoryMethod(synthesizedInterface, inputType, method);
            }
            else if (interfaceType == SynthesizedInterfaceType.Static && method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
                hasMembers = true;
                AddMethodToInterface(synthesizedInterface, method);
            }
            else if (interfaceType == SynthesizedInterfaceType.Default && !method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
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
            bool isStatic = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true;
            bool isPublic = property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true;

            if (!isPublic)
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
        if (hasMembers || (interfaceType == SynthesizedInterfaceType.Default && inputType.Interfaces.Count == 0))
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

            // Add version attribute
            AddVersionAttribute(synthesizedInterface, version);

            // Add GUID attribute
            AddGuidAttributeFromName(synthesizedInterface, interfaceName);

            // Add ExclusiveTo attribute
            AddExclusiveToAttribute(synthesizedInterface, inputType.FullName);

            if (interfaceType == SynthesizedInterfaceType.Factory)
            {
                AddActivatableAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
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
    /// Parameterized constructors are projected as <c>Create{ClassName}</c> factory methods
    /// in the factory interface. The return type is the runtime class itself.
    /// </remarks>
    /// <param name="synthesizedInterface">The factory interface to add the method to.</param>
    /// <param name="classType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="constructor">The parameterized constructor <see cref="MethodDefinition"/>.</param>
    private void AddFactoryMethod(TypeDefinition synthesizedInterface, TypeDefinition classType, MethodDefinition constructor)
    {
        // Look up the output class TypeDefinition to use as the return type
        string classFullName = classType.FullName;
        TypeDefinition outputClassType = _typeDefinitionMapping[classFullName].OutputType!;
        TypeSignature returnType = new TypeDefOrRefSignature(outputClassType, isValueType: false);

        TypeSignature[] parameterTypes = [.. constructor.Signature!.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodDefinition factoryMethod = new(
            name: "Create" + classType.Name!.Value,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            signature: MethodSignature.CreateInstance(returnType, parameterTypes));

        // Add parameter definitions with correct Windows Runtime attributes
        AddParameterDefinitions(factoryMethod, constructor);

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