// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
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

        // The activation factory '_objRef_*' field is a private implementation detail typed as the
        // implementation-only 'WindowsRuntimeObjectReference', so it is omitted from reference projections
        // (which compile against the stripped 'WinRT.Runtime' reference assembly).
        if (needsClassObjRef && !context.Settings.ReferenceProjection)
        {
            string fullName = classType.FullName ?? string.Empty;
            string objRefName = "_objRef_" + IidExpressionGenerator.EscapeTypeNameForIdentifier(GlobalPrefix + fullName, stripGlobal: true);
            writer.WriteLine();
            writer.Write($"private static WindowsRuntimeObjectReference {objRefName}");
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

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, context.Cache))
        {
            AttributedType factory = kv.Value;

            // Skip constructors generated from a removed factory interface: the interface is omitted
            // from the projection and ABI, so its IID / ABI Methods class would not exist to dispatch to.
            if (factory.Type is { IsRemoved: true })
            {
                continue;
            }

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
        string typeName = classType.GetRawName();
        int gcPressure = ClassFactory.GetGcPressureAmount(classType);

        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = classType.FullName ?? string.Empty;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, factoryType);
            ClassFactory.WriteStaticFactoryObjRef(writer, context, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
            string marshalingType = GetMarshalingTypeName(classType);

            // Compute the platform attribute string from the activation factory interface's
            // [ContractVersion] attribute
            string platformAttribute = CustomAttributeFactory.GetPlatformAttribute(context, factoryType);
            int methodIndex = 0;
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (method.IsSpecial)
                {
                    methodIndex++;

                    continue;
                }

                // Skip removed constructor overloads; the factory vtable slot is preserved (methodIndex
                // still advances) so the remaining constructors dispatch through the correct slot.
                if (method.IsRemoved)
                {
                    methodIndex++;

                    continue;
                }

                MethodSignatureInfo sig = new(method);
                string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Parameters.Count.ToString(CultureInfo.InvariantCulture);
                string argsName = callbackName + "Args";

                // Emit the public constructor.
                writer.WriteLine();

                CustomAttributeFactory.WriteObsoleteAttribute(writer, method);

                writer.WriteIf(!string.IsNullOrEmpty(platformAttribute), platformAttribute);

                writer.Write($"public unsafe {typeName}(");
                MethodFactory.WriteParameterList(writer, context, sig);

                // In ref mode the constructor keeps its public signature but gets a 'throw null' body,
                // and the args struct + factory callback class below are skipped (they are private
                // implementation details referencing implementation-only 'WinRT.Runtime' types).
                if (context.Settings.ReferenceProjection)
                {
                    RefModeStubFactory.EmitRefModeConstructorBody(writer);
                    methodIndex++;

                    continue;
                }

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
                        writer.WriteIf(i > 0, ", ");

                        string raw = sig.Parameters[i].GetRawName();
                        writer.Write(IdentifierEscaping.EscapeIdentifier(raw));
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
            // No factory type means '[Activatable(uint version)]',  emit a parameterless default ctor
            if (context.Settings.ReferenceProjection)
            {
                // Ref mode keeps the public signature with a 'throw null' body (the impl-mode body
                // calls the base ctor with implementation-only activation types).
                writer.WriteLine();
                writer.Write($"public {typeName}(");
                RefModeStubFactory.EmitRefModeConstructorBody(writer);

                return;
            }

            // The impl-mode default ctor calls the 'WindowsRuntimeObject' base constructor with
            // the activation factory object reference. The default interface IID is needed too.
            string fullName = classType.FullName ?? string.Empty;
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

    /// <summary>
    /// Determines whether <see cref="WriteAttributedTypes"/> emits at least one public constructor for
    /// the given runtime class.
    /// </summary>
    /// <remarks>
    /// Used in reference-projection mode to decide whether a sealed class needs a synthetic non-public
    /// parameterless constructor to suppress the C# compiler's implicit public default constructor
    /// (see <see cref="RefModeStubFactory.EmitSyntheticPrivateCtor"/>). Emitting it matters for more than
    /// tidiness: the implementation projection never emits an implicit public default constructor, so
    /// leaving one on the reference surface would let consumers compile a <c>new T()</c> call that fails
    /// at runtime against the implementation projection.
    /// </remarks>
    public static bool EmitsAnyConstructor(TypeDefinition classType, MetadataCache cache)
    {
        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, cache))
        {
            AttributedType factory = kv.Value;

            // A removed factory interface is skipped entirely by 'WriteAttributedTypes', so it emits nothing
            if (factory.Type is { IsRemoved: true })
            {
                continue;
            }

            // A default '[Activatable(uint version)]' (no factory interface) always emits 'public TypeName()'
            if (factory.Activatable && factory.Type is null)
            {
                return true;
            }

            // Both activation and composable factories emit one constructor per factory method, so a factory
            // whose methods are all special or removed (or which has none at all) emits no constructors.
            if ((factory.Activatable || factory.Composable) && factory.Type is { } factoryType && factoryType.HasActivatableFactoryMethod())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether <see cref="WriteAttributedTypes"/> emits at least one parameterless public
    /// constructor for the given runtime class (a default <c>[Activatable]</c> ctor, an activation-factory
    /// method with no parameters, or a composable-factory method with no user parameters).
    /// </summary>
    /// <remarks>
    /// Used in reference-projection mode to decide whether an unsealed class already exposes a
    /// parameterless constructor that derived projected classes can chain to, or whether a synthetic
    /// non-public one must be emitted (see <see cref="RefModeStubFactory.EmitSyntheticPrivateCtor"/>).
    /// </remarks>
    public static bool EmitsParameterlessConstructor(TypeDefinition classType, MetadataCache cache)
    {
        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, cache))
        {
            AttributedType factory = kv.Value;

            // A removed factory interface is skipped entirely by 'WriteAttributedTypes', so it emits nothing
            if (factory.Type is { IsRemoved: true })
            {
                continue;
            }

            // A default '[Activatable(uint version)]' (no factory interface) emits 'public TypeName()'.
            if (factory.Activatable && factory.Type is null)
            {
                return true;
            }

            if (factory.Type is null)
            {
                continue;
            }

            // An activation- or composable-factory method emits a parameterless ctor when it has no user
            // parameters. Composable factory methods carry two trailing ABI parameters (the base and inner
            // interfaces) that are not surfaced on the projected constructor, so they are excluded.
            if (factory.Activatable || factory.Composable)
            {
                foreach (MethodDefinition method in factory.Type.Methods)
                {
                    // Special methods and removed overloads emit no constructor
                    if (method.IsSpecial || method.IsRemoved)
                    {
                        continue;
                    }

                    MethodSignatureInfo sig = new(method);
                    int userParamCount = factory.Composable && sig.Parameters.Count >= 2
                        ? sig.Parameters.Count - 2
                        : sig.Parameters.Count;

                    if (userParamCount == 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
