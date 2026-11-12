// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using TestComponent;
using Windows.Data.Json;
using Windows.Globalization;
using Windows.Networking.Connectivity;
using WindowsRuntime.InteropServices;

namespace ActivationFactoryReconnection;

// Validates that cached activation factories for servers in another process recover after that process dies. The activation
// factories are provided by 'FakeOutOfProcessServer', which simulates proxies to servers that can be killed: all their proxies
// then fail with a given 'HRESULT' ('RPC_S_SERVER_UNAVAILABLE' by default), like real COM proxies do. The expected behavior is
// that the call observing the dead server fails (it's never retried), and that the following call uses a new activation factory.
internal static unsafe class Program
{
    private static int Main()
    {
        WindowsRuntimeActivationFactory.SetWindowsRuntimeActivationHandler(FakeOutOfProcessServer.GetActivationFactory);

        // Default constructor ('IActivationFactory', shared activation helper in 'WindowsRuntimeObject')
        CheckReconnection(ServerClass.JsonObject, static () => new JsonObject().Stringify() == "{}", 110);

        // Factory constructor (activation factory callback)
        CheckReconnection(ServerClass.Language, static () => new Language("en-US").LanguageTag == "en-US", 120);

        // Static method
        CheckReconnection(ServerClass.JsonValue, static () => JsonValue.CreateNumberValue(42).GetNumber() == 42, 130);

        // Static property (the activation factory is not agile here, so this also covers context-aware object references)
        CheckReconnection(ServerClass.ApplicationLanguages, static () => ApplicationLanguages.PrimaryLanguageOverride == "fake-language", 140);

        // Static event
        CheckStaticEventReconnection(150);

        // All 'HRESULT'-s indicating that the server is definitely gone must disconnect the activation factory
        CheckDisconnectedErrorCode(FakeOutOfProcessServer.RPC_E_DISCONNECTED, 160);
        CheckDisconnectedErrorCode(FakeOutOfProcessServer.RPC_E_SERVER_DIED, 165);
        CheckDisconnectedErrorCode(FakeOutOfProcessServer.RPC_E_SERVER_DIED_DNE, 170);
        CheckDisconnectedErrorCode(FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE, 175);
        CheckDisconnectedErrorCode(FakeOutOfProcessServer.RPC_S_CALL_FAILED_DNE, 180);

        // 'RPC_S_CALL_FAILED' doesn't mean that the server is gone (eg. static event handlers registered through it
        // could still be raised), so it must not disconnect the activation factory
        CheckCallFailedErrorCodeDoesNotDisconnect(185);

        // A dead server for a class must not cause the (healthy) activation factories for other classes to be resolved again
        CheckHealthyActivationFactoriesAreReused(195);

        // Context-aware activation factories must not be reused from another context (eg. an STA thread)
        CheckContextAwareActivationFactory(205);

        // Composable constructors (parameterless, shared activation helper in 'WindowsRuntimeObject', and parameterized,
        // activation factory callback), both using the same cached activation factory
        CheckReconnection(ServerClass.Composable, static () => new Composable().Value == 0, 210);
        CheckReconnection(ServerClass.Composable, static () => new Composable(42).Value == 42, 220);

        return 100;
    }

    private static void CheckReconnection(ServerClass serverClass, Func<bool> callback, int exitCode)
    {
        // The first call resolves the activation factory (unless another call already did), the following ones must reuse it
        int activationFactoryCount = Math.Max(FakeOutOfProcessServer.GetActivationFactoryCount(serverClass), 1);
        int disconnectedCallCount = FakeOutOfProcessServer.GetDisconnectedCallCount(serverClass);

        Check(callback(), exitCode);
        Check(callback(), exitCode + 1);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(serverClass) == activationFactoryCount, exitCode + 2);

        FakeOutOfProcessServer.Kill(serverClass);

