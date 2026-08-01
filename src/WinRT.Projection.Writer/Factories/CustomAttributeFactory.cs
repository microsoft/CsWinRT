// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.References;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Custom attribute carry-over and platform attribute helpers.
/// </summary>
internal static class CustomAttributeFactory
{
    /// <summary>
    /// The projected name of the Windows Runtime <c>[Experimental]</c> attribute.
    /// </summary>
    /// <remarks>
    /// The two attributes model the same concept, but the .NET one is richer: it requires a diagnostic
    /// id, which lets user code opt into an experimental API by suppressing that specific id (the
    /// Windows Runtime attribute carries no arguments at all). See <see cref="ProjectedExperimentalArgs"/>.
    /// </remarks>
    private const string ProjectedExperimentalName = "System.Diagnostics.CodeAnalysis.Experimental";

    /// <summary>
    /// The pre-formatted arguments emitted for <see cref="ProjectedExperimentalName"/>.
    /// </summary>
    /// <remarks>
    /// They are the same for every experimental Windows Runtime API: the Windows Runtime attribute has
    /// no arguments to derive a per-API id or message from, so all of them share one CsWinRT id.
    /// </remarks>
    private static readonly string[] ProjectedExperimentalArgs =
    [
        $"\"{WellKnownDiagnostics.ExperimentalWindowsRuntimeApiId}\"",
        $"UrlFormat = \"{WellKnownDiagnostics.UrlFormat}\"",
        $"Message = \"{WellKnownDiagnostics.ExperimentalWindowsRuntimeApiMessage}\""
    ];

    /// <summary>
    /// Returns the formatted argument list for emitting <paramref name="attribute"/> as a C# attribute.
    /// </summary>
    /// <param name="attribute">The custom attribute to format.</param>
    /// <returns>A list of pre-formatted positional + named argument strings (in order).</returns>
    public static List<string> WriteCustomAttributeArgs(CustomAttribute attribute)
    {
        List<string> result = [];

        if (attribute.Signature is null)
        {
            return result;
        }

        // Detect AttributeUsage which takes an AttributeTargets enum
        ITypeDefOrRef? attrType = attribute.Constructor?.DeclaringType;
        bool isAttributeUsage = attrType?.Name?.Value is "AttributeUsageAttribute" or "AttributeUsage";

        for (int i = 0; i < attribute.Signature.FixedArguments.Count; i++)
        {
            CustomAttributeArgument arg = attribute.Signature.FixedArguments[i];
            uint? targetsValue = null;

            if (isAttributeUsage && i == 0)
            {
                if (arg.Element is uint u)
                {
                    targetsValue = u;
                }
                else if (arg.Element is int s)
                {
                    targetsValue = unchecked((uint)s);
                }
            }

            if (targetsValue is uint tv)
            {
                result.Add(FormatAttributeTargets(tv));
            }
            else
            {
                result.Add(FormatCustomAttributeArg(arg));
            }
        }
        for (int i = 0; i < attribute.Signature.NamedArguments.Count; i++)
        {
            CustomAttributeNamedArgument named = attribute.Signature.NamedArguments[i];
            result.Add(named.MemberName?.Value + " = " + FormatCustomAttributeArg(named.Argument));
        }
        return result;
    }

    /// <summary>
    /// Formats an AttributeTargets uint value as a bitwise OR of <c>global::System.AttributeTargets.X</c>.
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
        [
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
        ];
        List<string> values = [];
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

