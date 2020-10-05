# Breaking Changes for C#/WinRT in RC2

1.  With IDynamicInterfaceCastable support, all projected interfaces can now use a simple C#-style cast.  Unprojected interfaces must use the `.As<T>()` extension method.

2.  Calling any of the [RuntimeReflectionExtension](https://docs.microsoft.com/dotnet/api/system.reflection.runtimereflectionextensions?view=net-5.0) methods is likely to cause issues. The runtime type of a projected class now carries around some state information and helper methods, and should not be dynamically reflected on.

3. Some WinRT types may have differences in default values when projected. This may raise issues if relying on default values.

4. The latest Roslyn compiler is required for C# projects, which come with the following NuGet package:

    ```xml
    <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="3.8.0-4.20472.6"
    ```