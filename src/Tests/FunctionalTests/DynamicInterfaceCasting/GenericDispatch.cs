// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

internal static class GenericDispatch
{
    public interface IListChecker
    {
        bool Check<T>(object value, bool cast);
    }

    public sealed class ListChecker : IListChecker
    {
        public bool Check<T>(object value, bool cast) => CheckList<T>(value, cast);
    }

    public interface IDirectChecker
    {
        bool Check<T>(object value, bool cast);
    }

    public sealed class DirectChecker : IDirectChecker
    {
        public bool Check<T>(object value, bool cast) => cast ? CheckList<T>(value, true) : value is IList<T>;
    }

    public abstract class AbstractChecker
    {
        public abstract bool Check<T>(object value, bool cast);
    }

    public sealed class AbstractCheckerImpl : AbstractChecker
    {
        public override bool Check<T>(object value, bool cast) => CheckList<T>(value, cast);
    }

    public class VirtualChecker
    {
        public virtual bool Check<T>(object value, bool cast) => false;
    }

    public sealed class VirtualCheckerImpl : VirtualChecker
    {
        public override bool Check<T>(object value, bool cast) => CheckList<T>(value, cast);
    }

    public interface ITypeChecker
    {
        bool Check<T>(object value, bool cast);
    }

    public sealed class TypeChecker : ITypeChecker
    {
        public bool Check<T>(object value, bool cast) => CheckType<T>(value, cast);
    }

    public static bool Forward<T>(object value, bool cast) => CheckList<T>(value, cast);

    private static bool CheckList<T>(object value, bool cast)
    {
        return CheckType<IList<T>>(value, cast) && (!cast || ((IList<T>)value).Count == 3);
    }

    private static bool CheckType<T>(object value, bool cast)
    {
        return cast ? ReferenceEquals(value, (T)value) : value is T;
    }

    public static unsafe object Wrap(object vector)
    {
        void* target = WindowsRuntimeMarshal.ConvertToUnmanaged(vector);

        try
        {
            return new Inspectable(target);
        }
        finally
        {
            WindowsRuntimeMarshal.Free(target);
            GC.KeepAlive(vector);
        }
    }

    // Match UnitTest's opaque IDIC wrapper instead of letting the native vector select a typed RCW
    private sealed class Inspectable : WindowsRuntimeObject
    {
        public unsafe Inspectable(void* target)
            : base(WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(target, WellKnownInterfaceIIDs.IID_IInspectable, out _))
        {
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        protected override bool HasUnwrappableNativeObjectReference => true;

        protected override bool IsOverridableInterface(in Guid iid) => false;
#pragma warning restore CS0672 // Member overrides obsolete member
    }
}