    private static string FormatCustomAttributeArg(CustomAttributeArgument arg)
    {
        // The arg can hold scalar, type, enum or string values.
        object? element = arg.Element;
        return element switch
        {
            null => "null",
            string s => "@\"" + EscapeVerbatimString(s) + "\"",
            Utf8String us => "@\"" + EscapeVerbatimString(us.Value) + "\"",
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

            // Always prepend 'global::' to typeof() arguments: when the generated file's namespace
            // context happens to contain a 'Windows' sub-namespace (e.g. 'TestComponentCSharp.Windows.*'),
            // an unqualified 'Windows.Foundation.X' would resolve to 'TestComponentCSharp.Windows.Foundation.X'
            // first under C# name lookup and fail with CS0234. 'global::' forces fully-qualified resolution.
            TypeSignature ts when ts.FullName is { Length: > 0 } fn => "typeof(global::" + fn + ")",
            TypeSignature => "typeof(object)",
            _ => element.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Escapes a string for use inside a C# verbatim string literal (<c>@"..."</c>).
    /// </summary>
    /// <remarks>
    /// The WinMD attribute string value carries source-level escape sequences (e.g. <c>\"</c>
    /// for an embedded quote). the original code un-escapes these before emitting a verbatim string,
    /// so a WinMD value of <c>\"quotes\"</c> becomes the verbatim source text <c>""quotes""</c>
    /// (which decodes to <c>"quotes"</c> at runtime).
    /// Logic:
    /// - <c>\</c> followed by <c>\</c> / <c>'</c> / <c>"</c>: drop the backslash, keep the char.
    /// - <c>\</c> followed by anything else: keep both <c>\</c> and the char.
    /// - Each emitted <c>"</c> is doubled (<c>""</c>) per verbatim-string escape rules.
    /// </remarks>
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
                _ = sb.Append('\\');
            }

            prevEscape = false;
            _ = sb.Append(c);

            if (c == '"')
            {
                _ = sb.Append('"');
            }
        }

        if (prevEscape)
        {
            _ = sb.Append('\\');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the <c>SupportedOSPlatform</c> string (<c>"WindowsX.Y.Z.0"</c>) for a
    /// <c>[ContractVersion]</c> attribute, or empty if no platform mapping exists. Honors the
    /// active context's <see cref="ProjectionEmitContext.CheckPlatform"/> mode flag to deduplicate
    /// platforms within a single class scope.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="attribute">The <c>[ContractVersion]</c> attribute to inspect.</param>
    /// <returns>The platform string (with surrounding quotes), or an empty string.</returns>
    private static string GetPlatform(ProjectionEmitContext context, CustomAttribute attribute)
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

            if (contractName.Length == 0)
            {
                return string.Empty;
            }
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

        if (!ContractPlatforms.TryGetPlatform(contractName, contractVersion, out string? platform))
        {
            return string.Empty;
        }

        if (context.CheckPlatform)
        {
            // Suppress when this platform is <= the previously seen platform for the class.
            if (string.CompareOrdinal(platform, context.Platform) <= 0)
            {
                return string.Empty;
            }

            // Only seed Platform on first non-empty observation: higher platforms emit but don't update Platform.
            context.SeedPlatform(platform);
        }

        return "\"Windows" + platform + "\"";
    }

    /// <summary>
    /// Writes the <c>[SupportedOSPlatform]</c> attribute for a <c>[ContractVersion]</c> attribute
    /// on <paramref name="member"/>. Only writes for reference projection.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="member">The member to inspect for <c>[ContractVersion]</c>.</param>
    public static void WritePlatformAttribute(IndentedTextWriter writer, ProjectionEmitContext context, IHasCustomAttribute member)
    {
        int before = writer.Length;
        WritePlatformAttributeBody(writer, context, member);
        if (writer.Length > before)
        {
            writer.WriteLine();
        }
    }

    /// <inheritdoc cref="WritePlatformAttribute(IndentedTextWriter, ProjectionEmitContext, IHasCustomAttribute)"/>
    /// <returns>A callback emitting the attribute body (no trailing newline). Emits nothing when no <c>[SupportedOSPlatform]</c> applies (or when not in reference-projection mode). The blank-line suppression in the writer collapses any template line that holds only this callback when it expands to empty.</returns>
    public static IndentedTextWriterCallback WritePlatformAttribute(ProjectionEmitContext context, IHasCustomAttribute member)
    {
        return writer => WritePlatformAttributeBody(writer, context, member);
    }

    /// <summary>
    /// Writes just the attribute body (no trailing newline) for
    /// <see cref="WritePlatformAttribute(IndentedTextWriter, ProjectionEmitContext, IHasCustomAttribute)"/>.
    /// In non-reference-projection mode this emits nothing.
    /// </summary>
    internal static void WritePlatformAttributeBody(IndentedTextWriter writer, ProjectionEmitContext context, IHasCustomAttribute member)
    {
        if (!context.Settings.ReferenceProjection)
        {
            return;
        }

        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;

            if (attrType is null)
            {
                continue;
            }

            string name = attrType.GetRawName();

            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name[..^"Attribute".Length];
            }

