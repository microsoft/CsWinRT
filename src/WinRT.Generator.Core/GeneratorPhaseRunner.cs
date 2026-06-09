// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

#pragma warning disable CS1573

namespace WindowsRuntime.Generator;

/// <summary>
/// Runs the body of a single generator phase, wrapping unexpected exceptions in the per-tool
/// <c>Unhandled*Exception</c> and optionally logging a progress message before the body runs.
/// </summary>
/// <typeparam name="TArgs">The per-tool args record (must implement <see cref="IGeneratorArgs"/>).</typeparam>
/// <param name="args">The parsed per-tool args, forwarded to every <see cref="RunPhase(string, Action{TArgs})"/> body.</param>
/// <param name="wrapUnhandled">Wraps an unexpected exception into the per-tool <c>Unhandled*Exception</c> with the given phase name.</param>
/// <param name="log">Logs a progress message to the user (typically <c>ConsoleApp.Log</c> from ConsoleAppFramework).</param>
internal readonly struct GeneratorPhaseRunner<TArgs>(
    TArgs args,
    Func<string, Exception, Exception> wrapUnhandled,
    Action<string> log)
    where TArgs : IGeneratorArgs
{
    /// <summary>
    /// Gets the parsed per-tool args, forwarded to every <see cref="RunPhase(string, Action{TArgs})"/> body.
    /// </summary>
    public TArgs Args => args;

    /// <summary>
    /// Runs <paramref name="body"/>, wrapping any unexpected exception in the per-tool
    /// <c>Unhandled*Exception</c> with <paramref name="phaseName"/> as the phase tag.
    /// </summary>
    /// <param name="phaseName">The phase name used by the per-tool <c>Unhandled*Exception</c>.</param>
    /// <param name="body">The body of the phase to run. The captured <see cref="Args"/> is forwarded as its argument.</param>
    public void RunPhase(string phaseName, Action<TArgs> body)
    {
        try
        {
            body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled(phaseName, e);
        }
    }

    /// <inheritdoc cref="RunPhase(string, Action{TArgs})"/>
    /// <param name="logMessage">The progress message to log before the body runs.</param>
    public void RunPhase(string phaseName, string logMessage, Action<TArgs> body)
    {
        try
        {
            log(logMessage);

            body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled(phaseName, e);
        }
    }

    /// <inheritdoc cref="RunPhase(string, Action{TArgs})"/>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, Func<TArgs, T> body)
    {
        try
        {
            return body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled(phaseName, e);
        }
    }

    /// <inheritdoc cref="RunPhase(string, Action{TArgs})"/>
    /// <param name="logMessage">The progress message to log before the body runs.</param>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, string logMessage, Func<TArgs, T> body)
    {
        try
        {
            log(logMessage);

            return body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled(phaseName, e);
        }
    }
}

