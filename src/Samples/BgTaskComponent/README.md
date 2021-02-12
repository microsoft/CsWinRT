# Background Task C#/WinRT Authoring Sample

This sample demonstrates how to author an out-of-process C#/WinRT component using background tasks, and how to consume the component as a project reference from a packaged .NET 5 WPF app.

To build this sample, set **WpfApp.Package** as the startup project before building the solution. The background task raises a toast notification and is triggered by a time zone change. 

This sample includes the following projects:

- **BgTaskComponent**: This is a C#/WinRT component with an example background task that pops a toast notification.
- **WpfApp** and **WpfApp.Package**: These projects demonstrate hosting the background task component in a packaged .NET 5 desktop (WPF) application.
  - **WpfApp** has a project reference to **BgTaskComponent**.
  - **WpfApp.Package** is a packaging app with a reference to **WpfApp**. The packaging app is required for hosting out-of-process WinRT components.

There are a few modifications to note that relate to those described in the [authoring docs](https://github.com/microsoft/CsWinRT/blob/master/docs/authoring.md):

- Note that **WinRT.Host.runtimeconfig.json** is part of the packaging project **WpfApp.Package**, and not **WpfApp** itself.
- In addition to registering the background task with the manifest designer, the following extension and class registration must be manually added to **Package.appxmanifest**. Note you do not need to create your own manifest file for activatable class registrations.

  ```xml
  <!-- To host the BgTaskComponent, you must add this activatable class entry -->
  <Extensions>
      <Extension Category="windows.activatableClass.inProcessServer">
          <InProcessServer>
              <Path>WinRT.Host.dll</Path>
              <ActivatableClass ActivatableClassId="BgTaskComponent.ToastBgTask" ThreadingModel="both" />
          </InProcessServer>
      </Extension>
   </Extensions>
   ```

- In **WPFApp.Package.wapproj**, the following `ItemGroup` is added in order to copy the hosting/component assemblies and the runtimeconfig file on deployment:

    ```xml
   <!-- Define TFM for refactoring paths below-->
    <PropertyGroup>
      <TargetFramework>net5.0-windows$(TargetPlatformVersion)</TargetFramework>
    </PropertyGroup>
    <!-- C#/WinRT version 1.1.2-prerelease.210208.6 requires copying the following -->
    <ItemGroup>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\$(TargetFramework)\runtimes\win-$(Platform)\native\WinRT.Host.dll">
        <Link>WinRT.Host.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\$(TargetFramework)\WinRT.Host.Shim.dll">
        <Link>WinRT.Host.Shim.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\$(TargetFramework)\WinRT.Runtime.dll">
        <Link>WinRT.Runtime.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\$(TargetFramework)\Microsoft.Windows.SDK.NET.dll">
        <Link>Microsoft.Windows.SDK.NET.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\$(TargetFramework)\BgTaskComponent.dll">
        <Link>BgTaskComponent.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="WinRT.Host.runtimeconfig.json">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
    </ItemGroup>
    ```
