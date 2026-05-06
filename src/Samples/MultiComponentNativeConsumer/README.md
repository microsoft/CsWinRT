# MultiComponentNativeConsumer sample

This sample demonstrates a single C++ native consumer (`ConsoleApplication1.vcxproj`) using
**multiple CsWinRT-authored components simultaneously** via the new aggregator path:

- `ClassLibrary1\` — a Windows Runtime component with `ClassLibrary1.Class1` (Add/Multiply).
- `ClassLibrary2\` — a Windows Runtime component with `ClassLibrary2.Greeter` (Greet/Shout).
- `ConsoleApplication1\` — a C++/WinRT consumer that activates classes from BOTH components.

The CsWinRT NuGet package's `build\native\Microsoft.Windows.CsWinRT.targets` auto-imports
into the vcxproj, walks the resolved `<ProjectReference>` set for components, synthesizes a
temporary aggregator csproj that produces the merged `WinRT.Component.dll`, and copies the
generated DLLs into the consumer's output directory.

## JIT mode (default)

```cmd
msbuild ConsoleApp40.slnx /p:Configuration=Debug /p:Platform=x64 /restore
.\ConsoleApplication1\x64\Debug\ConsoleApplication1.exe
```

Expected output:

```
Hello, http://aka.ms/cppwinrt!
ClassLibrary1.Class1.Add(1, 2) = 3
ClassLibrary1.Class1.Multiply(3, 4) = 12
ClassLibrary2.Greeter.Greet(...) = Hello from ClassLibrary2, world!
ClassLibrary2.Greeter.Shout(...) = CAN YOU HEAR ME!!!
```

The deployed OutDir contains: the consumer exe, `ClassLibrary1.dll` / `ClassLibrary2.dll`
(per-component managed dlls), the merged `WinRT.Component.dll`, `WinRT.Interop.dll`,
`WinRT.Sdk.Projection.dll`, and the host bridge (`WinRT.Host.dll` / `WinRT.Host.Shim.dll`).
The activation manifest points at `WinRT.Host.dll`, which probes `WinRT.Component.dll` first
and dispatches via the merged `ABI.WinRT_Component.ManagedExports.GetActivationFactory`.

## AOT mode

```cmd
msbuild ConsoleApp40.slnx /p:Configuration=Release /p:Platform=x64 /p:PublishAot=true /restore
```

When `PublishAot=true` is set on the consumer vcxproj, the aggregator csproj is published
with `PublishAot=true` + `NativeLib=Shared` + `OutputType=Exe` (with stub Main +
`CustomNativeMain`) so ILC produces a single native `WinRT.Component.dll` containing every
component, projection .dll, and `WinRT.Runtime` statically linked. The native consumer's
manifest needs to be updated to point at `WinRT.Component.dll` directly (instead of
`WinRT.Host.dll`) for AOT builds.

> **Status:** the AOT *build* pipeline is fully wired up and produces a native
> `WinRT.Component.dll` with `DllGetActivationFactory` / `DllCanUnloadNow` PE exports.
> Runtime activation currently fails with `CLASS_E_CLASSNOTAVAILABLE` because ILC strips the
> cswinrt-emitted per-component `ManagedExports` types (referenced only via
> `[UnsafeAccessor]`). A separate trimming-preservation fix is needed for end-to-end AOT
> activation; this sample's AOT build is included as a reference for that follow-up work.

## What you need to run this

- A NuGet feed pointing at a CsWinRT 3.0 prerelease build of `Microsoft.Windows.CsWinRT`
  (configured via `nuget.config` or your global feed list).
- VS 2022 17.13+ with the .NET 10 SDK preview.
