# Event source cache disconnection: issue and PR drafts

## Issue draft

**Title:** Bug: EventSourceCache propagates disconnected weak-reference errors after an out-of-process server exits

### Description

`EventSourceCache.GetState` and `EventSourceCache.Update` resolve their cached
`IWeakReference` to determine whether the target is still alive. They handle a
null result but not a disconnected COM server. `Resolve` can instead throw
`RPC_E_DISCONNECTED` (`0x80010108`) or
`HRESULT_FROM_WIN32(RPC_S_SERVER_UNAVAILABLE)` (`0x800706BA`).

The cache is keyed by the native interface pointer. If a new object's interface
pointer reuses an existing cache key after server termination, accessing the old
weak reference can prevent event registration on the new object. A WinGet CI
failure reached `EventSourceCache.GetState` during a new configuration processor's
diagnostics subscription and failed with `0x800706BA`.

### Steps To Reproduce

Use the test-only commit `2e9bc2eb5d54e567e9d646cba86cd1e7adce2311` from
`JohnMcPMS/CsWinRT`, branch `event-source-inv`. It adds the repro without changing
the runtime.

From a clean checkout of that commit on Windows, run:

```powershell
$solutionDir = Join-Path (Get-Location).Path "src\"
dotnet test .\src\Tests\EventSourceCacheRepro\EventSourceCacheRepro.csproj `
    -c Release -p:Platform=x64 -p:IsDotnetBuild=true `
    "-p:SolutionDir=$solutionDir" -p:LibBuildTFMs=net8.0 `
    --logger "console;verbosity=normal"
```

Four cases fail: lookup and replacement for each of the two disconnected-server
HRESULTs. Seven control cases pass.

The repro references the actual `WinRT.Runtime` project. It uses reflection to
create a single private cache entry and injects an `IWeakReference` whose
`Resolve` throws the selected COM error. It does not modify the global cache,
require native test components, launch a COM server, or rely on timing and
pointer reuse. This isolates the cache's error-handling defect; it is not an
end-to-end reproduction of server restart.

### Expected Behavior

Disconnected-server errors should invalidate the cached target in the same way
as a null result. Lookup should return no cached state, and replacement should
install the new target and clear the old states. Other resolution failures,
including `E_FAIL` and `E_ACCESSDENIED`, should still propagate.

### Version Info

- Affected source: `c28d756c4f042cf573d269c6ac39de05087e86c8`, also present in CsWinRT 2.3.1.
- Repro environment: Windows x64, OS build `10.0.28665.0`.
- .NET SDK `10.0.403`; tests execute on .NET `8.0.33`.
- Verified with both the `net8.0` and `netstandard2.0` runtime implementations.
- The focused repro does not need Windows SDK metadata or the `TestWinRT` checkout.

### Additional Context

The motivating failure was in WinGet build 392855 on October 30, 2026:

https://dev.azure.com/shine-oss/winget-cli/_build/results?buildId=392855

Its stack includes `IWeakReference.Resolve`, `EventSourceCache.GetState`, and
`ConfigurationProcessor.add_Diagnostics`. This defect does **not** establish the
cause of the separate COM activation timeout in WinGet build 393006.

A fix and regression tests are implemented on `JohnMcPMS/CsWinRT` branch
`event-source-inv`; I intend to submit the fix.

## PR draft

**Title:** Treat disconnected event source cache targets as expired

### Changes

Share the target-liveness check between `EventSourceCache.GetState` and
`EventSourceCache.Update`. Treat only `RPC_E_DISCONNECTED` and
`HRESULT_FROM_WIN32(RPC_S_SERVER_UNAVAILABLE)` as an expired target. This lets
lookup return no cached state and lets replacement install a fresh target and
clear stale registrations. Preserve live-target behavior and propagate unrelated
errors.

Add 11 regression cases to the existing `UnitTest` project and a small
`EventSourceCacheRepro` project that runs those same tests without the native test
components. The test-only commit preserves a deterministic failing baseline.

### Validation

From the repository root:

```powershell
$solutionDir = Join-Path (Get-Location).Path "src\"
dotnet test .\src\Tests\EventSourceCacheRepro\EventSourceCacheRepro.csproj `
    -c Release -p:Platform=x64 -p:IsDotnetBuild=true `
    "-p:SolutionDir=$solutionDir" -p:LibBuildTFMs=net8.0 `
    --logger "console;verbosity=normal"

dotnet test .\src\Tests\EventSourceCacheRepro\EventSourceCacheRepro.csproj `
    -c Release -p:Platform=x64 -p:IsDotnetBuild=true `
    "-p:SolutionDir=$solutionDir" -p:LibBuildTFMs=netstandard2.0 `
    --logger "console;verbosity=normal"

git diff --check
```

- Before the fix: 4 failed, 7 passed against `net8.0`.
- After the fix: 11 passed against `net8.0`; 11 passed against `netstandard2.0`.
- `git diff --check`: passed.
- .NET Standard build emits the existing SDK `NETSDK1212` trimming warning.
- Full native/projection solution tests were not run.
- An additional .NET 9 runtime build could not restore `Microsoft.NET.ILLink.Tasks`
  9.0.22: the repository feed returned HTTP 401 and NuGet.org returned a TLS
  handshake failure. No .NET 9 validation result is claimed.

The deterministic tests inject the disconnected weak-reference failure rather
than reproduce an entire COM server lifecycle. No public API or projection
generation changes are required.