            if (name == "ContractVersion" && attr.Signature?.FixedArguments.Count == 2)
            {
                string platform = GetPlatform(context, attr);

                if (!string.IsNullOrEmpty(platform))
                {
                    writer.Write($"[global::System.Runtime.Versioning.SupportedOSPlatform({platform})]");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Convenience overload of <see cref="WritePlatformAttribute(IndentedTextWriter, ProjectionEmitContext, IHasCustomAttribute)"/>
    /// that leases an <see cref="IndentedTextWriter"/> from <see cref="IndentedTextWriterPool"/>,
    /// emits the <c>[SupportedOSPlatform]</c> attribute (if any) into it, and returns the
    /// resulting string (including the trailing newline). Returns the empty string when no
    /// attribute is emitted. Used by callers that materialize the result once and use it
    /// inside multiple <see cref="IndentedTextWriter.WriteIf(bool, string)"/> calls within a loop.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="member">The member to inspect for <c>[ContractVersion]</c>.</param>
    /// <returns>The emitted attribute, or <see cref="string.Empty"/> when none.</returns>
    public static string GetPlatformAttribute(ProjectionEmitContext context, IHasCustomAttribute member)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = writerOwner.Writer;
        WritePlatformAttribute(writer, context, member);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the Windows Runtime metadata custom attributes carried over from <paramref name="member"/>
    /// to the projection (e.g. <c>[AttributeUsage]</c>, and — in reference projections only —
    /// <c>[Overload]</c>, <c>[Experimental]</c>, <c>[ContractVersion]</c>, plus the synthesized
    /// <c>[SupportedOSPlatform]</c>).
    /// </summary>
    /// <remarks>
    /// Attributes are emitted in metadata order, one line per application. Windows Runtime attributes
    /// marked <c>[allowmultiple]</c> can be applied several times to the same member, so every
    /// application is carried over. The <c>[AllowMultiple]</c> metadata attribute itself is not
    /// projected: it is folded into the <c>AllowMultiple</c> named argument of the projected
    /// <c>[AttributeUsage]</c>, matching how .NET models repeatability. Which applications survive is
    /// decided by <see cref="ShouldCarryOverAttribute"/>: implementation projections keep only
    /// <c>[AttributeUsage]</c>, since every other metadata attribute is dead, untrimmable weight in an
    /// assembly that is only ever loaded at runtime (see that method for the full rationale).
    /// </remarks>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="member">The metadata member whose custom attributes to emit.</param>
    /// <param name="enablePlatformAttrib">Whether to also emit a <c>[SupportedOSPlatform]</c> attribute synthesized from any <c>[ContractVersion]</c>.</param>
    public static void WriteCustomAttributes(IndentedTextWriter writer, ProjectionEmitContext context, IHasCustomAttribute member, bool enablePlatformAttrib)
    {
        const string AttributeUsageName = "System.AttributeUsage";
        const string SupportedOSPlatformName = "System.Runtime.Versioning.SupportedOSPlatform";

        // Applications are collected in metadata order, with one entry per application. A Windows Runtime
        // attribute marked '[allowmultiple]' (e.g. '[TemplateVisualState]') can legitimately be applied
        // several times to the same member, and every application has to be carried over.
        List<(string Name, IReadOnlyList<string> Args)> attributes = [];

        // The arguments of the recorded '[AttributeUsage]' application, if any, so that a separate
        // '[AllowMultiple]' application can be folded into it once all attributes have been seen
        List<string>? attributeUsageArgs = null;
        bool allowMultiple = false;
        bool hasPlatform = false;

        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;

            if (attrType is null)
            {
                continue;
            }

            (string ns, string name) = attrType.Names();
            string strippedName = name.EndsWith("Attribute", StringComparison.Ordinal)
                ? name[..^"Attribute".Length]
                : name;

            // Skip attributes handled separately
            if (strippedName is "GCPressure" or "Guid" or "Flags" or "ProjectionInternal")
            {
                continue;
            }

            // '[AllowMultiple]' is never carried over itself: it is observed here (in both projection
            // modes) and folded into the projected '[AttributeUsage]' after all attributes are seen.
            if (ns == WindowsFoundationMetadata && strippedName == "AllowMultiple")
            {
                allowMultiple = true;
            }

            // '[SupportedOSPlatform]' takes a single platform, so only the first resolved one is emitted
            // (matching the member-level behavior in 'WritePlatformAttributeBody'). A member can carry
            // several '[ContractVersion]' attributes, but only one of them can define the minimum platform.
            if (!hasPlatform && context.Settings.ReferenceProjection && enablePlatformAttrib && strippedName == "ContractVersion" && attr.Signature?.FixedArguments.Count == 2)
            {
                string platform = GetPlatform(context, attr);

                if (!string.IsNullOrEmpty(platform))
                {
                    attributes.Add((SupportedOSPlatformName, [platform]));

                    hasPlatform = true;
                }
            }

            bool isAttributeUsage = strippedName == "AttributeUsage";

            if (!ShouldCarryOverAttribute(context, ns, strippedName, isAttributeUsage))
            {
                continue;
            }

            // '[Experimental]' is custom-mapped to the .NET attribute of the same name, which requires a
            // diagnostic id that Windows Runtime metadata has no counterpart for, so a CsWinRT-owned one
            // is synthesized (see 'ProjectedExperimentalArgs')
            if (ns == WindowsFoundationMetadata && strippedName == "Experimental")
            {
                attributes.Add((ProjectedExperimentalName, ProjectedExperimentalArgs));

                continue;
            }

            // Only format the arguments of attributes that are actually carried over. Implementation
            // projections drop almost everything, and they are the hot publish-time path.
            List<string> args = WriteCustomAttributeArgs(attr);
            string fullAttrName = isAttributeUsage ? AttributeUsageName : ns + "." + strippedName;

            attributes.Add((fullAttrName, args));

            if (isAttributeUsage)
            {
                attributeUsageArgs = args;
            }
        }

        // Windows Runtime models attribute repeatability with a separate '[AllowMultiple]' metadata
        // attribute, whereas .NET models it as a named argument on '[AttributeUsage]'. Fold the former
        // into the latter, synthesizing an '[AttributeUsage]' if the metadata doesn't declare one (in
        // which case the .NET default of 'AllowMultiple = false' would make the projection unusable).
        if (attributeUsageArgs is not null)
        {
            attributeUsageArgs.Add("AllowMultiple = " + (allowMultiple ? "true" : "false"));
        }
        else if (allowMultiple)
        {
            attributes.Add((AttributeUsageName, ["global::System.AttributeTargets.All", "AllowMultiple = true"]));
        }

        foreach ((string attributeName, IReadOnlyList<string> attributeArgs) in attributes)
        {
            WriteAttribute(writer, attributeName, attributeArgs);
        }
    }

    /// <summary>
    /// Writes the projected form of the Windows Runtime <c>[Experimental]</c> attribute, on its own line.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    public static void WriteExperimentalAttribute(IndentedTextWriter writer)
    {
        WriteAttribute(writer, ProjectedExperimentalName, ProjectedExperimentalArgs);
    }

    /// <summary>
    /// Writes a single attribute application, on its own line.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="attributeName">The fully-qualified attribute name (without the <c>global::</c> prefix).</param>
    /// <param name="attributeArgs">The pre-formatted positional + named argument strings (in order).</param>
    private static void WriteAttribute(IndentedTextWriter writer, string attributeName, IReadOnlyList<string> attributeArgs)
    {
        writer.Write($"[global::{attributeName}");

        if (attributeArgs.Count > 0)
        {
            writer.Write("(");
            for (int i = 0; i < attributeArgs.Count; i++)
            {
                writer.WriteIf(i > 0, ", ");

                writer.Write(attributeArgs[i]);
            }
            writer.Write(")");
        }

        writer.WriteLine("]");
    }

    /// <summary>
    /// Writes a <c>[System.Obsolete]</c> attribute when <paramref name="member"/> is deprecated but
    /// not removed. Removed members are omitted from the projection entirely, so they get no attribute.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="member">The member to inspect for <c>[Windows.Foundation.Metadata.Deprecated]</c>.</param>
    public static void WriteObsoleteAttribute(IndentedTextWriter writer, IHasCustomAttribute member)
    {
        if (!member.IsDeprecatedNotRemoved)
        {
            return;
        }

        string? message = member.DeprecatedMessage;

        if (string.IsNullOrEmpty(message))
        {
            writer.WriteLine("[global::System.Obsolete]");
        }
        else
        {
            writer.WriteLine($"[global::System.Obsolete(@\"{EscapeVerbatimString(message)}\")]");
        }
    }

    /// <summary>
    /// Returns whether a Windows Runtime metadata attribute application should be carried over to the projection.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="ns">The namespace of the attribute type.</param>
    /// <param name="strippedName">The name of the attribute type, with any <c>Attribute</c> suffix removed.</param>
    /// <param name="isAttributeUsage">Whether the application is an <c>[AttributeUsage]</c> attribute.</param>
    /// <returns>Whether the attribute application should be emitted.</returns>
    /// <remarks>
    /// <para>
    /// <c>[AttributeUsage]</c> is carried over in both projection modes. It is not Windows Runtime
    /// metadata being preserved for tooling, but the .NET modeling of the attribute type's own usage
    /// contract: its <c>AllowMultiple</c> and <c>Inherited</c> arguments drive
    /// <c>Attribute.GetCustomAttributes(inherit: true)</c> semantics at runtime for user code applying a
    /// projected attribute. It is also negligible in size (34 applications across the entire Windows SDK).
    /// </para>
    /// <para>
    /// Every other metadata attribute is carried over into reference projections only. Implementation
    /// projections are only ever loaded at runtime, never compiled against (user code compiles against
    /// the reference projection instead), so attributes whose only consumers are compilers, analyzers
    /// and metadata tooling are pure dead weight there. Attribute blobs cannot be trimmed by ILLink or
    /// ILC either, so anything carried over is permanent, unremovable metadata in the shipped app.
    /// </para>
    /// </remarks>
    private static bool ShouldCarryOverAttribute(ProjectionEmitContext context, string ns, string strippedName, bool isAttributeUsage)
    {
        if (isAttributeUsage)
        {
            return true;
        }

        if (!context.Settings.ReferenceProjection)
        {
            return false;
        }

        // Metadata attributes without a projected form are always dropped. '[Experimental]' is listed
        // here as it is custom-mapped rather than carried over (see 'WriteCustomAttributes').
        if (ns == WindowsFoundationMetadata)
        {
            return strippedName is "ContractVersion" or "ApiContract" or "DefaultOverload" or "Overload" or "Experimental";
        }

        // Attributes from any other namespace (e.g. '[Microsoft.UI.Xaml.TemplatePart]') are themselves
        // projected types, so their applications are carried over as-is
        return true;
    }

    /// <summary>
    /// Writes the projected type-level custom attributes for <paramref name="type"/>. Each emitted
    /// attribute is on its own line, terminated with a newline. If no attributes apply, emits nothing.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type definition.</param>
    /// <param name="enablePlatformAttrib">Whether to also emit a <c>[SupportedOSPlatform]</c> attribute synthesized from any <c>[ContractVersion]</c>.</param>
    public static void WriteTypeCustomAttributes(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, bool enablePlatformAttrib)
    {
        WriteCustomAttributes(writer, context, type, enablePlatformAttrib);
        WriteObsoleteAttribute(writer, type);
    }

    /// <inheritdoc cref="WriteTypeCustomAttributes(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, bool)"/>
    /// <returns>A callback emitting each applicable attribute on its own line. The trailing newline of the last attribute is dropped so the callback can be interpolated into a multiline template line without producing a stray blank line.</returns>
    public static IndentedTextWriterCallback WriteTypeCustomAttributes(ProjectionEmitContext context, TypeDefinition type, bool enablePlatformAttrib)
    {
        return writer => WriteTypeCustomAttributesBody(writer, context, type, enablePlatformAttrib);
    }

    /// <summary>
    /// Writes just the attribute lines (no trailing newline on the last one) for
    /// <see cref="WriteTypeCustomAttributes(IndentedTextWriter, ProjectionEmitContext, TypeDefinition, bool)"/>.
    /// Used by the callback variant so the callback can be inlined inside a multiline raw-string
    /// template — the surrounding template line's own newline becomes the trailing newline.
    /// </summary>
    internal static void WriteTypeCustomAttributesBody(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, bool enablePlatformAttrib)
    {
        int before = writer.Length;

        WriteCustomAttributes(writer, context, type, enablePlatformAttrib);
        WriteObsoleteAttribute(writer, type);

        // If anything was written, the buffer ends with a trailing newline that came from the
        // last attribute's WriteLine. Trim it so the callback can be inlined into a multiline
        // template line without producing a stray blank line.
        if (writer.Length > before && writer.Length > 0 && writer.Back() == '\n')
        {
            writer.Remove(writer.Length - 1, 1);
        }
    }
}
