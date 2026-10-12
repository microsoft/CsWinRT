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
/// Creates synthetic metadata with runtime class hierarchies covering how the implementable bases of derived
/// classes relate to the bases of their base classes, referencing Windows Foundation interfaces where needed.
/// </summary>
internal static class ImplementableHierarchyMetadata
{
    /// <summary>An unsealed class implementing <c>IClosable</c>.</summary>
    public const string ClosableBase = "ClosableBase";

    /// <summary>A class deriving from <see cref="ClosableBase"/> and implementing <c>IIterator&lt;int&gt;</c>, whose .NET form also declares <c>Dispose()</c>.</summary>
    public const string IteratorDerived = "IteratorDerived";

    /// <summary>An unsealed class implementing <c>IIterable&lt;int&gt;</c>.</summary>
    public const string IntIterableBase = "IntIterableBase";

    /// <summary>A class deriving from <see cref="IntIterableBase"/> and implementing <c>IIterable&lt;int&gt;</c> again.</summary>
    public const string SameIterableDerived = "SameIterableDerived";

    /// <summary>A class deriving from <see cref="IntIterableBase"/> and implementing <c>IIterable&lt;string&gt;</c>.</summary>
    public const string OtherIterableDerived = "OtherIterableDerived";

    /// <summary>An unsealed class whose composable constructor has a parameter named <c>instance</c>.</summary>
    public const string InstanceParameter = "InstanceParameterWidget";

    /// <summary>An unsealed class that is deprecated with <c>DeprecationType.Remove</c>.</summary>
    public const string Removed = "RemovedWidget";

    /// <summary>A class deriving from <see cref="ClosableBase"/> and adding an ordinary interface that also declares <c>void Dispose()</c>.</summary>
    public const string DisposeDerived = "DisposeDerived";

    /// <summary>
    /// An unsealed class with two <c>[Protected]</c> interfaces both declaring <c>void Hook()</c>, one of which also declares a removed
    /// <c>void Gone()</c>, and a <c>[Protected]</c> event and property.
    /// </summary>
    public const string ProtectedBase = "ProtectedBase";

    /// <summary>A class deriving from <see cref="ProtectedBase"/> and adding a public interface declaring <c>void Hook()</c>.</summary>
    public const string PublicHookDerived = "PublicHookDerived";

    /// <summary>An unsealed class implementing <c>IVector&lt;int&gt;</c>, whose .NET form declares <c>int Count { get; }</c>.</summary>
    public const string VectorBase = "VectorBase";

    /// <summary>A class deriving from <see cref="VectorBase"/> and adding an interface declaring <c>int Count { get; set; }</c>.</summary>
    public const string SettableCountDerived = "SettableCountDerived";

    /// <summary>A sealed class whose <c>[Activatable]</c> factory interface is removed, alongside default activation.</summary>
    public const string RemovedFactory = "RemovedFactoryWidget";

    /// <summary>A sealed class with a constructor <c>Create(int)</c> and a static <c>Create(int)</c>.</summary>
    public const string CreateCollision = "CreateCollisionWidget";

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

        TypeReference closable = new(module, foundation, "Windows.Foundation", "IClosable");
        TypeReference iterable = new(module, foundation, "Windows.Foundation.Collections", "IIterable`1");
        TypeReference iterator = new(module, foundation, "Windows.Foundation.Collections", "IIterator`1");

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

        int seed = 0x2a7c1e40;

        TypeDefinition AddExclusiveInterface(string name, TypeDefinition owner)
        {
            TypeDefinition iface = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(iface);
            AddGuid(iface, new Guid(seed++, 0x3b12, 0x4c9d, 0x8e, 0x21, 0x6a, 0x54, 0xf0, 0xc3, 0x7d, 0x02));
            iface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

            return iface;
        }

