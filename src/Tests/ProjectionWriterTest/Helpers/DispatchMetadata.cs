// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class DispatchMetadata
{
    public static void AddElementEnum(string path)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(path);
        TypeDefinition type = new("Contoso", "DispatchElement",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            new TypeReference(module, module.CorLibTypeFactory.CorLibScope, "System", "Enum"));
        type.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.Int32));
        type.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            type.ToTypeSignature(true)) { Constant = Constant.FromValue(1) });
        module.TopLevelTypes.Add(type);
        module.Write(path);
    }
}
