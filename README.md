<h1 align="center">C#/WinRT Language Projection</h1>

<p align="center">
  <a style="text-decoration:none" href="https://www.nuget.org/packages/Microsoft.Windows.CsWinRT">
    <img src="https://img.shields.io/nuget/v/Microsoft.Windows.CsWinRT" alt="NuGet badge" /></a>
</p>

C#/WinRT provides **Windows Runtime (WinRT) API** projection support for C#. A *projection* is an adapter that lets developers use WinRT APIs naturally in their language of choice. The C#/WinRT projection abstracts interop details and maps WinRT types to their .NET equivalents, such as strings, URIs, value types, and generic collections.

C#/WinRT includes tools that:
- Generate C# source for consuming WinRT APIs from `*.winmd` files
- Generate `*.winmd` files for authoring WinRT components

Generated C# source can be compiled into interop assemblies, similar to how [C++/WinRT](https://github.com/Microsoft/cppwinrt) produces headers for C++. This design removes the need for the C# compiler or .NET runtime to have built-in WinRT support.

## Getting started

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

## Contributing

We welcome feedback and contributions! We ask that **before you start work on a feature that you would like to contribute**, please read our [Contributor's Guide](CONTRIBUTING.md).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).

For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
