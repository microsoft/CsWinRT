// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// General-purpose helpers from C++ <c>helpers.h</c> and <c>code_writers.h</c>.
/// </summary>
internal static class Helpers
{
    private static readonly HashSet<string> s_csharpKeywords = new(System.StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue",
        "decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally",
        "fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock","long",
        "namespace","new","null","object","operator","out","override","params","private","protected","public",
        "readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string","struct","switch",
        "this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using","virtual","void",
        "volatile","while"
    };

    /// <summary>Mirrors C++ <c>is_keyword</c>.</summary>
    public static bool IsKeyword(string s) => s_csharpKeywords.Contains(s);

    /// <summary>Mirrors C++ <c>write_escaped_identifier</c>: prefix C# keywords with @.</summary>
    public static void WriteEscapedIdentifier(TextWriter w, string identifier)
    {
        if (IsKeyword(identifier))
        {
            w.Write("@");
        }
        w.Write(identifier);
    }

    /// <summary>Mirrors C++ <c>internal_accessibility</c>.</summary>
    public static string InternalAccessibility(Settings settings) =>
        settings.Internal || settings.Embedded ? "internal" : "public";

    /// <summary>Strip everything from a backtick onwards (C++ <c>write_code</c> behavior for type names).</summary>
    public static string StripBackticks(string typeName)
    {
        int idx = typeName.IndexOf('`');
        return idx >= 0 ? typeName.Substring(0, idx) : typeName;
    }

    /// <summary>Returns true if the type has the named CustomAttribute.</summary>
    public static bool HasAttribute(IHasCustomAttribute member, string ns, string name)
        => TypeCategorization.HasAttribute(member, ns, name);

    /// <summary>Returns the matching CustomAttribute, or null.</summary>
    public static CustomAttribute? GetAttribute(IHasCustomAttribute member, string ns, string name)
        => TypeCategorization.GetAttribute(member, ns, name);

    /// <summary>Returns true if the InterfaceImpl is the [Default] interface.</summary>
    public static bool IsDefaultInterface(InterfaceImplementation impl)
        => HasAttribute(impl, "Windows.Foundation.Metadata", "DefaultAttribute");

    /// <summary>Returns true if the InterfaceImpl is [Overridable].</summary>
    public static bool IsOverridable(InterfaceImplementation impl)
        => HasAttribute(impl, "Windows.Foundation.Metadata", "OverridableAttribute");

    /// <summary>True if a method is the special "remove_xxx" event remover (mirrors C++ <c>is_remove_overload</c>).</summary>
    public static bool IsRemoveOverload(MethodDefinition m)
        => m.IsSpecialName && (m.Name?.Value?.StartsWith("remove_", System.StringComparison.Ordinal) == true);

    /// <summary>Method has [NoExceptionAttribute] or is a remove overload.</summary>
    public static bool IsNoExcept(MethodDefinition m)
        => IsRemoveOverload(m) || HasAttribute(m, "Windows.Foundation.Metadata", "NoExceptionAttribute");

    /// <summary>Property has [NoExceptionAttribute].</summary>
    public static bool IsNoExcept(PropertyDefinition p)
        => HasAttribute(p, "Windows.Foundation.Metadata", "NoExceptionAttribute");

    /// <summary>Mirrors C++ <c>get_default_interface</c>: returns the [Default] interface.</summary>
    public static ITypeDefOrRef? GetDefaultInterface(TypeDefinition type)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (IsDefaultInterface(impl) && impl.Interface is not null)
            {
                return impl.Interface;
            }
        }
        return null;
    }

    /// <summary>Mirrors C++ <c>get_property_methods</c>: returns (getter, setter) for a property.</summary>
    public static (MethodDefinition? Getter, MethodDefinition? Setter) GetPropertyMethods(PropertyDefinition prop)
    {
        return (prop.GetMethod, prop.SetMethod);
    }

    /// <summary>Mirrors C++ <c>get_event_methods</c>: returns (add, remove) for an event.</summary>
    public static (MethodDefinition? Add, MethodDefinition? Remove) GetEventMethods(EventDefinition evt)
    {
        return (evt.AddMethod, evt.RemoveMethod);
    }

    /// <summary>Mirrors C++ <c>get_delegate_invoke</c>: returns the Invoke method of a delegate type.</summary>
    public static MethodDefinition? GetDelegateInvoke(TypeDefinition type)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            if (m.IsSpecialName && m.Name == "Invoke")
            {
                return m;
            }
        }
        return null;
    }

    /// <summary>Get the (uint32_t arg) value out of a [ContractVersionAttribute] (mirrors C++ <c>get_contract_version</c>).</summary>
    public static int? GetContractVersion(TypeDefinition type)
    {
        CustomAttribute? attr = GetAttribute(type, "Windows.Foundation.Metadata", "ContractVersionAttribute");
        if (attr is null) { return null; }
        // C++ reads index 1 - the second positional arg
        if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 1)
        {
            object? v = attr.Signature.FixedArguments[1].Element;
            if (v is uint u) { return (int)u; }
            if (v is int i) { return i; }
        }
        return null;
    }

    /// <summary>Get the (uint32_t arg) value out of a [VersionAttribute] (mirrors C++ <c>get_version</c>).</summary>
    public static int? GetVersion(TypeDefinition type)
    {
        CustomAttribute? attr = GetAttribute(type, "Windows.Foundation.Metadata", "VersionAttribute");
        if (attr is null) { return null; }
        if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 0)
        {
            object? v = attr.Signature.FixedArguments[0].Element;
            if (v is uint u) { return (int)u; }
            if (v is int i) { return i; }
        }
        return null;
    }

    /// <summary>Mirrors C++ <c>has_default_constructor</c>.</summary>
    public static bool HasDefaultConstructor(TypeDefinition type)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            if (m.IsRuntimeSpecialName && m.Name == ".ctor" && m.Parameters.Count == 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Mirrors C++ <c>is_constructor</c>.</summary>
    public static bool IsConstructor(MethodDefinition m)
        => m.IsRuntimeSpecialName && m.Name == ".ctor";

    /// <summary>Mirrors C++ <c>is_special</c>.</summary>
    public static bool IsSpecial(MethodDefinition m)
        => m.IsSpecialName || m.IsRuntimeSpecialName;
}

