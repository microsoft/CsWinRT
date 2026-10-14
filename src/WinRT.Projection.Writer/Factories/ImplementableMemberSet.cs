// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// How a member an interface requires is satisfied on an implementable base.
/// </summary>
internal enum ImplementableMemberResolution
{
    /// <summary>
    /// No member with that signature exists yet, so it has to be declared.
    /// </summary>
    Declare,

    /// <summary>
    /// A public member with the same signature exists (declared here or inherited), which implements the interface member implicitly.
    /// </summary>
    Implemented,

    /// <summary>
    /// A protected member with the same signature exists, so the interface member needs an explicit implementation forwarding to it.
    /// </summary>
    Forward,

    /// <summary>
    /// A member with the same signature exists, but with a different type, or without an accessor the interface member requires.
    /// C# cannot declare both, so the base cannot be generated.
    /// </summary>
    Conflict
}

/// <summary>
/// Tracks the members declared on an implementable base (including the ones it inherits from the bases of its base classes),
/// so that every interface member is satisfied by exactly one author-facing member.
/// </summary>
/// <remarks>
/// <para>
/// Interfaces declared in their .NET form (see <see cref="MappedInterfaceAbstractMemberFactory"/>) and ordinary Windows Runtime
/// interfaces can require the same member (e.g. <c>Dispose()</c>), and an author-facing member can be <see langword="protected"/>,
/// in which case it cannot implement an interface member implicitly. Members are therefore keyed by their projected C# signature
/// (name and parameter types, without parameter names), which is what decides whether two declarations would collide.
/// </para>
/// <para>
/// Explicit interface implementations are tracked separately: each interface requiring a member that is satisfied by a protected
/// member gets its own one, even when they share the author-facing member.
/// </para>
/// </remarks>
internal sealed class ImplementableMemberSet
{
    /// <summary>
    /// The return type text used to reserve a member name that must not be declared.
    /// </summary>
    private const string ReservedType = "<reserved>";

    /// <summary>
    /// The declared methods, keyed by name and parameter types.
    /// </summary>
    private readonly Dictionary<string, (string ReturnType, bool IsProtected)> _methods = new(StringComparer.Ordinal);

    /// <summary>
    /// The declared properties and indexers, keyed by name (or <c>this[&lt;index type&gt;]</c>).
    /// </summary>
    private readonly Dictionary<string, (string Type, bool HasGetter, bool HasSetter, bool IsProtected)> _properties = new(StringComparer.Ordinal);

    /// <summary>
    /// The declared events, keyed by name.
    /// </summary>
    private readonly Dictionary<string, (string Type, bool IsProtected)> _events = new(StringComparer.Ordinal);

    /// <summary>
    /// The explicit interface implementations written so far.
    /// </summary>
    private readonly HashSet<string> _explicitImplementations = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether a member conflict was found, meaning the base cannot be generated.
    /// </summary>
    public bool HasConflict { get; private set; }

    /// <summary>
    /// Records that a member conflict was found.
    /// </summary>
    public void MarkConflict()
    {
        HasConflict = true;
    }

    /// <summary>
    /// Resolves a method an interface requires, declaring it if no method with its signature exists yet.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="parameterList">The projected parameter list, as declared (e.g. <c>int index, out string value</c>).</param>
    /// <param name="returnType">The projected return type.</param>
    /// <param name="isProtected">Whether the method would be declared <see langword="protected"/>.</param>
    /// <returns>How the method is satisfied.</returns>
    public ImplementableMemberResolution ResolveMethod(string name, string parameterList, string returnType, bool isProtected)
    {
        string key = GetMethodKey(name, parameterList);
        string normalizedReturnType = NormalizeType(returnType);

        if (!_methods.TryGetValue(key, out (string ReturnType, bool IsProtected) existing))
        {
            _methods.Add(key, (normalizedReturnType, isProtected));

            return ImplementableMemberResolution.Declare;
        }

        if (existing.ReturnType != normalizedReturnType)
        {
            HasConflict = true;

            return ImplementableMemberResolution.Conflict;
        }

        return existing.IsProtected ? ImplementableMemberResolution.Forward : ImplementableMemberResolution.Implemented;
    }

