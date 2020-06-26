[![Build status](https://dev.azure.com/microsoft/Dart/_apis/build/status/cswinrt%20Nuget)](https://dev.azure.com/microsoft/Dart/_build/latest?definitionId=45187)

# The C#/WinRT Language Projection

See also:
* Documentation: http://aka.ms/cswinrt
* NuGet package: http://aka.ms/cswinrt/nuget

C#/WinRT provides Windows Runtime (WinRT) projection support for the C# language. A "projection" is an adapter that enables programming the WinRT APIs in a natural and familiar way for the target language. For example, the C#/WinRT projection hides the details of interop between C# and WinRT interfaces, and provides mappings of many WinRT types to appropriate .NET equivalents, such as strings, URIs, common value types, and generic collections.  

WinRT APIs are defined in `*.winmd` format, and C#/WinRT includes tooling that generates .NET Standard 2.0 C# code that can be compiled into interop assemblies, similar to how [C++/WinRT](https://github.com/Microsoft/cppwinrt) generates headers for the C++ language projection. This means that neither the C# compiler nor the .NET Runtime require built-in knowledge of WinRT any longer.

C#/WinRT is part of the [xlang](https://github.com/microsoft/xlang) family of projects that help developers create APIs that can run on multiple platforms and be used with a variety of languages. The mission of C#/WinRT is not to support cross-platform execution directly, but to support the cross-platform goals of .NET Core. 

C#/WinRT is also part of [Project Reunion](https://github.com/microsoft/ProjectReunion) - a set of libraries, frameworks, components, and tools that you can use in your apps to access powerful platform functionality across many versions of Windows. Project Reunion combines Win32 native app capabilities with modern API usage techniques, so your apps light up everywhere your users are. Project Reunion also includes [WinUI](https://docs.microsoft.com/en-us/windows/apps/winui/), [WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/), [MSIX](https://docs.microsoft.com/en-us/windows/msix/overview), [C++/WinRT](https://github.com/microsoft/CppWinRT/), and [Rust/WinRT](https://github.com/microsoft/winrt-rs).

### Motivation
.NET Core is the focus for the .NET platform. It is an open-source, cross-platform runtime that can be used to build device, cloud, and IoT applications. Previous versions of .NET Framework and .NET Core have built-in knowledge of WinRT which is a Windows-specific technology. By lifting this projection support out of the compiler and runtime, we are supporting efforts to make .NET more efficient for its .NET 5 release. More information about .NET can be found at https://docs.microsoft.com/en-us/dotnet/core/

[WinUI3.0](https://github.com/Microsoft/microsoft-ui-xaml) is the effort to lift official native Microsoft UI controls and features out of the operating system, so app developers can use the latest controls and visuals on any in-market version of the OS. C#/WinRT is needed to support the changes required for lifting the XAML APIs out of Windows.UI.XAML and into Microsoft.UI.XAML.

However, C#/WinRT is a general effort and is intended to support other scenarios and versions of the .NET runtime, compatible down to .NET Standard 2.0.

# Usage
The [C#/WinRT NuGet Package](https://aka.ms/cswinrt/nuget) provides distinct support for both WinRT component projects and application projects.

## Component Project
A component project adds a NuGet reference to C#/WinRT to invoke cswinrt.exe at build time, generate projection sources, and compile these into an interop assembly. For an example of this, see the [Test Projection](https://github.com/microsoft/CsWinRT/blob/master/Projections/Test/Test.csproj). Command line options can be displayed by running **cswinrt -?**.  The interop assembly is then typically distributed as a NuGet package itself. 

## Application Project
An application project adds NuGet references to both the component interop assembly produced above, and to C#/WinRT to include the winrt.runtime assembly. If a third party WinRT component is distributed without an official interop assembly, an application project may add a reference to C#/WinRT to generate its own private component interop assembly.  There are versioning concerns related to this scenario, so the preferred solution is for the third party to publish an interop assembly directly.

### Sample
The following msbuild project fragment demonstrates a simple invocation of cswinrt to generate projection sources for types in the Contoso namespace.  These sources are then included in the project build.

```
  <Target Name="GenerateProjection">
    <PropertyGroup>
      <CsWinRTParams>
# This sample demonstrates using a response file for cswinrt execution.
# Run "cswinrt -h" to see all command line options.
-verbose
# Include Windows SDK metadata to satisfy references to 
# Windows types from project-specific metadata.
-in 10.0.18362.0
# Don't project referenced Windows types, as these are 
# provided by the Windows interop assembly.
-exclude Windows 
# Reference project-specific winmd files, defined elsewhere,
# such as from a NuGet package.
-in @(ContosoWinMDs->'"%(FullPath)"', ' ')
# Include project-specific namespaces/types in the projection
-include Contoso 
# Write projection sources to the "Generated Files" folder,
# which should be excluded from checkin (e.g., .gitignored).
-out "$(IntermediateOutputPath)/Generated Files"
      </CsWinRTParams>
    </PropertyGroup>
    <WriteLinesToFile
        File="$(CsWinRTResponseFile)" Lines="$(CsWinRTParams)"
        Overwrite="true" WriteOnlyWhenDifferent="true" />
    <Message Text="$(CsWinRTCommand)" Importance="$(CsWinRTVerbosity)" />
    <Exec Command="$(CsWinRTCommand)" />
  </Target>

  <Target Name="IncludeProjection" BeforeTargets="CoreCompile" DependsOnTargets="GenerateProjection">
    <ItemGroup>
      <Compile Include="$(IntermediateOutputPath)/Generated Files/*.cs" Exclude="@(Compile)" />
    </ItemGroup>
  </Target>
```
# Building

C#/WinRT currently requires the following packages to build:
- Visual Studio 16.6 (more specifically, MSBuild 16.6.0 for "net5.0" TFM support)
- .NET 5 SDK 5.0.100-preview.5.20279.10
- WinUI 3 3.0.0-preview1.200515.3 

**Note:** As prereleases may make breaking changes before final release, any other combinations above may work but are not supported and will generate a build warning.

The build.cmd script takes care of all related configuration steps and is the simplest way to get started building C#/WinRT. The build script is intended to be executed from a Visual Studio Developer command prompt.  It installs prerequisites such as nuget and the .NET 5 SDK, configures the environment to use .NET 5 (creating a global.json if necessary), builds the compiler, and builds and executes the unit tests.

After a successful command-line build, the cswinrt.sln can be launched from the same command prompt, to inherit the necessary environment. 

**Note:**  By default, projection projects only generate source files for Release configurations, where cswinrt.exe can execute in seconds.  To generate projection sources for Debug configurations, set the project property GenerateTestProjection to 'true'.  In either case, existing projection sources under the "Generated Files" folder will still be compiled into the projection assembly.  This configuration permits a faster inner loop in Visual Studio.

# Structure
The C#/WinRT compiler and unit tests are all contained within the Visual Studio 2019 solution file, \cswinrt\cswinrt.sln.  

## /cswinrt

The **/cswinrt** folder contains the sources and cswinrt.vcxproj project file for building the C#/WinRT compiler, cswinrt.exe.  The projection's base library is contained in /cswinrt/strings/WinRT.cs, which is processed by /strings.props to generate string literals contained in the compiler.

The compiler uses the [WinMD NuGet package](http://aka.ms/winmd/nuget) for parsing [ECMA-335 metadata](http://www.ecma-international.org/publications/standards/Ecma-335.htm) files.  The WinMD github repo includes a [winmd.natvis](https://github.com/microsoft/winmd/blob/master/vs/winmd.natvis) script for debugging metadata parsing.  A symlink can be used to install the script:
  > for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\winmd.natvis" "c:\git\winmd\vs\winmd.natvis" 
  
The C#/WinRT project also contains a cswinrt.natvis script for debugging the C# projection writing, which can also be installed with a symlink:
> for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\cswinrt.natvis" "c:\git\cswinrt\cswinrt\cswinrt.natvis"

See also [Deploying .natvis files](https://docs.microsoft.com/en-us/visualstudio/debugger/create-custom-views-of-native-objects?view=vs-2015#BKMK_natvis_location).

## /WinRT.Runtime

The **/WinRT.Runtime** folder contains the WinRT.Runtime project for building the C#/WinRT runtime assembly, winrt.runtime.dll.  There are two versions of this assembly, providing an abstraction layer over both .NET Standard 2.0 and .NET 5 runtimes.  While all code generated by cswinrt.exe is compatible with .NET Standard 2.0, there are runtime differences which must be addressed.  The primary difference is that the .NET 5 winrt.runtime.dll provides Xaml reference tracking support, which is necessary for WinUI 3 applications to manage memory correctly.  Future performance enhancements, such as function pointer support, may also be limited to the .NET 5 winrt.runtime.dll.

## /nuget

The **/nuget** folder contains source files for producing a C#/WinRT NuGet package, which is regularly built, signed, and published to nuget.org by Microsoft.  The C#/WinRT NuGet package contains the cswinrt.exe compiler, and both versions of the winrt.runtime.dll.

## /TestWinRT

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo should be cloned into the root of the C#/WinRT repo, via **get_testwinrt.cmd**, so that the cswinrt.sln can resolve its reference to TestComponent.vcxproj.  The resulting TestComponent.dll and TestComponent.winmd files are consumed by the UnitTest project above.

## /TestComponentCSharp

The **/TestComponentCSharp** folder contains an implementation of a WinRT test component, defined in class.idl and used by the UnitTest project.  To complement the general TestComponent above, the TestComponentCSharp  tests scenarios specific to the C#/WinRT language projection.

## /Projections

The **/Projections** folder contains several projects for generating and building projections from the Windows SDK, WinUI, and Test metadata (produced by the TestWinRT and TestComponentCSharp projects).

## /UnitTest

The **/UnitTest** folder contains unit tests for validating the Windows SDK, WinUI, and Test projections generated above.  All pull requests should ensure that this project executes without errors.

## /WinUIDesktopSample

The **/WinUIDesktopSample** contains an end-to-end sample app that uses the Windows SDK and WinUI projections generated above.

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