/// <summary>
/// Mirrors C++ <c>method_signature</c>: enumerates parameters and return value of a method.
/// </summary>
internal sealed class MethodSig
{
    public MethodDefinition Method { get; }
    public List<ParamInfo> Params { get; }
    public ParameterDefinition? ReturnParam { get; }

    public MethodSig(MethodDefinition method) : this(method, null) { }

    public MethodSig(MethodDefinition method, AsmResolver.DotNet.Signatures.GenericContext? genCtx)
    {
        Method = method;
        Params = new List<ParamInfo>(method.Parameters.Count);
        // The return parameter is the one with sequence 0 (if any)
        ReturnParam = null;
        foreach (ParameterDefinition p in method.ParameterDefinitions)
        {
            if (p.Sequence == 0)
            {
                ReturnParam = p;
                break;
            }
        }

        // Iterate signature parameters
        if (method.Signature is MethodSignature sig)
        {
            _substitutedReturnType = genCtx is not null && sig.ReturnType is not null
                ? sig.ReturnType.InstantiateGenericTypes(genCtx.Value)
                : sig.ReturnType;
            for (int i = 0; i < sig.ParameterTypes.Count; i++)
            {
                TypeSignature pt = sig.ParameterTypes[i];
                if (genCtx is not null) { pt = pt.InstantiateGenericTypes(genCtx.Value); }
                Params.Add(new ParamInfo(method.Parameters[i], pt));
            }
        }
    }

#pragma warning disable IDE0032 // Use auto property — manual backing field needed for substituted return type
    private readonly TypeSignature? _substitutedReturnType;
#pragma warning restore IDE0032

    public TypeSignature? ReturnType => _substitutedReturnType is TypeSignature t &&
                                        t is not CorLibTypeSignature { ElementType: ElementType.Void }
                                          ? _substitutedReturnType
                                          : null;

    public string ReturnParamName(string defaultName = "__return_value__")
        => ReturnParam?.Name?.Value ?? defaultName;
}

/// <summary>One param: links the parameter definition to its signature type.</summary>
internal sealed record ParamInfo(Parameter Parameter, TypeSignature Type);

/// <summary>Param category mirroring C++ <c>param_category</c>.</summary>
internal enum ParamCategory
{
    In,
    Ref,
    Out,
    PassArray,
    FillArray,
    ReceiveArray,
}

/// <summary>Helpers for parameter analysis.</summary>
internal static class ParamHelpers
{
    public static ParamCategory GetParamCategory(ParamInfo p)
    {
        bool isArray = p.Type is SzArrayTypeSignature;
        bool isOut = p.Parameter.Definition?.IsOut == true;
        bool isIn = p.Parameter.Definition?.IsIn == true;
        // Check both the captured signature type and the parameter's own type (handles cases where
        // the signature is wrapped in a ByReferenceTypeSignature only on one side after substitution).
        // Also peel custom modifiers (e.g. modreq[InAttribute]) which can hide a ByRef beneath.
        bool isByRef = IsByRefType(p.Type) || IsByRefType(p.Parameter.ParameterType);
        if (isArray)
        {
            if (isIn) { return ParamCategory.PassArray; }
            if (isByRef) { return ParamCategory.ReceiveArray; }
            return ParamCategory.FillArray;
        }
        if (isOut) { return ParamCategory.Out; }
        if (isByRef) { return ParamCategory.Ref; }
        return ParamCategory.In;
    }

    private static bool IsByRefType(TypeSignature? sig)
    {
        // Strip custom modifiers (e.g. modreq[InAttribute] or modopt[IsExternalInit]) before checking byref.
        TypeSignature? cur = sig;
        while (cur is CustomModifierTypeSignature cm)
        {
            cur = cm.BaseType;
        }
        return cur is ByReferenceTypeSignature;
    }
}
