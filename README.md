[![Build Status](https://dev.azure.com/microsoft/Dart/_apis/build/status%2FCsWinRT%20GitHub%20CI?branchName=master)](https://dev.azure.com/microsoft/Dart/_build/latest?definitionId=112455&branchName=master)

# The C#/WinRT Language Projection

C#/WinRT provides Windows Runtime (WinRT) projection support for the C# language. A "projection" is an adapter that enables programming the WinRT APIs in a natural and familiar way for the target language. The C#/WinRT projection hides the details of interop between C# and WinRT interfaces, and provides mappings of many WinRT types to appropriate .NET equivalents, such as strings, URIs, common value types, and generic collections.  

WinRT APIs are defined in `*.winmd` format, and C#/WinRT includes tooling that generates C# code for consumption scenarios, or generates a `*.winmd` for authoring scenarios. Generated C# source code can be compiled into interop assemblies, similar to how [C++/WinRT](https://github.com/Microsoft/cppwinrt) generates headers for the C++ language projection. This means that neither the C# compiler nor the .NET Runtime require built-in knowledge of WinRT any longer.

## Motivation

[.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) is the focus for the .NET platform. It is an open-source, cross-platform runtime that can be used to build device, cloud, and IoT applications. Previous versions of .NET Framework and .NET Core have built-in knowledge of WinRT which is a Windows-specific technology. By lifting this projection support out of the compiler and runtime, we are supporting efforts to make .NET more efficient for .NET 5 onwards. 

[WinUI 3](https://github.com/Microsoft/microsoft-ui-xaml) is the effort to lift official native Microsoft UI controls and features out of the operating system, so app developers can use the latest controls and visuals on any in-market version of the OS. C#/WinRT is needed to support the changes required for lifting the XAML APIs out of Windows.UI.XAML and into Microsoft.UI.XAML.

However, C#/WinRT is a general effort and is intended to support other scenarios and versions of the .NET runtime. While our focus is on supporting .NET 5, we aspire to generate projections that are compatible down to .NET Standard 2.0. Please refer to our issues backlog for more information.

## What's New

See our [release notes](https://github.com/microsoft/CsWinRT/releases) for the latest C#/WinRT releases and corresponding .NET SDK versions. C#/WinRT runtime and Windows SDK projection updates typically become available in a future .NET SDK update, which follows a monthly release cadence. We also make updates to the C#/WinRT tool itself, which are shipped through the C#/WinRT NuGet package. Details on breaking changes and specific issues can be found in the releases notes.

## Using C#/WinRT

Download the C#/WinRT NuGet package here: https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/

You can also build a C#/WinRT package yourself from source: see our [Contributor's Guide](CONTRIBUTING.md) for more information on building the repo.

### Documentation

- [Usage guide](docs/usage.md) - usage guide for developers
- [C#/WinRT NuGet properties](nuget/readme.md) - documentation on customizing C#/WinRT NuGet package properties
- [Repository structure](docs/structure.md) - detailed breakdown of this repository
- [COM Interop guide](docs/interop.md) - for recommendations on migrating from System.Runtime.InteropServices

For additional documentation and walkthroughs, visit <http://aka.ms/cswinrt>.

### C#/WinRT Architecture

The C#/WinRT runtime assembly, `WinRT.Runtime.dll`, is required by all C#/WinRT assemblies.  It provides an abstraction layer over the .NET runtime, supporting .NET 5+. The runtime assembly implements several features for all projected C#/WinRT types, including WinRT activation, marshaling logic, and [COM wrapper](https://docs.microsoft.com/dotnet/standard/native-interop/com-wrappers) lifetime management.

## Contributing

The C#/WinRT team welcomes feedback and contributions! There are several ways to contribute to the project:

- **[File a new issue](https://github.com/microsoft/CsWinRT/issues/new/choose)**<br>
- **[Ask a question](https://github.com/microsoft/CsWinRT/discussions/categories/q-a)**<br>
- **[Start a discussion](https://github.com/microsoft/CsWinRT/discussions)**<br>
- **[Make a feature request](https://github.com/microsoft/CsWinRT/issues/new?assignees=&labels=enhancement&template=feature_request.md&title=)**<br>

We ask that **before you start work on a feature that you would like to contribute**, please read our [Contributor's Guide](CONTRIBUTING.md), which also includes steps on building the C#/WinRT repo.

## Related Projects

C#/WinRT is part of the [xlang](https://github.com/microsoft/xlang) family of projects that help developers create APIs that can run on multiple platforms and be used with a variety of languages. The mission of C#/WinRT is not to support cross-platform execution directly, but to support the cross-platform goals of .NET Core. 

C#/WinRT is also part of the [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) - a set of libraries, frameworks, components, and tools that you can use in your apps to access powerful platform functionality across many versions of Windows. The Windows App SDK combines Win32 native app capabilities with modern API usage techniques, so your apps light up everywhere your users are. The Windows App SDK also includes [WinUI](https://docs.microsoft.com/en-us/windows/apps/winui/), [WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/), [MSIX](https://docs.microsoft.com/en-us/windows/msix/overview), [C++/WinRT](https://github.com/microsoft/CppWinRT/), and [Rust/WinRT](https://github.com/microsoft/winrt-rs).

### License Info

Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
