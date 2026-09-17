// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Errors;
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

        // Record any author-specified '[Overload]' name so finalization can honor it
        RecordUserSpecifiedOverloadName(inputMethod, outputMethod);
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

        // Copy custom attributes from the input method. Constructors are exposed to Windows Runtime
        // through activation factory methods, so their '.ctor' row carries no '[Experimental]' marker.
        CopyCustomAttributes(inputMethod, outputMethod, skipExperimentalAttribute: isConstructor);
    }

    /// <summary>
    /// Adds parameter definitions to an output method with correct Windows Runtime attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The In/Out flags on the input parameters are honored as-is when present. This allows
    /// authors to opt-in or opt-out of the WinRT defaults explicitly (e.g. <c>[In] ref T</c>
    /// preserves the <c>In</c> flag, <c>[Out] ref T</c> preserves the <c>Out</c> flag). When
    /// no In/Out flag is set on the input parameter, the WinRT default is inferred from the
    /// parameter type:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="System.ReadOnlySpan{T}"/> → <c>[in] T[]</c> (PassArray)</item>
    ///   <item><see cref="System.Span{T}"/> → <c>[out] T[]</c> without BYREF (FillArray)</item>
    ///   <item><c>out T[]</c> (byref to <c>SzArray</c>) → <c>[out] T[]</c> with BYREF (ReceiveArray); already captured by the input's <c>Out</c> flag.</item>
    ///   <item>Any other by-reference type (e.g. <c>ref Guid</c> on a COM interop interface) → <c>[in]</c>, matching the MIDL convention for <c>ref const T</c> parameters.</item>
    ///   <item>All other params → <c>[in]</c>.</item>
    /// </list>
    /// <para>
    /// Array and span shapes that have no Windows Runtime representation are rejected with a well-known
    /// error: a by-reference span (e.g. <c>out Span&lt;T&gt;</c>), or a <c>ref</c>/<c>in</c> array (only
    /// <c>out T[]</c> is a valid by-reference array). See <see cref="GetWinRTParameterAttributes"/>.
    /// </para>
    /// </remarks>
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
                paramattributes = GetWinRTParameterAttributes(inputMethod, inputParam, inputParamTypes[sigIndex]);
            }

            outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                paramattributes));
        }
    }

    /// <summary>
    /// Determines the Windows Runtime parameter attributes based on the input parameter and its type,
    /// validating that the parameter uses a supported Windows Runtime calling convention.
    /// </summary>
    /// <remarks>
    /// If the input parameter already has <see cref="ParameterAttributes.In"/> or
    /// <see cref="ParameterAttributes.Out"/> set, those flags are preserved unchanged. Otherwise,
    /// the type drives the default per the rules documented on <see cref="AddParameterDefinitions"/>.
    /// By-reference spans (e.g. <c>out Span&lt;T&gt;</c>) and <c>ref</c>/<c>in</c> arrays have no
    /// Windows Runtime representation and throw a <see cref="Errors.WellKnownWinMDException"/>.
    /// </remarks>
    /// <param name="inputMethod">The method that declares the parameter (used for error context).</param>
    /// <param name="inputParam">The input <see cref="ParameterDefinition"/> (provides the In/Out direction flags).</param>
    /// <param name="inputParamType">The input parameter <see cref="TypeSignature"/>.</param>
    /// <returns>The Windows Runtime <see cref="ParameterAttributes"/> for the parameter.</returns>
    /// <exception cref="Errors.WellKnownWinMDException">Thrown if the parameter uses an unsupported array or span convention.</exception>
    private static ParameterAttributes GetWinRTParameterAttributes(
        MethodDefinition inputMethod,
        ParameterDefinition inputParam,
        TypeSignature inputParamType)
    {
        // Look through any custom modifiers (e.g. the 'modreq(InAttribute)' emitted for 'in' parameters
        // on abstract, virtual, interface, or delegate members) to inspect the real underlying signature
        TypeSignature parameterType = inputParamType.StripCustomModifiers();

        // Validate by-reference parameters that wrap a span or array. Windows Runtime spans are always
        // passed by value, and Windows Runtime arrays use one of three conventions: 'ReadOnlySpan<T>'
        // (PassArray), 'Span<T>' (FillArray), or 'out T[]' (ReceiveArray). A by-reference span, or a
        // 'ref'/'in' array, has no Windows Runtime representation and is rejected here.
        if (parameterType is ByReferenceTypeSignature byReference)
        {
            TypeSignature elementType = byReference.BaseType.StripCustomModifiers();

            if (elementType.IsTypeOfSpan() || elementType.IsTypeOfReadOnlySpan())
            {
                throw WellKnownWinMDExceptions.ByReferenceSpanParameterNotSupported(
                    inputMethod.DeclaringType!.FullName,
                    inputMethod.Name!.Value,
                    inputParam.Name!.Value);
            }

            // 'out T[]' (ReceiveArray) is valid, but 'ref T[]'/'in T[]' are not. By-reference non-array
            // types (e.g. 'ref Guid riid' on a COM interop interface) are still allowed and handled below.
            if (elementType is SzArrayTypeSignature && !inputParam.IsOut)
            {
                throw WellKnownWinMDExceptions.ByReferenceArrayParameterNotSupported(
                    inputMethod.DeclaringType!.FullName,
                    inputMethod.Name!.Value,
                    inputParam.Name!.Value);
            }
        }

        // Preserve any 'In'/'Out' direction flags the input parameter already carries
        ParameterAttributes inputDirectionFlags = inputParam.Attributes & (ParameterAttributes.In | ParameterAttributes.Out);

        if (inputDirectionFlags != 0)
        {
            return inputDirectionFlags;
        }

        // 'Span<T>' → 'FillArray' pattern: '[out]' without 'BYREF'
        if (parameterType.IsTypeOfSpan())
        {
            return ParameterAttributes.Out;
        }

        // By-reference parameters with no explicit direction flag (e.g. 'ref Guid riid'
        // on a COM interop interface) default to '[in]', matching the MIDL convention for
        // 'ref const T' parameters. 'ReadOnlySpan<T>' and everything else also default to '[in]'.
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

        MethodDefinition? getter = null;
        MethodDefinition? setter = null;

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

            getter = new("get_" + inputProperty.Name.Value, attributes, getSignature);
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

            setter = new("put_" + inputProperty.Name.Value, attributes, setSignature);
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

        // Copy custom attributes from the input property. The accessor attributes (e.g. '[Deprecated]')
        // are emitted on the accessor (the getter, or the setter for write-only properties) rather than
        // the property row, matching the placement used by MIDL so that they resolve consistently
        CopyCustomAttributes(inputProperty, outputProperty, skipAccessorAttributes: true);
        CopyAccessorAttributes(inputProperty, getter ?? setter ?? (IHasCustomAttribute)outputProperty);
    }

    /// <summary>
    /// Adds a setter-only property to the output type. This is used when a class adds a public setter
    /// for a property whose getter is already defined on a public interface.
    /// </summary>
    private void AddSetterOnlyPropertyToType(TypeDefinition outputType, PropertyDefinition inputProperty)
    {
        TypeSignature propertyType = MapTypeSignatureToOutput(inputProperty.Signature!.ReturnType);

        PropertyDefinition outputProperty = new(
            inputProperty.Name!.Value,
            0,
            PropertySignature.CreateInstance(propertyType));

        MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                                 MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;

        MethodSignature setSignature = MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [propertyType]);
        MethodDefinition setter = new("put_" + inputProperty.Name.Value, attrs, setSignature);
        setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));

        outputType.Methods.Add(setter);
        outputProperty.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));

        outputType.Properties.Add(outputProperty);

        // Copy custom attributes from the input property. The accessor attributes (e.g. '[Deprecated]')
        // are emitted on the setter accessor rather than the property row, matching the placement used by MIDL
        CopyCustomAttributes(inputProperty, outputProperty, skipAccessorAttributes: true);
        CopyAccessorAttributes(inputProperty, setter);
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

        MethodDefinition adder;

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

            adder = new("add_" + inputEvent.Name.Value, attributes, addSignature);
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

        // Copy custom attributes from the input event. The accessor attributes (e.g. '[Deprecated]') are
        // emitted on the 'add' accessor rather than the event row, matching the placement used by MIDL
        CopyCustomAttributes(inputEvent, outputEvent, skipAccessorAttributes: true);
        CopyAccessorAttributes(inputEvent, adder);
    }
}