// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Helpers;
using WindowsRuntime.WinMDGenerator.Models;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Emits an API contract type as an empty struct in the WinMD.
    /// </summary>
    /// <remarks>
    /// In C#, API contracts are projected as enums with <c>[ApiContract]</c>, but in WinMD
    /// metadata they are represented as empty structs per the Windows Runtime type system spec.
    /// </remarks>
    /// <param name="inputType">The API contract enum <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddApiContractType(TypeDefinition inputType)
    {
        string fullName = inputType.FullName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "ValueType", "mscorlib");

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes,
            baseType: baseType);

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;
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
        if (inputType.IsApiContract)
        {
            AddApiContractType(inputType);
            return;
        }

        string fullName = inputType.FullName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "Enum", "mscorlib");

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes,
            baseType: baseType);

        // Add the 'value__' field
        TypeSignature underlyingType = GetEnumUnderlyingType(inputType);
        FieldDefinition valueField = new(
            name: "value__",
            attributes: FieldAttributes.Private | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            signature: new FieldSignature(underlyingType));

        outputType.Fields.Add(valueField);

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;

        // Enum literal fields use the enum type itself (not the underlying type)
        TypeSignature enumTypeSignature = new TypeDefOrRefSignature(outputType, isValueType: true);

        // Add enum members
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (field.IsSpecialName)
            {
                continue; // Skip 'value__'
            }

            if (!field.IsPublic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                name: field.Name!.Value,
                attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
                signature: new FieldSignature(enumTypeSignature));

            if (field.Constant is not null)
            {
                outputField.Constant = new Constant(field.Constant.Type, new DataBlobSignature(field.Constant.Value!.Data));
            }

            outputType.Fields.Add(outputField);

            // Copy custom attributes from the input field: Windows Runtime metadata supports member
            // markers (e.g. '[Deprecated]' and '[Experimental]') on individual enum members
            CopyCustomAttributes(field, outputField);
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
    /// (private per Windows Runtime delegate convention) and the <c>Invoke</c> method with mapped parameter
    /// and return types. Also adds the <c>[Guid]</c> attribute.
    /// </remarks>
    /// <param name="inputType">The delegate <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddDelegateType(TypeDefinition inputType)
    {
        string fullName = inputType.FullName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "MulticastDelegate", "mscorlib");

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes,
            baseType: baseType);

        _outputModule.TopLevelTypes.Add(outputType);

        // Register early so self-referencing signatures can find this type
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;

        // Add '.ctor(object, IntPtr)' — private per Windows Runtime delegate convention
        MethodDefinition ctor = new(
            name: ".ctor",
            attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            signature: MethodSignature.CreateInstance(
                returnType: _outputModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    _outputModule.CorLibTypeFactory.Object,
                    _outputModule.CorLibTypeFactory.IntPtr]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        ctor.ParameterDefinitions.Add(new ParameterDefinition(1, "object"u8, 0));
        ctor.ParameterDefinitions.Add(new ParameterDefinition(2, "method"u8, 0));
        outputType.Methods.Add(ctor);

        // Add 'Invoke' method
        MethodDefinition? inputInvoke = inputType.Methods.FirstOrDefault(m => m.Name?.Value == "Invoke");
        if (inputInvoke is not null)
        {
            TypeSignature returnType = inputInvoke.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
                ? _outputModule.CorLibTypeFactory.Void
                : MapTypeSignatureToOutput(inputInvoke.Signature.ReturnType);

            TypeSignature[] parameterTypes = [.. inputInvoke.Signature.ParameterTypes
                .Select(MapTypeSignatureToOutput)];

            MethodDefinition invoke = new(
                name: "Invoke",
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                signature: MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            // Add parameter names with '[In]' attribute
            int paramIndex = 1;
            foreach (ParameterDefinition inputParam in inputInvoke.ParameterDefinitions)
            {
                invoke.ParameterDefinitions.Add(new ParameterDefinition(
                    sequence: (ushort)paramIndex++,
                    name: inputParam.Name!.Value,
                    attributes: ParameterAttributes.In));
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
        string fullName = inputType.FullName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Interface |
            TypeAttributes.Abstract;

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes);

        _outputModule.TopLevelTypes.Add(outputType);

        // Register early so self-referencing signatures can find this type
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;

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
        foreach (EventDefinition @event in inputType.Events)
        {
            AddEventToType(outputType, @event, isInterfaceParent: true);
        }

        // Add interface implementations
        foreach (InterfaceImplementation interfaceImplementation in inputType.Interfaces)
        {
            if (interfaceImplementation.Interface is not null)
            {
                ITypeDefOrRef outputInterfaceRef = EnsureTypeReference(ImportTypeReference(interfaceImplementation.Interface));

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
    /// Creates the WinMD struct type with all public instance fields mapped to their Windows Runtime
    /// type equivalents. Static fields are excluded per Windows Runtime struct conventions.
    /// </remarks>
    /// <param name="inputType">The struct <see cref="TypeDefinition"/> from the input assembly.</param>
    private void AddStructType(TypeDefinition inputType)
    {
        string fullName = inputType.FullName;

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "ValueType", "mscorlib");

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes,
            baseType: baseType);

        _outputModule.TopLevelTypes.Add(outputType);

        // Register early so self-referencing signatures can find this type
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;

        // Add public fields
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (!field.IsPublic || field.IsStatic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                name: field.Name!.Value,
                attributes: FieldAttributes.Public,
                signature: new FieldSignature(MapTypeSignatureToOutput(field.Signature!.FieldType)));

            outputType.Fields.Add(outputField);

            // Copy custom attributes from the input field: Windows Runtime metadata supports member
            // markers (e.g. '[Deprecated]' and '[Experimental]') on individual struct fields
            CopyCustomAttributes(field, outputField);
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
        string fullName = inputType.FullName;

        // A public unsealed class with at least one public constructor is the only shape that receives a public
        // composition factory (see 'AddSynthesizedInterface'), and therefore the only shape native code can derive
        // from and turn into the inner object of a COM aggregate. Validate the aggregation constraints for exactly
        // those classes: unsealed classes that never get one (abstract base types, or types whose constructors are
        // all non-public) are not composable, so none of the restrictions below apply to them.
        if (HasPublicCompositionFactory(inputType))
        {
            ValidateComposableClass(inputType);
        }

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            TypeAttributes.WindowsRuntime |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Class |
            TypeAttributes.BeforeFieldInit;

        // Sealed for: sealed classes and static classes (abstract+sealed in metadata)
        // Windows Runtime doesn't support abstract base classes, so non-static abstract classes
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
        if (inputType.BaseType is not null && inputType.BaseType.FullName != "System.Object")
        {
            // Check if the base type is abstract; Windows Runtime doesn't support projecting abstract classes
            TypeDefinition? baseTypeDef = SafeResolve(inputType.BaseType);
            baseType = baseTypeDef is not null && baseTypeDef.IsAbstract
                ? GetOrCreateTypeReference("System", "Object", "mscorlib")
                : ImportTypeReference(inputType.BaseType);
        }
        else
        {
            baseType = GetOrCreateTypeReference("System", "Object", "mscorlib");
        }

        TypeDefinition outputType = new(
            ns: inputType.Namespace?.Value,
            name: inputType.Name!.Value,
            attributes: typeAttributes,
            baseType: baseType);

        _outputModule.TopLevelTypes.Add(outputType);

        // Register in the mapping early so self-referencing method signatures can find it
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);

        _typeDefinitionMapping[fullName] = declaration;

        bool hasConstructor = false;
        bool hasDefaultConstructor = false;
        bool hasAtLeastOneNonPublicConstructor = false;
        bool isStaticClass = inputType.IsAbstract && inputType.IsSealed;
        bool isComposable = HasPublicCompositionFactory(inputType);

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

                // Overridable members live on the '[Overridable]' exclusive interface of a composable
                // class, not on its public surface (see 'AddSynthesizedInterface')
                if (isComposable &&
                    (IsComposableOverridableMember(inputType, method) ||
                     IsAuthoredOverridableInterfaceMember(inputType, method.Name?.Value ?? "")))
                {
                    continue;
                }

                // An override of an authored base class member is already declared by that base class
                if (method.IsVirtual && !method.IsNewSlot && OverridesAuthoredBaseMember(inputType, method.Name?.Value ?? ""))
                {
                    continue;
                }

                AddMethodToClass(outputType, method);
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

            // Skip properties that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(property.Name?.Value ?? ""))
            {
                continue;
            }

            // Overridable properties live on the '[Overridable]' exclusive interface of a composable class
            if (isComposable &&
                (IsComposableOverridableProperty(inputType, property) ||
                 IsAuthoredOverridableInterfaceProperty(inputType, property)))
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
        foreach (EventDefinition @event in inputType.Events)
        {
            // Skip events that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(@event.Name?.Value ?? ""))
            {
                continue;
            }

            bool hasPublicAdder = @event.AddMethod?.IsPublic == true;
            bool hasPublicRemover = @event.RemoveMethod?.IsPublic == true;

            if (hasPublicAdder || hasPublicRemover)
            {
                AddEventToType(outputType, @event, isInterfaceParent: false);
            }
        }

        // Implicit constructor if none defined
        if (!hasConstructor && !hasAtLeastOneNonPublicConstructor && !isStaticClass)
        {
            MethodDefinition defaultCtor = new(
                name: ".ctor",
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                signature: MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            outputType.Methods.Add(defaultCtor);

            hasDefaultConstructor = true;
        }

        // Add interface implementations (excluding mapped and unmappable interfaces)
        foreach (InterfaceImplementation interfaceImplementation in inputType.Interfaces)
        {
            if (interfaceImplementation.Interface is null || !IsPubliclyAccessible(interfaceImplementation.Interface))
            {
                continue;
            }

            string interfaceName = GetInterfaceFullName(interfaceImplementation.Interface);

            // Skip interfaces that have a Windows Runtime mapping — they'll be added as their
            // mapped equivalents by 'ProcessCustomMappedInterfaces' below
            if (_mapper.HasMappingForType(interfaceName))
            {
                continue;
            }

            // Skip .NET interfaces that have no Windows Runtime equivalent
            if (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                continue;
            }

            ITypeDefOrRef outputInterfaceRef = EnsureTypeReference(ImportTypeReference(interfaceImplementation.Interface));

            InterfaceImplementation outputInterfaceImplementation = new(outputInterfaceRef);

            // An authored interface marked '[WindowsRuntimeOverridable]' is the overridable surface of the composable
            // classes implementing it, so its interface implementation carries '[Overridable]' (this is where MIDL
            // places it too). It is only meaningful on a class native code can derive from and turn into the inner
            // object of a COM aggregate, so it is skipped for every other shape, where the interface stays ordinary.
            if (isComposable && IsAuthoredOverridableInterface(interfaceImplementation.Interface))
            {
                AddOverridableAttribute(outputInterfaceImplementation);
            }

            outputType.Interfaces.Add(outputInterfaceImplementation);
        }

        // Composable classes use their composition factory for all construction, including the
        // parameterless case. Direct activation through IActivationFactory is only valid for
        // runtime classes that are not composable.
        if (hasDefaultConstructor && !inputType.IsAbstract && !HasPublicCompositionFactory(inputType))
        {
            int version = GetVersion(inputType);
            AddActivatableAttribute(outputType, (uint)version, null);
        }

        // Process custom mapped interfaces ('IList' -> 'IVector', 'IDisposable' -> 'IClosable', etc.)
        ProcessCustomMappedInterfaces(inputType, outputType);

        // Add synthesized interfaces ('IFooClass', 'IFooFactory', 'IFooStatic')
        AddSynthesizedInterfaces(inputType, outputType, declaration);

        // Add explicit interface implementation methods (private methods with qualified names)
        AddExplicitInterfaceImplementations(inputType, outputType);

        // If no default synthesized interface was created but the class implements
        // user interfaces, mark the first interface implementation as '[Default]'.
        // The '[Default]' goes on the Windows Runtime equivalent of the first user-declared .NET interface.
        if (declaration.DefaultInterface is null && outputType.Interfaces.Count > 0)
        {
            InterfaceImplementation? defaultImpl = FindDefaultInterface(inputType, outputType);

            defaultImpl ??= outputType.Interfaces.FirstOrDefault(static implementation =>
                !implementation.CustomAttributes.Any(attribute =>
                    attribute.Constructor?.DeclaringType?.FullName is
                        "Windows.Foundation.Metadata.OverridableAttribute" or
                        "Windows.Foundation.Metadata.ProtectedAttribute"));

            if (defaultImpl is not null)
            {
                AddDefaultAttribute(defaultImpl);
            }
        }
    }

    /// <summary>
    /// Checks whether a runtime class receives a public composition factory (i.e. whether it is composable).
    /// </summary>
    /// <param name="inputType">The class <see cref="TypeDefinition"/> from the input assembly.</param>
    /// <returns>Whether <paramref name="inputType"/> is projected as a composable Windows Runtime class.</returns>
    /// <remarks>
    /// This is the single source of truth for "is this class composable", and mirrors exactly the condition under
    /// which <c>AddSynthesizedInterface</c> emits factory members (and therefore a <c>[Composable]</c> attribute)
    /// for it. Sealed classes get an activation factory instead, while abstract classes and unsealed classes
    /// with no public constructor get no factory at all.
    /// </remarks>
    private bool HasPublicCompositionFactory(TypeDefinition inputType)
    {
        if (inputType.IsSealed || inputType.IsAbstract)
        {
            return false;
        }

        TypeDefinition? currentType = inputType;

        while (currentType.BaseType is { FullName: not "System.Object" } baseType)
        {
            TypeDefinition? resolvedBaseType = SafeResolve(baseType);

            if (resolvedBaseType?.DeclaringModule != _inputModule)
            {
                return false;
            }

            currentType = resolvedBaseType;
        }

        foreach (MethodDefinition method in inputType.Methods)
        {
            if (method.IsConstructor && method.IsPublic)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether an authored interface is declared as an <c>[Overridable]</c> interface of the composable
    /// runtime classes implementing it (i.e. whether it carries <c>[WindowsRuntimeOverridable]</c>).
    /// </summary>
    /// <param name="interfaceRef">The implemented interface from the input assembly.</param>
    /// <returns>Whether <paramref name="interfaceRef"/> is an authored overridable interface.</returns>
    /// <remarks>
    /// This is the explicit counterpart of <see cref="IsComposableOverridableMember"/>: instead of synthesizing an
    /// <c>I{ClassName}Overrides</c> interface out of the <c>virtual</c> members of the class, the author declares
    /// the overridable surface as a real Windows Runtime interface (exactly like an <c>[overridable] interface</c>
    /// member in MIDL). That interface is nameable from the authored component itself, so the class can dispatch to
    /// the most derived implementation of its members through the controlling outer object.
    /// </remarks>
    private bool IsAuthoredOverridableInterface(ITypeDefOrRef interfaceRef)
    {
        TypeDefinition? interfaceType = SafeResolve(interfaceRef);

        if (interfaceType?.DeclaringModule != _inputModule)
        {
            return false;
        }

        foreach (CustomAttribute attribute in interfaceType.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType?.FullName == WindowsRuntimeOverridableAttributeName)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasUsableDefaultInterface(TypeDefinition inputType)
    {
        foreach (InterfaceImplementation implementation in inputType.Interfaces)
        {
            if (implementation.Interface is null ||
                !IsPubliclyAccessible(implementation.Interface) ||
                IsAuthoredOverridableInterface(implementation.Interface))
            {
                continue;
            }

            string interfaceName = GetInterfaceFullName(implementation.Interface);

            if (_mapper.HasMappingForType(interfaceName) ||
                !TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAuthoredOverridableInterfaceMember(TypeDefinition inputType, string memberName)
    {
        foreach (InterfaceImplementation implementation in inputType.Interfaces)
        {
            if (implementation.Interface is null ||
                !IsAuthoredOverridableInterface(implementation.Interface))
            {
                continue;
            }

            if (SafeResolve(implementation.Interface)?.Methods.Any(method => method.Name?.Value == memberName) == true)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAuthoredOverridableInterfaceProperty(TypeDefinition inputType, PropertyDefinition property)
    {
        return
            (property.GetMethod is not null &&
             IsAuthoredOverridableInterfaceMember(inputType, property.GetMethod.Name?.Value ?? "")) ||
            (property.SetMethod is not null &&
             IsAuthoredOverridableInterfaceMember(inputType, property.SetMethod.Name?.Value ?? ""));
    }

    /// <summary>
    /// Checks whether an instance member of a composable runtime class is projected onto its
    /// <c>[Overridable]</c> exclusive interface.
    /// </summary>
    /// <param name="inputType">The class declaring the member.</param>
    /// <param name="method">The method (or property accessor) from the input assembly.</param>
    /// <returns>Whether <paramref name="method"/> belongs on the overridable interface.</returns>
    /// <remarks>
    /// <para>
    /// A member is overridable when a derived type can override it, i.e. when it introduces a new virtual
    /// slot and is not sealed into it. Both <c>public virtual</c> and <c>protected virtual</c> members
    /// qualify: Windows Runtime has no notion of a public overridable member, so every overridable member
    /// is surfaced as protected by the language projections (this is the same shape XAML uses for
    /// <c>OnPointerPressed</c> and friends).
    /// </para>
    /// <para>
    /// Members that reuse an inherited slot (a C# <c>override</c>, or <c>Finalize</c>) are deliberately
    /// excluded: the overridable interface of the base runtime class already declares them. So are members
    /// that are virtual only because they implement an interface (those are sealed into their slot).
    /// </para>
    /// </remarks>
    private bool IsComposableOverridableMember(TypeDefinition inputType, MethodDefinition method)
    {
        return
            !method.IsStatic &&
            !method.IsConstructor &&
            method.IsVirtual &&
            method.IsNewSlot &&
            !method.IsFinal &&
            !IsMemberFromImplementedInterface(inputType, method.Name?.Value ?? "") &&
            (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
    }

    private bool IsMemberFromImplementedInterface(TypeDefinition inputType, string memberName)
    {
        foreach (InterfaceImplementation interfaceImplementation in GatherAllInterfaces(inputType))
        {
            TypeDefinition? interfaceType = SafeResolve(interfaceImplementation.Interface);

            if (interfaceType?.Methods.Any(method => method.Name?.Value == memberName) == true)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether an instance member of a composable runtime class is projected onto its
    /// <c>[Protected]</c> exclusive interface.
    /// </summary>
    /// <param name="method">The method (or property accessor) from the input assembly.</param>
    /// <returns>Whether <paramref name="method"/> belongs on the protected interface.</returns>
    /// <remarks>
    /// Protected members are the ones a derived type can call but not override, so overridable members
    /// (see <see cref="IsComposableOverridableMember"/>) are excluded here and get their own interface.
    /// <c>private protected</c> members are excluded as well: they are not reachable from a type outside
    /// the authored component, so they are not part of the Windows Runtime surface of the class.
    /// </remarks>
    private static bool IsComposableProtectedMember(MethodDefinition method)
    {
        return
            !method.IsStatic &&
            !method.IsConstructor &&
            !method.IsVirtual &&
            (method.IsFamily || method.IsFamilyOrAssembly);
    }

    /// <summary>
    /// Gets the accessor that determines which synthesized interface a property belongs to.
    /// </summary>
    /// <param name="property">The property from the input assembly.</param>
    /// <returns>The getter, or the setter for write-only properties.</returns>
    private static MethodDefinition? GetPrimaryAccessor(PropertyDefinition property)
    {
        return property.GetMethod ?? property.SetMethod;
    }

    /// <summary>
    /// Checks whether a property of a composable runtime class is projected onto its <c>[Overridable]</c> interface.
    /// </summary>
    /// <param name="inputType">The class declaring the property.</param>
    /// <param name="property">The property from the input assembly.</param>
    /// <returns>Whether <paramref name="property"/> belongs on the overridable interface.</returns>
    private bool IsComposableOverridableProperty(TypeDefinition inputType, PropertyDefinition property)
    {
        return GetPrimaryAccessor(property) is { } accessor && IsComposableOverridableMember(inputType, accessor);
    }

    /// <summary>
    /// Checks whether a property of a composable runtime class is projected onto its <c>[Protected]</c> interface.
    /// </summary>
    /// <param name="property">The property from the input assembly.</param>
    /// <returns>Whether <paramref name="property"/> belongs on the protected interface.</returns>
    private static bool IsComposableProtectedProperty(PropertyDefinition property)
    {
        return GetPrimaryAccessor(property) is { } accessor && IsComposableProtectedMember(accessor);
    }

    /// <summary>
    /// Checks whether a member overrides an overridable member declared by an authored base runtime class.
    /// </summary>
    /// <param name="inputType">The class <see cref="TypeDefinition"/> from the input assembly.</param>
    /// <param name="memberName">The metadata name of the member to check.</param>
    /// <returns>Whether an authored base class declares an overridable member with the same name.</returns>
    /// <remarks>
    /// An override introduces no new Windows Runtime surface: the member is already declared by the
    /// <c>[Overridable]</c> exclusive interface of the base runtime class, and the CCW of the derived
    /// object dispatches to it virtually. Re-declaring it on the derived class would create a second,
    /// conflicting member with the same name on the runtime class.
    /// </remarks>
    private bool OverridesAuthoredBaseMember(TypeDefinition inputType, string memberName)
    {
        TypeDefinition? baseType = SafeResolve(inputType.BaseType);

        while (baseType is not null &&
               baseType.DeclaringModule == _inputModule &&
               !baseType.IsAbstract)
        {
            foreach (MethodDefinition baseMethod in baseType.Methods)
            {
                if (baseMethod.Name?.Value == memberName &&
                    ((baseMethod.IsVirtual &&
                      !baseMethod.IsFinal &&
                      (baseMethod.IsPublic || baseMethod.IsFamily || baseMethod.IsFamilyOrAssembly)) ||
                     IsAuthoredOverridableInterfaceMember(baseType, memberName)))
                {
                    return true;
                }
            }

            baseType = SafeResolve(baseType.BaseType);
        }

        return false;
    }

    /// <summary>
    /// Validates that a runtime class receiving a public composition factory can take part in COM aggregation.
    /// </summary>
    /// <param name="inputType">The class <see cref="TypeDefinition"/> from the input assembly.</param>
    /// <exception cref="Exception">
    /// Thrown if the class implements an interface that cannot take part in COM aggregation, or if one of its
    /// public constructors takes a parameter a composition factory method cannot marshal.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is only ever called for a class that actually gets a <c>[Composable]</c> factory, i.e. an unsealed
    /// class with at least one public constructor (see <see cref="HasPublicCompositionFactory"/>).
    /// </para>
    /// <para>
    /// A composable runtime class can become the inner object of a COM aggregate, and the COM aggregation
    /// contract requires every interface the aggregate exposes to share the identity and the reference count
    /// of the controlling outer object. C#/WinRT achieves this by giving the CCW of the aggregated object a
    /// private, per-aggregate copy of the vtable of every interface it can expose, with only the <c>IUnknown</c>
    /// and <c>IInspectable</c> entries replaced by ones delegating to the controlling outer. It can only do that
    /// for the Windows Runtime interfaces authored in the component itself (those are the only ones whose CCW
    /// vtables are generated together with the composable class).
    /// </para>
    /// <para>
    /// Custom-mapped interfaces (e.g. <c>IDisposable</c>, <c>IList&lt;T&gt;</c>, <c>INotifyPropertyChanged</c>),
    /// generic instantiations, and interfaces coming from the Windows SDK or from another component all get their
    /// CCW vtables from shared infrastructure that the projection has no handle to, so no per-aggregate copy can
    /// be made for them. Rather than silently handing out interface pointers with a second COM identity,
    /// composition is rejected outright: seal the class (making it a normal activatable runtime class), make its
    /// constructors non-public, or drop the offending interfaces.
    /// </para>
    /// </remarks>
    private void ValidateComposableClass(TypeDefinition inputType)
    {
        ValidateComposableClassInterfaces(inputType);
        ValidateComposableFactoryMethods(inputType);
    }

    /// <summary>
    /// Validates that every interface a composable runtime class exposes can take part in COM aggregation.
    /// </summary>
    /// <param name="inputType">The composable class <see cref="TypeDefinition"/> from the input assembly.</param>
    /// <exception cref="Exception">Thrown if the class implements an interface that cannot take part in COM aggregation.</exception>
    private void ValidateComposableClassInterfaces(TypeDefinition inputType)
    {
        List<string>? unsupportedInterfaces = null;

        // Walk the class hierarchy: the CCW of the composable class also exposes every Windows Runtime
        // interface it inherits from its authored base classes, and all of those are reachable through
        // the non-delegating inner object of the aggregate as well.
        TypeDefinition? currentType = inputType;

        while (currentType is not null && currentType.DeclaringModule == _inputModule)
        {
            foreach (InterfaceImplementation interfaceImplementation in currentType.Interfaces)
            {
                if (interfaceImplementation.Interface is null || !IsPubliclyAccessible(interfaceImplementation.Interface))
                {
                    continue;
                }

                string interfaceName = GetInterfaceFullName(interfaceImplementation.Interface);

                // .NET interfaces with no Windows Runtime equivalent are not projected at all, so they
                // never end up in the interface entries of the CCW, and are not a problem here.
                if (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
                {
                    continue;
                }

                // Custom-mapped interfaces are projected, but their ABI types (and therefore their CCW
                // vtables) live in 'WinRT.Runtime' or in the interop assembly, and are shared by every
                // managed type in the application, so they can't be made aggregation-aware. The same is
                // true for '[GeneratedComInterface]' interfaces, whose CCW vtables come from the runtime
                // marshalling infrastructure in the BCL rather than from the CsWinRT projection.
                if (!_mapper.HasMappingForType(interfaceName) &&
                    interfaceImplementation.Interface is TypeDefinition interfaceDefinition &&
                    interfaceDefinition.DeclaringModule == _inputModule &&
                    interfaceDefinition.IsInterface &&
                    !interfaceDefinition.FindCustomAttributes("System.Runtime.InteropServices.Marshalling", "GeneratedComInterfaceAttribute").Any())
                {
                    continue;
                }

                unsupportedInterfaces ??= [];

                if (!unsupportedInterfaces.Contains(interfaceName))
                {
                    unsupportedInterfaces.Add(interfaceName);
                }
            }

            currentType = SafeResolve(currentType.BaseType);
        }

        if (unsupportedInterfaces is not null)
        {
            throw WellKnownWinMDExceptions.ComposableClassInterfaceNotSupported(
                inputType.FullName,
                string.Join("', '", unsupportedInterfaces));
        }
    }

    /// <summary>
    /// Validates that every public constructor of a composable runtime class can be projected as a
    /// composition factory method.
    /// </summary>
    /// <param name="inputType">The composable class <see cref="TypeDefinition"/> from the input assembly.</param>
    /// <exception cref="Exception">Thrown if a public constructor takes an array or a generic parameter.</exception>
    /// <remarks>
    /// Composition factory methods get a dedicated CCW body (they run the COM aggregation handshake at the ABI
    /// level, so the controlling outer object is never wrapped in an RCW while it is being constructed), and that
    /// body does not support the extra marshalling state that array and generic instance parameters need.
    /// Rejecting them here means the component author gets a build error with an actionable message, rather than
    /// a composition factory that always fails with <c>E_NOTIMPL</c> at runtime.
    /// </remarks>
    private static void ValidateComposableFactoryMethods(TypeDefinition inputType)
    {
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsConstructor || !method.IsPublic)
            {
                continue;
            }

            foreach (TypeSignature parameterType in method.Signature!.ParameterTypes)
            {
                if (parameterType is ArrayBaseTypeSignature or GenericInstanceTypeSignature)
                {
                    throw WellKnownWinMDExceptions.ComposableClassConstructorParameterNotSupported(
                        inputType.FullName,
                        parameterType.FullName);
                }
            }
        }
    }

    /// <summary>
    /// Finds the output interface that should receive <c>[Default]</c> — the Windows Runtime equivalent
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

        foreach (InterfaceImplementation inputImpl in inputType.Interfaces)
        {
            if (inputImpl.Interface is null || IsAuthoredOverridableInterface(inputImpl.Interface))
            {
                continue;
            }

            string inputInterfaceName = GetInterfaceFullName(inputImpl.Interface);

            if (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(inputInterfaceName))
            {
                continue;
            }

            string targetName = _mapper.HasMappingForType(inputInterfaceName)
                ? _mapper.GetMappedType(inputInterfaceName).GetMappedTypeInfo().FullName
                : inputInterfaceName;

            foreach (InterfaceImplementation outputImpl in outputType.Interfaces)
            {
                if (outputImpl.Interface is not null &&
                    GetInterfaceFullName(outputImpl.Interface) == targetName &&
                    !outputImpl.CustomAttributes.Any(attribute =>
                        attribute.Constructor?.DeclaringType?.FullName is
                            "Windows.Foundation.Metadata.OverridableAttribute" or
                            "Windows.Foundation.Metadata.ProtectedAttribute"))
                {
                    return outputImpl;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Adds explicit interface implementation methods from the input class to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Applies Windows Runtime conventions: <c>set_</c> → <c>put_</c>, event <c>add</c> returns
    /// <c>EventRegistrationToken</c>, event <c>remove</c> takes <c>EventRegistrationToken</c>.
    /// Also creates the corresponding property/event definitions and <c>MethodImpl</c> entries
    /// to wire the explicit implementations to their interface methods.
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    private void AddExplicitInterfaceImplementations(TypeDefinition inputType, TypeDefinition outputType)
    {
        TypeReference eventRegistrationTokenType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation",
            name: "EventRegistrationToken",
            assemblyName: "Windows.Foundation.FoundationContract");

        TypeSignature tokenSignature = eventRegistrationTokenType.ToTypeSignature(true);

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

            // Apply Windows Runtime naming: 'set_' to 'put_'
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
                returnType = tokenSignature;
                parameterTypes = [.. method.Signature!.ParameterTypes.Select(MapTypeSignatureToOutput)];
                paramNames = ["handler"];
            }
            else if (winrtShortName.StartsWith("remove_", StringComparison.Ordinal))
            {
                // Event remove: takes EventRegistrationToken named "token", returns void
                returnType = _outputModule.CorLibTypeFactory.Void;
                parameterTypes = [tokenSignature];
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

            MethodAttributes attributes =
                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot;

            if (method.IsSpecialName)
            {
                attributes |= MethodAttributes.SpecialName;
            }

            MethodDefinition outputMethod = new(
                name: winrtFullName,
                attributes: attributes,
                signature: MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            for (int i = 0; i < paramNames.Length; i++)
            {
                ParameterAttributes paramAttr = i < parameterTypes.Length && i < method.ParameterDefinitions.Count
                    ? GetWinRTParameterAttributes(method, method.ParameterDefinitions[i], method.Signature!.ParameterTypes[i])
                    : ParameterAttributes.In;

                outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                    sequence: (ushort)(i + 1),
                    name: paramNames[i],
                    attributes: paramAttr));
            }

            outputType.Methods.Add(outputMethod);

            if (winrtShortName.StartsWith("get_", StringComparison.Ordinal) || winrtShortName.StartsWith("put_", StringComparison.Ordinal))
            {
                string propName = $"{interfaceQualName}.{winrtShortName[4..]}";
                PropertyDefinition? existingProp = outputType.Properties.FirstOrDefault(p => p.Name?.Value == propName);

                if (existingProp is null)
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
                string eventName = $"{interfaceQualName}.{winrtShortName[4..]}";
                ITypeDefOrRef eventType = parameterTypes.Length > 0 && parameterTypes[0] is TypeDefOrRefSignature typeDefOrRefSignature
                    ? typeDefOrRefSignature.Type
                    : parameterTypes.Length > 0 && parameterTypes[0] is GenericInstanceTypeSignature genericInstanceSignature
                        ? new TypeSpecification(genericInstanceSignature)
                        : GetOrCreateTypeReference("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract");
                EventDefinition @event = new(eventName, 0, eventType);

                @event.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.AddOn));
                outputType.Events.Add(@event);
            }
            else if (winrtShortName.StartsWith("remove_", StringComparison.Ordinal))
            {
                string eventName = $"{interfaceQualName}.{winrtShortName[7..]}";
                EventDefinition? existingEvent = outputType.Events.FirstOrDefault(e => e.Name?.Value == eventName);

                existingEvent?.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.RemoveOn));
            }

            TypeDeclaration interfaceDecl = _typeDefinitionMapping[interfaceQualName];

            if (interfaceDecl.OutputType is not null)
            {
                MemberReference interfaceMethodRef = new(
                    parent: interfaceDecl.OutputType,
                    name: winrtShortName,
                    signature: MethodSignature.CreateInstance(returnType, parameterTypes));

                outputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, outputMethod));
            }
        }
    }
}
