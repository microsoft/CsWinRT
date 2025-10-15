<h1 align="center">C#/WinRT Language Projection</h1>

<p align="center">
  <a style="text-decoration:none" href="https://www.nuget.org/packages/Microsoft.Windows.CsWinRT">
    <img src="https://img.shields.io/nuget/v/Microsoft.Windows.CsWinRT" alt="NuGet badge" /></a>
</p>

C#/WinRT provides **Windows Runtime (WinRT)** projection support for the C# language. A *projection* is an adapter that lets developers use WinRT APIs naturally in their language of choice. The C#/WinRT projection abstracts interop details and maps WinRT types to their .NET equivalents, such as strings, URIs, value types, and generic collections.

C#/WinRT includes tools that:
- Generate C# source for consuming WinRT APIs from `*.winmd` files
- Generate `*.winmd` files for authoring WinRT components

Generated C# source can be compiled into interop assemblies, similar to how [C++/WinRT](https://github.com/Microsoft/cppwinrt) produces headers for C++. This design removes the need for the C# compiler or .NET runtime to have built-in WinRT knowledge.

## Getting started with C#/WinRT

- [Usage guide](docs/usage.md)
- [Customizing C#/WinRT](nuget/readme.md)
- [NativeAOT and Trimming support](docs/aot-trimming.md)
- [Authoring C#/WinRT components](docs/authoring.md)
- [About WinRT.Host.dll](docs/hosting.md)
- [C#/WinRT version history](docs/version-history.md)
- [Repository structure](docs/structure.md)
- [COM Interop guide](docs/interop.md)
- Related projects
    - [xlang](https://github.com/microsoft/xlang)
    - [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)

For additional documentation and walkthroughs, visit http://aka.ms/cswinrt.

## Usage

Install package from [NuGet](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT).

Or you can build from the source code. See our [Contributor's Guide](CONTRIBUTING.md) for more information on build instructions.

## Contributing

We welcome feedback and contributions! We ask that **before you start work on a feature that you would like to contribute**, please read our [Contributor's Guide](CONTRIBUTING.md).

## Motivation

Earlier versions of .NET Framework and Core included built-in WinRT projection support within the compiler and runtime, but this was a Windows-specific feature and conflicted with its cross-platform goals. To decouple WinRT support and keep .NET platform-agnostic, that functionality was removed and reimplemented as a standalone project in this repo.

[WinUI 3](https://github.com/Microsoft/microsoft-ui-xaml) separates Microsoft’s native UI controls from the OS so developers can use the latest modern UI features across supported Windows versions.
C#/WinRT enables this by providing the necessary projection for `Windows.UI.Xaml` and `Microsoft.UI.Xaml` APIs.

However, C#/WinRT is a general effort and is intended to support other scenarios and versions of the .NET runtime. While our focus is on supporting .NET 10 and later, we aspire to generate projections that are compatible down to .NET Standard 2.0. Please refer to our issues backlog for more information.

## Architecture Overview

All C#/WinRT assemblies depend on `WinRT.Runtime.dll`, which provides:
- A runtime abstraction layer for .NET 10 and later
- Support for all projected C#/WinRT types (e.g., WinRT activation and marshaling)
- Lifetime management of [COM wrappers](https://docs.microsoft.com/dotnet/standard/native-interop/com-wrappers)

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
