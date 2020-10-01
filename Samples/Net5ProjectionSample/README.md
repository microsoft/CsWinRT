# C#/WinRT End to End Sample

This sample demonstrates how to invoke C#/WinRT to build a .NET5 projection for a C++/WinRT Runtime Component, create a NuGet package for the component, and reference the NuGet from a .NET5 console app. To run this sample, first build the CppWinRTProjectionSample solution which generates the projection and interop assembly, as well as the NuGet package. Then, you can build the ConsoleAppSample solution which restores this NuGet package and uses the component.

**Note**: This sample is accurate as of RC1, and there are expected tooling improvements to be made for the developer experience in our future GA release.