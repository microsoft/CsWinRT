// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <inheritdoc cref="SignatureGenerator"/>
internal partial class SignatureGenerator
{
    /// <summary>The signature for <see cref="sbyte"/>.</summary>
    private const string SByteSignature = "i1";

    /// <summary>The signature for <see cref="byte"/>.</summary>
    private const string ByteSignature = "u1";

    /// <summary>The signature for <see cref="short"/>.</summary>
    private const string ShortSignature = "i2";

    /// <summary>The signature for <see cref="ushort"/>.</summary>
    private const string UShortSignature = "u2";

    /// <summary>The signature for <see cref="int"/>.</summary>
    private const string IntSignature = "i4";

    /// <summary>The signature for <see cref="uint"/>.</summary>
    private const string UIntSignature = "u4";

    /// <summary>The signature for <see cref="long"/>.</summary>
    private const string LongSignature = "i8";

    /// <summary>The signature for <see cref="ulong"/>.</summary>
    private const string ULongSignature = "u8";

    /// <summary>The signature for <see cref="float"/>.</summary>
    private const string FloatSignature = "f4";

    /// <summary>The signature for <see cref="double"/>.</summary>
    private const string DoubleSignature = "f8";

    /// <summary>The signature for <see cref="bool"/>.</summary>
    private const string BoolSignature = "b1";

    /// <summary>The signature for <see cref="char"/>.</summary>
    private const string CharSignature = "c2";

    /// <summary>The signature for <see cref="object"/>.</summary>
    private const string ObjectSignature = "cinterface(IInspectable)";

    /// <summary>The signature for <see cref="string"/>.</summary>
    private const string StringSignature = "string";

    /// <summary>The signature for <see cref="Type"/>.</summary>
    private const string TypeSignature = "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";

    /// <summary>The signature for <see cref="Guid"/>.</summary>
    private const string GuidSignature = "g16";
}
