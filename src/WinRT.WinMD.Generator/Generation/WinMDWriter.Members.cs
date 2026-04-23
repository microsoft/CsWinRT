// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Discovery;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinMDWriter
{
    private void AddMethodToInterface(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodAttributes attrs =
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.Abstract |
            MethodAttributes.Virtual |
            MethodAttributes.NewSlot;

        if (inputMethod.IsSpecialName)
        {
            attrs |= MethodAttributes.SpecialName;
        }

        MethodDefinition outputMethod = new(
            inputMethod.Name!.Value,
            attrs,
            MethodSignature.CreateInstance(returnType, parameterTypes));

        // Add parameter definitions with correct attributes for WinRT array conventions
        AddParameterDefinitions(outputMethod, inputMethod);

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    private void AddMethodToClass(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        bool isConstructor = inputMethod.IsConstructor;
        MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig;

        if (isConstructor)
        {
            attrs |= MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName;
        }
        else if (inputMethod.IsStatic)
        {
            attrs |= MethodAttributes.Static;
        }
        else
        {
            attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
        }

        if (inputMethod.IsSpecialName && !isConstructor)
        {
            attrs |= MethodAttributes.SpecialName;
        }

        MethodSignature signature = isConstructor || !inputMethod.IsStatic
            ? MethodSignature.CreateInstance(returnType, parameterTypes)
            : MethodSignature.CreateStatic(returnType, parameterTypes);

        MethodDefinition outputMethod = new(
            inputMethod.Name!.Value,
            attrs,
            signature)
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        // Add parameter definitions with correct attributes for WinRT array conventions
        AddParameterDefinitions(outputMethod, inputMethod);

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    /// <summary>
    /// Adds parameter definitions to an output method with correct WinRT attributes.
    /// Handles Span/ReadOnlySpan → array parameter attribute mapping:
    /// - ReadOnlySpan&lt;T&gt; → [in] T[] (PassArray)
    /// - Span&lt;T&gt; → [out] T[] without BYREF (FillArray)
    /// - out T[] → [out] T[] with BYREF (ReceiveArray)
    /// - All other params → [in]
    /// </summary>
    private static void AddParameterDefinitions(MethodDefinition outputMethod, MethodDefinition inputMethod)
    {
        int paramIndex = 1;
        IList<TypeSignature> inputParamTypes = inputMethod.Signature!.ParameterTypes;

        foreach (ParameterDefinition inputParam in inputMethod.ParameterDefinitions)
        {
            int sigIndex = paramIndex - 1;
            ParameterAttributes paramAttrs = ParameterAttributes.In;

            if (sigIndex < inputParamTypes.Count)
            {
                paramAttrs = GetWinRTParameterAttributes(inputParamTypes[sigIndex]);
            }

            outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                paramAttrs));
        }
    }

    /// <summary>
    /// Determines the WinRT parameter attributes based on the input parameter type.
    /// </summary>
    private static ParameterAttributes GetWinRTParameterAttributes(TypeSignature inputParamType)
    {
        // out parameters (ByRef) stay as Out
        if (inputParamType is ByReferenceTypeSignature)
        {
            return ParameterAttributes.Out;
        }

        // Span<T> → FillArray pattern: [out] without BYREF
        if (inputParamType is GenericInstanceTypeSignature gits)
        {
            string typeName = AssemblyAnalyzer.GetQualifiedName(gits.GenericType);
            if (typeName == "System.Span`1")
            {
                return ParameterAttributes.Out;
            }
        }

        // ReadOnlySpan<T> and everything else → [in]
        return ParameterAttributes.In;
    }

    private void AddPropertyToType(TypeDefinition outputType, PropertyDefinition inputProperty, bool isInterfaceParent)
    {
        TypeSignature propertyType = MapTypeSignatureToOutput(inputProperty.Signature!.ReturnType);

        // For interface parents (synthesized interfaces), always use instance signatures
        // even when the original property was static — interface methods are always instance
        bool isStatic = !isInterfaceParent && (inputProperty.GetMethod?.IsStatic == true || inputProperty.SetMethod?.IsStatic == true);

        PropertyDefinition outputProperty = new(
            inputProperty.Name!.Value,
            0,
            isStatic ? PropertySignature.CreateStatic(propertyType) : PropertySignature.CreateInstance(propertyType));

        // Add getter
        if (inputProperty.GetMethod != null)
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature getSignature = isStatic
                ? MethodSignature.CreateStatic(propertyType)
                : MethodSignature.CreateInstance(propertyType);

            MethodDefinition getter = new("get_" + inputProperty.Name.Value, attrs, getSignature);
            if (!isInterfaceParent)
            {
                getter.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            outputType.Methods.Add(getter);
            outputProperty.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
        }

        // Add setter (WinRT uses "put_" prefix)
        if (inputProperty.SetMethod != null && inputProperty.SetMethod.IsPublic)
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature setSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, [propertyType])
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [propertyType]);

            MethodDefinition setter = new("put_" + inputProperty.Name.Value, attrs, setSignature);
            if (!isInterfaceParent)
            {
                setter.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }

            // Add parameter
            setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));

            outputType.Methods.Add(setter);
            outputProperty.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
        }

        outputType.Properties.Add(outputProperty);

        // Copy custom attributes from the input property
        CopyCustomAttributes(inputProperty, outputProperty);
    }

    private void AddEventToType(TypeDefinition outputType, EventDefinition inputEvent, bool isInterfaceParent)
    {
        ITypeDefOrRef eventType = ImportTypeReference(inputEvent.EventType!);
        TypeReference eventRegistrationTokenType = GetOrCreateTypeReference(
            "Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");

        EventDefinition outputEvent = new(inputEvent.Name!.Value, 0, eventType);

        // For interface parents (synthesized interfaces), always use instance signatures
        bool isStatic = !isInterfaceParent && inputEvent.AddMethod?.IsStatic == true;

        // Add method
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature handlerSig = eventType.ToTypeSignature(false);
            TypeSignature tokenSig = eventRegistrationTokenType.ToTypeSignature(true);

            MethodSignature addSignature = isStatic
                ? MethodSignature.CreateStatic(tokenSig, [handlerSig])
                : MethodSignature.CreateInstance(tokenSig, [handlerSig]);

            MethodDefinition adder = new("add_" + inputEvent.Name.Value, attrs, addSignature);
            if (!isInterfaceParent)
            {
                adder.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            adder.ParameterDefinitions.Add(new ParameterDefinition(1, "handler", ParameterAttributes.In));
            outputType.Methods.Add(adder);
            outputEvent.Semantics.Add(new MethodSemantics(adder, MethodSemanticsAttributes.AddOn));
        }

        // Remove method
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature tokenSig = eventRegistrationTokenType.ToTypeSignature(true);

            MethodSignature removeSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, [tokenSig])
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSig]);

            MethodDefinition remover = new("remove_" + inputEvent.Name.Value, attrs, removeSignature);
            if (!isInterfaceParent)
            {
                remover.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            remover.ParameterDefinitions.Add(new ParameterDefinition(1, "token", ParameterAttributes.In));
            outputType.Methods.Add(remover);
            outputEvent.Semantics.Add(new MethodSemantics(remover, MethodSemanticsAttributes.RemoveOn));
        }

        outputType.Events.Add(outputEvent);

        // Copy custom attributes from the input event
        CopyCustomAttributes(inputEvent, outputEvent);
    }
}