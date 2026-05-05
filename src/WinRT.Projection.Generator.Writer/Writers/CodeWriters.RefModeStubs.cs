// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Reference-projection stub emission helpers. In reference projection mode, all method/property/
/// event bodies (and certain other constructs like static factory objref getters, activation
/// factory objref getters, and the synthetic private ctor for classes without explicit
/// constructors) collapse to <c>throw null</c>. Mirrors C++ <c>code_writers.h:1639/1655/1671/
/// 1685/1699/1713/2755/2796/2217/2240/6851/9536</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Emits the body of an <c>_objRef_*</c> property getter in reference projection mode.
    /// Mirrors C++ <c>write_static_objref_definition</c> / <c>write_activation_factory_objref_definition</c>
    /// (code_writers.h:2755 and 2796) which emit <c>{ get { throw null; } }</c> in ref mode.
    /// </summary>
    public static void EmitRefModeObjRefGetterBody(TypeWriter w)
    {
        w.Write("\n{\n    get\n    {\n        throw null;\n    }\n}\n");
    }

    /// <summary>
    /// Emits the synthetic <c>private TypeName() { throw null; }</c> ctor used in reference
    /// projection mode to suppress the C# compiler's implicit public default constructor when
    /// no explicit ctors are emitted by <c>WriteAttributedTypes</c>.
    /// Mirrors C++ <c>code_writers.h:9536</c>.
    /// </summary>
    public static void EmitSyntheticPrivateCtor(TypeWriter w, string typeName)
    {
        w.Write("\nprivate ");
        w.Write(typeName);
        w.Write("() { throw null; }\n");
    }

    /// <summary>
    /// Emits the body of a delegate factory <c>Invoke</c> method in reference projection mode.
    /// Mirrors C++ <c>code_writers.h:6851</c> which emits <c>throw null;</c> for the activator
    /// factory delegate's <c>Invoke</c> body in ref mode.
    /// </summary>
    public static void EmitRefModeInvokeBody(TypeWriter w)
    {
        w.Write("        throw null;\n    }\n}\n");
    }
}
