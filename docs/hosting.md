# Managed Component Hosting

## Overview
This document describes the behavior and implementation of winrt.host.dll, a native DLL which provides hosting for managed C#/WinRT runtime components.  Winrt.host.dll may be explicitly registered with an ActivatableClass manifest entry, or it may be renamed to support manifest-free activation.  The details of how winrt.host.dll resolves and forwards activation requests are described below.

## Conventions
To mininmize the need for explicit mapping data, a set of file naming conventions flows across the activation path, from class to host to target:
*  If the client is using manifest-free, the host name is derived from the class name.  In turn, the target name is also derived from the class name. 
* If the client is using a manifest, the host name is explicitly specified and can be anything.  The target name may then be derived from either the class name or the host name (if renamed).
* If the host and target file names are fixed, a runtimeconfig.json entry can be used to provide an explicit mapping from class ID to target assembly.

## Glossary
For brevity, the following terms are used throughout:
* __Client__: the client of the activation request (caller of RoGetActivationFactory)
* __Target__: the managed assembly implementing the runtime class
* __Class__: the activatable runtime class
* __Host__: an activation adapter that hosts a managed implementation

## Probing
Probing is the algorithm for finding a target assembly, based on the runtime class name or the host file name.  It is designed to maximize availability of the "good name" for a target assembly (__"Acme.Controls.Widget.dll"__ in the examples below).  

The target assembly may be discovered by:
1. __Class Name__
   1. Probes the fully qualified class name, and successive dot-separated substrings of it, with and without a ".Server" suffix.  The ".Server" suffix is included in the search to support different activation scenarios (see examples section).
   1. If the class name is "Acme.Controls.Widget", the following files are probed:
      1. Acme.Controls.Widget.Server.dll
      1. Acme.Controls.Widget.dll
      1. Acme.Controls.Server.dll
      1. Acme.Controls.dll
      1. Acme.Server.dll
      1. Acme.dll
1. __Host Name__ (if renamed from winrt.host.dll) 
   1. Gets the host's file name via GetModuleFileName, short-circuits for "winrt.host.dll"
   1. Probes successive dot-separated substrings of the host file name, with a ".Server" suffix.  The host name itself is skipped in the search.
   1. If the host name is "Acme.Controls.Widget.Host.dll", the following files are probed:
      1. Acme.Controls.Widget.Host.Server.dll
      1. (Acme.Controls.Widget.Host.dll is skipped)
      1. Acme.Controls.Widget.Server.dll
      1. Acme.Controls.Widget.dll
      1. Acme.Controls.Server.dll
      1. Acme.Controls.dll
      1. Acme.Server.dll
      1. Acme.dll

## Host
From the activating client's perspective, winrt.host.dll is the implementation of the runtime class.  Using the HostFxr library to host the CLR, it provides an adapter from native client code to the managed target assembly that actually implements the class.  The host may be renamed from winrt.host.dll, to provide host-based probing to find the target assembly.
 
### Exports
Per WinRT activation contract, the host exports:
* DllCanUnloadNow 
* DllGetActivationFactory

To support forwarding scenarios, the host also exports:
* DllGetActivationFactoryFromAssembly

__DllCanUnloadNow__ may either:
   1. Unconditionally return false (this is the behavior of the .NET winrthost.dll implementation)
   1. Track all loaded target assemblies, forward the call to them, and return false if any target does 

Given that the host would likely use Reg-Free Activation, which does not support COM memory management, it makes little sense to adopt 2. 

__DllGetActivationFactory__:
   1. Searches for target assembly based on: 
      1. Class Name Probing (see above), or
      1. Host Name Probing (see above)
   1. If target found, 
      1. Forwards to DllGetActivationFactoryFromAssembly with target name

__DllGetActivationFactoryFromAssembly__:
   1. Provides an 'overload' of DllGetActivationFactory that accepts an explicit target assembly
   2. Uses the HostFxr library to load the CLR and a managed winrt.host.shim assembly:
      1. The behavior mirrors details in the .Net WinRT-activation spec
      1. A .runtimeconfig.json can be used to explicitly select a runtime version to address conflicts
   1. Binds to the shim's static factory method, WinRT.Module.GetActivationFactory
   1. Calls GetActivationFactory, passing the target assembly path and runtime class name
   1. GetActivationFactory:
      1. Uses reflection to find the target type
      1. Creates an IActivationFactory implementation for the target type
      1. Creates a CCW for the factory and returns it as an IntPtr
   1. The returned factory:
       1. Implements ActivateInstance by constructing the target type
       1. Creates a CCW for the factory and returns it as an IntPtr
	
