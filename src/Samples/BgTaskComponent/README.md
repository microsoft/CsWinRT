# Background Task C#/WinRT Authoring Sample

This sample demonstrates how to author an out-of-process C#/WinRT component using background tasks, and consming the component as a project reference from a packaged .NET 5 WPF app.

This sample includes the following projects:

- **BgTaskComponent**: This is a Windows Runtime component with an example background task that pops a toast notification. It uses C#/WinRT authoring to author the component.
- **WpfApp** and **WpfApp.Package**: These projects demonstrate hosting the background task component in a packaged .NET5 desktop (WPF) application. 

There are a few modifications to note that are different/additional to those described in the [authoring docs](https://github.com/microsoft/CsWinRT/blob/master/docs/authoring.md):

- Note that **WinRT.Host.runtimeconfig.json** is part of the packaging project **WpfApp.Package**, and not **WpfApp** itself.
- In addition to registering the background task with the manifest designer, the following class registration must be manually added to **Package.appxmanifest**. Note you do not need to create your own manifest file for activatable class registrations. 
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
- In **WPFApp.Package.wapproj**, add the following `ItemGroup`:

  ```xml
  <ItemGroup>
      <!-- This C#/WinRT version requires copying the following -->
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\runtimes\win-x64\native\WinRT.Host.dll">
        <Link>WinRT.Host.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\WinRT.Host.Shim.dll">
        <Link>WinRT.Host.Shim.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\WinRT.Runtime.dll">
        <Link>WinRT.Runtime.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\Microsoft.Windows.SDK.NET.dll">
        <Link>Microsoft.Windows.SDK.NET.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="..\WpfApp\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\BgTaskComponent.dll">
        <Link>BgTaskComponent.dll</Link>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
      <Content Include="WinRT.Host.runtimeconfig.json">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
    </ItemGroup>
    ```
  
  
