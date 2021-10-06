# Embedded C#/WinRT Support

## Overview

Embedded support is a C#/WinRT feature that allows you to have the WinRT layer of your projection compiled into the projection itself. This is done by setting the build property `CsWinRTEmbedded` to `true` in the projection's project file. 

Enabling embedded support brings in the WinRT.Runtime sources to the project and automatically sets them to be compiled with the project. And any types that will be projected via `CsWinRTIncludes` will be embedded into the project .dll as well. This means projects using embedded support no longer have a dependency on `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll`. 

For an example, you can look at the [sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/TestEmbedded). 