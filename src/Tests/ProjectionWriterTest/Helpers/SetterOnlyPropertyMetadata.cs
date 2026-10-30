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
/// Creates synthetic metadata with a setter-only property on a versioned <c>[exclusiveto]</c>
/// statics interface, mirroring how MIDL adds a setter to a property whose getter shipped earlier.
/// </summary>
internal static class SetterOnlyPropertyMetadata
{
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

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(static argument => argument.ArgumentType).ToArray()));

            return new CustomAttribute(constructor, new CustomAttributeSignature(arguments));
        }

        void AddGuid(TypeDefinition type, string value)
        {
            byte[] bytes = new Guid(value).ToByteArray();

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

        TypeDefinition owner = new("Contoso", "Widget",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            module.CorLibTypeFactory.Object.Type);

        module.TopLevelTypes.Add(owner);

        TypeDefinition MakeInterface(string name, string guid)
        {
            TypeDefinition iface = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(iface);
            AddGuid(iface, guid);
            iface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

            return iface;
        }

        MethodDefinition MakeGetter(string name, TypeSignature type) => new($"get_{name}",
            MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
            MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            MethodSignature.CreateInstance(type));

        MethodDefinition MakeSetter(string name, TypeSignature type) => new($"put_{name}",
            MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
            MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [type]));

        TypeSignature propertyType = module.CorLibTypeFactory.Int32;

        // v1 declares the getter, exactly as the original contract version shipped it.
        TypeDefinition statics = MakeInterface("IWidgetStatics", "2a7c1f60-8e41-4b12-9d55-71c0d84e0b21");

        MethodDefinition getter = MakeGetter("Current", propertyType);
        statics.Methods.Add(getter);

        PropertyDefinition getterProperty = new("Current", 0, PropertySignature.CreateInstance(propertyType));
        getterProperty.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
        statics.Properties.Add(getterProperty);

        // v2 adds ONLY the setter, and does not inherit v1: versioned WinRT interfaces are flat.
        TypeDefinition statics2 = MakeInterface("IWidgetStatics2", "3b8d2071-9f52-4c23-8e66-82d1e95f1c32");

        MethodDefinition setter = MakeSetter("Current", propertyType);
        statics2.Methods.Add(setter);

        PropertyDefinition setterProperty = new("Current", 0, PropertySignature.CreateInstance(propertyType));
        setterProperty.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
        statics2.Properties.Add(setterProperty);

        owner.CustomAttributes.Add(Attribute("StaticAttribute",
            new CustomAttributeArgument(systemType, statics.ToTypeSignature()),
            new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 0x01000000u)));

        owner.CustomAttributes.Add(Attribute("StaticAttribute",
            new CustomAttributeArgument(systemType, statics2.ToTypeSignature()),
            new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 0x02000000u)));

        // A setter-only property whose getter exists nowhere: not on a base interface, not on a peer.
        TypeDefinition orphan = MakeInterface("IWidgetStatics3", "4c9e3182-af63-4d34-9f77-93e2fa602d43");

        MethodDefinition orphanSetter = MakeSetter("Detached", propertyType);
        orphan.Methods.Add(orphanSetter);

        PropertyDefinition orphanProperty = new("Detached", 0, PropertySignature.CreateInstance(propertyType));
        orphanProperty.Semantics.Add(new MethodSemantics(orphanSetter, MethodSemanticsAttributes.Setter));
        orphan.Properties.Add(orphanProperty);

        owner.CustomAttributes.Add(Attribute("StaticAttribute",
            new CustomAttributeArgument(systemType, orphan.ToTypeSignature()),
            new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 0x03000000u)));

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }
}
