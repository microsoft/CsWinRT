# Embedded C#/WinRT Support

## Overview

Embedded support is a C#/WinRT feature that allows you to have the WinRT layer of your projection compiled into the projection itself. This is done by setting the build property `CsWinRTEmbedded` to `true` in the projection's project file. 

Enabling embedded support brings in the WinRT.Runtime sources to the project and automatically sets them to be compiled with the project. And any types that will be projected via `CsWinRTIncludes` will be embedded into the project .dll as well. This means projects using embedded support no longer have a dependency on `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll`. 

For an example, you can look at the [sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/TestEmbedded). 

## Scenarios

This feature allows C# apps and libraries to target `net5.0`, `netcoreapp3.1`, and `netstandard2.0` while also using Windows 10 APIs.
Moreover, a library can target `netstandard2.0` and be able to run on NetFX, Net Core and Net 5. 

## Usage 

Using embedded support allows you to target non-Windows TFMs and older version of .NET. 
To target You will need to set a few properties:
  * `LangVersion` - must be set to `9` because the WinRT.Runtime sources use C# 9
  * `CsWinRTEmbedded` - must be set to `true` to include the sources
  * `CsWinRTWindowsMetadata` - must be set to the Windows 10 release to use for Windows APIs

Targeting `netstandard2.0` requires adding a few package references, in order to use APIs that have been deprecated. 

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
when the component is consumed via `ProjectReference`. You will need to edit the .vcxproj file of all the components by enabling Windows Desktop Compatible. 
This can be done with the following code

```vcxproj
  <PropertyGroup Label="Configuration">
    <DesktopCompatible>true</DesktopCompatible>
  </PropertyGroup>
```

