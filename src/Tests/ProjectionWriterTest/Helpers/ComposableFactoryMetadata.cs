// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Creates synthetic metadata with an unsealed runtime class activated through two <c>[Composable]</c> factory
/// interfaces, as MIDL emits for constructors added in a later contract version (e.g. WinUI's <c>DataTemplate</c>).
/// </summary>
/// <remarks>
/// The metadata has the same shape MIDL produces for the following IDL (the constructor added in version 2 goes on a
/// second factory interface, <c>IWidgetFactory2</c>), without the contract attributes the projection does not need:
/// <code>
/// namespace Contoso
/// {
///     [contractversion(2)]
///     apicontract ContosoContract {};
///
///     [contract(ContosoContract, 1)]
///     unsealed runtimeclass Widget
///     {
///         Widget();
///
///         [contract(ContosoContract, 2)]
///         Widget(Int32 count);
///
///         void DoStuffWidget();
///     }
///
///     [contract(ContosoContract, 1)]
///     unsealed runtimeclass DerivedWidget : Widget
///     {
///         DerivedWidget();
///
///         void DoStuffDerivedWidget();
///     }
/// }
/// </code>
/// </remarks>
internal static class ComposableFactoryMetadata
{
    /// <summary>The unsealed class with two composable factories.</summary>
    public const string ClassName = "Widget";

    /// <summary>An unsealed class deriving from <see cref="ClassName"/>, which chains to its base-chaining constructors.</summary>
    public const string DerivedClassName = "DerivedWidget";

    public static string Create(string directory)
    {
        ModuleDefinition module = new("Contoso.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };

        _ = new AssemblyDefinition("Contoso", new Version(255, 255, 255, 255))
        {
            Modules = { module },
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };

        AssemblyReference corlib = (AssemblyReference)module.CorLibTypeFactory.CorLibScope;
        corlib.Version = new Version(255, 255, 255, 255);

        AssemblyReference foundation = new("Windows.Foundation.FoundationContract", new Version(255, 255, 255, 255))
        {
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };

        TypeSignature systemType = new TypeReference(module, corlib, "System", "Type").ToTypeSignature(false);
        CorLibTypeSignature objectType = module.CorLibTypeFactory.Object;
        int seed = 0x2a7c4e10;

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(static argument => argument.ArgumentType).ToArray()));

            return new CustomAttribute(constructor, new CustomAttributeSignature(arguments));
        }

        TypeDefinition AddExclusiveInterface(string name, TypeDefinition owner)
        {
            TypeDefinition iface = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(iface);

            byte[] bytes = new Guid(seed++, 0x5b13, 0x4c8a, 0x91, 0x2e, 0x6d, 0x40, 0xb7, 0xa9, 0x3f, 0x02).ToByteArray();

            iface.CustomAttributes.Add(Attribute("GuidAttribute",
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(bytes, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 6)),
                new(module.CorLibTypeFactory.Byte, bytes[8]),
                new(module.CorLibTypeFactory.Byte, bytes[9]),
                new(module.CorLibTypeFactory.Byte, bytes[10]),
                new(module.CorLibTypeFactory.Byte, bytes[11]),
                new(module.CorLibTypeFactory.Byte, bytes[12]),
                new(module.CorLibTypeFactory.Byte, bytes[13]),
                new(module.CorLibTypeFactory.Byte, bytes[14]),
                new(module.CorLibTypeFactory.Byte, bytes[15])));
            iface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

            return iface;
        }

        TypeDefinition AddClass(string name, ITypeDefOrRef baseType)
        {
            TypeDefinition owner = new("Contoso", name, TypeAttributes.Public | TypeAttributes.WindowsRuntime, baseType);

            module.TopLevelTypes.Add(owner);

            TypeDefinition defaultInterface = AddExclusiveInterface($"I{name}", owner);

            defaultInterface.Methods.Add(new MethodDefinition($"DoStuff{name}",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)));

            InterfaceImplementation implementation = new(defaultInterface);
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            owner.Interfaces.Add(implementation);

            return owner;
        }

        // Adds a '[Composable]' factory with one 'CreateInstance' constructor taking 'arguments' (the outer and the inner trail them)
        void AddComposableFactory(TypeDefinition owner, string interfaceName, (TypeSignature Type, string Name)[] arguments, uint version)
        {
            TypeDefinition factory = AddExclusiveInterface(interfaceName, owner);

            MethodDefinition method = new("CreateInstance",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(owner.ToTypeSignature(), [.. arguments.Select(static argument => argument.Type), objectType, objectType.MakeByReferenceType()]));

            for (int p = 0; p < arguments.Length; p++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(p + 1), arguments[p].Name, ParameterAttributes.In));
            }

            method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(arguments.Length + 1), "baseInterface", ParameterAttributes.In));
            method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(arguments.Length + 2), "innerInterface", ParameterAttributes.Out));
            factory.Methods.Add(method);

            // 'Windows.Foundation.Metadata.CompositionType': 2 is public
            owner.CustomAttributes.Add(Attribute("ComposableAttribute",
                new CustomAttributeArgument(systemType, factory.ToTypeSignature()),
                new CustomAttributeArgument(module.CorLibTypeFactory.Int32, 2),
                new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, version)));
        }

        // Mirrors 'DataTemplate': a parameterless constructor, plus one taking an argument added in a later version (MIDL
        // encodes contract version 'N' as 'N << 16')
        TypeDefinition widget = AddClass(ClassName, objectType.Type);

        AddComposableFactory(widget, $"I{ClassName}Factory", [], version: 0x10000);
        AddComposableFactory(widget, $"I{ClassName}Factory2", [(module.CorLibTypeFactory.Int32, "count")], version: 0x20000);

        TypeDefinition derived = AddClass(DerivedClassName, widget);

        AddComposableFactory(derived, $"I{DerivedClassName}Factory", [], version: 0x10000);

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }
}
