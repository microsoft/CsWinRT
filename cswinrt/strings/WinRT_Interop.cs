using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

namespace WinRT.Interop
{
    // IActivationFactory
    [Guid("00000035-0000-0000-C000-000000000046")]
    internal struct IActivationFactoryVftbl
    {
        public unsafe delegate int _ActivateInstance(IntPtr pThis, out IntPtr instance);

        public IInspectable.Vftbl IInspectableVftbl;
        public _ActivateInstance ActivateInstance;
    }

    // standard accessors/mutators
    internal delegate int _get_PropertyAsBoolean(IntPtr thisPtr, out byte value);
    internal delegate int _put_PropertyAsBoolean(IntPtr thisPtr, byte value);
    internal delegate int _get_PropertyAsChar(IntPtr thisPtr, out ushort value);
    internal delegate int _put_PropertyAsChar(IntPtr thisPtr, ushort value);
    internal delegate int _get_PropertyAsSByte(IntPtr thisPtr, out sbyte value);
    internal delegate int _put_PropertyAsSByte(IntPtr thisPtr, sbyte value);
    internal delegate int _get_PropertyAsByte(IntPtr thisPtr, out byte value);
    internal delegate int _put_PropertyAsByte(IntPtr thisPtr, byte value);
    internal delegate int _get_PropertyAsInt16(IntPtr thisPtr, out short value);
    internal delegate int _put_PropertyAsInt16(IntPtr thisPtr, short value);
    internal delegate int _get_PropertyAsUInt16(IntPtr thisPtr, out ushort value);
    internal delegate int _put_PropertyAsUInt16(IntPtr thisPtr, ushort value);
    internal delegate int _get_PropertyAsInt32(IntPtr thisPtr, out int value);
    internal delegate int _put_PropertyAsInt32(IntPtr thisPtr, int value);
    internal delegate int _get_PropertyAsUInt32(IntPtr thisPtr, out uint value);
    internal delegate int _put_PropertyAsUInt32(IntPtr thisPtr, uint value);
    internal delegate int _get_PropertyAsInt64(IntPtr thisPtr, out long value);
    internal delegate int _put_PropertyAsInt64(IntPtr thisPtr, long value);
    internal delegate int _get_PropertyAsUInt64(IntPtr thisPtr, out ulong value);
    internal delegate int _put_PropertyAsUInt64(IntPtr thisPtr, ulong value);
    internal delegate int _get_PropertyAsFloat(IntPtr thisPtr, out float value);
    internal delegate int _put_PropertyAsFloat(IntPtr thisPtr, float value);
    internal delegate int _get_PropertyAsDouble(IntPtr thisPtr, out double value);
    internal delegate int _put_PropertyAsDouble(IntPtr thisPtr, double value);
    internal delegate int _get_PropertyAsObject(IntPtr thisPtr, out IntPtr value);
    internal delegate int _put_PropertyAsObject(IntPtr thisPtr, IntPtr value);
    internal delegate int _get_PropertyAsGuid(IntPtr thisPtr, out Guid value);
    internal delegate int _put_PropertyAsGuid(IntPtr thisPtr, Guid value);
    internal delegate int _get_PropertyAsString(IntPtr thisPtr, out IntPtr value);
    internal delegate int _put_PropertyAsString(IntPtr thisPtr, IntPtr value);
    internal delegate int _get_PropertyAsVector3(IntPtr thisPtr, out Windows.Foundation.Numerics.Vector3 value);
    internal delegate int _put_PropertyAsVector3(IntPtr thisPtr, Windows.Foundation.Numerics.Vector3 value);
    internal delegate int _get_PropertyAsQuaternion(IntPtr thisPtr, out Windows.Foundation.Numerics.Quaternion value);
    internal delegate int _put_PropertyAsQuaternion(IntPtr thisPtr, Windows.Foundation.Numerics.Quaternion value);
    internal delegate int _get_PropertyAsMatrix4x4(IntPtr thisPtr, out Windows.Foundation.Numerics.Matrix4x4 value);
    internal delegate int _put_PropertyAsMatrix4x4(IntPtr thisPtr, Windows.Foundation.Numerics.Matrix4x4 value);
    internal delegate int _add_EventHandler(IntPtr thisPtr, IntPtr handler, out Windows.Foundation.EventRegistrationToken token);
    internal delegate int _remove_EventHandler(IntPtr thisPtr, Windows.Foundation.EventRegistrationToken token);

    // IDelegate
    internal struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }
}
