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
    /// Emits the closing <c>)</c> of a constructor parameter list followed by a <c>throw null</c> body,
    /// for constructors emitted in reference projection mode. The caller must already have written the
    /// constructor signature up to (but not including) the closing <c>)</c> of the parameter list.
    /// </summary>
    /// <remarks>
    /// Reference projections only need the public constructor signatures (their bodies are never run and
    /// are stripped from the produced reference assembly), so the body is emitted as <c>throw null</c>
    /// rather than the real activation logic, which would reference implementation-only 'WinRT.Runtime'
    /// types (object references, activation factory callbacks, ABI IID accessors).
    /// </remarks>
    /// <param name="writer">The writer to emit to.</param>
    public static void EmitRefModeConstructorBody(IndentedTextWriter writer)
    {
        writer.WriteLine(isMultiline: true, """
            )
            {
                throw null;
            }
            """);
    }

    /// <summary>
    /// Emits the body of a delegate factory <c>Invoke</c> method in reference projection mode.
    /// </summary>
    /// <param name="writer">The writer to emit to. Must be at the class-scope indent level on
    /// entry (i.e. the inner method body and the outer class body braces are both closed by
    /// this method).</param>
    public static void EmitRefModeInvokeBody(IndentedTextWriter writer)
    {
        writer.WriteLine(isMultiline: true, """
                    throw null;
                }
            }
            """);
    }
}