        // An unsealed class with a default interface declaring a single method, and a composable factory whose
        // only constructor takes the given parameters (followed by the outer and the inner)
        TypeDefinition AddClass(string name, ITypeDefOrRef baseType, params (string Name, TypeSignature Type)[] constructorParameters)
        {
            TypeDefinition owner = new("Contoso", name, TypeAttributes.Public | TypeAttributes.WindowsRuntime, baseType);

            module.TopLevelTypes.Add(owner);

            TypeDefinition defaultInterface = AddExclusiveInterface($"I{name}", owner);

            defaultInterface.Methods.Add(new MethodDefinition($"{name}Method",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)));

            InterfaceImplementation implementation = new(defaultInterface);
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            owner.Interfaces.Add(implementation);

            TypeDefinition factory = AddExclusiveInterface($"I{name}Factory", owner);
            MethodDefinition method = new("CreateInstance",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(owner.ToTypeSignature(), [.. constructorParameters.Select(static parameter => parameter.Type), objectType, objectType.MakeByReferenceType()]));

            for (int p = 0; p < constructorParameters.Length; p++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(p + 1), constructorParameters[p].Name, ParameterAttributes.In));
            }

            method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(constructorParameters.Length + 1), "baseInterface", ParameterAttributes.In));
            method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(constructorParameters.Length + 2), "innerInterface", ParameterAttributes.Out));
            factory.Methods.Add(method);

            // 'Windows.Foundation.Metadata.CompositionType.Public' is 2
            owner.CustomAttributes.Add(Attribute("ComposableAttribute",
                new CustomAttributeArgument(systemType, factory.ToTypeSignature()),
                new CustomAttributeArgument(module.CorLibTypeFactory.Int32, 2),
                new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 1u)));

            return owner;
        }

        ITypeDefOrRef Instantiate(TypeReference genericType, TypeSignature argument)
        {
            return new TypeSpecification(new GenericInstanceTypeSignature(genericType, false, [argument]));
        }

        MethodAttributes abstractMethod = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig;

        MethodDefinition AddMethod(TypeDefinition iface, string name, TypeSignature returnType, params TypeSignature[] parameters)
        {
            MethodDefinition method = new(name, abstractMethod, MethodSignature.CreateInstance(returnType, parameters));

            for (int p = 0; p < parameters.Length; p++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(p + 1), $"value{p}", ParameterAttributes.In));
            }

            iface.Methods.Add(method);

            return method;
        }

        void AddProperty(TypeDefinition iface, string name, TypeSignature type, bool hasSetter)
        {
            PropertyDefinition property = new(name, 0, PropertySignature.CreateInstance(type));
            MethodDefinition getter = new($"get_{name}", abstractMethod | MethodAttributes.SpecialName, MethodSignature.CreateInstance(type));

            iface.Methods.Add(getter);
            property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));

            if (hasSetter)
            {
                MethodDefinition setter = new($"put_{name}", abstractMethod | MethodAttributes.SpecialName, MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [type]));

                setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));
                iface.Methods.Add(setter);
                property.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
            }

            iface.Properties.Add(property);
        }

        void AddEvent(TypeDefinition iface, string name)
        {
            TypeReference handlerType = new(module, foundation, "Windows.Foundation", "EventHandler`1");
            TypeSignature handler = new GenericInstanceTypeSignature(handlerType, false, [objectType]);
            TypeSignature token = new TypeReference(module, foundation, "Windows.Foundation", "EventRegistrationToken").ToTypeSignature(true);
            EventDefinition evt = new(name, 0, new TypeSpecification(handler));
            MethodDefinition add = new($"add_{name}", abstractMethod | MethodAttributes.SpecialName, MethodSignature.CreateInstance(token, [handler]));
            MethodDefinition remove = new($"remove_{name}", abstractMethod | MethodAttributes.SpecialName, MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [token]));

            add.ParameterDefinitions.Add(new ParameterDefinition(1, "handler", ParameterAttributes.In));
            remove.ParameterDefinitions.Add(new ParameterDefinition(1, "token", ParameterAttributes.In));
            iface.Methods.Add(add);
            iface.Methods.Add(remove);
            evt.Semantics.Add(new MethodSemantics(add, MethodSemanticsAttributes.AddOn));
            evt.Semantics.Add(new MethodSemantics(remove, MethodSemanticsAttributes.RemoveOn));
            iface.Events.Add(evt);
        }

        // A sealed class with a default interface declaring a single method
        TypeDefinition AddSealedClass(string name)
        {
            TypeDefinition owner = new("Contoso", name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime, objectType.Type);

            module.TopLevelTypes.Add(owner);

            TypeDefinition defaultInterface = AddExclusiveInterface($"I{name}", owner);

            _ = AddMethod(defaultInterface, $"{name}Method", module.CorLibTypeFactory.Void);

            InterfaceImplementation implementation = new(defaultInterface);
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            owner.Interfaces.Add(implementation);

            return owner;
        }

        TypeSignature deprecationType = new TypeReference(module, foundation, "Windows.Foundation.Metadata", "DeprecationType").ToTypeSignature(true);

        CustomAttribute Removal()
        {
            return Attribute("DeprecatedAttribute",
                new CustomAttributeArgument(module.CorLibTypeFactory.String, "Removed."),
                new CustomAttributeArgument(deprecationType, 1),
                new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, 1u));
        }

        CustomAttributeArgument version = new(module.CorLibTypeFactory.UInt32, 1u);

        TypeDefinition closableBase = AddClass(ClosableBase, objectType.Type);
        closableBase.Interfaces.Add(new InterfaceImplementation(closable));

        TypeDefinition iteratorDerived = AddClass(IteratorDerived, closableBase);
        iteratorDerived.Interfaces.Add(new InterfaceImplementation(Instantiate(iterator, module.CorLibTypeFactory.Int32)));

        TypeDefinition intIterableBase = AddClass(IntIterableBase, objectType.Type);
        intIterableBase.Interfaces.Add(new InterfaceImplementation(Instantiate(iterable, module.CorLibTypeFactory.Int32)));

        TypeDefinition sameIterableDerived = AddClass(SameIterableDerived, intIterableBase);
        sameIterableDerived.Interfaces.Add(new InterfaceImplementation(Instantiate(iterable, module.CorLibTypeFactory.Int32)));

        TypeDefinition otherIterableDerived = AddClass(OtherIterableDerived, intIterableBase);
        otherIterableDerived.Interfaces.Add(new InterfaceImplementation(Instantiate(iterable, module.CorLibTypeFactory.String)));

        _ = AddClass(InstanceParameter, objectType.Type, ("instance", module.CorLibTypeFactory.Int32));

        // 'Windows.Foundation.Metadata.DeprecationType.Remove' is 1
        _ = AddClass(Removed, objectType.Type);

        // Its exclusive interfaces are removed along with it, as they are in real metadata
        foreach (TypeDefinition type in module.TopLevelTypes.Where(static type => type.Name?.Value.Contains(Removed) == true).ToArray())
        {
            type.CustomAttributes.Add(Removal());
        }

        TypeDefinition disposeDerived = AddClass(DisposeDerived, closableBase);
        TypeDefinition disposer = AddExclusiveInterface("IDisposer", disposeDerived);

        _ = AddMethod(disposer, "Dispose", module.CorLibTypeFactory.Void);
        disposeDerived.Interfaces.Add(new InterfaceImplementation(disposer));

        TypeDefinition protectedBase = AddClass(ProtectedBase, objectType.Type);

        foreach (string hooksName in (string[])["IProtectedHooks", "IProtectedHooks2"])
        {
            TypeDefinition hooks = AddExclusiveInterface(hooksName, protectedBase);
            InterfaceImplementation implementation = new(hooks);

            _ = AddMethod(hooks, "Hook", module.CorLibTypeFactory.Void);
            implementation.CustomAttributes.Add(Attribute("ProtectedAttribute"));
            protectedBase.Interfaces.Add(implementation);

            if (hooksName == "IProtectedHooks")
            {
                AddMethod(hooks, "Gone", module.CorLibTypeFactory.Void).CustomAttributes.Add(Removal());
                AddProperty(hooks, "Level", module.CorLibTypeFactory.Int32, hasSetter: true);
                AddEvent(hooks, "Changed");
            }
            else
            {
                AddProperty(hooks, "Level", module.CorLibTypeFactory.Int32, hasSetter: false);
                AddEvent(hooks, "Changed");
            }
        }

        TypeDefinition publicHookDerived = AddClass(PublicHookDerived, protectedBase);
        TypeDefinition publicHooks = AddExclusiveInterface("IPublicHooks", publicHookDerived);

        _ = AddMethod(publicHooks, "Hook", module.CorLibTypeFactory.Void);
        AddProperty(publicHooks, "Level", module.CorLibTypeFactory.Int32, hasSetter: false);
        AddEvent(publicHooks, "Changed");
        publicHookDerived.Interfaces.Add(new InterfaceImplementation(publicHooks));

        TypeDefinition vectorBase = AddClass(VectorBase, objectType.Type);
        vectorBase.Interfaces.Add(new InterfaceImplementation(Instantiate(new TypeReference(module, foundation, "Windows.Foundation.Collections", "IVector`1"), module.CorLibTypeFactory.Int32)));
        vectorBase.Interfaces.Add(new InterfaceImplementation(Instantiate(iterable, module.CorLibTypeFactory.Int32)));

        TypeDefinition settableCountDerived = AddClass(SettableCountDerived, vectorBase);
        TypeDefinition settableCount = AddExclusiveInterface("ISettableCount", settableCountDerived);

        AddProperty(settableCount, "Count", module.CorLibTypeFactory.Int32, hasSetter: true);
        settableCountDerived.Interfaces.Add(new InterfaceImplementation(settableCount));

        TypeDefinition removedFactory = AddSealedClass(RemovedFactory);
        TypeDefinition removedFactoryInterface = AddExclusiveInterface($"I{RemovedFactory}Factory", removedFactory);

        _ = AddMethod(removedFactoryInterface, "CreateWithValue", removedFactory.ToTypeSignature(), module.CorLibTypeFactory.Int32);
        removedFactoryInterface.CustomAttributes.Add(Removal());
        removedFactory.CustomAttributes.Add(Attribute("ActivatableAttribute", version));
        removedFactory.CustomAttributes.Add(Attribute("ActivatableAttribute", new CustomAttributeArgument(systemType, removedFactoryInterface.ToTypeSignature()), version));

        TypeDefinition createCollision = AddSealedClass(CreateCollision);
        TypeDefinition createFactory = AddExclusiveInterface($"I{CreateCollision}Factory", createCollision);
        TypeDefinition createStatics = AddExclusiveInterface($"I{CreateCollision}Statics", createCollision);

        _ = AddMethod(createFactory, "Create", createCollision.ToTypeSignature(), module.CorLibTypeFactory.Int32);
        _ = AddMethod(createStatics, "Create", createCollision.ToTypeSignature(), module.CorLibTypeFactory.Int32);
        createCollision.CustomAttributes.Add(Attribute("ActivatableAttribute", new CustomAttributeArgument(systemType, createFactory.ToTypeSignature()), version));
        createCollision.CustomAttributes.Add(Attribute("StaticAttribute", new CustomAttributeArgument(systemType, createStatics.ToTypeSignature()), version));

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }

    /// <summary>
    /// Finds the installed Windows Foundation contract, which declares the interfaces the classes implement.
    /// </summary>
    public static string FindWindowsFoundationWinmd()
    {
        string referencesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Windows Kits",
            "10",
            "References");

        string? winmdPath = Directory
            .EnumerateFiles(referencesDirectory, "Windows.Foundation.FoundationContract.winmd", SearchOption.AllDirectories)
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return winmdPath ?? throw new FileNotFoundException($"No Windows Foundation contract was found under '{referencesDirectory}'.");
    }
}