        // The call observing the dead server must fail exactly once (no retries), without resolving a new activation factory
        Check(Throws(callback, FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE), exitCode + 3);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(serverClass) == disconnectedCallCount + 1, exitCode + 4);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(serverClass) == activationFactoryCount, exitCode + 5);

        // The next call must resolve a new activation factory (ie. "start" a new server), and all following calls must reuse it
        Check(callback(), exitCode + 6);
        Check(callback(), exitCode + 7);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(serverClass) == activationFactoryCount + 1, exitCode + 8);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(serverClass) == disconnectedCallCount + 1, exitCode + 9);
    }

    private static void CheckStaticEventReconnection(int exitCode)
    {
        NetworkStatusChangedEventHandler handler1 = static _ => { };
        NetworkStatusChangedEventHandler handler2 = static _ => { };

        NetworkInformation.NetworkStatusChanged += handler1;

        Check(FakeOutOfProcessServer.NetworkStatusChangedHandlerCount == 1, exitCode);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.NetworkInformation) == 1, exitCode + 1);

        FakeOutOfProcessServer.Kill(ServerClass.NetworkInformation);

        // Removing the last handler unregisters the event, which fails because the server is dead
        Check(Throws(() => NetworkInformation.NetworkStatusChanged -= handler1, FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE), exitCode + 2);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.NetworkInformation) == 1, exitCode + 3);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.NetworkInformation) == 1, exitCode + 4);

        // Adding a handler must resolve a new activation factory, and register the event with it
        NetworkInformation.NetworkStatusChanged += handler2;

        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.NetworkInformation) == 2, exitCode + 5);
        Check(FakeOutOfProcessServer.NetworkStatusChangedHandlerCount == 2, exitCode + 6);

        // Removing a handler that was added through the old activation factory is a no-op (the new one doesn't know about it)
        NetworkInformation.NetworkStatusChanged -= handler1;

        Check(FakeOutOfProcessServer.NetworkStatusChangedHandlerCount == 2, exitCode + 7);

        // Removing the new handler must unregister the event from the new activation factory
        NetworkInformation.NetworkStatusChanged -= handler2;

        Check(FakeOutOfProcessServer.NetworkStatusChangedHandlerCount == 1, exitCode + 8);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.NetworkInformation) == 1, exitCode + 9);
    }

    private static void CheckDisconnectedErrorCode(int hresult, int exitCode)
    {
        int activationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue);
        int disconnectedCallCount = FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.JsonValue);

        FakeOutOfProcessServer.FailureHResult = hresult;
        FakeOutOfProcessServer.Kill(ServerClass.JsonValue);

        Check(Throws(CreateNumberValue, hresult), exitCode);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue) == activationFactoryCount, exitCode + 1);

        // The activation factory must have been marked as disconnected, so a new one must be used for the next call
        Check(CreateNumberValue(), exitCode + 2);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue) == activationFactoryCount + 1, exitCode + 3);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.JsonValue) == disconnectedCallCount + 1, exitCode + 4);

        FakeOutOfProcessServer.FailureHResult = FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE;
    }

    private static void CheckCallFailedErrorCodeDoesNotDisconnect(int exitCode)
    {
        int activationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue);
        int disconnectedCallCount = FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.JsonValue);

        FakeOutOfProcessServer.FailureHResult = FakeOutOfProcessServer.RPC_S_CALL_FAILED;
        FakeOutOfProcessServer.Kill(ServerClass.JsonValue);

        // The activation factory must not be marked as disconnected, so the same one must be used again for the next call
        Check(Throws(CreateNumberValue, FakeOutOfProcessServer.RPC_S_CALL_FAILED), exitCode);
        Check(Throws(CreateNumberValue, FakeOutOfProcessServer.RPC_S_CALL_FAILED), exitCode + 1);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue) == activationFactoryCount, exitCode + 2);
        Check(FakeOutOfProcessServer.GetDisconnectedCallCount(ServerClass.JsonValue) == disconnectedCallCount + 2, exitCode + 3);

        // A following failure with a 'HRESULT' indicating that the server is gone must still disconnect the activation factory
        FakeOutOfProcessServer.FailureHResult = FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE;

        Check(Throws(CreateNumberValue, FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE), exitCode + 4);
        Check(CreateNumberValue(), exitCode + 5);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue) == activationFactoryCount + 1, exitCode + 6);
    }

    private static void CheckHealthyActivationFactoriesAreReused(int exitCode)
    {
        int jsonValueActivationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue);
        int jsonObjectActivationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonObject);
        int languageActivationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.Language);

        FakeOutOfProcessServer.Kill(ServerClass.Language);

        Check(Throws(static () => new Language("en-US").LanguageTag == "en-US", FakeOutOfProcessServer.RPC_S_SERVER_UNAVAILABLE), exitCode);

        // The activation factories for the other classes are still alive, so they must not be resolved again
        Check(CreateNumberValue(), exitCode + 1);
        Check(new JsonObject().Stringify() == "{}", exitCode + 2);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonValue) == jsonValueActivationFactoryCount, exitCode + 3);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.JsonObject) == jsonObjectActivationFactoryCount, exitCode + 4);

        // Only the activation factory for the class whose server died must be resolved again
        Check(new Language("en-US").LanguageTag == "en-US", exitCode + 5);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.Language) == languageActivationFactoryCount + 1, exitCode + 6);
    }

    private static void CheckContextAwareActivationFactory(int exitCode)
    {
        int activationFactoryCount = FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.ApplicationLanguages);
        bool isPrimaryLanguageOverrideValid = false;

        Thread thread = new(() => isPrimaryLanguageOverrideValid = ApplicationLanguages.PrimaryLanguageOverride == "fake-language");

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        // The activation factory cached by the main thread is not in the context of the STA thread, so a new one must be resolved
        Check(isPrimaryLanguageOverrideValid, exitCode);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.ApplicationLanguages) == activationFactoryCount + 1, exitCode + 1);

        // Same in the opposite direction, when going back to the main thread (and the new activation factory must then be reused)
        Check(ApplicationLanguages.PrimaryLanguageOverride == "fake-language", exitCode + 2);
        Check(ApplicationLanguages.PrimaryLanguageOverride == "fake-language", exitCode + 3);
        Check(FakeOutOfProcessServer.GetActivationFactoryCount(ServerClass.ApplicationLanguages) == activationFactoryCount + 2, exitCode + 4);
    }

    private static bool CreateNumberValue()
    {
        return JsonValue.CreateNumberValue(42).GetNumber() == 42;
    }

    private static bool Throws(Func<bool> callback, int hresult)
    {
        try
        {
            _ = callback();

            return false;
        }
        catch (Exception exception)
        {
            return exception.HResult == hresult;
        }
    }

    private static bool Throws(Action callback, int hresult)
    {
        try
        {
            callback();

            return false;
        }
        catch (Exception exception)
        {
            return exception.HResult == hresult;
        }
    }

    private static void Check(bool condition, int exitCode)
    {
        if (!condition)
        {
            Console.WriteLine($"Check failed with exit code {exitCode}.");

            Environment.Exit(exitCode);
        }
    }
}
