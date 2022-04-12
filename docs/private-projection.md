# Private Projection Support

## Overview

Support for private projections is an option on the C#/WinRT tool that allows for projections to be generated with accessibility scoped to the module generating the projection. 

## How-To

The process is nearly identical to how you generate a projection today. The difference is a new set of build properties that look similar to the properties used to generate projections globally.
First you must set a property `CsWinRTPrivateProjection` to `true`. Then specify your includes/excludes using `CsWinRTIncludesPrivate` and `CsWinRTExcludesPrivate`. 

## Notes

Note that this means projected types are not accessible outside the module.

You can generate part of your projection as global and part as private, but you must take care to keep the types disjoint otherwise you will generate duplicates of the types projected as both.
Similarly, an app can't reference two libraries that both make the same private projection -- this is an ambiguity issue because types of the same name exist in different assemblies. 

## Difference with Embedded Projection

Private projection support allows developers to include their component's projection sources in their library or app without exposing it 
as part of the library or app's public API surface and without needing to create a separate projection binary for it. 
While embedded support does something similar, embedded support makes all interactions with the WinRT projections (and the support needed for it) embedded in the developer's library or app.
This hides the uses of WinRT from the library/app's public API surface, allowing developers to use any TargetFramework. 