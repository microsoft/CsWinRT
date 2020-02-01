[![Build status](https://dev.azure.com/microsoft/Dart/_apis/build/status/CsWinRT%20Nuget)](https://dev.azure.com/microsoft/Dart/_build/latest?definitionId=45187)

# The C#/WinRT Language Projection

C#/WinRT provides packaged WinRT projection support for the C# language.  It is compatible with .NET Standard 2.0 and later and does not require any built-in knowledge of WinRT by the CLR or C# compiler.  

C#/WinRT is part of the [xlang](https://github.com/microsoft/xlang) family of projects that help developers create APIs that can run on multiple platforms and be used with a variety of languages.

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

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo should be cloned into the root of the C#/WinRT repo so that the cswinrt.sln can resolve the reference to TestComponent.vcxproj.  The resulting TestComponent.dll,winmd files are consumed by the UnitTest project above.


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
