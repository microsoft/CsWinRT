// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage.Streams;

[GeneratedComInterface]
[Guid("45BDE3DC-4F6E-4789-88F7-B824B132EB80")]
internal partial interface IStatusCallback
{
    [PreserveSig]
    uint GetStatus();
}

internal interface IBackgroundTaskProbe
{
    bool WasRun { get; }
}

internal sealed class Operation : IBuffer, IStringable, IDisposable
{
    public uint Capacity => 42;
    public uint Length { get; set; }
    string IStringable.ToString() => nameof(Operation);
    public void Dispose() { }
}

// Each shape has a distinct interface set, so helper sharing cannot hide a naming collision.
namespace First
{
    internal sealed class Operation : IBuffer
    {
        public uint Capacity => 1;
        public uint Length { get; set; }
    }

    internal static class Container
    {
        internal sealed class Operation : IBuffer, IStringable
        {
            public uint Capacity => 2;
            public uint Length { get; set; }
            string IStringable.ToString() => nameof(Operation);
        }
    }

    internal sealed class Operation<T> : IBuffer, IDisposable
    {
        public uint Capacity => 3;
        public uint Length { get; set; }
        public void Dispose() { }
    }

    internal static class Container<T>
    {
        internal sealed class Operation<U> : IBuffer, IStatusCallback
        {
            public uint Capacity => 4;
            public uint Length { get; set; }
            public uint GetStatus() => 5;
        }
    }
}

namespace Second
{
    internal sealed class Operation : IBackgroundTask, IBackgroundTaskProbe
    {
        public bool WasRun { get; private set; }
        public void Run(IBackgroundTaskInstance taskInstance) => WasRun = true;
    }

    internal static class Container
    {
        internal sealed class Operation : IBackgroundTask, IBackgroundTaskProbe, IStringable
        {
            public bool WasRun { get; private set; }
            public void Run(IBackgroundTaskInstance taskInstance) => WasRun = true;
            string IStringable.ToString() => nameof(Operation);
        }
    }

    internal sealed class Operation<T> : IBackgroundTask, IBackgroundTaskProbe, IDisposable
    {
        public bool WasRun { get; private set; }
        public void Run(IBackgroundTaskInstance taskInstance) => WasRun = true;
        public void Dispose() { }
    }

    internal static class Container<T>
    {
        internal sealed class Operation<U> : IBackgroundTask, IBackgroundTaskProbe, IStatusCallback
        {
            public bool WasRun { get; private set; }
            public void Run(IBackgroundTaskInstance taskInstance) => WasRun = true;
            public uint GetStatus() => 6;
        }
    }
}

namespace Third
{
    internal sealed class Operation : IBuffer
    {
        public uint Capacity => 7;
        public uint Length { get; set; }
    }
}
