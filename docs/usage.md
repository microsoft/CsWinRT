# Usage

The [C#/WinRT NuGet package](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/) provides tooling for the following scenarios. For more information on using the NuGet package, refer to the [NuGet documentation](../nuget/readme.md).

## Generate and distribute an interop assembly

A projection project adds a NuGet reference to C#/WinRT to invoke cswinrt.exe at build time, generate projection sources, and compile these into an interop assembly. For an example of this, see the [Projection Sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/Net5ProjectionSample) and associated [walkthrough](https://docs.microsoft.com/windows/uwp/csharp-winrt/net-projection-from-cppwinrt-component). Command line options can be displayed by running **cswinrt -?**.  The interop assembly is then typically distributed as a NuGet package itself for application projects to use.

### Projection Project 

To invoke cswinrt to generate projection sources for types in the **Contoso** namespace, you need to add the following property to your C# library project file. In this project you would also need to reference the CsWinRT NuGet package and the project-specific `.winmd` files you want to project, whether through a NuGet package, project reference or direct reference. By default, the **Windows** and **Microsoft** namespaces are not projected.

```  
<PropertyGroup>
  <CsWinRTIncludes>Contoso</CsWinRTIncludes>
</PropertyGroup>
```

To further customize C#/WinRT behavior, refer to the [CsWinRT NuGet documentation](../nuget/readme.md).

### Application Project

An interop assembly is typically distributed as a NuGet package for application projects to reference. This package will require a dependency on C#/WinRT to include `WinRT.Runtime.dll` for .NET 5 targets. If the interop assembly is not distributed as a NuGet package, an application project adds references to both the component interop assembly produced above and to C#/WinRT to include the `WinRT.Runtime.dll` assembly. If a third party WinRT component is distributed without an official interop assembly, an application project may add a reference to C#/WinRT to generate its own private component interop assembly.  There are versioning concerns related to this scenario, so the preferred solution is for the third party to publish an interop assembly directly.

## Author a C#/WinRT Component

C#/WinRT provides authoring and hosting support for .NET 5 component authors. For more details, refer to this [walkthrough](https://docs.microsoft.com/en-us/windows/uwp/csharp-winrt/create-windows-runtime-component-cswinrt) and [authoring docs](https://github.com/microsoft/CsWinRT/blob/master/docs/authoring.md).
