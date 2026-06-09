// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.Generator;

/// <summary>
/// Runs the body of a single generator phase, wrapping unexpected exceptions in the per-tool
/// <c>Unhandled*Exception</c> and optionally logging a progress message before the body runs.
/// </summary>
/// <typeparam name="TArgs">The per-tool args record (must implement <see cref="IGeneratorArgs"/>).</typeparam>
/// <remarks>
/// Each generator's <c>Run</c> method historically wrapped every phase (loading, processing, emit, ...)
/// in an identical <c>try</c>/<c>catch</c> that re-threw as the per-tool <c>Unhandled*Exception</c>
/// (with the phase name as the constructor argument). <see cref="GeneratorPhaseRunner{TArgs}"/> captures
/// the parsed <typeparamref name="TArgs"/> instance plus the per-tool <c>wrapUnhandled</c> and <c>log</c>
/// delegates (it is returned bound to all three by <see cref="GeneratorHost.CreateRunner{TArgs}"/>) and
/// lets each phase call site collapse to a single <see cref="RunPhase(string, Action{TArgs})"/> (or
/// overload) invocation. The captured <typeparamref name="TArgs"/> is forwarded to every body delegate,
/// so phases that only depend on it can be expressed as a <c>static</c> lambda (or method group) with
/// zero per-call allocations. Per-tool exception identity is fully preserved because the original
/// <c>wrapUnhandled</c> delegate is invoked unchanged.
/// </remarks>
internal readonly struct GeneratorPhaseRunner<TArgs>
    where TArgs : IGeneratorArgs
{
    /// <summary>
    /// The per-tool <c>wrapUnhandled</c> delegate.
    /// </summary>
    private readonly Func<string, Exception, Exception> _wrapUnhandled;

    /// <summary>
    /// The per-tool progress logger.
    /// </summary>
    private readonly Action<string> _log;

    /// <summary>
    /// Creates a new <see cref="GeneratorPhaseRunner{TArgs}"/> bound to the given args and per-tool delegates.
    /// </summary>
    /// <param name="args">The parsed per-tool args, forwarded to every <see cref="RunPhase(string, Action{TArgs})"/> body.</param>
    /// <param name="wrapUnhandled">Wraps an unexpected exception into the per-tool <c>Unhandled*Exception</c> with the given phase name.</param>
    /// <param name="log">Logs a progress message to the user (typically <c>ConsoleApp.Log</c> from ConsoleAppFramework).</param>
    internal GeneratorPhaseRunner(TArgs args, Func<string, Exception, Exception> wrapUnhandled, Action<string> log)
    {
        Args = args;
        _wrapUnhandled = wrapUnhandled;
        _log = log;
    }

    /// <summary>
    /// Gets the parsed per-tool args, forwarded to every <see cref="RunPhase(string, Action{TArgs})"/> body.
    /// </summary>
    public TArgs Args { get; }

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
            throw _wrapUnhandled(phaseName, e);
        }
    }

    /// <summary>
    /// Logs <paramref name="logMessage"/> and then runs <paramref name="body"/>, wrapping any
    /// unexpected exception in the per-tool <c>Unhandled*Exception</c> with <paramref name="phaseName"/>
    /// as the phase tag.
    /// </summary>
    /// <param name="phaseName">The phase name used by the per-tool <c>Unhandled*Exception</c>.</param>
    /// <param name="logMessage">The progress message to log before the body runs.</param>
    /// <param name="body">The body of the phase to run. The captured <see cref="Args"/> is forwarded as its argument.</param>
    public void RunPhase(string phaseName, string logMessage, Action<TArgs> body)
    {
        try
        {
            _log(logMessage);
            body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw _wrapUnhandled(phaseName, e);
        }
    }

    /// <summary>
    /// Runs <paramref name="body"/> and returns its result, wrapping any unexpected exception
    /// in the per-tool <c>Unhandled*Exception</c> with <paramref name="phaseName"/> as the phase tag.
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="body"/>.</typeparam>
    /// <param name="phaseName">The phase name used by the per-tool <c>Unhandled*Exception</c>.</param>
    /// <param name="body">The body of the phase to run. The captured <see cref="Args"/> is forwarded as its argument.</param>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, Func<TArgs, T> body)
    {
        try
        {
            return body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw _wrapUnhandled(phaseName, e);
        }
    }

    /// <summary>
    /// Logs <paramref name="logMessage"/>, then runs <paramref name="body"/> and returns its result,
    /// wrapping any unexpected exception in the per-tool <c>Unhandled*Exception</c> with
    /// <paramref name="phaseName"/> as the phase tag.
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="body"/>.</typeparam>
    /// <param name="phaseName">The phase name used by the per-tool <c>Unhandled*Exception</c>.</param>
    /// <param name="logMessage">The progress message to log before the body runs.</param>
    /// <param name="body">The body of the phase to run. The captured <see cref="Args"/> is forwarded as its argument.</param>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, string logMessage, Func<TArgs, T> body)
    {
        try
        {
            _log(logMessage);
            return body(Args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw _wrapUnhandled(phaseName, e);
        }
    }
}

