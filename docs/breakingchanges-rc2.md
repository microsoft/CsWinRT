# Breaking Changes for C#/WinRT in RC2

1.  With IDynamicInterfaceCastable support, all projected interfaces can now use a simple C#-style cast.  Unprojected interfaces must use the `.As<T>()` extension method.

2.  Calling any of the [RuntimeReflectionExtension](https://docs.microsoft.com/dotnet/api/system.reflection.runtimereflectionextensions?view=net-5.0) methods is likely to cause issues. The runtime type of a projected class now carries around some state information and helper methods, and should not be dynamically reflected on.

3. Some WinRT types may have differences in default values when projected. This may raise issues if relying on default values. For example, `Windows.Foundation.DateTime` is projected to `System.DateTimeOffset` in C# and these types have different default values.

4. The latest Roslyn compiler is required for C# projects, which come with the following NuGet package:

    ```xml
    <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="3.8.0-4.20472.6"
    ```

5. Because of strong-name signing, you cannot combine pre-RC2 and post-RC2 projection assemblies. For example, with WinUI and the Windows SDK, both would need to move up to the latest (RC2) version together.

6. The `add` event accessor return type has changed from `System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken` to `void`. We no longer project the `System.Runtime.InteropServices.WindowsRuntime` type, as it has been removed from .NET5.

7. This update of C#/WinRT with .NET5 RC2 is not compatible with WinUI 3 Preview 2. This will be fixed in WinUI3 Preview 3.
