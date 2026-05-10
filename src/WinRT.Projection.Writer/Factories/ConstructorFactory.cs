// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the constructor surface (RCW base-chaining ctors, factory-driven activatable ctors,
/// composable ctors) for projected runtime classes.
/// </summary>
/// <remarks>
/// The implementation is split across several partial files:
/// <list type="bullet">
///   <item><description><c>ConstructorFactory.AttributedTypes.cs</c> - factory-driven activatable + statics constructors.</description></item>
///   <item><description><c>ConstructorFactory.FactoryCallbacks.cs</c> - per-factory args struct + callback class emission.</description></item>
///   <item><description><c>ConstructorFactory.Composable.cs</c> - composable (derivable) class constructors.</description></item>
/// </list>
/// </remarks>
internal static partial class ConstructorFactory
{
    /// <summary>
    /// Reads the <c>[MarshalingBehaviorAttribute]</c> on the class and returns the corresponding
    /// <c>CreateObjectReferenceMarshalingType.*</c> expression.
    /// </summary>
    internal static string GetMarshalingTypeName(TypeDefinition classType)
    {
        for (int i = 0; i < classType.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = classType.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            if (attrType.Namespace?.Value != WindowsFoundationMetadata ||
                attrType.Name?.Value != "MarshalingBehaviorAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is int v)
                {
                    return v switch
                    {
                        2 => "CreateObjectReferenceMarshalingType.Agile",
                        3 => "CreateObjectReferenceMarshalingType.Standard",
                        _ => "CreateObjectReferenceMarshalingType.Unknown",
                    };
                }
            }
        }
        return "CreateObjectReferenceMarshalingType.Unknown";
    }
}
