// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Helpers;
using WindowsRuntime.WinMDGenerator.Models;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Processes custom mapped interfaces for a class type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This maps .NET collection interfaces, <see cref="IDisposable"/>, <c>INotifyPropertyChanged</c>, etc.
    /// to their Windows Runtime equivalents (e.g., <see cref="IList{T}"/> → <c>IVector&lt;T&gt;</c>,
    /// <see cref="IDisposable"/> → <c>IClosable</c>) and adds the required explicit implementation methods
    /// and <c>MethodImpl</c> records.
    /// </para>
    /// <para>
    /// When a type implements both generic <c>IEnumerable&lt;T&gt;</c> and non-generic <c>IEnumerable</c>,
    /// the non-generic <c>IBindableIterable</c> mapping is skipped since <c>IIterable&lt;T&gt;</c> supersedes it.
    /// </para>
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    private void ProcessCustomMappedInterfaces(TypeDefinition inputType, TypeDefinition outputType)
    {
        // Gather all interfaces from the type and its base types (matching old generator's 'GetInterfaces')
        List<InterfaceImplementation> allInterfaces = GatherAllInterfaces(inputType);

        // Collect all mapped interfaces and determine if they are publicly or explicitly implemented
        List<(InterfaceImplementation interfaceImplementation, string interfaceName, MappedType mapping, bool isPublic)> mappedInterfaces = [];

        foreach (InterfaceImplementation interfaceImplementation in allInterfaces)
        {
            if (interfaceImplementation.Interface is null)
            {
                continue;
            }

            string interfaceName = GetInterfaceFullName(interfaceImplementation.Interface);

            if (!_mapper.HasMappingForType(interfaceName))
            {
                continue;
            }

            MappedType mapping = _mapper.GetMappedType(interfaceName);

            // Determine if the interface is publicly implemented.
            // Check if the class has public methods that match the .NET interface members.
            // For mapped interfaces, the .NET method names differ from Windows Runtime names
            // (e.g., 'Add' vs 'Append'), so we check the .NET interface's members.
            bool isPublic = IsInterfacePubliclyImplemented(classType: inputType, interfaceImplementation, _runtimeContext);

            mappedInterfaces.Add((interfaceImplementation, interfaceName, mapping, isPublic));
        }

        // If generic 'IEnumerable<T>' ('IIterable') is present, skip non-generic 'IEnumerable' ('IBindableIterable')
        bool hasGenericEnumerable = allInterfaces.Any(i => i.Interface is not null && GetInterfaceFullName(i.Interface) == "System.Collections.Generic.IEnumerable`1");

        foreach ((InterfaceImplementation interfaceImplementation, string interfaceName, MappedType mapping, bool isPublic) in mappedInterfaces)
        {
            MappedTypeInfo mappingInfo = mapping.GetMappedTypeInfo();

            // Skip non-generic 'IEnumerable' when generic 'IEnumerable<T>' is also implemented
            if (hasGenericEnumerable && interfaceName == "System.Collections.IEnumerable")
            {
                continue;
            }

            // Add the mapped interface as an implementation on the output type
            TypeReference mappedInterfaceRef = GetOrCreateTypeReference(mappingInfo.Namespace, mappingInfo.Name, mappingInfo.Assembly);

            // Check if the output type already implements this mapped interface
            // (e.g., 'IObservableVector<T>' already brings 'IVector<T>')
            string mappedFullName = mappingInfo.FullName;
            bool alreadyImplemented = outputType.Interfaces.Any(i =>
            {
                string? existingName = i.Interface is TypeSpecification existingTypeSpecification && existingTypeSpecification.Signature is GenericInstanceTypeSignature existingGenericInstanceSignature
                    ? existingGenericInstanceSignature.GenericType.FullName
                    : i.Interface?.FullName;
                return existingName == mappedFullName;
            });

            if (alreadyImplemented)
            {
                continue;
            }

            ITypeDefOrRef mappedInterfaceTypeRef;

            // For generic interfaces, handle type arguments (mapping 'KeyValuePair' -> 'IKeyValuePair', etc.)
            if (interfaceImplementation.Interface is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst)
            {
                TypeSpecification mappedSpec = new(new GenericInstanceTypeSignature(
                    genericType: mappedInterfaceRef,
                    isValueType: false,
                    typeArguments: genericInst.TypeArguments.Select(MapCustomMappedTypeArgument)));

                outputType.Interfaces.Add(new InterfaceImplementation(mappedSpec));

                mappedInterfaceTypeRef = mappedSpec;
            }
            else
            {
                outputType.Interfaces.Add(new InterfaceImplementation(mappedInterfaceRef));

                mappedInterfaceTypeRef = mappedInterfaceRef;
            }

            // Add explicit implementation methods for the mapped interface
            AddCustomMappedTypeMembers(outputType, mappingInfo.Name, mappedInterfaceTypeRef, isPublic);
        }
    }

    /// <summary>
    /// Maps a type argument for custom mapped interfaces, transforming mapped types
    /// like <c>KeyValuePair</c> to <c>IKeyValuePair</c>.
    /// </summary>
    /// <remarks>
    /// Recursively handles nested generic instances (e.g., <c>IList&lt;KeyValuePair&lt;K, V&gt;&gt;</c>
    /// becomes <c>IVector&lt;IKeyValuePair&lt;K, V&gt;&gt;</c>).
    /// </remarks>
    /// <param name="arg">The type argument signature to map.</param>
    /// <returns>The mapped <see cref="TypeSignature"/>.</returns>
    private TypeSignature MapCustomMappedTypeArgument(TypeSignature arg)
    {
        TypeSignature mapped = MapTypeSignatureToOutput(arg);

        // Check if the mapped type itself has a Windows Runtime mapping (e.g. 'KeyValuePair' -> 'IKeyValuePair')
        if (mapped is TypeDefOrRefSignature typeDefOrRefSignature)
        {
            string typeName = typeDefOrRefSignature.Type.FullName;

            if (_mapper.HasMappingForType(typeName))
            {
                MappedType innerMapping = _mapper.GetMappedType(typeName);
                MappedTypeInfo innerTypeInfo = innerMapping.GetMappedTypeInfo();

                TypeReference innerRef = GetOrCreateTypeReference(innerTypeInfo.Namespace, innerTypeInfo.Name, innerTypeInfo.Assembly);

                return innerRef.ToTypeSignature(typeDefOrRefSignature.IsValueType);
            }
        }

        // For generic instances, recursively map type arguments
        if (mapped is GenericInstanceTypeSignature genericInstanceSignature)
        {
            string typeName = genericInstanceSignature.GenericType.FullName;

            if (_mapper.HasMappingForType(typeName))
            {
                MappedType innerMapping = _mapper.GetMappedType(typeName);
                MappedTypeInfo innerTypeInfo = innerMapping.GetMappedTypeInfo();

                return new GenericInstanceTypeSignature(
                    genericType: GetOrCreateTypeReference(innerTypeInfo.Namespace, innerTypeInfo.Name, innerTypeInfo.Assembly),
                    isValueType: false,
                    typeArguments: genericInstanceSignature.TypeArguments.Select(MapCustomMappedTypeArgument));
            }
        }

        return mapped;
    }

    /// <summary>
    /// Adds the explicit implementation methods for a specific mapped Windows Runtime interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method generates all methods, properties, and events defined by the mapped Windows Runtime interface
    /// (e.g., <c>IVector&lt;T&gt;</c> members like <c>Append</c>, <c>GetAt</c>, <c>Size</c>). Each member
    /// is emitted with the correct Windows Runtime signature and a <c>MethodImpl</c> entry linking it to the
    /// interface method declaration.
    /// </para>
    /// <para>
    /// When <paramref name="isPublic"/> is <see langword="false"/>, method names are prefixed with the
    /// qualified interface name for explicit implementation (e.g., <c>IVector&lt;String&gt;.Append</c>).
    /// </para>
    /// </remarks>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="mappedTypeName">The short name of the mapped Windows Runtime interface (e.g., <c>"IVector`1"</c>).</param>
    /// <param name="mappedInterfaceRef">The type reference for the mapped Windows Runtime interface in the output module.</param>
    /// <param name="isPublic">Whether the interface is publicly implemented on the class.</param>
    private void AddCustomMappedTypeMembers(
        TypeDefinition outputType,
        string mappedTypeName,
        ITypeDefOrRef mappedInterfaceRef,
        bool isPublic)
    {
        // Build the qualified prefix for explicit implementation method names.
        // For generic types, use short type names (e.g. "IMap`2<String, Int32>" not "IMap`2<System.String, System.Int32>")
        string qualifiedPrefix = FormatQualifiedInterfaceName(mappedInterfaceRef);

        TypeSignature[]? parentGenericArgs = null;

        // Store parent interface generic type arguments for 'MethodImpl' signature conversion.
        // 'MethodImpl' declarations on generic interfaces should reference methods using !0, !1 etc. (not resolved types).
        if (mappedInterfaceRef is TypeSpecification mappedTypeSpecification && mappedTypeSpecification.Signature is GenericInstanceTypeSignature mappedGenericInstanceSignature)
        {
            parentGenericArgs = [.. mappedGenericInstanceSignature.TypeArguments];
        }

        // Look up a type signature in the parent interface's generic arguments by identity.
        // Returns the corresponding GenericParameterSignature (!0, !1) or null if not found.
        GenericParameterSignature? FindParentGenericParam(TypeSignature signature)
        {
            if (parentGenericArgs is null)
            {
                return null;
            }

            string signatureFullName = signature.FullName;

            for (int i = 0; i < parentGenericArgs.Length; i++)
            {
                if (parentGenericArgs[i].FullName == signatureFullName)
                {
                    return new GenericParameterSignature(_outputModule, GenericParameterType.Type, i);
                }
            }

            return null;
        }

        // Convert a resolved type signature to use generic parameters (!0, !1) for 'MethodImpl' declarations.
        // For parent interface generic args, substitutes resolved types back to !0, !1.
        // For all GenericInstanceTypeSignature in signatures, converts to open form
        // (e.g., EventHandler`1<Object> -> EventHandler`1<!0>) matching Windows Runtime metadata conventions.
        TypeSignature ToGenericParam(TypeSignature signature)
        {
            if (parentGenericArgs is not null)
            {
                if (FindParentGenericParam(signature) is GenericParameterSignature genericParameter)
                {
                    return genericParameter;
                }
            }

            return signature switch
            {
                GenericInstanceTypeSignature innerGenericInstanceSignature => ToOpenGenericForm(innerGenericInstanceSignature),
                SzArrayTypeSignature szArray => new SzArrayTypeSignature(ToGenericParam(szArray.BaseType)),
                ByReferenceTypeSignature byRef => new ByReferenceTypeSignature(ToGenericParam(byRef.BaseType)),
                _ => signature
            };
        }

        // Convert a GenericInstanceTypeSignature to its open form for 'MethodImpl' declarations.
        // E.g., KeyValuePair<String, Int32> -> KeyValuePair<!0, !1> when those are parent interface args.
        GenericInstanceTypeSignature ToOpenGenericForm(GenericInstanceTypeSignature genericInstanceSignature)
        {
            TypeSignature[] openArgs = new TypeSignature[genericInstanceSignature.TypeArguments.Count];

            for (int i = 0; i < genericInstanceSignature.TypeArguments.Count; i++)
            {
                TypeSignature arg = genericInstanceSignature.TypeArguments[i];

                // Try parent interface generic arg substitution (first match wins for duplicate args)
                if (FindParentGenericParam(arg) is GenericParameterSignature parentGenericParameter)
                {
                    openArgs[i] = parentGenericParameter;
                }
                else if (arg is GenericInstanceTypeSignature nestedGenericInstanceSignature)
                {
                    openArgs[i] = ToOpenGenericForm(nestedGenericInstanceSignature);
                }
                else
                {
                    // Use the generic type's own parameter
                    openArgs[i] = new GenericParameterSignature(_outputModule, GenericParameterType.Type, i);
                }
            }

            return new GenericInstanceTypeSignature(genericInstanceSignature.GenericType, genericInstanceSignature.IsValueType, openArgs);
        }

        void AddMappedMethod(string name, (string name, TypeSignature type, ParameterAttributes attributes)[]? parameters, TypeSignature? returnType)
        {
            string methodName = isPublic ? name : $"{qualifiedPrefix}.{name}";

            MethodAttributes attributes = isPublic
                ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot)
                : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot);

            TypeSignature[] paramTypes = parameters?.Select(p => p.type).ToArray() ?? [];
            MethodSignature signature = MethodSignature.CreateInstance(returnType ?? _outputModule.CorLibTypeFactory.Void, paramTypes);

            MethodDefinition method = new(methodName, attributes, signature)
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            if (parameters is not null)
            {
                int idx = 1;

                foreach ((string name, TypeSignature type, ParameterAttributes attributes) p in parameters)
                {
                    method.ParameterDefinitions.Add(new ParameterDefinition((ushort)idx++, p.name, p.attributes));
                }
            }

            outputType.Methods.Add(method);

            // Add 'MethodImpl' pointing to the mapped interface method (use generic params !0, !1 for declaration signature)
            TypeSignature[] implParamTypes = parameters?.Select(p => ToGenericParam(p.type)).ToArray() ?? [];
            TypeSignature implReturnType = ToGenericParam(returnType ?? _outputModule.CorLibTypeFactory.Void);
            MemberReference interfaceMethodRef = new(mappedInterfaceRef, name, MethodSignature.CreateInstance(implReturnType, implParamTypes));

            outputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, method));
        }

        void AddMappedProperty(string name, TypeSignature propertyType, bool hasSetter)
        {
            string propName = isPublic ? name : $"{qualifiedPrefix}.{name}";
            string getMethodName = isPublic ? $"get_{name}" : $"{qualifiedPrefix}.get_{name}";

            // Getter
            MethodAttributes getattributes = isPublic
                ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName)
                : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName);

            MethodDefinition getter = new(getMethodName, getattributes, MethodSignature.CreateInstance(propertyType))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };
            outputType.Methods.Add(getter);

            // Property
            PropertyDefinition prop = new(propName, 0, PropertySignature.CreateInstance(propertyType));

            prop.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));

            // 'MethodImpl' for getter (use generic params for declaration signature)
            TypeSignature implPropertyType = ToGenericParam(propertyType);
            MemberReference getterRef = new(mappedInterfaceRef, $"get_{name}", MethodSignature.CreateInstance(implPropertyType, []));

            outputType.MethodImplementations.Add(new MethodImplementation(getterRef, getter));

            if (hasSetter)
            {
                string putMethodName = isPublic ? $"put_{name}" : $"{qualifiedPrefix}.put_{name}";
                MethodDefinition setter = new(putMethodName, getattributes, MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [propertyType]))
                {
                    ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
                };

                setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));
                outputType.Methods.Add(setter);
                prop.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));

                MemberReference setterRef = new(mappedInterfaceRef, $"put_{name}", MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [implPropertyType]));

                outputType.MethodImplementations.Add(new MethodImplementation(setterRef, setter));
            }

            outputType.Properties.Add(prop);
        }

        // Helper to get generic type arguments from the mapped interface
        TypeSignature GetGenericArg(int index)
        {
            return mappedInterfaceRef is TypeSpecification typeSpecification && typeSpecification.Signature is GenericInstanceTypeSignature genericInstanceSignature && index < genericInstanceSignature.TypeArguments.Count
                ? genericInstanceSignature.TypeArguments[index]
                : _outputModule.CorLibTypeFactory.Object;
        }

        TypeSignature uint32Sig = _outputModule.CorLibTypeFactory.UInt32;
        TypeSignature boolSig = _outputModule.CorLibTypeFactory.Boolean;
        TypeSignature objectSig = _outputModule.CorLibTypeFactory.Object;
        TypeSignature stringSig = _outputModule.CorLibTypeFactory.String;

        TypeSignature GetTypeRef(string @namespace, string name, string assembly, bool isValueType = false) =>
            GetOrCreateTypeReference(@namespace, name, assembly).ToTypeSignature(isValueType);

        TypeSignature GetGenericTypeRef(string @namespace, string name, string assembly, params TypeSignature[] args) =>
            new GenericInstanceTypeSignature(GetOrCreateTypeReference(@namespace, name, assembly), false, args);

        // Generate members for each known mapped type
        switch (mappedTypeName)
        {
            case "IClosable":
                AddMappedMethod("Close", null, null);
                break;

            case "IIterable`1":
                AddMappedMethod("First", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract", GetGenericArg(0)));
                break;

            case "IMap`2":
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetView", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1)));
                AddMappedMethod("HasKey",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Insert",
                    [("key", GetGenericArg(0), ParameterAttributes.In), ("value", GetGenericArg(1), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Lookup",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], GetGenericArg(1));
                AddMappedMethod("Remove",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IMapView`2":
                AddMappedMethod("HasKey",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Lookup",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], GetGenericArg(1));
                AddMappedMethod("Split",
                    [("first", new ByReferenceTypeSignature(GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1))), ParameterAttributes.Out),
                     ("second", new ByReferenceTypeSignature(GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1))), ParameterAttributes.Out)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IVector`1":
                AddMappedMethod("Append",
                    [("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], GetGenericArg(0));
                AddMappedMethod("GetMany",
                    [("startIndex", uint32Sig, ParameterAttributes.In), ("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], uint32Sig);
                AddMappedMethod("GetView", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract", GetGenericArg(0)));
                AddMappedMethod("IndexOf",
                    [("value", GetGenericArg(0), ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedMethod("InsertAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAt",
                    [("index", uint32Sig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAtEnd", null, null);
                AddMappedMethod("ReplaceAll",
                    [("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], null);
                AddMappedMethod("SetAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IVectorView`1":
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], GetGenericArg(0));
                AddMappedMethod("GetMany",
                    [("startIndex", uint32Sig, ParameterAttributes.In), ("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], uint32Sig);
                AddMappedMethod("IndexOf",
                    [("value", GetGenericArg(0), ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IBindableIterable":
                AddMappedMethod("First", null,
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "IBindableIterator", "Microsoft.UI"));
                break;

            case "IBindableVector":
                AddMappedMethod("Append",
                    [("value", objectSig, ParameterAttributes.In)], null);
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], objectSig);
                AddMappedMethod("GetView", null,
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "IBindableVectorView", "Microsoft.UI"));
                AddMappedMethod("IndexOf",
                    [("value", objectSig, ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedMethod("InsertAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", objectSig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAt",
                    [("index", uint32Sig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAtEnd", null, null);
                AddMappedMethod("SetAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", objectSig, ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "INotifyPropertyChanged":
                // Event: PropertyChanged
                AddMappedEvent(outputType, "PropertyChanged",
                    GetTypeRef("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI"),
                    mappedInterfaceRef, isPublic);
                break;

            case "ICommand":
                AddMappedEvent(outputType, "CanExecuteChanged",
                    GetGenericTypeRef("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract", objectSig),
                    mappedInterfaceRef, isPublic);
                AddMappedMethod("CanExecute",
                    [("parameter", objectSig, ParameterAttributes.In)], boolSig);
                AddMappedMethod("Execute",
                    [("parameter", objectSig, ParameterAttributes.In)], null);
                break;

            case "INotifyCollectionChanged":
                AddMappedEvent(outputType, "CollectionChanged",
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI"),
                    mappedInterfaceRef, isPublic);
                break;

            case "INotifyDataErrorInfo":
                AddMappedProperty("HasErrors", boolSig, false);
                AddMappedEvent(outputType, "ErrorsChanged",
                    GetGenericTypeRef("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract",
                        GetTypeRef("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI")),
                    mappedInterfaceRef, isPublic);
                AddMappedMethod("GetErrors",
                    [("propertyName", stringSig, ParameterAttributes.In)],
                    GetGenericTypeRef("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract", objectSig));
                break;

            case "IXamlServiceProvider":
                AddMappedMethod("GetService",
                    [("type", GetTypeRef("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", isValueType: true), ParameterAttributes.In)],
                    objectSig);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Adds a mapped event with <c>add</c>/<c>remove</c> methods and <c>MethodImpl</c> records.
    /// </summary>
    /// <remarks>
    /// The event add method returns <c>EventRegistrationToken</c> and the remove method accepts one,
    /// following Windows Runtime event conventions. <c>MethodImpl</c> entries are created using open generic
    /// form for handler types in declaration signatures to match Windows Runtime metadata conventions.
    /// </remarks>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="handlerType">The handler delegate type signature.</param>
    /// <param name="mappedInterfaceRef">The mapped Windows Runtime interface reference for <c>MethodImpl</c> declarations.</param>
    /// <param name="isPublic">Whether the event is publicly implemented.</param>
    private void AddMappedEvent(
        TypeDefinition outputType,
        string eventName,
        TypeSignature handlerType,
        ITypeDefOrRef mappedInterfaceRef,
        bool isPublic)
    {
        string qualifiedPrefix = mappedInterfaceRef.FullName ?? "";
        TypeReference tokenType = GetOrCreateTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
        TypeSignature tokenSignature = tokenType.ToTypeSignature(true);

        ITypeDefOrRef handlerTypeRef = handlerType is TypeDefOrRefSignature typeDefOrRefSignature ? typeDefOrRefSignature.Type : (handlerType is GenericInstanceTypeSignature genericInstanceSignature ? new TypeSpecification(genericInstanceSignature) : tokenType);

        string addName = isPublic ? $"add_{eventName}" : $"{qualifiedPrefix}.add_{eventName}";
        string removeName = isPublic ? $"remove_{eventName}" : $"{qualifiedPrefix}.remove_{eventName}";

        MethodAttributes attributes = isPublic
            ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName)
            : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName);

        // Add method
        MethodDefinition adder = new(addName, attributes, MethodSignature.CreateInstance(tokenSignature, [handlerType]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        adder.ParameterDefinitions.Add(new ParameterDefinition(1, "handler", ParameterAttributes.In));
        outputType.Methods.Add(adder);

        // Remove method
        MethodDefinition remover = new(removeName, attributes, MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSignature]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        remover.ParameterDefinitions.Add(new ParameterDefinition(1, "token", ParameterAttributes.In));
        outputType.Methods.Add(remover);

        // Event
        EventDefinition @event = new(isPublic ? eventName : $"{qualifiedPrefix}.{eventName}", 0, handlerTypeRef);

        @event.Semantics.Add(new MethodSemantics(adder, MethodSemanticsAttributes.AddOn));
        @event.Semantics.Add(new MethodSemantics(remover, MethodSemanticsAttributes.RemoveOn));
        outputType.Events.Add(@event);

        // MethodImpls — use open generic form for handler type in declaration signatures
        // to match Windows Runtime metadata conventions (e.g., EventHandler`1<!0> not EventHandler`1<Object>)
        TypeSignature implHandlerType = handlerType is GenericInstanceTypeSignature handlerGenericInstanceSignature
            ? ToOpenGenericFormStatic(handlerGenericInstanceSignature, _outputModule)
            : handlerType;
        MemberReference addRef = new(mappedInterfaceRef, $"add_{eventName}", MethodSignature.CreateInstance(tokenSignature, [implHandlerType]));

        outputType.MethodImplementations.Add(new MethodImplementation(addRef, adder));

        MemberReference removeRef = new(mappedInterfaceRef, $"remove_{eventName}", MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSignature]));

        outputType.MethodImplementations.Add(new MethodImplementation(removeRef, remover));
    }

    /// <summary>
    /// Gathers all interfaces from a type and its base type chain, including interfaces
    /// inherited from interfaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolves generic type parameters through the base class chain and deduplicates
    /// by fully-qualified name. Only publicly accessible interfaces are included in the result.
    /// </para>
    /// <para>
    /// This is equivalent to the old generator's <c>GetInterfaces</c> method and ensures all
    /// transitive interface implementations are discovered.
    /// </para>
    /// </remarks>
    /// <param name="type">The <see cref="TypeDefinition"/> to gather interfaces from.</param>
    /// <returns>A list of all <see cref="InterfaceImplementation"/> entries found.</returns>
    private List<InterfaceImplementation> GatherAllInterfaces(TypeDefinition type)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<InterfaceImplementation> result = [];

        void CollectFromType(TypeDefinition typeDef, TypeSignature[]? genericArgs)
        {
            foreach (InterfaceImplementation interfaceImplementation in typeDef.Interfaces)
            {
                if (interfaceImplementation.Interface is null)
                {
                    continue;
                }

                // If we have generic args, substitute them in the interface reference
                ITypeDefOrRef resolvedInterface = interfaceImplementation.Interface;

                if (genericArgs is not null && interfaceImplementation.Interface is TypeSpecification typeSpecification &&
                    typeSpecification.Signature is GenericInstanceTypeSignature genericInstanceSignature)
                {
                    // Resolve generic parameters in type arguments (recursively for nested generics)
                    IEnumerable<TypeSignature> resolvedArgs = genericInstanceSignature.TypeArguments.Select(arg => ResolveGenericArg(arg, genericArgs));

                    resolvedInterface = new TypeSpecification(new GenericInstanceTypeSignature(
                        genericType: genericInstanceSignature.GenericType,
                        isValueType: genericInstanceSignature.IsValueType,
                        typeArguments: resolvedArgs));
                }

                string name = resolvedInterface.FullName ?? "";

                if (!seen.Add(name))
                {
                    continue;
                }

                if (IsPubliclyAccessible(resolvedInterface))
                {
                    result.Add(new InterfaceImplementation(resolvedInterface));
                }

                // Also collect interfaces inherited by this interface
                TypeDefinition? interfaceDef = resolvedInterface is TypeSpecification interfaceTypeSpecification
                    ? SafeResolve((interfaceTypeSpecification.Signature as GenericInstanceTypeSignature)?.GenericType)
                    : SafeResolve(resolvedInterface);

                if (interfaceDef is not null)
                {
                    // Pass the resolved interface's generic args down
                    TypeSignature[]? innerArgs = resolvedInterface is TypeSpecification innerTypeSpecification &&
                        innerTypeSpecification.Signature is GenericInstanceTypeSignature innerGenericInstanceSignature
                        ? [.. innerGenericInstanceSignature.TypeArguments]
                        : null;

                    CollectFromType(interfaceDef, innerArgs);
                }
            }
        }

        // Collect from the type itself
        CollectFromType(type, null);

        // Walk base types, resolving generic arguments
        ITypeDefOrRef? baseTypeRef = type.BaseType;

        while (baseTypeRef is not null)
        {
            TypeDefinition? baseDef = SafeResolve(baseTypeRef);
            if (baseDef is null)
            {
                break;
            }

            // Get generic arguments from the base type reference
            TypeSignature[]? baseGenericArgs = baseTypeRef is TypeSpecification baseTypeSpecification &&
                baseTypeSpecification.Signature is GenericInstanceTypeSignature baseGenericInstanceSignature
                ? [.. baseGenericInstanceSignature.TypeArguments]
                : null;

            CollectFromType(baseDef, baseGenericArgs);

            baseTypeRef = baseDef.BaseType;
        }

        return result;
    }

    /// <summary>
    /// Recursively resolves generic parameters in a type signature using the provided generic arguments.
    /// </summary>
    /// <remarks>
    /// Handles nested generic instances like <c>KeyValuePair&lt;!0, !1&gt;</c> where the
    /// generic parameter indices need to be substituted with the concrete type arguments
    /// from the parent type.
    /// </remarks>
    /// <param name="arg">The type signature that may contain generic parameters.</param>
    /// <param name="genericArgs">The generic type arguments to substitute.</param>
    /// <returns>The resolved <see cref="TypeSignature"/> with generic parameters replaced.</returns>
    private static TypeSignature ResolveGenericArg(TypeSignature arg, TypeSignature[] genericArgs)
    {
        if (arg is GenericParameterSignature genericParameter && genericParameter.Index < genericArgs.Length)
        {
            return genericArgs[genericParameter.Index];
        }

        if (arg is GenericInstanceTypeSignature nestedGenericInstanceSignature)
        {
            TypeSignature[] resolvedInnerArgs = [.. nestedGenericInstanceSignature.TypeArguments.Select(a => ResolveGenericArg(a, genericArgs))];

            return new GenericInstanceTypeSignature(nestedGenericInstanceSignature.GenericType, nestedGenericInstanceSignature.IsValueType, resolvedInnerArgs);
        }

        return arg;
    }

    /// <summary>
    /// Converts a <see cref="GenericInstanceTypeSignature"/> to its open form for <c>MethodImpl</c> declarations.
    /// </summary>
    /// <remarks>
    /// Replaces all resolved type arguments with generic parameter references (<c>!0</c>, <c>!1</c>, etc.)
    /// to match Windows Runtime metadata conventions. For example, <c>EventHandler`1&lt;Object&gt;</c> becomes
    /// <c>EventHandler`1&lt;!0&gt;</c>.
    /// </remarks>
    /// <param name="genericInstanceSignature">The generic instance type signature to convert.</param>
    /// <param name="module">The output module used to create generic parameter signatures.</param>
    /// <returns>A new <see cref="GenericInstanceTypeSignature"/> with open generic parameters.</returns>
    private static GenericInstanceTypeSignature ToOpenGenericFormStatic(GenericInstanceTypeSignature genericInstanceSignature, ModuleDefinition module)
    {
        TypeSignature[] openArgs = new TypeSignature[genericInstanceSignature.TypeArguments.Count];

        for (int i = 0; i < genericInstanceSignature.TypeArguments.Count; i++)
        {
            openArgs[i] = new GenericParameterSignature(module, GenericParameterType.Type, i);
        }

        return new GenericInstanceTypeSignature(genericInstanceSignature.GenericType, genericInstanceSignature.IsValueType, openArgs);
    }

    /// <summary>
    /// Determines if a mapped interface is publicly implemented on the class.
    /// </summary>
    /// <remarks>
    /// Checks if the class (or any type in its base class hierarchy) declares public methods
    /// whose names match the .NET interface members. This determines whether the mapped Windows Runtime
    /// interface members should be emitted as public or as explicit (private) implementations.
    /// </remarks>
    /// <param name="classType">The class <see cref="TypeDefinition"/> to check.</param>
    /// <param name="interfaceImplementation">The interface implementation to check.</param>
    /// <param name="runtimeContext">The runtime context for resolving types, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the interface has at least one publicly implemented member; otherwise, <see langword="false"/>.</returns>
    private static bool IsInterfacePubliclyImplemented(TypeDefinition classType, InterfaceImplementation interfaceImplementation, RuntimeContext? runtimeContext)
    {
        TypeDefinition? interfaceDef = interfaceImplementation.Interface is TypeSpecification typeSpecification
            ? (typeSpecification.Signature as GenericInstanceTypeSignature)?.GenericType.Resolve(runtimeContext)
            : interfaceImplementation.Interface?.Resolve(runtimeContext);

        if (interfaceDef is null)
        {
            return false;
        }

        // Walk the class hierarchy to find public implementations
        TypeDefinition? current = classType;

        while (current is not null)
        {
            foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
            {
                string methodName = interfaceMethod.Name?.Value ?? "";
                if (current.Methods.Any(m => m.IsPublic && m.Name?.Value == methodName))
                {
                    return true;
                }
            }

            current = current.BaseType?.Resolve(runtimeContext);
        }

        return false;
    }

    /// <summary>
    /// Formats a qualified interface name for use in explicit method names.
    /// </summary>
    /// <remarks>
    /// Uses short type names for generic arguments (e.g., <c>"IMap`2&lt;String, Int32&gt;"</c>
    /// instead of <c>"IMap`2&lt;System.String, System.Int32&gt;"</c>) to match the naming
    /// convention used in WinMD explicit interface implementations.
    /// </remarks>
    /// <param name="typeRef">The interface type reference to format.</param>
    /// <returns>The formatted qualified interface name.</returns>
    private static string FormatQualifiedInterfaceName(ITypeDefOrRef typeRef)
    {
        if (typeRef is TypeSpecification spec && spec.Signature is GenericInstanceTypeSignature genericInstanceSignature)
        {
            string baseName = genericInstanceSignature.GenericType.FullName ?? "";
            string args = string.Join(", ", genericInstanceSignature.TypeArguments.Select(FormatShortTypeName));

            return $"{baseName}<{args}>";
        }

        return typeRef.FullName ?? "";
    }

    /// <summary>
    /// Formats a type signature using short names (e.g., <c>"String"</c> instead of <c>"System.String"</c>).
    /// </summary>
    /// <param name="signature">The type signature to format.</param>
    /// <returns>The short-form type name.</returns>
    private static string FormatShortTypeName(TypeSignature signature)
    {
        if (signature is GenericInstanceTypeSignature genericInstanceSignature)
        {
            string baseName = genericInstanceSignature.GenericType.FullName ?? "";
            string args = string.Join(", ", genericInstanceSignature.TypeArguments.Select(FormatShortTypeName));

            return $"{baseName}<{args}>";
        }

        return signature is CorLibTypeSignature corLib
            ? corLib.Type.Name?.Value ?? signature.FullName
            : signature.FullName;
    }

    /// <summary>
    /// Collects the names of members that belong to custom mapped or unmapped .NET interfaces.
    /// </summary>
    /// <remarks>
    /// These members should be excluded from the WinMD class definition since they are replaced
    /// by the Windows Runtime mapped interface members. This includes method names, property names (and their
    /// accessor names without the <c>get_</c>/<c>set_</c> prefix), and event names.
    /// </remarks>
    /// <param name="inputType">The input class <see cref="TypeDefinition"/>.</param>
    /// <returns>A set of member names that should be excluded from the class.</returns>
    private HashSet<string> CollectCustomMappedMemberNames(TypeDefinition inputType)
    {
        HashSet<string> memberNames = new(StringComparer.Ordinal);

        List<InterfaceImplementation> allInterfaces = GatherAllInterfaces(inputType);

        foreach (InterfaceImplementation interfaceImplementation in allInterfaces)
        {
            if (interfaceImplementation.Interface is null)
            {
                continue;
            }

            string interfaceName = GetInterfaceFullName(interfaceImplementation.Interface);

            // Include members from both mapped interfaces and unmapped interfaces
            if (!_mapper.HasMappingForType(interfaceName) &&
                !TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                continue;
            }

            TypeDefinition? interfaceDef = interfaceImplementation.Interface is TypeSpecification typeSpecification
                ? SafeResolve((typeSpecification.Signature as GenericInstanceTypeSignature)?.GenericType)
                : SafeResolve(interfaceImplementation.Interface);

            if (interfaceDef is null)
            {
                continue;
            }

            // Add all method names from this interface
            foreach (MethodDefinition method in interfaceDef.Methods)
            {
                string methodName = method.Name?.Value ?? "";
                _ = memberNames.Add(methodName);

                // For property accessors, also add the property name
                if (methodName.StartsWith("get_", StringComparison.Ordinal) || methodName.StartsWith("set_", StringComparison.Ordinal))
                {
                    _ = memberNames.Add(methodName[4..]);
                }
            }

            // Add property names
            foreach (PropertyDefinition prop in interfaceDef.Properties)
            {
                _ = memberNames.Add(prop.Name?.Value ?? "");
            }

            // Add event names
            foreach (EventDefinition @event in interfaceDef.Events)
            {
                _ = memberNames.Add(@event.Name?.Value ?? "");
            }
        }

        return memberNames;
    }
}