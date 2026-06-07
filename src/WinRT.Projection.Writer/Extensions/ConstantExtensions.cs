// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="Constant"/>.
/// </summary>
internal static class ConstantExtensions
{
    /// <param name="constant">The input constant.</param>
    extension(Constant constant)
    {
        /// <summary>
        /// Formats a metadata constant value as a C# literal.
        /// </summary>
        public string FormatLiteral()
        {
            static string FormatHexValue(uint value)
            {
                // Match printf "%#0x" semantics: for '0', output "0"; for non-zero, output "0x<hex>" with no padding
                return value == 0
                    ? "0"
                    : $"0x{value:x}";
            }

            // The data contains raw bytes representing the value
            byte[] data = constant.Value?.Data ?? [];

            return constant.Type switch
            {
                ElementType.I1 => ((sbyte)data[0]).ToString(CultureInfo.InvariantCulture),
                ElementType.U1 => data[0].ToString(CultureInfo.InvariantCulture),
                ElementType.I2 => BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture),
                ElementType.U2 => BitConverter.ToUInt16(data, 0).ToString(CultureInfo.InvariantCulture),
                ElementType.I4 => FormatHexValue((uint)BitConverter.ToInt32(data, 0)),
                ElementType.U4 => FormatHexValue(BitConverter.ToUInt32(data, 0)),
                ElementType.I8 => BitConverter.ToInt64(data, 0).ToString(CultureInfo.InvariantCulture),
                ElementType.U8 => BitConverter.ToUInt64(data, 0).ToString(CultureInfo.InvariantCulture),
                _ => "0"
            };
        }
    }
}