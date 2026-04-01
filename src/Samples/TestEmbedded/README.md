# C#/WinRT Embedded Sample

These samples demonstrate how to use [C#/WinRT Embedded Support](../../../docs/embedded.md).

## Structure

This sample is composed of a few projects to demonstrate C#/WinRT embedded support:

- C++/WinRT component projects: *Alpha*, *Beta*, *Gamma*
- *TestEmbeddedLibrary*: a C# library project using C#/WinRT embedded support to embed the WinRT.Runtime and Windows SDK sources into the projection/library output.
- C#/.NET apps of various flavors that use the embedded projection (either by referencing *TestEmbeddedLibrary*, or by referencing the C++/WinRT components and generating the projection directly in the app project):
    - *Net8App*: References *TestEmbeddedLibrary* to consume the embedded projection.
    - *Net8App.Bootstrap*: References *Alpha*, *Beta*, and *Gamma* and creates an embeddded projection in the app itself.
    - *NetCore3App*: References *TestEmbeddedLibrary* to consume the embedded projection.
    - *NetFrameworkApp*: References *Alpha*, *Beta*, and *Gamma* and creates an embeddded projection in the app itself.

Looking at the C#/WinRT embedded projection, `TestEmbeddedLibrary`, we see it targets several different frameworks, 
demonstrating support for the latest Windows SDK on all platforms. 

```xml
    <TargetFrameworks>net8.0-windows;netstandard2.0;net48</TargetFrameworks>
```

Of the apps, `Net8App` and `NetCore3App` reference the same C#/WinRT embedded projection (*TestEmbeddedLibrary*) to make use of WinRT APIs 
like `Windows.Devices.Geolocation`. The other two apps, `Net8App.Bootstrap` and `NetFrameworkApp`, demonstrate how an app can generate and embed the projection itself using a package reference to C#/WinRT.

## Build and run the sample

Open `TestEmbedded.sln` in Visual Studio. Select one of the application projects described above as the startup project in Visual Studio. Then build and run the application.
