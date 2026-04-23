// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Adds a method definition to a WinMD interface type.
    /// </summary>
    /// <remarks>
    /// Interface methods in WinMD are abstract virtual methods. The return type and parameter types
    /// are mapped from .NET to Windows Runtime equivalents. Custom attributes from the input method are copied
    /// to the output.
    /// </remarks>
    /// <param name="outputType">The output interface <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="inputMethod">The input <see cref="MethodDefinition"/> to add.</param>
    private void AddMethodToInterface(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodAttributes attributes =
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.Abstract |
            MethodAttributes.Virtual |
            MethodAttributes.NewSlot;

        if (inputMethod.IsSpecialName)
        {
            attributes |= MethodAttributes.SpecialName;
        }

        MethodDefinition outputMethod = new(
            name: inputMethod.Name!.Value,
            attributes: attributes,
            signature: MethodSignature.CreateInstance(returnType, parameterTypes));

        // Add parameter definitions with correct attributes for Windows Runtime array conventions
        AddParameterDefinitions(outputMethod, inputMethod);

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    /// <summary>
    /// Adds a method definition to a WinMD class type.
    /// </summary>
    /// <remarks>
    /// Class methods in WinMD are final virtual methods (sealed). Constructors receive
    /// <c>SpecialName</c> and <c>RuntimeSpecialName</c> attributes. Static methods are emitted
    /// as static. All methods use <c>Runtime | Managed</c> implementation attributes since the
    /// actual implementation is provided at runtime by the Windows Runtime projection.
    /// </remarks>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="inputMethod">The input <see cref="MethodDefinition"/> to add.</param>
    private void AddMethodToClass(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        bool isConstructor = inputMethod.IsConstructor;
        MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig;

        if (isConstructor)
        {
            attributes |= MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName;
        }
        else if (inputMethod.IsStatic)
        {
            attributes |= MethodAttributes.Static;
        }
        else
        {
            attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
        }

        if (inputMethod.IsSpecialName && !isConstructor)
        {
            attributes |= MethodAttributes.SpecialName;
        }

        MethodSignature signature = isConstructor || !inputMethod.IsStatic
            ? MethodSignature.CreateInstance(returnType, parameterTypes)
            : MethodSignature.CreateStatic(returnType, parameterTypes);

        MethodDefinition outputMethod = new(
            name: inputMethod.Name!.Value,
            attributes: attributes,
            signature: signature)
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        // Add parameter definitions with correct attributes for Windows Runtime array conventions
        AddParameterDefinitions(outputMethod, inputMethod);

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    /// <summary>
    /// Adds parameter definitions to an output method with correct Windows Runtime attributes.
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
            ParameterAttributes paramattributes = ParameterAttributes.In;

            if (sigIndex < inputParamTypes.Count)
            {
                paramattributes = GetWinRTParameterAttributes(inputParamTypes[sigIndex]);
            }

            outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                paramattributes));
        }
    }

    /// <summary>
    /// Determines the Windows Runtime parameter attributes based on the input parameter type.
    /// </summary>
    private static ParameterAttributes GetWinRTParameterAttributes(TypeSignature inputParamType)
    {
        // out parameters (ByRef) stay as Out
        if (inputParamType is ByReferenceTypeSignature)
        {
            return ParameterAttributes.Out;
        }

        // Span<T> → FillArray pattern: [out] without BYREF
        if (inputParamType is GenericInstanceTypeSignature genericInstanceSignature)
        {
            string typeName = genericInstanceSignature.GenericType.QualifiedName;
            if (typeName == "System.Span`1")
            {
                return ParameterAttributes.Out;
            }
        }

        // ReadOnlySpan<T> and everything else → [in]
        return ParameterAttributes.In;
    }

    /// <summary>
    /// Adds a property definition to a WinMD type (interface or class).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows Runtime properties use <c>get_</c> for getters and <c>put_</c> for setters (instead of .NET's <c>set_</c>).
    /// For interface parents (including synthesized interfaces), the methods are emitted as abstract virtual
    /// even when the original property was static, since Windows Runtime interface methods are always instance methods.
    /// </para>
    /// <para>
    /// Custom attributes from the input property are copied to the output property.
    /// </para>
    /// </remarks>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="inputProperty">The input <see cref="PropertyDefinition"/> to add.</param>
    /// <param name="isInterfaceParent">Whether the parent type is an interface (forces instance signatures).</param>
    private void AddPropertyToType(TypeDefinition outputType, PropertyDefinition inputProperty, bool isInterfaceParent)
    {
        TypeSignature propertyType = MapTypeSignatureToOutput(inputProperty.Signature!.ReturnType);

        // For interface parents (synthesized interfaces), always use instance signatures
        // even when the original property was static — interface methods are always instance
        bool isStatic = !isInterfaceParent && (inputProperty.GetMethod?.IsStatic == true || inputProperty.SetMethod?.IsStatic == true);

        PropertyDefinition outputProperty = new(
            name: inputProperty.Name!.Value,
            attributes: PropertyAttributes.None,
            signature: isStatic ? PropertySignature.CreateStatic(propertyType) : PropertySignature.CreateInstance(propertyType));

        // Add getter
        if (inputProperty.GetMethod is not null)
        {
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attributes |= MethodAttributes.Static;
            }
            else
            {
                attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature getSignature = isStatic
                ? MethodSignature.CreateStatic(propertyType)
                : MethodSignature.CreateInstance(propertyType);

            MethodDefinition getter = new("get_" + inputProperty.Name.Value, attributes, getSignature);
            if (!isInterfaceParent)
            {
                getter.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            outputType.Methods.Add(getter);
            outputProperty.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
        }

        // Add setter (Windows Runtime uses "put_" prefix)
        if (inputProperty.SetMethod is not null && inputProperty.SetMethod.IsPublic)
        {
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attributes |= MethodAttributes.Static;
            }
            else
            {
                attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature setSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, [propertyType])
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [propertyType]);

            MethodDefinition setter = new("put_" + inputProperty.Name.Value, attributes, setSignature);
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

    /// <summary>
    /// Adds an event definition to a WinMD type (interface or class).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows Runtime events always use <c>EventRegistrationToken</c> for the add/remove pattern:
    /// the <c>add_</c> method returns an <c>EventRegistrationToken</c>, and the <c>remove_</c>
    /// method accepts one. This differs from the .NET event pattern where both accessors are <c>void</c>.
    /// </para>
    /// <para>
    /// For interface parents (including synthesized interfaces), the methods are emitted as abstract virtual
    /// even when the original event was static.
    /// </para>
    /// </remarks>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> in the WinMD.</param>
    /// <param name="inputEvent">The input <see cref="EventDefinition"/> to add.</param>
    /// <param name="isInterfaceParent">Whether the parent type is an interface (forces instance signatures).</param>
    private void AddEventToType(TypeDefinition outputType, EventDefinition inputEvent, bool isInterfaceParent)
    {
        ITypeDefOrRef eventType = ImportTypeReference(inputEvent.EventType!);

        TypeReference eventRegistrationTokenType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation",
            name: "EventRegistrationToken",
            assemblyName: "Windows.Foundation.FoundationContract");

        EventDefinition outputEvent = new(inputEvent.Name!.Value, 0, eventType);

        // For interface parents (synthesized interfaces), always use instance signatures
        bool isStatic = !isInterfaceParent && inputEvent.AddMethod?.IsStatic == true;

        // Add method
        {
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attributes |= MethodAttributes.Static;
            }
            else
            {
                attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature handlerSignature = eventType.ToTypeSignature(false);
            TypeSignature tokenSignature = eventRegistrationTokenType.ToTypeSignature(true);

            MethodSignature addSignature = isStatic
                ? MethodSignature.CreateStatic(tokenSignature, [handlerSignature])
                : MethodSignature.CreateInstance(tokenSignature, [handlerSignature]);

            MethodDefinition adder = new("add_" + inputEvent.Name.Value, attributes, addSignature);
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
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attributes |= MethodAttributes.Static;
            }
            else
            {
                attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature tokenSignature = eventRegistrationTokenType.ToTypeSignature(true);

            MethodSignature removeSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, [tokenSignature])
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSignature]);

            MethodDefinition remover = new("remove_" + inputEvent.Name.Value, attributes, removeSignature);
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