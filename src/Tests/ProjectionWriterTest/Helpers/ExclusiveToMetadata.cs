// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class ExclusiveToMetadata
{
    public const string SecondaryInterfaceIid = "326899c0-4412-4e58-9599-99d0a89bca02";

    public static string Create(string directory, bool fastAbi, bool overridable = false)
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
            TypeAttributes.Public | TypeAttributes.WindowsRuntime | (overridable ? 0 : TypeAttributes.Sealed),
            module.CorLibTypeFactory.Object.Type);
        module.TopLevelTypes.Add(owner);

        if (fastAbi)
        {
            owner.CustomAttributes.Add(Attribute("FastAbiAttribute"));
        }

        TypeDefinition handler = new("Contoso", "ChangedHandler",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "MulticastDelegate"));
        module.TopLevelTypes.Add(handler);
        AddGuid(handler, "326899c0-4412-4e58-9599-99d0a89bca04");
        handler.Methods.Add(new MethodDefinition(".ctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.Object, module.CorLibTypeFactory.IntPtr]))
        {
            ImplAttributes = MethodImplAttributes.Runtime
        });
        handler.Methods.Add(new MethodDefinition("Invoke",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.SpecialName,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void))
        {
            ImplAttributes = MethodImplAttributes.Runtime
        });

        TypeDefinition token = new("Windows.Foundation", "EventRegistrationToken",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "ValueType"));
        token.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Int64));
        module.TopLevelTypes.Add(token);

        MethodDefinition AddMethod(TypeDefinition type, string name, TypeSignature returnType, params TypeSignature[] parameters)
        {
            MethodDefinition method = new(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(returnType, parameters));
            type.Methods.Add(method);

            for (int i = 0; i < parameters.Length; i++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(i + 1), $"value{i}", 0));
            }

            return method;
        }

        void AddInterface(string name, string iid, string methodName, bool isDefault, bool hasEvent, bool isOverridable = false)
        {
            TypeDefinition type = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);
            module.TopLevelTypes.Add(type);
            AddGuid(type, iid);
            type.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));
            InterfaceImplementation implementation = new(type);
            owner.Interfaces.Add(implementation);

            if (isDefault)
            {
                implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            }

            if (isOverridable)
            {
                implementation.CustomAttributes.Add(Attribute("OverridableAttribute"));
            }

            _ = AddMethod(type, methodName, module.CorLibTypeFactory.Int32);

            if (hasEvent)
            {
                string eventName = $"{name}Changed";
                MethodDefinition adder = AddMethod(type, $"add_{eventName}", token.ToTypeSignature(true), handler.ToTypeSignature());
                MethodDefinition remover = AddMethod(type, $"remove_{eventName}", module.CorLibTypeFactory.Void, token.ToTypeSignature(true));
                adder.IsSpecialName = true;
                remover.IsSpecialName = true;
                EventDefinition @event = new(eventName, 0, handler);
                @event.Semantics.Add(new MethodSemantics(adder, MethodSemanticsAttributes.AddOn));
                @event.Semantics.Add(new MethodSemantics(remover, MethodSemanticsAttributes.RemoveOn));
                type.Events.Add(@event);
            }
        }

        AddInterface("IWidget", "326899c0-4412-4e58-9599-99d0a89bca01", "GetDefaultValue", isDefault: true, hasEvent: true);
        AddInterface("IWidget2", SecondaryInterfaceIid, "GetValue", isDefault: false, hasEvent: !overridable, isOverridable: overridable);
        AddInterface("IWidget3", "326899c0-4412-4e58-9599-99d0a89bca03", "GetOtherValue", isDefault: false, hasEvent: false);

        string path = Path.Combine(directory, "Contoso.winmd");
        module.Write(path);
        return path;
    }
}
