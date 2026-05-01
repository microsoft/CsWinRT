// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Activator/composer constructor emission. Mirrors C++ <c>write_factory_constructors</c>
/// and <c>write_composable_constructors</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Mirrors C++ <c>write_attributed_types</c>: emits constructors and static members
    /// for the given runtime class.
    /// </summary>
    public static void WriteAttributedTypes(TypeWriter w, TypeDefinition classType)
    {
        if (_cacheRef is null) { return; }

        // Track whether we need to emit the static _objRef_<RuntimeClassName> field (used by
        // default constructors). Emit it once per class if any [Activatable] factory exists.
        bool needsClassObjRef = false;

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, _cacheRef))
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
            string objRefName = "_objRef_" + EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);
            w.Write("\nprivate static WindowsRuntimeObjectReference ");
            w.Write(objRefName);
            w.Write("\n{\n    get\n    {\n        var __");
            w.Write(objRefName);
            w.Write(" = field;\n        if (__");
            w.Write(objRefName);
            w.Write(" != null && __");
            w.Write(objRefName);
            w.Write(".IsInCurrentContext)\n        {\n            return __");
            w.Write(objRefName);
            w.Write(";\n        }\n        return field = WindowsRuntimeObjectReference.GetActivationFactory(\"");
            w.Write(fullName);
            w.Write("\");\n    }\n}\n");
        }

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, _cacheRef))
        {
            AttributedType factory = kv.Value;
            if (factory.Activatable)
            {
                WriteFactoryConstructors(w, factory.Type, classType);
            }
            else if (factory.Composable)
            {
                WriteComposableConstructors(w, factory.Type, classType, factory.Visible ? "public" : "protected");
            }
        }
    }

    /// <summary>
    /// Mirrors C++ <c>write_factory_constructors</c>.
    /// </summary>
    public static void WriteFactoryConstructors(TypeWriter w, TypeDefinition? factoryType, TypeDefinition classType)
    {
        string typeName = classType.Name?.Value ?? string.Empty;
        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = GetObjRefName(w, factoryType);
            WriteStaticFactoryObjRef(w, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(w, classType);
            string marshalingType = GetMarshalingTypeName(classType);
            int methodIndex = 0;
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (Helpers.IsSpecial(method)) { methodIndex++; continue; }
                MethodSig sig = new(method);
                string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Params.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string argsName = callbackName + "Args";

                // Emit the public constructor.
                w.Write("\npublic unsafe ");
                w.Write(typeName);
                w.Write("(");
                WriteParameterList(w, sig);
                w.Write(")\n  : base(");
                if (sig.Params.Count == 0)
                {
                    w.Write("default");
                }
                else
                {
                    w.Write(callbackName);
                    w.Write(".Instance, ");
                    w.Write(defaultIfaceIid);
                    w.Write(", ");
                    w.Write(marshalingType);
                    w.Write(", WindowsRuntimeActivationArgsReference.CreateUnsafe(new ");
                    w.Write(argsName);
                    w.Write("(");
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        if (i > 0) { w.Write(", "); }
                        string raw = sig.Params[i].Parameter.Name ?? "param";
                        w.Write(Helpers.IsKeyword(raw) ? "@" + raw : raw);
                    }
                    w.Write("))");
                }
                w.Write(")\n{\n}\n");

                if (sig.Params.Count > 0)
                {
                    EmitFactoryArgsStruct(w, sig, argsName);
                    EmitFactoryCallbackClass(w, sig, callbackName, argsName, factoryObjRefName, methodIndex);
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
            string objRefName = "_objRef_" + EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);

            // Find the default interface IID to use.
            string defaultIfaceIid = GetDefaultInterfaceIid(w, classType);

            w.Write("\npublic ");
            w.Write(typeName);
            w.Write("()\n  : base(default(WindowsRuntimeActivationTypes.DerivedSealed), ");
            w.Write(objRefName);
            w.Write(", ");
            w.Write(defaultIfaceIid);
            w.Write(", CreateObjectReferenceMarshalingType.Agile)\n{\n}\n");
        }
    }

    /// <summary>
    /// Reads the <c>[MarshalingBehaviorAttribute]</c> on the class and returns the corresponding
    /// <c>CreateObjectReferenceMarshalingType.*</c> expression. Mirrors C++
    /// <c>get_marshaling_type_name</c>.
    /// </summary>
    private static string GetMarshalingTypeName(TypeDefinition classType)
    {
        for (int i = 0; i < classType.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = classType.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            if (attrType.Namespace?.Value != "Windows.Foundation.Metadata" ||
                attrType.Name?.Value != "MarshalingBehaviorAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                AsmResolver.DotNet.Signatures.CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
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

    /// <summary>Emits the <c>private readonly ref struct &lt;Name&gt;Args(args...) {...}</c>.</summary>
    private static void EmitFactoryArgsStruct(TypeWriter w, MethodSig sig, string argsName)
    {
        w.Write("\nprivate readonly ref struct ");
        w.Write(argsName);
        w.Write("(");
        WriteParameterList(w, sig);
        w.Write(")\n{\n");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("    public readonly ");
            // Use the projected type as field type. For arrays (ReadOnlySpan<T>) we'd need different
            // handling, but those don't appear in factory ctors in the SDK projection.
            WriteProjectedSignature(w, p.Type, true);
            w.Write(" ");
            w.Write(pname);
            w.Write(" = ");
            w.Write(pname);
            w.Write(";\n");
        }
        w.Write("}\n");
    }

    /// <summary>Emits the <c>private sealed class &lt;Name&gt; : WindowsRuntimeActivationFactoryCallback.DerivedSealed</c>.</summary>
    private static void EmitFactoryCallbackClass(TypeWriter w, MethodSig sig, string callbackName, string argsName, string factoryObjRefName, int factoryMethodIndex)
    {
        w.Write("\nprivate sealed class ");
        w.Write(callbackName);
        w.Write(" : WindowsRuntimeActivationFactoryCallback.DerivedSealed\n{\n");
        w.Write("    public static readonly ");
        w.Write(callbackName);
        w.Write(" Instance = new();\n\n");
        w.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
        w.Write("    public override unsafe void Invoke(WindowsRuntimeActivationArgsReference additionalParameters, out void* retval)\n    {\n");
        w.Write("        using WindowsRuntimeObjectReferenceValue activationFactoryValue = ");
        w.Write(factoryObjRefName);
        w.Write(".AsValue();\n");
        w.Write("        void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();\n");
        w.Write("        ref readonly ");
        w.Write(argsName);
        w.Write(" args = ref additionalParameters.GetValueRefUnsafe<");
        w.Write(argsName);
        w.Write(">();\n");

        // Bind each arg from the args struct to a local of its ABI-marshalable input type.
        // For simple cases (primitives, blittable structs, enums) this is a direct copy. For
        // string params we'd need to marshal to HSTRING. For runtime classes we'd need to
        // marshal to IInspectable*. For now, only emit the body for the cases we can handle;
        // otherwise emit throw null! so the file still compiles.
        bool canEmit = true;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (!IsBlittablePrimitive(sig.Params[i].Type) &&
                !IsBlittableStruct(sig.Params[i].Type) &&
                !IsEnumType(sig.Params[i].Type))
            {
                canEmit = false;
                break;
            }
        }

        if (!canEmit)
        {
            w.Write("        throw null!;\n    }\n}\n");
            return;
        }

        // Bind arg locals.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("        ");
            WriteProjectedSignature(w, p.Type, true);
            w.Write(" ");
            w.Write(pname);
            w.Write(" = args.");
            w.Write(pname);
            w.Write(";\n");
        }

        w.Write("        void* __retval = default;\n");
        // delegate* signature: void*, then each ABI param type, then void**, then int.
        w.Write("        RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*, ");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            WriteAbiType(w, TypeSemanticsFactory.Get(sig.Params[i].Type));
            w.Write(", ");
        }
        w.Write("void**, int>**)ThisPtr)[");
        w.Write((6 + factoryMethodIndex).ToString(System.Globalization.CultureInfo.InvariantCulture));
        w.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write(", ");
            // For enums, cast to underlying type. For bool, cast to byte. For char, cast to ushort.
            if (IsEnumType(p.Type))
            {
                w.Write("(");
                w.Write(GetAbiPrimitiveType(p.Type));
                w.Write(")");
                w.Write(pname);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("(byte)(");
                w.Write(pname);
                w.Write(" ? 1 : 0)");
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("(ushort)");
                w.Write(pname);
            }
            else
            {
                w.Write(pname);
            }
        }
        w.Write(", &__retval));\n");
        w.Write("        retval = __retval;\n    }\n}\n");
    }

    /// <summary>Returns the IID expression for the class's default interface.</summary>
    private static string GetDefaultInterfaceIid(TypeWriter w, TypeDefinition classType)
    {
        ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(classType);
        if (defaultIface is null) { return "default(global::System.Guid)"; }
        return w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, defaultIface)));
    }

    /// <summary>
    /// Mirrors C++ <c>write_composable_constructors</c>.
    /// </summary>
    public static void WriteComposableConstructors(TypeWriter w, TypeDefinition? composableType, TypeDefinition classType, string visibility)
    {
        if (composableType is null) { return; }
        string typeName = classType.Name?.Value ?? string.Empty;
        foreach (MethodDefinition method in composableType.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            // Composable factory methods have signature like:
            //   T CreateInstance(args, object baseInterface, out object innerInterface)
            // For the constructor on the projected class, we exclude the trailing two params.
            MethodSig sig = new(method);
            int userParamCount = sig.Params.Count >= 2 ? sig.Params.Count - 2 : sig.Params.Count;
            w.Write("\n");
            w.Write(visibility);
            w.Write(" unsafe ");
            w.Write(typeName);
            w.Write("(");
            for (int i = 0; i < userParamCount; i++)
            {
                if (i > 0) { w.Write(", "); }
                WriteProjectionParameter(w, sig.Params[i]);
            }
            w.Write(") : base(default(WindowsRuntimeObjectReference)) => throw null!;\n");
        }
    }
}
