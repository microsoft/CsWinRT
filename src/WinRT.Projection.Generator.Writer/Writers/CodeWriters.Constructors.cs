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
            // Factory ctors require a generated callback class hierarchy (Args ref struct +
            // sealed Callback class with Invoke method that does the marshalling and ABI dispatch).
            // That's tracked separately as commit-8-misc; for now keep the throw-null stub.
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (Helpers.IsSpecial(method)) { continue; }
                MethodSig sig = new(method);
                w.Write("\npublic unsafe ");
                w.Write(typeName);
                w.Write("(");
                WriteParameterList(w, sig);
                w.Write(") : base(default(WindowsRuntimeObjectReference)) => throw null!;\n");
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
