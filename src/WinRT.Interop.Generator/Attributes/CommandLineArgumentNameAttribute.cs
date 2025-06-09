// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.Attributes;

/// <summary>
/// An attribute indicating the name of a given command line argument.
/// </summary>
/// <param name="name">The command line argument name.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class CommandLineArgumentNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the command line argument name.
    /// </summary>
    public string Name { get; } = name;
}
