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
/// Creates synthetic metadata with runtime classes covering each shape of activation MIDL emits.
/// </summary>
/// <remarks>
/// MIDL routes every constructor of an unsealed class through a <c>[Composable]</c> factory interface, whose
/// methods take the real arguments followed by the outer and the inner, and gives a class with no constructor an
/// empty one. A protected constructor is marked by the composition type. A sealed class is instead marked
/// <c>[Activatable]</c>, without a factory interface for its parameterless constructor, and with one for the rest.
/// </remarks>
internal static class ActivationMetadata
{
    /// <summary>An unsealed class with no constructor.</summary>
    public const string NoConstructor = "NoConstructorWidget";

    /// <summary>An unsealed class with only a public parameterless constructor.</summary>
    public const string ParameterlessConstructor = "ParameterlessWidget";

    /// <summary>An unsealed class with only a constructor taking an argument.</summary>
    public const string ParameterizedConstructor = "ParameterizedWidget";

    /// <summary>An unsealed class with only a protected parameterless constructor.</summary>
    public const string ProtectedConstructor = "ProtectedWidget";

    /// <summary>A sealed class with only a parameterless constructor.</summary>
    public const string SealedDefault = "SealedWidget";

    /// <summary>A sealed class with a parameterless constructor, one taking an argument, and a static returning the class.</summary>
    public const string SealedWithFactory = "SealedFactoryWidget";

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

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(static argument => argument.ArgumentType).ToArray()));

            return new CustomAttribute(constructor, new CustomAttributeSignature(arguments));
        }

        void AddGuid(TypeDefinition type, Guid value)
        {
            byte[] bytes = value.ToByteArray();

            type.CustomAttributes.Add(Attribute("GuidAttribute",
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
        }

        TypeDefinition AddExclusiveInterface(string name, TypeDefinition owner, int seed)
        {
            TypeDefinition iface = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(iface);
            AddGuid(iface, new Guid(seed, 0x6d21, 0x4f2e, 0x9a, 0x33, 0x5c, 0x71, 0xc0, 0xd8, 0x4e, 0x01));
            iface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

            return iface;
        }

        int seed = 0x1f9b3c50;

        TypeDefinition AddOwner(string name, TypeAttributes sealing)
        {
            TypeDefinition owner = new("Contoso", name, TypeAttributes.Public | TypeAttributes.WindowsRuntime | sealing, objectType.Type);

            module.TopLevelTypes.Add(owner);

            // Every runtime class needs a default interface for the instance base to implement
            TypeDefinition defaultInterface = AddExclusiveInterface($"I{name}", owner, seed++);

            defaultInterface.Methods.Add(new MethodDefinition("DoStuff",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)));

            InterfaceImplementation implementation = new(defaultInterface);
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            owner.Interfaces.Add(implementation);

            return owner;
        }

        MethodDefinition AddMethod(TypeDefinition iface, string name, TypeSignature returnType, params TypeSignature[] arguments)
        {
            MethodDefinition method = new(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(returnType, arguments));

            for (int p = 0; p < arguments.Length; p++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(p + 1), $"value{p}", ParameterAttributes.In));
            }

            iface.Methods.Add(method);

            return method;
        }

        // 'constructors' holds the real arguments of each constructor (none at all leaves the factory empty)
        void AddClass(string name, TypeSignature[][] constructors, bool isPublic)
        {
            TypeDefinition owner = AddOwner(name, default);
            TypeDefinition factory = AddExclusiveInterface($"I{name}Factory", owner, seed++);

            for (int i = 0; i < constructors.Length; i++)
            {
                TypeSignature[] arguments = constructors[i];

                // The outer and the inner always trail the real arguments
                MethodDefinition method = new(i == 0 ? "CreateInstance" : $"CreateInstance{i + 1}",
                    MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                    MethodSignature.CreateInstance(owner.ToTypeSignature(), [.. arguments, objectType, objectType.MakeByReferenceType()]));

                for (int p = 0; p < arguments.Length; p++)
                {
                    method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(p + 1), $"value{p}", ParameterAttributes.In));
                }

                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(arguments.Length + 1), "baseInterface", ParameterAttributes.In));
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(arguments.Length + 2), "innerInterface", ParameterAttributes.Out));
                factory.Methods.Add(method);
            }

            // 'Windows.Foundation.Metadata.CompositionType': 1 is protected, 2 is public
            owner.CustomAttributes.Add(Attribute("ComposableAttribute",
                new CustomAttributeArgument(systemType, factory.ToTypeSignature()),
                new CustomAttributeArgument(module.CorLibTypeFactory.Int32, isPublic ? 2 : 1),
                new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 1u)));
        }

        AddClass(NoConstructor, [], isPublic: true);
        AddClass(ParameterlessConstructor, [[]], isPublic: true);
        AddClass(ParameterizedConstructor, [[module.CorLibTypeFactory.Int32]], isPublic: true);
        AddClass(ProtectedConstructor, [[]], isPublic: false);

        CustomAttributeArgument version = new(module.CorLibTypeFactory.UInt32, 1u);

        TypeDefinition sealedDefault = AddOwner(SealedDefault, TypeAttributes.Sealed);

        sealedDefault.CustomAttributes.Add(Attribute("ActivatableAttribute", version));

        TypeDefinition sealedWithFactory = AddOwner(SealedWithFactory, TypeAttributes.Sealed);
        TypeDefinition sealedFactory = AddExclusiveInterface($"I{SealedWithFactory}Factory", sealedWithFactory, seed++);
        TypeDefinition sealedStatics = AddExclusiveInterface($"I{SealedWithFactory}Statics", sealedWithFactory, seed++);

        _ = AddMethod(sealedFactory, "CreateWithValue", sealedWithFactory.ToTypeSignature(), module.CorLibTypeFactory.Int32);
        _ = AddMethod(sealedStatics, "GetDefault", sealedWithFactory.ToTypeSignature());

        sealedWithFactory.CustomAttributes.Add(Attribute("ActivatableAttribute", version));
        sealedWithFactory.CustomAttributes.Add(Attribute("ActivatableAttribute", new CustomAttributeArgument(systemType, sealedFactory.ToTypeSignature()), version));
        sealedWithFactory.CustomAttributes.Add(Attribute("StaticAttribute", new CustomAttributeArgument(systemType, sealedStatics.ToTypeSignature()), version));

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }
}