## Examples

### Reg-Free Activation with Generic Host

Class | Host | Target
- | - | -
"Acme.Controls.Widget" | WinRT.Host.dll | Acme.Controls.Widget.dll

1. Client registers class with generic host, via fusion or appx manifest:
   1. Fusion
		<file name="winrt.host.dll">
		  <activatableClass name="Acme.Controls.Widget" threadingModel="both" />
		</file>
   1. Appx
		<InProcessServer> 
		  <Path>winrt.host.dll</Path> 
		  <ActivatableClass ActivatableClassId="Acme.Controls.Widget" ThreadingModel="both" /> </InProcessServer>
1. Client activates class:
		RoGetActivationFactory("Acme.Controls.Widget")
1. Host finds target via Class Name Probing (see above):
   1. "Acme.Controls.Widget.Server.dll",
   1. "Acme.Controls.Widget.dll"

### Reg-Free Activation with Renamed Host
This example demonstrates how a target DLL that has already taken the "good name" can be supported, by using an appropriately renamed host.

Class | Host | Target
- | - | -
"Acme.Controls.Widget" | Acme.Controls.Widget.Host.dll | Acme.Controls.Widget.dll

1. Client registers class with renamed host, via fusion or appx manifest:
   1. Fusion
    ```xml	
    <file name="Acme.Controls.Widget.Host.dll">
        <activatableClass name="Acme.Controls.Widget" threadingModel="both" />
    </file>
   ```
   1. Appx
   ```xml
    <InProcessServer> 
        <Path>Acme.Controls.Widget.Host.dll</Path> 
        <ActivatableClass ActivatableClassId="Acme.Controls.Widget" ThreadingModel="both" /> 
    </InProcessServer>
   ```
1. Client activates class:
		RoGetActivationFactory("Acme.Controls.Widget")
1. Host finds target via Class Name or Host Name Probing (see above):
   1. "Acme.Controls.Widget.Host.Server.dll",
   1. "Acme.Controls.Widget.Server.dll",
   1. "Acme.Controls.Widget.dll"

### Reg-Free Activation with Target Assembly Mapping
This example demonstrates how an activatableClass entry can be added to the host runtimeconfig.json to provide an explicit mapping from class ID to target assembly.  This is useful when both the host dll and the target assembly have fixed names.

Class | Host | Target
- | - | -
"Acme.Controls.Widget" | WinRT.Host.dll | Widget.dll

This scenario follows a procedure similar to __Reg-Free Activation with Generic Host__ above.  In addition, the host dll's runtimeconfig.json contains an activatableClass section, similar to the following:
```json
    {
      "runtimeOptions": { ... },
      "activatableClasses": {
        "Acme.Controls.Widget": "Widget.dll"
      }
    }
```

### Manifest-Free Activation
This example demonstrates support for client code using manifest-free activation, which constrains the host name to match the class name.

Class | Host | Target
- | - | -
"Acme.Controls.Widget" | Acme.Controls.Widget.dll | Acme.Controls.Widget.Server.dll

1. Client discovers host based on runtime class name:
		Acme.Controls.Widget.dll
1. Client explicitly activates class:
   1. LoadLibrary("Acme.Controls.Widget.dll")
   1. GetProcAddress("DllGetActivationFactory")
   1. DllGetActivationFactory("Acme.Controls.Widget")
3. Host finds target via Class Name or Host Name Probing (see above):
   1. "Acme.Controls.Widget.Server.dll"

## Other Considerations

### Non-Goals
The Host is neutral with respect to:
* ThreadingModel
* InProc/OOP
In both cases, support is assumed to be provided by the component and/or activation logic

### Performance
The examples above demonstrate that in typical cases, only a few probes are needed to find the target assembly.
Even so, caching of probing results can be used to minimize steady-state overhead.

## See also

[Managed WinRT Activation of .NET Core components](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/WinRT-activation.md)


