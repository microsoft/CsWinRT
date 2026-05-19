// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class ConstructorFactory
{
    /// <summary>
    /// Emits:
    /// 1. Public/protected constructors for each composable factory method (with proper body).
    /// 2. Static factory callback class (per ctor) for parameterized composable activation.
    /// 3. Four protected base-chaining constructors used by derived projected types.
    /// </summary>
    public static void WriteComposableConstructors(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition? composableType, TypeDefinition classType, string visibility)
    {
        if (composableType is null)
        {
            return;
        }

        string typeName = classType.Name?.Value ?? string.Empty;

        // Emit the factory objref + IIDs at the top so the parameterized ctors can reference it.
        if (composableType.Methods.Count > 0)
        {
            string runtimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, composableType);
            ClassFactory.WriteStaticFactoryObjRef(writer, context, composableType, runtimeClassFullName, factoryObjRefName);
        }

        string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
        string marshalingType = GetMarshalingTypeName(classType);
        string defaultIfaceObjRef;
        ITypeDefOrRef? defaultIface = classType.GetDefaultInterface();
        defaultIfaceObjRef = defaultIface is not null ? ObjRefNameGenerator.GetObjRefName(context, defaultIface) : string.Empty;
        int gcPressure = ClassFactory.GetGcPressureAmount(classType);
        // Compute the platform attribute string from the composable factory interface's
        // [ContractVersion] attribute
        string platformAttribute = CustomAttributeFactory.GetPlatformAttribute(context, composableType);

        int methodIndex = 0;
        foreach (MethodDefinition method in composableType.Methods)
        {
            if (method.IsSpecial)
            {
                methodIndex++;

                continue;
            }

            // Composable factory methods have signature like:
            //   T CreateInstance(args, object baseInterface, out object innerInterface)
            // For the constructor on the projected class, we exclude the trailing two params.
            MethodSignatureInfo sig = new(method);
            int userParamCount = sig.Parameters.Count >= 2 ? sig.Parameters.Count - 2 : sig.Parameters.Count;
            // the callback / args type name suffix is the TOTAL ABI param count
            // (size(method.Signature().Parameters())), NOT the user-visible param count. Using the
            // total count guarantees uniqueness against other composable factory overloads that
            // might share the same user-param count but differ in trailing baseInterface shape.
            string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Parameters.Count.ToString(CultureInfo.InvariantCulture);
            string argsName = callbackName + "Args";
            bool isParameterless = userParamCount == 0;

            writer.WriteLine();

            writer.WriteIf(!string.IsNullOrEmpty(platformAttribute), platformAttribute);

            writer.Write(visibility);

            if (!isParameterless)
            {
                writer.Write(" unsafe ");
            }
            else
            {
                writer.Write(" ");
            }

            writer.Write($"{typeName}(");
            for (int i = 0; i < userParamCount; i++)
            {
                WriteProjectionParameterCallback p = MethodFactory.WriteProjectionParameter(context, sig.Parameters[i]);
                writer.Write($"{(i > 0 ? ", " : "")}{p}");
            }

            writer.Write(isMultiline: true, """
                )
                  :base(
                """);
            if (isParameterless)
            {
                // base(default(WindowsRuntimeActivationTypes.DerivedComposed), <factoryObjRef>, <iid>, <marshalingType>)
                string factoryObjRef = ObjRefNameGenerator.GetObjRefName(context, composableType);
                writer.Write($"default(WindowsRuntimeActivationTypes.DerivedComposed), {factoryObjRef}, {defaultIfaceIid}, {marshalingType}");
            }
            else
            {
                writer.Write($"{callbackName}.Instance, {defaultIfaceIid}, {marshalingType}, WindowsRuntimeActivationArgsReference.CreateUnsafe(new {argsName}(");
                for (int i = 0; i < userParamCount; i++)
                {
                    writer.WriteIf(i > 0, ", ");

                    string raw = sig.Parameters[i].GetRawName();
                    writer.Write(IdentifierEscaping.EscapeIdentifier(raw));
                }
                writer.Write("))");
            }

            writer.WriteLine(isMultiline: true, """
                )
                """);
            using (writer.WriteBlock())
            {
                writer.WriteLine(isMultiline: true, $$"""
                    if (GetType() == typeof({{typeName}}))
                    {
                        {{defaultIfaceObjRef}} = NativeObjectReference;
                    }
                    """);

                if (gcPressure > 0)
                {
                    writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(CultureInfo.InvariantCulture)});");
                }
            }

            // Emit args struct + callback class for parameterized composable factories.
            // skips both the args struct AND the callback class entirely in ref mode. The
            // public ctor above still references these types, but reference assemblies don't
            // need their bodies' references to resolve (only the public API surface matters).
            if (!isParameterless && !context.Settings.ReferenceProjection)
            {
                EmitFactoryArgsStruct(writer, context, sig, argsName, userParamCount);
                string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, composableType);
                EmitFactoryCallbackClass(writer, context, sig, callbackName, argsName, factoryObjRefName, methodIndex, isComposable: true, userParamCount: userParamCount);
            }

            methodIndex++;
        }

        if (context.Settings.ReferenceProjection)
        {
            return;
        }

        // Emit the four base-chaining constructors used by derived projected types.
        string gcPressureBody = gcPressure > 0
            ? "GC.AddMemoryPressure(" + gcPressure.ToString(CultureInfo.InvariantCulture) + ");"
            : string.Empty;

        // Each entry pairs the constructor's parameter list with the matching ':base(...)' arg list.
        (string Parameters, string BaseArgs)[] ctorSignatures =
        [
            ("WindowsRuntimeActivationTypes.DerivedComposed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType",
             "_, activationFactoryObjectReference, in iid, marshalingType"),
            ("WindowsRuntimeActivationTypes.DerivedSealed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType",
             "_, activationFactoryObjectReference, in iid, marshalingType"),
            ("WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters",
             "activationFactoryCallback, in iid, marshalingType, additionalParameters"),
            ("WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters",
             "activationFactoryCallback, in iid, marshalingType, additionalParameters"),
        ];

        foreach ((string parameters, string baseArgs) in ctorSignatures)
        {
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                protected {{typeName}}({{parameters}})
                  :base({{baseArgs}})
                """);
            using (writer.WriteBlock())
            {
                writer.WriteLineIf(!string.IsNullOrEmpty(gcPressureBody), gcPressureBody);
            }
        }
    }
}
