// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Reference-projection stub emission helpers. In reference projection mode, all method/property/
/// event bodies (and certain other constructs like static factory objref getters, activation
/// factory objref getters, and the synthetic private ctor for classes without explicit
/// constructors) collapse to <c>throw null</c>.
/// </summary>
internal static class RefModeStubFactory
{
    /// <summary>
    /// Emits the body of an <c>_objRef_*</c> property getter in reference projection mode.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    public static void EmitRefModeObjRefGetterBody(IndentedTextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine("""
            {
                get
                {
                    throw null;
                }
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits the synthetic <c>private TypeName() { throw null; }</c> ctor used in reference
    /// projection mode to suppress the C# compiler's implicit public default constructor when
    /// no explicit ctors are emitted by <c>WriteAttributedTypes</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="typeName">The type name to emit the synthetic constructor for.</param>
    public static void EmitSyntheticPrivateCtor(IndentedTextWriter writer, string typeName)
    {
        writer.WriteLine();
        writer.WriteLine($"private {typeName}() {{ throw null; }}");
    }

    /// <summary>
    /// Emits the body of a delegate factory <c>Invoke</c> method in reference projection mode.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    public static void EmitRefModeInvokeBody(IndentedTextWriter writer)
    {
        writer.WriteLine("""
                    throw null;
                }
            }
            """, isMultiline: true);
    }
}
