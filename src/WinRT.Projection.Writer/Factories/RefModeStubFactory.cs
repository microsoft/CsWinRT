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
    /// <remarks>
    /// For an unsealed class the ctor is emitted as <c>private protected</c> rather than <c>private</c>:
    /// a projected class can derive from another projected class (e.g. <c>UriActionEntity : ActionEntity</c>),
    /// and the derived class's own synthetic ctor implicitly chains to the base's parameterless ctor. The
    /// real <c>WindowsRuntimeObjectReference</c>-based ctor that derived classes chain to in the
    /// implementation projection is not emitted in a reference projection, so the synthetic ctor must be
    /// accessible to derived classes in the same projection. It stays non-public, so it still suppresses the
    /// implicit public default constructor.
    /// </remarks>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="typeName">The type name to emit the synthetic constructor for.</param>
    /// <param name="isSealed">Whether the type is sealed (and so can never be a base class).</param>
    public static void EmitSyntheticPrivateCtor(IndentedTextWriter writer, string typeName, bool isSealed)
    {
        string accessibility = isSealed ? "private" : "private protected";

        writer.WriteLine();
        writer.WriteLine($"{accessibility} {typeName}() {{ throw null; }}");
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
