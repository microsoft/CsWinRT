[![Build status](https://dev.azure.com/microsoft/Dart/_apis/build/status/CsWinRT%20Nuget)](https://dev.azure.com/microsoft/Dart/_build/latest?definitionId=45187)

# The C#/WinRT Language Projection

C#/WinRT provides WinRT projection support for the C# language. The projection allows the .NET runtime to interop with the Windows Runtime (WinRT). Windows Runtime APIs are defined in `*.winmd` format, and cswinrt includes tooling that generates .NET Standard 2.0 C# code that can be compiled into interop assemblies, similar to how [cppwinrt](https://github.com/Microsoft/cppwinrt) generates headers for the C++ language projection. This means that neither the C# compiler nor the .NET Runtime no longer need to have built-in knowledge of WinRT.

C#/WinRT is part of the [xlang](https://github.com/microsoft/xlang) family of projects that help developers create APIs that can run on multiple platforms and be used with a variety of languages. 

### Motivation
.NET Core is the focus for the .NET platform. It is an open-source, cross-platform runtime that and can be used to build device, cloud, and IoT applications. Previous versions of .NET Framework and .NET Core have built-in knowledge of WinRT which is a windows specific techology. By lifting this projection support out of the compiler and runtime, we are supporting efforts to make .NET more efficient for its .NET5 release. More information about .NET can be found at https://docs.microsoft.com/en-us/dotnet/core/

[WinUI3.0](https://github.com/Microsoft/microsoft-ui-xaml) is the effort to lift official native Microsoft UI controls and features out of the operating system, so app developers can use the latest controls and visuals on any in-market version of the OS. C#/WinRT is needed to support the changes required for lifting the XAML APIs out of Windows.UI.XAML and into Microsoft.UI.XAML.

### Goals
1. Create tooling that enables .net developers to consume `.winmds`
    - includes adding a reference to winmd 
    - includes component library authors to produce projection assemblies to redist with their libraries.
2. Maintain compat with the existing .NET projection
3. Production support as required for activation contract support (TBD)

#### Non-Goals
1. Cross platform implementation of an api 
2. Does not define how the project system will use the projection 

### Release
C#/WinRT will release with NET5, and be consumed by WinUI3.0. It will ship as a Nuget package on nuget.org.

# Structure
The C#/WinRT compiler and unit tests are all contained within the Visual Studio 2019 solution file, \cswinrt\cswinrt.sln.  

## /cswinrt

The **/cswinrt** folder contains the sources and cswinrt.vcxproj project file for building the C#/WinRT compiler, cswinrt.exe.  The projection's base library is contained in /cswinrt/strings/WinRT.cs, which is processed by /strings.props to generate string literals contained in the compiler.

The compiler uses the [WinMD NuGet package](http://aka.ms/winmd/nuget) for parsing [ECMA-335 metadata](http://www.ecma-international.org/publications/standards/Ecma-335.htm) files.  The WinMD github repo includes a [winmd.natvis](https://github.com/microsoft/winmd/blob/master/vs/winmd.natvis) script for debugging metadata parsing.  A symlink can be used to install the script:
  > for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\winmd.natvis" "c:\git\winmd\vs\winmd.natvis" 
  
The cswinrt project also contains a cswinrt.natvis script for debugging the C# projection writing, which can also be installed with a symlink:
> for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\cswinrt.natvis" "c:\git\cswinrt\cswinrt\cswinrt.natvis"

See also [Deploying .natvis files](https://docs.microsoft.com/en-us/visualstudio/debugger/create-custom-views-of-native-objects?view=vs-2015#BKMK_natvis_location).

## /nuget

The **/nuget** folder contains source files for producing a C#/WinRT NuGet package, which is regularly built, signed, and published to nuget.org by Microsoft.

## /testcomp

The **/testcomp** folder contains an implementation of a WinRT test component, defined in class.idl and used by the UnitTest project.

## /unittest

The **/unittest** folder contains unit tests for validating the projection generated for the TestComp project (above), for the TestWinRT\TestComponent project (below), and for foundational parts of the Windows SDK.  All pull requests should ensure that this project executes without errors.

## /winuitest

The **/winuitest** folder contains a smoke test for generating and building a complete Windows SDK and WinUI projection, ensuring that most edge cases are validated.  All pull requests should ensure that this project builds without errors.

## /TestWinRT

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo should be cloned into the root of the C#/WinRT repo, via get_testwinrt.cmd, so that the cswinrt.sln can resolve the reference to TestComponent.vcxproj.  The resulting TestComponent.dll,winmd files are consumed by the UnitTest project above.


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