    /// <summary>
    /// Resolves a property (or indexer) an interface requires, declaring it if no property with its name exists yet.
    /// </summary>
    /// <param name="name">The property name (or <c>this[&lt;index type&gt;]</c> for an indexer).</param>
    /// <param name="type">The projected property type.</param>
    /// <param name="hasGetter">Whether a getter is required.</param>
    /// <param name="hasSetter">Whether a setter is required.</param>
    /// <param name="isProtected">Whether the property would be declared <see langword="protected"/>.</param>
    /// <returns>How the property is satisfied.</returns>
    public ImplementableMemberResolution ResolveProperty(string name, string type, bool hasGetter, bool hasSetter, bool isProtected)
    {
        string normalizedType = NormalizeType(type);

        if (!_properties.TryGetValue(name, out (string Type, bool HasGetter, bool HasSetter, bool IsProtected) existing))
        {
            _properties.Add(name, (normalizedType, hasGetter, hasSetter, isProtected));

            return ImplementableMemberResolution.Declare;
        }

        // An existing property cannot gain an accessor, so it only satisfies interfaces needing a subset of the ones it has
        if (existing.Type != normalizedType || (hasGetter && !existing.HasGetter) || (hasSetter && !existing.HasSetter))
        {
            HasConflict = true;

            return ImplementableMemberResolution.Conflict;
        }

        return existing.IsProtected ? ImplementableMemberResolution.Forward : ImplementableMemberResolution.Implemented;
    }

    /// <summary>
    /// Resolves an event an interface requires, declaring it if no event with its name exists yet.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="type">The projected event handler type.</param>
    /// <param name="isProtected">Whether the event would be declared <see langword="protected"/>.</param>
    /// <returns>How the event is satisfied.</returns>
    public ImplementableMemberResolution ResolveEvent(string name, string type, bool isProtected)
    {
        string normalizedType = NormalizeType(type);

        if (!_events.TryGetValue(name, out (string Type, bool IsProtected) existing))
        {
            _events.Add(name, (normalizedType, isProtected));

            return ImplementableMemberResolution.Declare;
        }

        if (existing.Type != normalizedType)
        {
            HasConflict = true;

            return ImplementableMemberResolution.Conflict;
        }

        return existing.IsProtected ? ImplementableMemberResolution.Forward : ImplementableMemberResolution.Implemented;
    }

    /// <summary>
    /// Reserves a method and a property name that an emitted member occupies in metadata without declaring them in C# (e.g. the
    /// <c>Item</c> property and <c>get_Item</c> method behind an indexer).
    /// </summary>
    /// <param name="name">The member name to reserve.</param>
    /// <param name="parameterList">The parameter list of the method to reserve.</param>
    public void Reserve(string name, string parameterList)
    {
        _ = _properties.TryAdd(name, (ReservedType, true, true, false));
        _ = _methods.TryAdd(GetMethodKey(name, parameterList), (ReservedType, false));
    }

    /// <summary>
    /// Records an explicit interface implementation, if it has not been written yet.
    /// </summary>
    /// <param name="key">A key identifying the interface member being implemented.</param>
    /// <returns>Whether the explicit implementation still has to be written.</returns>
    public bool TryAddExplicitImplementation(string key)
    {
        return _explicitImplementations.Add(key);
    }

    /// <summary>
    /// Gets the key identifying a method by its name and parameter types (including modifiers, but not parameter names).
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="parameterList">The projected parameter list.</param>
    /// <returns>The key for the method.</returns>
    public static string GetMethodKey(string name, string parameterList)
    {
        StringBuilder builder = new();

        _ = builder.Append(name).Append('(');

        int depth = 0;
        int start = 0;
        bool isFirst = true;

        void AppendParameterType(int end)
        {
            string parameter = parameterList[start..end].Trim();

            if (parameter.Length == 0)
            {
                return;
            }

            // Drop the parameter name, which is the last token
            int nameStart = parameter.LastIndexOf(' ');
            string parameterType = nameStart < 0 ? parameter : parameter[..nameStart];

            _ = builder.Append(isFirst ? "" : ",").Append(NormalizeType(parameterType));

            isFirst = false;
        }

        for (int i = 0; i < parameterList.Length; i++)
        {
            switch (parameterList[i])
            {
                case '<' or '(' or '[':
                    depth++;
                    break;
                case '>' or ')' or ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    AppendParameterType(i);
                    start = i + 1;
                    break;
            }
        }

        AppendParameterType(parameterList.Length);

        return builder.Append(')').ToString();
    }

    /// <summary>
    /// Normalizes the text of a projected type, so equivalent types written differently compare equal.
    /// </summary>
    private static string NormalizeType(string type)
    {
        return type.Replace("global::", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
    }
}
