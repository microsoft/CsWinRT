// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using WindowsRuntime;

namespace AuthoringTest.ExcludedBases;

public interface IInheritedContract
{
    int FromInterface(int value);
}

[ExcludeFromWindowsMetadata]
public class ManagedBase<T>(int seed) : IInheritedContract, IDisposable, IWwwFormUrlDecoderEntry
{
    public bool IsDisposed { get; private set; }

    public string Name => "inherited";

    public string Value => "contract";

    public List<T> ManagedOnly() => [];

    public int Forwarded(int value) => seed + value;

    public virtual int Overridden(int value) => seed + value + 1;

    public int FromInterface(int value) => seed * value;

    public void Dispose() => IsDisposed = true;
}

public sealed class ConcreteDerived() : ManagedBase<int>(7), IInheritedContract
{
    public new int Forwarded(int value) => base.Forwarded(value);

    public override int Overridden(int value) => base.Overridden(value) + 100;

    public bool GetDisposed() => IsDisposed;
}

public sealed class RedeclaredDerived() : ManagedBase<int>(13), IDisposable, IWwwFormUrlDecoderEntry
{
    public bool GetDisposed() => IsDisposed;
}

[ExcludeFromWindowsMetadata]
public abstract class AbstractManagedBase(int seed)
{
    protected int Seed => seed;

    public int Forwarded(int value) => seed + value;

    public abstract int Compute(int value);
}

public sealed class AbstractDerived() : AbstractManagedBase(11)
{
    public new int Forwarded(int value) => base.Forwarded(value);

    public override int Compute(int value) => Seed * value;
}