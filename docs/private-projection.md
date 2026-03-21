# Private Projection Support (not supported yet)

## Overview

Support for private projections is an option on the C#/WinRT tool that allows for projections to be generated with accessibility scoped to the module generating the projection. 

## How-To

The process is nearly identical to how you generate a projection today. The difference is a new set of build properties that look similar to the properties used to generate projections globally.
First you must set a property `CsWinRTPrivateProjection` to `true`. Then specify your includes/excludes using `CsWinRTIncludesPrivate` and `CsWinRTExcludesPrivate`. 

## Notes

Note that this means projected types are not accessible outside the module.

You can generate part of your projection as global and part as private, but you must take care to keep the types disjoint otherwise you will generate duplicates of the types projected as both.
Similarly, an app can't reference two libraries that both make the same private projection -- this is an ambiguity issue because types of the same name exist in different assemblies. 
