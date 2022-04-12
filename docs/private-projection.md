# Private Projection Support

## Overview

Support for private projections is an option on the C#/WinRT tool that allows for projections to be generated with accessibility scoped to the module generating the projection. 
This means you can keep uses of your component's projection(s) internal to your project, rather than having a dependency on projections like the one for the Windows SDK (`Microsoft.Windows.SDK.NET.dll`). 


## How-To

The process is nearly identical to how you generate a projection today. The difference is a new set of build properties that look similar to the properties used to generate projections globally.
First you must set a property `CsWinRTPrivateProjection` to `true`. Then specify your includes/excludes using `CsWinRTIncludesPrivate` and `CsWinRTExcludesPrivate`. 
For example, you could 

## Notes

Note that this means projected types are not accessible outside the module. Therefore

You can generate part of your projection as global and part as private, but you must take care to keep the types disjoint otherwise you will generate duplicates of the types projected as both.
Similarly, an app can't reference two libraries that both make the same private projection -- this is an ambiguity issue because types of the same name exist in different assemblies. 

## Difference with Embedded Projection

Embedded projections include WinRT.Runtime sources and module generating an embedded projection uses its own assembly to resolve types. 
Where as private projections still use the global `WinRT.Runtime.dll`, and just keep the projection private to the module.