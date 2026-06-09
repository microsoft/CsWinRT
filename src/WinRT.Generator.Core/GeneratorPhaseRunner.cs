// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.Generator;

/// <summary>
/// Runs the body of a single generator phase, wrapping unexpected exceptions in the per-tool
/// <c>Unhandled*Exception</c> and optionally logging a progress message before the body runs.
/// </summary>
/// <remarks>
/// Each generator's <c>Run</c> method historically wrapped every phase (loading, processing, emit, ...)
/// in an identical <c>try</c>/<c>catch</c> that re-threw as the per-tool <c>Unhandled*Exception</c>
/// (with the phase name as the constructor argument). <see cref="GeneratorPhaseRunner"/> captures
/// the per-tool <c>wrapUnhandled</c> and <c>log</c> delegates once (it is returned bound to them by
/// <see cref="GeneratorHost.Prepare{TArgs}"/>) and lets each phase call site collapse to a single
/// <see cref="RunPhase(string, Action)"/> (or overload) invocation. Per-tool exception identity
/// is fully preserved because the original <c>wrapUnhandled</c> delegate is invoked unchanged.
/// </remarks>
internal readonly struct GeneratorPhaseRunner
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
    /// Creates a new <see cref="GeneratorPhaseRunner"/> bound to the given per-tool delegates.
    /// </summary>
    /// <param name="wrapUnhandled">Wraps an unexpected exception into the per-tool <c>Unhandled*Exception</c> with the given phase name.</param>
    /// <param name="log">Logs a progress message to the user (typically <c>ConsoleApp.Log</c> from ConsoleAppFramework).</param>
    internal GeneratorPhaseRunner(Func<string, Exception, Exception> wrapUnhandled, Action<string> log)
    {
        _wrapUnhandled = wrapUnhandled;
        _log = log;
    }

    /// <summary>
    /// Runs <paramref name="body"/>, wrapping any unexpected exception in the per-tool
    /// <c>Unhandled*Exception</c> with <paramref name="phaseName"/> as the phase tag.
    /// </summary>
    /// <param name="phaseName">The phase name used by the per-tool <c>Unhandled*Exception</c>.</param>
    /// <param name="body">The body of the phase to run.</param>
    public void RunPhase(string phaseName, Action body)
    {
        try
        {
            body();
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
    /// <param name="body">The body of the phase to run.</param>
    public void RunPhase(string phaseName, string logMessage, Action body)
    {
        try
        {
            _log(logMessage);
            body();
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
    /// <param name="body">The body of the phase to run.</param>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, Func<T> body)
    {
        try
        {
            return body();
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
    /// <param name="body">The body of the phase to run.</param>
    /// <returns>The value returned by <paramref name="body"/>.</returns>
    public T RunPhase<T>(string phaseName, string logMessage, Func<T> body)
    {
        try
        {
            _log(logMessage);
            return body();
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw _wrapUnhandled(phaseName, e);
        }
    }
}
