# What is it?

The Component Object Model (COM) API that underlines .NET and the Windows
Runtime supports the concept of Out Of Process (OOP) Servers. This allows for
using objects that are in a different process (or even a different machine) as
though they were in the local process. This library adds APIs to make the
process of creating the "server" in .NET much easier.

> \*\*Note\*\*: COM and Windows Runtime are Windows only.

# Why?

## Cross Language

Because this uses COM/WinRT for the communication any language that can use COM
can use this library. As an example of this, the sample includes a simple C++
console application that talks to the .NET/C# server.

## Complex Types

Most RPC/ICP systems are just sending messages between the two processes. At
most they can serialize an object graph. COM allows for more complicated
objects, where the returned types can have methods, events, and properties. If
you could do it with a local object, you can do it with a remote object. The
samples include using most of these abilities, including having the remote
process load a file as a stream and having the local process use the stream
without having to send the whole file across.

## Built in Support for Types

Many of the types you are used to using are supported out of the box like
collection types, map types, streams, etc. This allows you to not have to worry
about how the IPC works.

## Security

COM provides various ways to secure usage and creation of objects. For more
details, see [Security in COM](https://learn.microsoft.com/en-us/windows/win32/com/security-in-com).

# Troubleshooting

If you are having issues, check on these things:

* Make sure the client app includes the WinMDs (Interface and Metadata)
* Make sure that the interface project uses the
  `Windows.Foundation.Metadata.GuidAttribute` attribute, not the
  `System.Runtime.InteropServices.GuidAttribute` attribute.
* Make sure that the server project uses the
  `System.Runtime.InteropServices.GuidAttribute` attribute, not the
  `Windows.Foundation.Metadata.GuidAttribute` attribute.
