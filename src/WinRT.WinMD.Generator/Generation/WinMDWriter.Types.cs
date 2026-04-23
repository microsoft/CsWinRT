// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Helpers;
using WindowsRuntime.WinMDGenerator.Models;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Checks if an enum type represents a WinRT API contract (has <c>[ApiContract]</c> attribute).
    /// </summary>
    /// <param name="type">The enum <see cref="TypeDefinition"/> to check.</param>
    /// <returns><see langword="true"/> if the type has an <c>[ApiContract]</c> attribute; otherwise, <see langword="false"/>.</returns>
    private static bool IsApiContract(TypeDefinition type)
    {
        return type.CustomAttributes.Any(
            attr => attr.Constructor?.DeclaringType?.Name?.Value == "ApiContractAttribute");
    }

    /// <summary>
    /// Emits an API contract type as an empty struct in the WinMD.
    /// </summary>
    /// <remarks>
    /// In C#, API contracts are projected as enums with <c>[ApiContract]</c>, but in WinMD
    /// metadata they are represented as empty structs per the WinRT type system spec.
    /// </remarks>
    /// <param name="inputType">The API contract enum <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddApiContractType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "ValueType", "mscorlib");

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;
    }

    /// <summary>
    /// Adds an enum type to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Handles API contract types specially (emitted as empty structs). For regular enums,
    /// creates the WinMD enum type with the <c>value__</c> field and all public enum members
    /// with their constant values.
    /// </remarks>
    /// <param name="inputType">The enum <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddEnumType(TypeDefinition inputType)
    {
        // API contract types are projected as enums in C# but emitted as empty structs in WinMD
        if (IsApiContract(inputType))
        {
            AddApiContractType(inputType);
            return;
        }

        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "Enum", "mscorlib");

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Add the value__ field
        TypeSignature underlyingType = GetEnumUnderlyingType(inputType);
        FieldDefinition valueField = new(
            "value__",
            FieldAttributes.Private | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            new FieldSignature(underlyingType));
        outputType.Fields.Add(valueField);

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Enum literal fields use the enum type itself (not the underlying type)
        TypeSignature enumTypeSignature = new TypeDefOrRefSignature(outputType, isValueType: true);

        // Add enum members
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (field.IsSpecialName)
            {
                continue; // Skip value__
            }

            if (!field.IsPublic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                field.Name!.Value,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
                new FieldSignature(enumTypeSignature));

            if (field.Constant != null)
            {
                outputField.Constant = new Constant(field.Constant.Type, new DataBlobSignature(field.Constant.Value!.Data));
            }

            outputType.Fields.Add(outputField);
        }
    }

    /// <summary>
    /// Gets the underlying type of an enum (e.g., <c>Int32</c>, <c>UInt32</c>) by inspecting its <c>value__</c> field.
    /// </summary>
    /// <param name="enumType">The enum <see cref="TypeDefinition"/> to inspect.</param>
    /// <returns>The <see cref="TypeSignature"/> of the underlying type, defaulting to <c>Int32</c> if not found.</returns>
    private TypeSignature GetEnumUnderlyingType(TypeDefinition enumType)
    {
        foreach (FieldDefinition field in enumType.Fields)
        {
            if (field.IsSpecialName && field.Name?.Value == "value__")
            {
                return MapTypeSignatureToOutput(field.Signature!.FieldType);
            }
        }

        // Default to Int32
        return _outputModule.CorLibTypeFactory.Int32;
    }

    /// <summary>
    /// Adds a delegate type to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Creates the WinMD delegate type with the required <c>.ctor(object, IntPtr)</c> constructor
    /// (private per WinRT delegate convention) and the <c>Invoke</c> method with mapped parameter
    /// and return types. Also adds the <c>[Guid]</c> attribute.
    /// </remarks>
    /// <param name="inputType">The delegate <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddDelegateType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "MulticastDelegate", "mscorlib");

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add .ctor(object, IntPtr) — private per WinRT delegate convention
        MethodDefinition ctor = new(
            ".ctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [_outputModule.CorLibTypeFactory.Object,
                _outputModule.CorLibTypeFactory.IntPtr]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };
        ctor.ParameterDefinitions.Add(new ParameterDefinition(1, "object", 0));
        ctor.ParameterDefinitions.Add(new ParameterDefinition(2, "method", 0));
        outputType.Methods.Add(ctor);

        // Add Invoke method
        MethodDefinition? inputInvoke = inputType.Methods.FirstOrDefault(m => m.Name?.Value == "Invoke");
        if (inputInvoke != null)
        {
            TypeSignature returnType = inputInvoke.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
                ? _outputModule.CorLibTypeFactory.Void
                : MapTypeSignatureToOutput(inputInvoke.Signature.ReturnType);

            TypeSignature[] parameterTypes = [.. inputInvoke.Signature.ParameterTypes
                .Select(MapTypeSignatureToOutput)];

            MethodDefinition invoke = new(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            // Add parameter names with [In] attribute
            int paramIndex = 1;
            foreach (ParameterDefinition inputParam in inputInvoke.ParameterDefinitions)
            {
                invoke.ParameterDefinitions.Add(new ParameterDefinition(
                    (ushort)paramIndex++,
                    inputParam.Name!.Value,
                    ParameterAttributes.In));
            }

            outputType.Methods.Add(invoke);
        }

        // Add GUID attribute
        AddGuidAttribute(outputType, inputType);
    }

    /// <summary>
    /// Adds an interface type to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Creates the WinMD interface type with all public methods (excluding property/event accessors),
    /// properties, events, and interface implementations. Also adds the <c>[Guid]</c> attribute.
    /// </remarks>
    /// <param name="inputType">The interface <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddInterfaceType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Interface |
            TypeAttributes.Abstract;

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add methods (skip property/event accessors — they're added by property/event processing below)
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic || method.IsSpecialName)
            {
                continue;
            }

            AddMethodToInterface(outputType, method);
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            AddPropertyToType(outputType, property, isInterfaceParent: true);
        }

        // Add events
        foreach (EventDefinition evt in inputType.Events)
        {
            AddEventToType(outputType, evt, isInterfaceParent: true);
        }

        // Add interface implementations
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            if (impl.Interface != null)
            {
                ITypeDefOrRef outputInterfaceRef = EnsureTypeReference(ImportTypeReference(impl.Interface));
                outputType.Interfaces.Add(new InterfaceImplementation(outputInterfaceRef));
            }
        }

        // Add GUID attribute
        AddGuidAttribute(outputType, inputType);
    }

    /// <summary>
    /// Adds a struct (value type) to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Creates the WinMD struct type with all public instance fields mapped to their WinRT
    /// type equivalents. Static fields are excluded per WinRT struct conventions.
    /// </remarks>
    /// <param name="inputType">The struct <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddStructType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "ValueType", "mscorlib");

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add public fields
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (!field.IsPublic || field.IsStatic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                field.Name!.Value,
                FieldAttributes.Public,
                new FieldSignature(MapTypeSignatureToOutput(field.Signature!.FieldType)));
            outputType.Fields.Add(outputField);
        }
    }

    /// <summary>
    /// Adds a class (runtime class) type to the output WinMD.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the most complex type handler. It creates the WinMD runtime class with:
    /// </para>
    /// <list type="bullet">
    ///   <item>Public methods, properties, and events (excluding custom-mapped interface members).</item>
    ///   <item>Constructors (including an implicit default constructor if none are defined).</item>
    ///   <item>Interface implementations (excluding mapped and unmappable .NET interfaces).</item>
    ///   <item><c>[Activatable]</c> attribute if the class has a default constructor.</item>
    ///   <item>Custom-mapped interfaces (e.g., <c>IList</c> → <c>IVector</c>) via <see cref="ProcessCustomMappedInterfaces"/>.</item>
    ///   <item>Synthesized interfaces (<c>IFooClass</c>, <c>IFooFactory</c>, <c>IFooStatic</c>) via <see cref="AddSynthesizedInterfaces"/>.</item>
    ///   <item>Explicit interface implementation methods.</item>
    ///   <item>A <c>[Default]</c> attribute on the appropriate interface implementation.</item>
    /// </list>
    /// </remarks>
    /// <param name="inputType">The class <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddClassType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Class |
            TypeAttributes.BeforeFieldInit;

        // Sealed for: sealed classes and static classes (abstract+sealed in metadata)
        // WinRT doesn't support abstract base classes, so non-static abstract classes
        // are treated as regular unsealed runtime classes
        if (inputType.IsSealed)
        {
            typeAttributes |= TypeAttributes.Sealed;
        }

        // In C#, static classes are both abstract and sealed in metadata
        if (inputType.IsAbstract && inputType.IsSealed)
        {
            typeAttributes |= TypeAttributes.Abstract;
        }

        // Determine base type
        ITypeDefOrRef? baseType;
        if (inputType.BaseType != null && inputType.BaseType.FullName != "System.Object")
        {
            // Check if the base type is abstract; WinRT doesn't support projecting abstract classes
            TypeDefinition? baseTypeDef = SafeResolve(inputType.BaseType);
            baseType = baseTypeDef != null && baseTypeDef.IsAbstract
                ? GetOrCreateTypeReference("System", "Object", "mscorlib")
                : ImportTypeReference(inputType.BaseType);
        }
        else
        {
            baseType = GetOrCreateTypeReference("System", "Object", "mscorlib");
        }

        TypeDefinition outputType = new(
            inputType.EffectiveNamespace,
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register in the mapping early so self-referencing method signatures can find it
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        bool hasConstructor = false;
        bool hasDefaultConstructor = false;
        bool hasAtLeastOneNonPublicConstructor = false;
        bool isStaticClass = inputType.IsAbstract && inputType.IsSealed;

        // Collect members from custom mapped interfaces and unmapped interfaces to exclude from the class
        HashSet<string> customMappedMembers = CollectCustomMappedMemberNames(inputType);

        // Add methods (non-property/event accessors)
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (method.IsConstructor)
            {
                if (!method.IsPublic)
                {
                    hasAtLeastOneNonPublicConstructor = true;
                    continue;
                }

                hasConstructor = true;
                hasDefaultConstructor |= method.Parameters.Count == 0;
                AddMethodToClass(outputType, method);
            }
            else if (method.IsPublic && !method.IsSpecialName)
            {
                // Skip methods that belong to custom mapped or unmapped interfaces
                if (customMappedMembers.Contains(method.Name?.Value ?? ""))
                {
                    continue;
                }

                AddMethodToClass(outputType, method);
            }
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            // Skip properties that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(property.Name?.Value ?? ""))
            {
                continue;
            }

            // Only add if at least one accessor is public
            bool hasPublicGetter = property.GetMethod?.IsPublic == true;
            bool hasPublicSetter = property.SetMethod?.IsPublic == true;
            if (hasPublicGetter || hasPublicSetter)
            {
                AddPropertyToType(outputType, property, isInterfaceParent: false);
            }
        }

        // Add events
        foreach (EventDefinition evt in inputType.Events)
        {
            // Skip events that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(evt.Name?.Value ?? ""))
            {
                continue;
            }

            bool hasPublicAdder = evt.AddMethod?.IsPublic == true;
            bool hasPublicRemover = evt.RemoveMethod?.IsPublic == true;
            if (hasPublicAdder || hasPublicRemover)
            {
                AddEventToType(outputType, evt, isInterfaceParent: false);
            }
        }

        // Implicit constructor if none defined
        if (!hasConstructor && !hasAtLeastOneNonPublicConstructor && !isStaticClass)
        {
            MethodDefinition defaultCtor = new(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };
            outputType.Methods.Add(defaultCtor);
            hasDefaultConstructor = true;
        }

        // Add interface implementations (excluding mapped and unmappable interfaces)
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            if (impl.Interface == null || !IsPubliclyAccessible(impl.Interface))
            {
                continue;
            }

            string interfaceName = GetInterfaceQualifiedName(impl.Interface);

            // Skip interfaces that have a WinRT mapping — they'll be added as their
            // mapped equivalents by ProcessCustomMappedInterfaces below
            if (_mapper.HasMappingForType(interfaceName))
            {
                continue;
            }

            // Skip .NET interfaces that have no WinRT equivalent
            if (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                continue;
            }

            ITypeDefOrRef outputInterfaceRef = EnsureTypeReference(ImportTypeReference(impl.Interface));
            outputType.Interfaces.Add(new InterfaceImplementation(outputInterfaceRef));
        }

        // Add activatable attribute if it has a default constructor
        if (hasDefaultConstructor)
        {
            int version = GetVersion(inputType);
            AddActivatableAttribute(outputType, (uint)version, null);
        }

        // Process custom mapped interfaces (IList -> IVector, IDisposable -> IClosable, etc.)
        ProcessCustomMappedInterfaces(inputType, outputType);

        // Add synthesized interfaces (IFooClass, IFooFactory, IFooStatic)
        AddSynthesizedInterfaces(inputType, outputType, declaration);

        // Add explicit interface implementation methods (private methods with qualified names)
        AddExplicitInterfaceImplementations(inputType, outputType);

        // If no default synthesized interface was created but the class implements
        // user interfaces, mark the first interface implementation as [Default].
        // The [Default] goes on the WinRT equivalent of the first user-declared .NET interface.
        if (declaration.DefaultInterface == null && outputType.Interfaces.Count > 0)
        {
            InterfaceImplementation? defaultImpl = FindDefaultInterface(inputType, outputType);
            AddDefaultAttribute(defaultImpl ?? outputType.Interfaces[0]);
        }
    }

    /// <summary>
    /// Finds the output interface that should receive <c>[Default]</c> — the WinRT equivalent
    /// of the first user-declared .NET interface on the input type.
    /// </summary>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <returns>The matching <see cref="InterfaceImplementation"/>, or <see langword="null"/> if not found.</returns>
    private InterfaceImplementation? FindDefaultInterface(TypeDefinition inputType, TypeDefinition outputType)
    {
        if (inputType.Interfaces.Count == 0)
        {
            return null;
        }

        // Get the first user-declared interface
        InterfaceImplementation firstInputImpl = inputType.Interfaces[0];
        string firstIfaceName = GetInterfaceQualifiedName(firstInputImpl.Interface!);

        // Check if it's a mapped type (e.g., IList -> IVector)
        string targetName = _mapper.HasMappingForType(firstIfaceName)
            ? _mapper.GetMappedType(firstIfaceName).GetMapping() is var (ns, name, _, _, _)
                ? string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}"
                : firstIfaceName
            : firstIfaceName;

        // Find the matching interface on the output type
        foreach (InterfaceImplementation outputImpl in outputType.Interfaces)
        {
            if (outputImpl.Interface != null && GetInterfaceQualifiedName(outputImpl.Interface) == targetName)
            {
                return outputImpl;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds explicit interface implementation methods from the input class to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Applies WinRT conventions: <c>set_</c> → <c>put_</c>, event <c>add</c> returns
    /// <c>EventRegistrationToken</c>, event <c>remove</c> takes <c>EventRegistrationToken</c>.
    /// Also creates the corresponding property/event definitions and <c>MethodImpl</c> entries
    /// to wire the explicit implementations to their interface methods.
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    private void AddExplicitInterfaceImplementations(TypeDefinition inputType, TypeDefinition outputType)
    {
        TypeReference eventRegistrationTokenType = GetOrCreateTypeReference(
            "Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
        TypeSignature tokenSig = eventRegistrationTokenType.ToTypeSignature(true);

        foreach (MethodDefinition method in inputType.Methods)
        {
            if (method.IsPublic || method.Name?.Value?.Contains('.') != true)
            {
                continue;
            }

            string fullMethodName = method.Name.Value;
            int lastDot = fullMethodName.LastIndexOf('.');
            if (lastDot <= 0)
            {
                continue;
            }

            string interfaceQualName = fullMethodName[..lastDot];
            string shortMethodName = fullMethodName[(lastDot + 1)..];

            if (!_typeDefinitionMapping.ContainsKey(interfaceQualName))
            {
                continue;
            }

            // Apply WinRT naming: set_ to put_
            string winrtShortName = shortMethodName;
            if (winrtShortName.StartsWith("set_", StringComparison.Ordinal))
            {
                winrtShortName = "put_" + winrtShortName[4..];
            }

            string winrtFullName = $"{interfaceQualName}.{winrtShortName}";

            TypeSignature returnType;
            TypeSignature[] parameterTypes;
            string[] paramNames;

            if (winrtShortName.StartsWith("add_", StringComparison.Ordinal))
            {
                // Event add: returns EventRegistrationToken, param is handler type named "handler"
                returnType = tokenSig;
                parameterTypes = [.. method.Signature!.ParameterTypes.Select(MapTypeSignatureToOutput)];
                paramNames = ["handler"];
            }
            else if (winrtShortName.StartsWith("remove_", StringComparison.Ordinal))
            {
                // Event remove: takes EventRegistrationToken named "token", returns void
                returnType = _outputModule.CorLibTypeFactory.Void;
                parameterTypes = [tokenSig];
                paramNames = ["token"];
            }
            else
            {
                returnType = method.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
                    ? _outputModule.CorLibTypeFactory.Void
                    : MapTypeSignatureToOutput(method.Signature.ReturnType);
                parameterTypes = [.. method.Signature.ParameterTypes.Select(MapTypeSignatureToOutput)];
                paramNames = [.. method.ParameterDefinitions.Select(p => p.Name?.Value ?? "value")];
            }

            MethodAttributes attrs =
                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot;

            if (method.IsSpecialName)
            {
                attrs |= MethodAttributes.SpecialName;
            }

            MethodDefinition outputMethod = new(winrtFullName, attrs,
                MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            for (int i = 0; i < paramNames.Length; i++)
            {
                ParameterAttributes paramAttr = i < parameterTypes.Length
                    ? GetWinRTParameterAttributes(method.Signature!.ParameterTypes[i])
                    : ParameterAttributes.In;
                outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                    (ushort)(i + 1), paramNames[i], paramAttr));
            }

            outputType.Methods.Add(outputMethod);

            if (winrtShortName.StartsWith("get_", StringComparison.Ordinal) || winrtShortName.StartsWith("put_", StringComparison.Ordinal))
            {
                string propName = $"{interfaceQualName}.{winrtShortName[4..]}";
                PropertyDefinition? existingProp = outputType.Properties.FirstOrDefault(p => p.Name?.Value == propName);
                if (existingProp == null)
                {
                    TypeSignature propType = winrtShortName.StartsWith("get_", StringComparison.Ordinal) ? returnType : parameterTypes[0];
                    PropertyDefinition prop = new(propName, 0, PropertySignature.CreateInstance(propType));
                    prop.Semantics.Add(new MethodSemantics(outputMethod,
                        winrtShortName.StartsWith("get_", StringComparison.Ordinal) ? MethodSemanticsAttributes.Getter : MethodSemanticsAttributes.Setter));
                    outputType.Properties.Add(prop);
                }
                else
                {
                    existingProp.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.Setter));
                }
            }
            else if (winrtShortName.StartsWith("add_", StringComparison.Ordinal))
            {
                string evtName = $"{interfaceQualName}.{winrtShortName[4..]}";
                ITypeDefOrRef eventType = parameterTypes.Length > 0 && parameterTypes[0] is TypeDefOrRefSignature tdrs
                    ? tdrs.Type
                    : parameterTypes.Length > 0 && parameterTypes[0] is GenericInstanceTypeSignature gits
                        ? new TypeSpecification(gits)
                        : GetOrCreateTypeReference("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract");
                EventDefinition evt = new(evtName, 0, eventType);
                evt.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.AddOn));
                outputType.Events.Add(evt);
            }
            else if (winrtShortName.StartsWith("remove_", StringComparison.Ordinal))
            {
                string evtName = $"{interfaceQualName}.{winrtShortName[7..]}";
                EventDefinition? existingEvt = outputType.Events.FirstOrDefault(e => e.Name?.Value == evtName);
                existingEvt?.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.RemoveOn));
            }

            TypeDeclaration interfaceDecl = _typeDefinitionMapping[interfaceQualName];
            if (interfaceDecl.OutputType != null)
            {
                MemberReference interfaceMethodRef = new(interfaceDecl.OutputType, winrtShortName,
                    MethodSignature.CreateInstance(returnType, parameterTypes));
                outputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, outputMethod));
            }
        }
    }
}
