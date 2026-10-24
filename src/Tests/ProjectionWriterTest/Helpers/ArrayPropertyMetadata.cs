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

        TypeDefinition hresult = new("Windows.Foundation", "HResult",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        hresult.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        module.TopLevelTypes.Add(hresult);
        payload.Fields.Insert(0, new FieldDefinition("Error", FieldAttributes.Public, hresult.ToTypeSignature(true)));

        TypeDefinition dateTime = new("Windows.Foundation", "DateTime",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        dateTime.Fields.Add(new FieldDefinition("UniversalTime", FieldAttributes.Public, module.CorLibTypeFactory.Int64));
        module.TopLevelTypes.Add(dateTime);
        payload.Fields.Add(new FieldDefinition("Timestamp", FieldAttributes.Public, dateTime.ToTypeSignature(true)));

        TypeDefinition stamp = new("Contoso", "Stamp",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        stamp.Fields.Add(new FieldDefinition("When", FieldAttributes.Public, dateTime.ToTypeSignature(true)));
        stamp.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        module.TopLevelTypes.Add(stamp);
        TypeDefinition nestedPayload = new("Contoso", "NestedPayload",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            valueType);
        nestedPayload.Fields.Add(new FieldDefinition("Inner", FieldAttributes.Public, payload.ToTypeSignature(true)));
        nestedPayload.Fields.Add(new FieldDefinition("Text", FieldAttributes.Public, module.CorLibTypeFactory.String));
        module.TopLevelTypes.Add(nestedPayload);

        TypeDefinition reference = new("Windows.Foundation", "IReference`1",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);
        reference.GenericParameters.Add(new GenericParameter("T"));
        module.TopLevelTypes.Add(reference);
        TypeDefinition pair = new("Windows.Foundation.Collections", "IKeyValuePair`2",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);
        pair.GenericParameters.Add(new GenericParameter("K"));
        pair.GenericParameters.Add(new GenericParameter("V"));
        module.TopLevelTypes.Add(pair);
        TypeDefinition typeKind = new("Windows.UI.Xaml.Interop", "TypeKind",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "Enum"));
        typeKind.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.Int32));
        module.TopLevelTypes.Add(typeKind);
        TypeDefinition typeName = new("Windows.UI.Xaml.Interop", "TypeName",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime, valueType);
        typeName.Fields.Add(new FieldDefinition("Name", FieldAttributes.Public, module.CorLibTypeFactory.String));
        typeName.Fields.Add(new FieldDefinition("Kind", FieldAttributes.Public, typeKind.ToTypeSignature(true)));
        module.TopLevelTypes.Add(typeName);

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
        _ = AddMethod("AcceptObjects", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.Object));
        _ = AddMethod("AcceptWidgets", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(owner.ToTypeSignature()));
        _ = AddMethod("AcceptInterfaces", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(iface.ToTypeSignature()));
        _ = AddMethod("AcceptPayloads", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(payload.ToTypeSignature(true)));
        _ = AddMethod("AcceptDates", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(dateTime.ToTypeSignature(true)));
        MethodDefinition fillNames = AddMethod("FillNames", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.String));
        fillNames.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;
        MethodDefinition fillObjects = AddMethod("FillObjects", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.Object));
        fillObjects.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;
        MethodDefinition fill = AddMethod("FillValues", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(module.CorLibTypeFactory.Int32));
        fill.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;

        foreach ((string name, TypeSignature element) in new (string, TypeSignature)[]
        {
            ("Nullable", new GenericInstanceTypeSignature(reference, false, [module.CorLibTypeFactory.Int32])),
            ("Pairs", new GenericInstanceTypeSignature(pair, false, [module.CorLibTypeFactory.String, module.CorLibTypeFactory.String])),
            ("Types", typeName.ToTypeSignature(true))
        })
        {
            SzArrayTypeSignature array = new(element);
            _ = AddMethod($"Accept{name}", module.CorLibTypeFactory.Void, array);
            MethodDefinition fillArray = AddMethod($"Fill{name}", module.CorLibTypeFactory.Void, array);
            fillArray.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;
            _ = AddMethod($"Get{name}", array);
            MethodDefinition receiveArray = AddMethod($"Receive{name}", module.CorLibTypeFactory.Void, new ByReferenceTypeSignature(array));
            receiveArray.ParameterDefinitions[0].Attributes = ParameterAttributes.Out;
        }

        TypeSignature typeSignature = typeName.ToTypeSignature(true);
        MethodDefinition outputs = AddMethod("GetOutputs", typeSignature,
            module.CorLibTypeFactory.Object,
            new ByReferenceTypeSignature(module.CorLibTypeFactory.String),
            new ByReferenceTypeSignature(iface.ToTypeSignature()),
            new ByReferenceTypeSignature(module.CorLibTypeFactory.Object),
            new ByReferenceTypeSignature(nestedPayload.ToTypeSignature(true)),
            new ByReferenceTypeSignature(typeSignature),
            new ByReferenceTypeSignature(new GenericInstanceTypeSignature(reference, false, [module.CorLibTypeFactory.Int32])),
            new ByReferenceTypeSignature(new GenericInstanceTypeSignature(pair, false, [module.CorLibTypeFactory.String, module.CorLibTypeFactory.String])),
            new ByReferenceTypeSignature(new SzArrayTypeSignature(module.CorLibTypeFactory.Int32)),
            new ByReferenceTypeSignature(new SzArrayTypeSignature(typeSignature)));
        string[] outputNames = ["input", "text", "iface", "item", "payload", "type", "optional", "pair", "numbers", "types"];
        for (int i = 0; i < outputs.ParameterDefinitions.Count; i++)
        {
            outputs.ParameterDefinitions[i].Name = outputNames[i];
            outputs.ParameterDefinitions[i].Attributes = i == 0 ? ParameterAttributes.In : ParameterAttributes.Out;
        }

        MethodDefinition fillBoth = AddMethod("FillBoth", typeSignature,
            new SzArrayTypeSignature(stamp.ToTypeSignature(true)),
            new SzArrayTypeSignature(module.CorLibTypeFactory.Object), new SzArrayTypeSignature(typeSignature));
        fillBoth.ParameterDefinitions[0].Name = "stamps";
        fillBoth.ParameterDefinitions[1].Name = "first";
        fillBoth.ParameterDefinitions[2].Name = "second";
        foreach (ParameterDefinition parameter in fillBoth.ParameterDefinitions)
        {
            parameter.Attributes = ParameterAttributes.Out;
        }

        _ = AddMethod("AcceptStamps", module.CorLibTypeFactory.Void, new SzArrayTypeSignature(stamp.ToTypeSignature(true)));

        MethodDefinition finalOutputs = AddMethod("GetFinalOutputs", module.CorLibTypeFactory.Int32, [.. outputs.Signature!.ParameterTypes]);
        for (int i = 0; i < finalOutputs.ParameterDefinitions.Count; i++)
        {
            finalOutputs.ParameterDefinitions[i].Name = outputNames[i];
            finalOutputs.ParameterDefinitions[i].Attributes = i == 0 ? ParameterAttributes.In : ParameterAttributes.Out;
        }

        module.Write(path);
        return path;
    }
}
