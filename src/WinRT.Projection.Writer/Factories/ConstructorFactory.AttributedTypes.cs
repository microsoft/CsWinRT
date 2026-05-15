// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class ConstructorFactory
{
    /// <summary>
    /// Emits the activator and composer constructor wrappers for the given runtime class.
    /// </summary>
    public static void WriteAttributedTypes(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition classType)
    {
        if (context.Cache is null)
        {
            return;
        }

        // Track whether we need to emit the static _objRef_<RuntimeClassName> field (used by
        // default constructors). Emit it once per class if any [Activatable] factory exists.
        bool needsClassObjRef = false;

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, context.Cache))
        {
            AttributedType factory = kv.Value;

            if (factory.Activatable && factory.Type is null)
            {
                needsClassObjRef = true;
                break;
            }
        }

        if (needsClassObjRef)
        {
            string fullName = (classType.Namespace?.Value ?? string.Empty) + "." + (classType.Name?.Value ?? string.Empty);
            string objRefName = "_objRef_" + IidExpressionGenerator.EscapeTypeNameForIdentifier(GlobalPrefix + fullName, stripGlobal: true);
            writer.WriteLine();
            writer.Write($"private static WindowsRuntimeObjectReference {objRefName}");

            if (context.Settings.ReferenceProjection)
            {
                // in ref mode the activation factory objref getter body is just 'throw null;'.
                RefModeStubFactory.EmitRefModeObjRefGetterBody(writer);
            }
            else
            {
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    {
                        get
                        {
                            var __{{objRefName}} = field;
                            if (__{{objRefName}} != null && __{{objRefName}}.IsInCurrentContext)
                            {
                                return __{{objRefName}};
                            }
                            return field = WindowsRuntimeObjectReference.GetActivationFactory("{{fullName}}");
                        }
                    }
                    """);
            }
        }

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, context.Cache))
        {
            AttributedType factory = kv.Value;

            if (factory.Activatable)
            {
                WriteFactoryConstructors(writer, context, factory.Type, classType);
            }
            else if (factory.Composable)
            {
                WriteComposableConstructors(writer, context, factory.Type, classType, factory.Visible ? "public" : "protected");
            }
        }
    }

    /// <summary>
    /// Emits the public constructors generated from a [Activatable] factory type.
    /// </summary>
    public static void WriteFactoryConstructors(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition? factoryType, TypeDefinition classType)
    {
        string typeName = classType.Name?.Value ?? string.Empty;
        int gcPressure = ClassFactory.GetGcPressureAmount(classType);

        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, factoryType);
            ClassFactory.WriteStaticFactoryObjRef(writer, context, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
            string marshalingType = GetMarshalingTypeName(classType);
            // Compute the platform attribute string from the activation factory interface's
            // [ContractVersion] attribute
            string platformAttribute = CustomAttributeFactory.WritePlatformAttribute(context, factoryType);
            int methodIndex = 0;
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (method.IsSpecial())
                {
                    methodIndex++; continue;
                }

                MethodSignatureInfo sig = new(method);
                string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Parameters.Count.ToString(CultureInfo.InvariantCulture);
                string argsName = callbackName + "Args";

                // Emit the public constructor.
                writer.WriteLine();

                if (!string.IsNullOrEmpty(platformAttribute))
                {
                    writer.Write(platformAttribute);
                }

                writer.Write($"public unsafe {typeName}(");
                MethodFactory.WriteParameterList(writer, context, sig);
                writer.Write(isMultiline: true, """
                    )
                      :base(
                    """);
                if (sig.Parameters.Count == 0)
                {
                    writer.Write("default");
                }
                else
                {
                    writer.Write($"{callbackName}.Instance, {defaultIfaceIid}, {marshalingType}, WindowsRuntimeActivationArgsReference.CreateUnsafe(new {argsName}(");
                    for (int i = 0; i < sig.Parameters.Count; i++)
                    {
                        if (i > 0)
                        {
                            writer.Write(", ");
                        }

                        string raw = sig.Parameters[i].Parameter.Name ?? "param";
                        writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
                    }
                    writer.Write("))");
                }

                writer.WriteLine(isMultiline: true, """
                    )
                    {
                    """);
                if (gcPressure > 0)
                {
                    writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(CultureInfo.InvariantCulture)});");
                }

                writer.WriteLine("}");

                if (sig.Parameters.Count > 0)
                {
                    EmitFactoryArgsStruct(writer, context, sig, argsName);
                    EmitFactoryCallbackClass(writer, context, sig, callbackName, argsName, factoryObjRefName, methodIndex);
                }

                methodIndex++;
            }
        }
        else
        {
            // No factory type means [Activatable(uint version)] - emit a default ctor that calls
            // the WindowsRuntimeObject base constructor with the activation factory objref.
            // The default interface IID is needed too.
            string fullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string objRefName = "_objRef_" + IidExpressionGenerator.EscapeTypeNameForIdentifier(GlobalPrefix + fullName, stripGlobal: true);

            // Find the default interface IID to use.
            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);

            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                public {{typeName}}()
                  :base(default(WindowsRuntimeActivationTypes.DerivedSealed), {{objRefName}}, {{defaultIfaceIid}}, {{GetMarshalingTypeName(classType)}})
                {
                """);
            if (gcPressure > 0)
            {
                writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(CultureInfo.InvariantCulture)});");
            }

            writer.WriteLine("}");
        }
    }
}
