// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter;

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
            uint? targetsValue = null;
            if (isAttributeUsage && i == 0)
            {
                if (arg.Element is uint u) { targetsValue = u; }
                else if (arg.Element is int s) { targetsValue = unchecked((uint)s); }
            }
            if (targetsValue is uint tv)
            {
                result.Add(FormatAttributeTargets(tv));
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
            string s => "@\"" + EscapeVerbatimString(s) + "\"",
            AsmResolver.Utf8String us => "@\"" + EscapeVerbatimString(us.Value) + "\"",
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
            // Always prepend 'global::' to typeof() arguments. The C++ cswinrt tool does this for the
            // same reason: when the generated file's namespace context happens to contain a 'Windows'
            // sub-namespace (e.g. 'TestComponentCSharp.Windows.*'), an unqualified 'Windows.Foundation.X'
            // would resolve to 'TestComponentCSharp.Windows.Foundation.X' first under C# name lookup
            // and fail with CS0234. The 'global::' prefix forces fully-qualified resolution.
            TypeSignature ts when ts.FullName is { Length: > 0 } fn => "typeof(global::" + fn + ")",
            TypeSignature => "typeof(object)",
            _ => element.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Escapes a string for use inside a C# verbatim string literal (<c>@"..."</c>).
    /// Mirrors C++ <c>write_custom_attribute_args</c> string emission (code_writers.h:2401-2427):
    /// the WinMD attribute string value carries source-level escape sequences (e.g. <c>\"</c>
    /// for an embedded quote). The C++ tool un-escapes these before emitting a verbatim string,
    /// so a WinMD value of <c>\"quotes\"</c> becomes the verbatim source text <c>""quotes""</c>
    /// (which decodes to <c>"quotes"</c> at runtime).
    /// Logic:
    /// - <c>\</c> followed by <c>\</c> / <c>'</c> / <c>"</c>: drop the backslash, keep the char.
    /// - <c>\</c> followed by anything else: keep both <c>\</c> and the char.
    /// - Each emitted <c>"</c> is doubled (<c>""</c>) per verbatim-string escape rules.
    /// </summary>
    private static string EscapeVerbatimString(string s)
    {
        StringBuilder sb = new(s.Length);
        bool prevEscape = false;
        foreach (char c in s)
        {
            if (c == '\\' && !prevEscape)
            {
                prevEscape = true;
                continue;
            }
            if (prevEscape && c != '\\' && c != '\'' && c != '"')
            {
                sb.Append('\\');
            }
            prevEscape = false;
            sb.Append(c);
            if (c == '"') { sb.Append('"'); }
        }
        if (prevEscape) { sb.Append('\\'); }
        return sb.ToString();
    }

    /// <summary>
    /// Mirrors C++ <c>get_platform(writer&amp;, CustomAttribute)</c>: returns the formatted
    /// SupportedOSPlatform string ("WindowsX.Y.Z.0") for a [ContractVersion] attribute,
    /// or empty if no platform mapping exists. Honors writer's <see cref="TypeWriter.CheckPlatform"/>
    /// state to deduplicate platforms within a single class scope (mirrors C++
    /// _check_platform / _platform behavior in code_writers.h:2515-2525).
    /// </summary>
    private static string GetPlatform(TypeWriter w, CustomAttribute attribute)
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
        else if (arg0.Element is not null)
        {
            // AsmResolver returns Utf8String for string custom-attribute args.
            contractName = arg0.Element.ToString() ?? string.Empty;
            if (contractName.Length == 0) { return string.Empty; }
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
        if (w.CheckPlatform)
        {
            // Suppress when this platform is <= the previously seen platform for the class.
            if (string.CompareOrdinal(platform, w.Platform) <= 0)
            {
                return string.Empty;
            }
            // Only seed _platform on first non-empty observation (matches C++ behavior:
            // higher platforms emit but don't update _platform).
            if (w.Platform.Length == 0)
            {
                w.Platform = platform;
            }
        }
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
                string platform = GetPlatform(w, attr);
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
                string platform = GetPlatform(w, attr);
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
