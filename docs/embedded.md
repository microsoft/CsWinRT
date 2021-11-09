# Embedded C#/WinRT Support

## Overview

**Note**: Embedded support is currently **in preview**. 

Embedded support is a C#/WinRT feature that allows you to compile the c# projection for the component, within the library/app itself. 

This is done by setting the build property `CsWinRTEmbedded` to `true` in the app or library's project file. 

Enabling embedded support compiles the sources for WinRT.Runtime with the project.
For any types from the component which you want to embed in your project, they must be specified using `CsWinRTIncludes`.
The specified types will be projected and embedded into the project's *.dll*. 
This means projects using embedded support no longer have a dependency on a projection for the component, `WinRT.Runtime.dll` or `Microsoft.Windows.SDK.NET.dll` in the case of the Windows SDK projection.

For an example, you can look at the [sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/TestEmbedded). 

Embedded support introduces new features to the C#/WinRT toolchain:
 * Decreased component/app size: The developer only needs to include the minimum necessary parts of the Windows SDK required by their component or app, reducing the size of the output .dll significantly.
 * Downlevel support: App developers on .NET 5+ can target Windows 7 (`net5.0-windows`) and light-up on Windows 10 
  (and Windows 11) when consuming an embedded .NET 5+ library. 
 * Removes the need for multi-targeting: Library authors can support all .NET Standard 2.0 app consumers, including .NET 5+, without the need for multi-targeting. App consumers are able to target any .NET Standard 2.0 compatible TFM (e.g. `netcoreapp3.1`, `net48`, and `net5.0-windows`).

It is important to remember that embedded support constrains the scope of Windows Runtime types to your binary. 

## Scenarios

This feature allows C# apps and libraries to target `net5.0` (and above), .NET Framework 4.6.1+, `netcoreapp3.1`, and `netstandard2.0` while also using the Windows SDK.
Moreover, a library can target `netstandard2.0` and be able to run on NetFX, Net Core and Net 5. 

Note: the `netstandard2.0` generated projection is not optimized for .NET 5 and won't be as performant. 

## Usage 

Using embedded support allows you to target non-Windows specific TFMs and older versions of .NET. 
You will need to set a few properties:
  * `CsWinRTEmbedded` - must be set to `true` to include the sources
  * `CsWinRTIncludes` - Specify the types to be projected; error `CS0246` will highlight any missing namespaces
  * `CsWinRTWindowsMetadata` - must be set to the Windows SDK release to use for Windows APIs
  * `LangVersion` - must be set to `9` because the CsWinRT generated sources use C# 9

Targeting `netstandard2.0` requires adding the following two package references:

```xml
<PackageReference Include="System.Memory" Version="4.5.4" />
<PackageReference Include="System.Runtime.Extensions" Version="4.3.1" />
```

Here is an example project file for a library cross-targeting and embedding a C#/WinRT generated projection.

```csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net6.0-windows;net5.0-windows;netstandard2.0</TargetFrameworks>
    <Platforms>x64;x86</Platforms>
  </PropertyGroup>

  <PropertyGroup>
    <LangVersion>9</LangVersion>
    <CsWinRTEmbedded>true</CsWinRTEmbedded>
    <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.CsWinRT" Version="$(CsWinRTVersion)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\path\CppwinrtComponent\CppwinrtComponent.vcxproj" />
  </ItemGroup> 
  
  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Memory" Version="4.5.4" />
    <PackageReference Include="System.Runtime.Extensions" Version="4.3.1" />
  </ItemGroup>
    
  <PropertyGroup>
    <CsWinRTIncludes>
      CppwinrtComponent;
      Windows.Devices.Geolocation;
      Windows.Foundation;
    </CsWinRTIncludes>
  </PropertyGroup>
 
</Project>
```

You will need to enable Windows Desktop Compatible for all your referenced C++/WinRT components -- either via the properties or in the `.vcxproj` file.

If you try to use an embedded projection of a native (C++/WinRT) component, via ProjectReference, then you may encounter a runtime error `CLASS_NOT_REGISTERED`.
You can fix this by unloading the native component project in Visual Studio, and adding the following:

```vcxproj
  <PropertyGroup Label="Configuration">
    <DesktopCompatible>true</DesktopCompatible>
  </PropertyGroup>
```

Alternatively, you can right-click the C++ project in Visual Studio, select Properties > General > Project Defaults,
 and set Windows Desktop Compatible to Yes for all configurations/platforms.

If you find any other issues using embedded support, please [file an issue]()!
