// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class ArrayPropertyMetadata
{
    public static string Create(string directory, bool exclusiveTo, bool includeArrayProperties = true)
    {
        string path = ExclusiveToMetadata.Create(directory, fastAbi: false);
        ModuleDefinition module = ModuleDefinition.FromFile(path);
        TypeDefinition iface = module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget2");
        TypeDefinition owner = module.TopLevelTypes.Single(type => type.FullName == "Contoso.Widget");

        // The interop generator also needs a nontrivial projected type hierarchy.
        TypeDefinition derivedInterface = module.TopLevelTypes.Single(type => type.FullName == "Contoso.IWidget3");
        TypeDefinition derived = new("Contoso", "DerivedWidget",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime, owner);
        owner.IsSealed = false;
        owner.Interfaces.Remove(owner.Interfaces.Single(implementation => implementation.Interface == derivedInterface));
        CustomAttribute defaultAttribute = owner.Interfaces[0].CustomAttributes.Single(
            attribute => attribute.Constructor?.DeclaringType?.Name == "DefaultAttribute");
        derived.Interfaces.Add(new InterfaceImplementation(derivedInterface)
        {
            CustomAttributes = { new CustomAttribute(defaultAttribute.Constructor!, new CustomAttributeSignature()) }
        });
        CustomAttribute exclusiveAttribute = derivedInterface.CustomAttributes.Single(
            attribute => attribute.Constructor?.DeclaringType?.Name == "ExclusiveToAttribute");
        exclusiveAttribute.Signature!.FixedArguments[0] = new CustomAttributeArgument(
            exclusiveAttribute.Signature.FixedArguments[0].ArgumentType, derived.ToTypeSignature());
        module.TopLevelTypes.Add(derived);

        if (!includeArrayProperties)
        {
            owner.Interfaces.Remove(owner.Interfaces.Single(implementation => implementation.Interface == iface));
            module.TopLevelTypes.Remove(iface);
            module.Write(path);
            return path;
        }

        iface.Events.Clear();
        iface.Methods.Clear();

        if (!exclusiveTo)
        {
            iface.CustomAttributes.Remove(iface.CustomAttributes.Single(
                attribute => attribute.Constructor?.DeclaringType?.Name == "ExclusiveToAttribute"));
        }

        IResolutionScope corlib = module.CorLibTypeFactory.CorLibScope;
        TypeReference valueType = new(module, corlib, "System", "ValueType");
        TypeDefinition payload = new("Contoso", "Payload",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        payload.Fields.Add(new FieldDefinition("Id", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        payload.Fields.Add(new FieldDefinition("Text", FieldAttributes.Public, module.CorLibTypeFactory.String));
        module.TopLevelTypes.Add(payload);

        TypeDefinition dateTime = new("Windows.Foundation", "DateTime",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        dateTime.Fields.Add(new FieldDefinition("UniversalTime", FieldAttributes.Public, module.CorLibTypeFactory.Int64));
        module.TopLevelTypes.Add(dateTime);
        payload.Fields.Add(new FieldDefinition("Timestamp", FieldAttributes.Public, dateTime.ToTypeSignature(true)));

        TypeDefinition tag = new("Contoso", "Tag",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "Enum"));
        tag.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.Int32));
        tag.Fields.Add(new FieldDefinition("First", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            tag.ToTypeSignature(true)) { Constant = Constant.FromValue(1) });
        module.TopLevelTypes.Add(tag);

        MethodDefinition AddMethod(string name, TypeSignature returnType, params TypeSignature[] parameters)
        {
            MethodDefinition method = new(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(returnType, parameters));
            iface.Methods.Add(method);

            for (int i = 0; i < parameters.Length; i++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(i + 1), "value", ParameterAttributes.In));
            }

            return method;
        }

        void AddProperty(string name, TypeSignature type, string parameterName = "value")
        {
            MethodDefinition getter = AddMethod($"get_{name}", type);
            MethodDefinition setter = AddMethod($"put_{name}", module.CorLibTypeFactory.Void, type);
            getter.IsSpecialName = true;
            setter.IsSpecialName = true;
            setter.ParameterDefinitions[0].Name = parameterName;
            PropertyDefinition property = new(name, PropertyAttributes.None, PropertySignature.CreateInstance(type));
            property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
            property.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
            iface.Properties.Add(property);
        }

        AddProperty("Values", new SzArrayTypeSignature(module.CorLibTypeFactory.Int32));
        AddProperty("Flags", new SzArrayTypeSignature(module.CorLibTypeFactory.Boolean));
        AddProperty("Names", new SzArrayTypeSignature(module.CorLibTypeFactory.String));
        AddProperty("Objects", new SzArrayTypeSignature(module.CorLibTypeFactory.Object));
        AddProperty("Widgets", new SzArrayTypeSignature(owner.ToTypeSignature()));
        AddProperty("Interfaces", new SzArrayTypeSignature(iface.ToTypeSignature()));
        AddProperty("Payloads", new SzArrayTypeSignature(payload.ToTypeSignature(true)));
        AddProperty("Tags", new SzArrayTypeSignature(tag.ToTypeSignature(true)), parameterName: "event");
        AddProperty("Label", module.CorLibTypeFactory.String);

        _ = AddMethod("AcceptValues", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.Int32));
        _ = AddMethod("AcceptNames", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.String));
        MethodDefinition fill = AddMethod("FillValues", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.Int32));
        fill.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;

        module.Write(path);
        return path;
    }
}
