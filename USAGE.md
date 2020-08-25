# Usage

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
-target $(TargetFramework)
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