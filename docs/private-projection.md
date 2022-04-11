# Private Projection Support

## Overview

Support for private projections is an option on the C#/WinRT tool that allows for projections to be generated internal to the library or app. 
This means you can keep uses of Windows SDK APIs internal to your project, and not expose these types to consumers of your library or app. 

## How-To

The process is nearly identical to how you generate a projection today, and the difference is a new set of build properties that mirror the existing ones. 
First you must set a property `CsWinRTPrivateProjection` to `true`. Then specify your includes/excludes using `CsWinRTIncludesPrivate` and `CsWinRTExcludesPrivate`. 

## Notes


You can generate part of your projection as public and part as private, but you must take care to keep the types disjoint. 
Meaning no types that are generated as internal should also be generated as public. 
Similarly, an app can't reference two libraries that both make the same private projection -- this is an ambiguity issue because types of the same name exist in different assemblies. 
