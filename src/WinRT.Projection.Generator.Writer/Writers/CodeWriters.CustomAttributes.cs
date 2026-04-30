// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Custom attribute carry-over and platform attribute helpers. Mirrors C++ functions
/// in <c>code_writers.h</c> for <c>write_custom_attributes</c>, <c>write_custom_attribute_args</c>,
/// <c>write_platform_attribute</c>, <c>get_platform</c>, etc.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Mirrors C++ <c>write_custom_attribute_args</c>. Returns the formatted argument strings.
    /// </summary>
    public static List<string> WriteCustomAttributeArgs(TypeWriter w, CustomAttribute attribute)
    {
        List<string> result = new();
        if (attribute.Signature is null) { return result; }

        // Detect AttributeUsage which takes an AttributeTargets enum
        ITypeDefOrRef? attrType = attribute.Constructor?.DeclaringType;
        bool isAttributeUsage = attrType?.Name == "AttributeUsageAttribute" ||
                                attrType?.Name == "AttributeUsage";

        for (int i = 0; i < attribute.Signature.FixedArguments.Count; i++)
        {
            CustomAttributeArgument arg = attribute.Signature.FixedArguments[i];
            if (isAttributeUsage && i == 0 && arg.Element is uint targetsValue)
            {
                result.Add(FormatAttributeTargets(targetsValue));
            }
            else
            {
                result.Add(FormatCustomAttributeArg(w, arg));
            }
        }
        for (int i = 0; i < attribute.Signature.NamedArguments.Count; i++)
        {
            CustomAttributeNamedArgument named = attribute.Signature.NamedArguments[i];
            result.Add(named.MemberName?.Value + " = " + FormatCustomAttributeArg(w, named.Argument));
        }
        return result;
    }

    /// <summary>
    /// Formats an AttributeTargets uint value as a bitwise OR of <c>global::System.AttributeTargets.X</c>.
    /// Mirrors the C++ AttributeTargets handling in <c>write_custom_attribute_args</c>.
    /// </summary>
    private static string FormatAttributeTargets(uint value)
    {
        if (value == 0xFFFFFFFFu)
        {
            return "global::System.AttributeTargets.All";
        }
        // Map each bit to its corresponding enum name. Includes WinMD-specific values
        // that map to the same .NET enum (e.g., RuntimeClass=512 -> Class, ApiContract=8192 -> Struct).
        (uint Bit, string Name)[] entries =
        {
            (1, "Delegate"),
            (2, "Enum"),
            (4, "Event"),
            (8, "Field"),
            (16, "Interface"),
            (64, "Method"),
            (128, "Parameter"),
            (256, "Property"),
            (512, "Class"),     // RuntimeClass
            (1024, "Struct"),
            (2048, "All"),      // InterfaceImpl - not directly representable, use All
            (8192, "Struct"),   // ApiContract -> Struct
        };
        List<string> values = new();
        foreach ((uint bit, string name) in entries)
        {
            if ((value & bit) != 0)
            {
                values.Add("global::System.AttributeTargets." + name);
            }
        }
        if (values.Count == 0)
        {
            return "global::System.AttributeTargets.All";
        }
        return string.Join(" | ", values);
    }

    private static string FormatCustomAttributeArg(TypeWriter w, CustomAttributeArgument arg)
    {
        // The arg can hold scalar, type, enum or string values.
        object? element = arg.Element;
        return element switch
        {
            null => "null",
            string s => "\"" + EscapeString(s) + "\"",
            AsmResolver.Utf8String us => "\"" + EscapeString(us.Value) + "\"",
            bool b => b ? "true" : "false",
            byte by => by.ToString(CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
            short sh => sh.ToString(CultureInfo.InvariantCulture),
            ushort us2 => us2.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            uint ui => ui.ToString(CultureInfo.InvariantCulture) + "u",
            long l => l.ToString(CultureInfo.InvariantCulture),
            ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "ul",
            float f => f.ToString("R", CultureInfo.InvariantCulture) + "f",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            char c => "'" + c + "'",
            TypeSignature ts => "typeof(" + (ts.FullName ?? string.Empty) + ")",
            _ => element.ToString() ?? "null"
        };
    }

    private static string EscapeString(string s)
    {
        StringBuilder sb = new(s.Length);
        foreach (char c in s)
        {
            if (c == '\\') { sb.Append('\\').Append('\\'); }
            else if (c == '"') { sb.Append('\\').Append('"'); }
            else if (c == '\n') { sb.Append('\\').Append('n'); }
            else if (c == '\r') { sb.Append('\\').Append('r'); }
            else if (c == '\t') { sb.Append('\\').Append('t'); }
            else { sb.Append(c); }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Computes the platform string from the [ContractVersion] attribute pair, if any.
    /// Mirrors C++ <c>get_platform</c>.
    /// </summary>
    public static string GetPlatform(CustomAttribute attribute)
    {
        if (attribute.Signature is null || attribute.Signature.FixedArguments.Count < 2)
        {
            return string.Empty;
        }
        CustomAttributeArgument arg0 = attribute.Signature.FixedArguments[0];
        string contractName;
        if (arg0.Element is TypeSignature ts && ts.FullName is { } fn)
        {
            contractName = fn;
        }
        else if (arg0.Element is string s)
        {
            contractName = s;
        }
        else
        {
            return string.Empty;
        }

        // The version is a uint where the top 16 bits are the major version
        CustomAttributeArgument arg1 = attribute.Signature.FixedArguments[1];
        uint versionRaw = arg1.Element switch
        {
            uint u => u,
            int i => (uint)i,
            _ => 0u
        };
        int contractVersion = (int)(versionRaw >> 16);

        string platform = ContractPlatforms.GetPlatform(contractName, contractVersion);
        if (string.IsNullOrEmpty(platform)) { return string.Empty; }
        return "\"Windows" + platform + "\"";
    }

    /// <summary>
    /// Mirrors C++ <c>write_platform_attribute</c>: emits [SupportedOSPlatform("WindowsX.Y.Z.0")]
    /// for a [ContractVersion] attribute. Only writes for reference projection.
    /// </summary>
    public static void WritePlatformAttribute(TypeWriter w, IHasCustomAttribute member)
    {
        if (!w.Settings.ReferenceProjection) { return; }
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            string name = attrType.Name?.Value ?? string.Empty;
            // Strip 'Attribute' suffix
            if (name.EndsWith("Attribute", System.StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - "Attribute".Length);
            }
            if (name == "ContractVersion" && attr.Signature?.FixedArguments.Count == 2)
            {
                string platform = GetPlatform(attr);
                if (!string.IsNullOrEmpty(platform))
                {
                    w.Write("[global::System.Runtime.Versioning.SupportedOSPlatform(");
                    w.Write(platform);
                    w.Write(")]\n");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Mirrors C++ <c>write_custom_attributes</c>: carries selected custom attributes
    /// to the projection (e.g., [Obsolete], [Deprecated], [SupportedOSPlatform]).
    /// </summary>
    public static void WriteCustomAttributes(TypeWriter w, IHasCustomAttribute member, bool enablePlatformAttrib)
    {
        Dictionary<string, List<string>> attributes = new(System.StringComparer.Ordinal);
        bool allowMultiple = false;

        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            string ns = attrType.Namespace?.Value ?? string.Empty;
            string name = attrType.Name?.Value ?? string.Empty;
            // Strip 'Attribute' suffix
            string strippedName = name.EndsWith("Attribute", System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Attribute".Length)
                : name;

            // Skip attributes handled separately
            if (strippedName is "GCPressure" or "Guid" or "Flags" or "ProjectionInternal") { continue; }

            string fullAttrName = strippedName == "AttributeUsage"
                ? "System.AttributeUsage"
                : ns + "." + strippedName;

            List<string> args = WriteCustomAttributeArgs(w, attr);

            if (w.Settings.ReferenceProjection && enablePlatformAttrib && strippedName == "ContractVersion" && attr.Signature?.FixedArguments.Count == 2)
            {
                string platform = GetPlatform(attr);
                if (!string.IsNullOrEmpty(platform))
                {
                    if (!attributes.TryGetValue("System.Runtime.Versioning.SupportedOSPlatform", out List<string>? list))
                    {
                        list = new List<string>();
                        attributes["System.Runtime.Versioning.SupportedOSPlatform"] = list;
                    }
                    list.Add(platform);
                }
            }

            // Skip metadata attributes without a projection
            if (ns == "Windows.Foundation.Metadata")
            {
                if (strippedName == "AllowMultiple")
                {
                    allowMultiple = true;
                }
                if (strippedName == "ContractVersion")
                {
                    if (!w.Settings.ReferenceProjection) { continue; }
                }
                else if (strippedName is not ("DefaultOverload" or "Overload" or "AttributeUsage" or "Experimental"))
                {
                    continue;
                }
            }

            attributes[fullAttrName] = args;
        }

        // Add AllowMultiple to AttributeUsage if needed
        if (attributes.TryGetValue("System.AttributeUsage", out List<string>? usage))
        {
            usage.Add("AllowMultiple = " + (allowMultiple ? "true" : "false"));
        }

        foreach (KeyValuePair<string, List<string>> kv in attributes)
        {
            w.Write("[global::");
            w.Write(kv.Key);
            if (kv.Value.Count > 0)
            {
                w.Write("(");
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    w.Write(kv.Value[i]);
                }
                w.Write(")");
            }
            w.Write("]\n");
        }
    }

    /// <summary>Mirrors C++ <c>write_type_custom_attributes</c>.</summary>
    public static void WriteTypeCustomAttributes(TypeWriter w, TypeDefinition type, bool enablePlatformAttrib)
    {
        WriteCustomAttributes(w, type, enablePlatformAttrib);
    }
}
