# Structure

This sample is composed of C++/WinRT components, a C#/WinRT projection that uses embedded support 
(WinRT and Windows SDK), and apps of various flavors that use the embedded projection.

Looking at the C#/WinRT projection, `TestEmbeddedLibrary`, we see it targets several different frameworks, 
demonstrating support for the latest Windows SDK on all platforms. 

```xml
    <TargetFrameworks>net6.0-windows;net5.0-windows;netstandard2.0;net48</TargetFrameworks>
```

Of the apps,  `Net5App` and `NetCore3App` use the same C#/WinRT projection to make use of Windows 10 APIs 
like `Windows.Devices.Geolocation` via the embedded WinRT support provided by C#/WinRT.

The other two, `Net5App.Bootstrap` and `NetFrameworkApp` demonstrate how an app can generate and embed the projection itself.
