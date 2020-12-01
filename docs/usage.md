# Usage

## Component Project

A component project adds a NuGet reference to C#/WinRT to invoke cswinrt.exe at build time, generate projection sources, and compile these into an interop assembly. For an example of this, see the [Projection Sample](https://github.com/microsoft/CsWinRT/tree/master/Samples/Net5ProjectionSample). Command line options can be displayed by running **cswinrt -?**.  The interop assembly is then typically distributed as a NuGet package itself. 

### Example 

To invoke cswinrt to generate projection sources for types in the **Contoso** namespace, you need to add the following property to your C# library project file. In this project you would also need to reference the CsWinRT NuGet package and the project-specific `winmd` files you want to project, whether through a NuGet package, project reference or direct reference. By default, the **Windows** and **Microsoft** namespaces are not projected.

```  
<PropertyGroup>
  <CsWinRTIncludes>Contoso</CsWinRTIncludes>
</PropertyGroup>
```

To further customize C#/WinRT behavior, refer to the [CsWinRT NuGet documentation](../nuget/readme.md).

## Application Project

An application project adds NuGet references to both the component interop assembly produced above and to CsWinRT to include the `winrt.runtime.dll` assembly. If the interop assembly is distributed as a NuGet package itself, this package will require a dependency on C#/WinRT for .NET5 targets. If a third party WinRT component is distributed without an official interop assembly, an application project may add a reference to C#/WinRT to generate its own private component interop assembly.  There are versioning concerns related to this scenario, so the preferred solution is for the third party to publish an interop assembly directly.
