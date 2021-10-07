# Embedded C#/WinRT Support

## Overview

Embedded support is a C#/WinRT feature that allows you to have the WinRT layer of your projection compiled into the projection itself. This is done by setting the build property `CsWinRTEmbedded` to `true` in the app or library's project file. 

Enabling embedded support brings in the WinRT.Runtime sources to the project and automatically sets them to be compiled with the project.
Any self-contained usage of Windows SDK types must be specified using the property `CsWinRTIncludes`. The specified types will be projected and embedded into the project's *.dll*. This means projects using embedded support no longer have a dependency on `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll`.

For an example, you can look at the [sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/TestEmbedded). 

## Scenarios

This feature allows C# apps and libraries to target `net5.0` (and above), .NET Framework 4.6.1+,`netcoreapp3.1`, and `netstandard2.0` while also using the Windows SDK.
Moreover, a library can target `netstandard2.0` and be able to run on NetFX, Net Core and Net 5. 

## Usage 

Using embedded support allows you to target non-Windows TFMs and older versions of .NET. 
You will need to set a few properties:
  * `CsWinRTEmbedded` - must be set to `true` to include the sources
  * `CsWinRTWindowsMetadata` - must be set to the Windows SDK release to use for Windows APIs
  * `LangVersion` - must be set to `9` because the WinRT.Runtime sources use C# 9

Targeting `netstandard2.0` requires adding three package references, in order to use APIs that have been deprecated. 

```xml
<PackageReference Include="System.Memory" Version="4.5.4" />
<PackageReference Include="System.Runtime.Extensions" Version="4.3.1" />
<PackageReference Include="System.Numerics.Vectors" Version="4.4.0" />
```

Here is an example project file for a library cross-targeting and embedding WinRT support.

```csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net6.0;net5.0;netcoreapp3.1;netstandard2.0</TargetFrameworks>
    <Platforms>x64;x86</Platforms>
  </PropertyGroup>

  <PropertyGroup>
    <LangVersion>9</LangVersion>
    <CsWinRTEmbedded>true</CsWinRTEmbedded>
    <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
  </PropertyGroup>

  <ItemGroup>
    <!-- either use the private produced by local build, or find a way to simulate cswinrt nuget reference -->
    <PackageReference Include="Microsoft.Windows.CsWinRT" Version="1.3.6-prerelease.211004.13" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\path\CppwinrtComponent\CppwinrtComponent.vcxproj" />
  </ItemGroup> 
  
  <ItemGroup Condition="'$(TargetFramework)' == 'netstandard2.0'">
    <PackageReference Include="System.Memory" Version="4.5.4" />
    <PackageReference Include="System.Runtime.Extensions" Version="4.3.1" />
    <PackageReference Include="System.Numerics.Vectors" Version="4.4.0" />
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

There is a known issue consuming C++/WinRT authored WinRT Components in a library using embedded support 
when the component is consumed via `ProjectReference`. 
You will need to edit the .vcxproj file of all the components by enabling Windows Desktop Compatible. 

If you try to use an embedded projection of a native (C++/WinRT) component, via ProjectReference, then you may encounter a runtime error `CLASS_NOT_REGISTERED`.
You can fix this by unloading the native component project in Visual Studio, and adding the following:

```vcxproj
  <PropertyGroup Label="Configuration">
    <DesktopCompatible>true</DesktopCompatible>
  </PropertyGroup>
```
If you find any other issues using embedded support, please [file an issue]()!
