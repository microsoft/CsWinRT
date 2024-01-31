// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;

namespace WinRT
{
    partial class Projections
    {
        /// <summary>Registers the custom ABI type mapping for the <see cref="EventRegistrationToken"/> type.</summary>
        public static void RegisterEventRegistrationTokenMapping() => RegisterCustomAbiTypeMapping(
            typeof(EventRegistrationToken),
            typeof(ABI.WinRT.EventRegistrationToken),
            "Windows.Foundation.EventRegistrationToken");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> type.</summary>
        public static void RegisterNullableOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(Nullable<>),
            typeof(ABI.System.Nullable<>),
            "Windows.Foundation.IReference`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="int"/> type.</summary>
        public static void RegisterNullableIntMapping() => RegisterCustomAbiTypeMapping(
            typeof(int?),
            typeof(ABI.System.Nullable_int),
            "Windows.Foundation.IReference`1<Int32>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="byte"/> type.</summary>
        public static void RegisterNullableByteMapping() => RegisterCustomAbiTypeMapping(
            typeof(byte?),
            typeof(ABI.System.Nullable_byte),
            "Windows.Foundation.IReference`1<UInt8>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="sbyte"/> type.</summary>
        public static void RegisterNullableSByteMapping() => RegisterCustomAbiTypeMapping(
            typeof(sbyte?),
            typeof(ABI.System.Nullable_sbyte),
            "Windows.Foundation.IReference`1<Int8>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="short"/> type.</summary>
        public static void RegisterNullableShortMapping() => RegisterCustomAbiTypeMapping(
            typeof(short?),
            typeof(ABI.System.Nullable_short),
            "Windows.Foundation.IReference`1<Int16>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="ushort"/> type.</summary>
        public static void RegisterNullableUShortMapping() => RegisterCustomAbiTypeMapping(
            typeof(ushort?),
            typeof(ABI.System.Nullable_ushort),
            "Windows.Foundation.IReference`1<UInt16>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="uint"/> type.</summary>
        public static void RegisterNullableUIntMapping() => RegisterCustomAbiTypeMapping(
            typeof(uint?),
            typeof(ABI.System.Nullable_uint),
            "Windows.Foundation.IReference`1<UInt32>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="long"/> type.</summary>
        public static void RegisterNullableLongMapping() => RegisterCustomAbiTypeMapping(
            typeof(long?),
            typeof(ABI.System.Nullable_long),
            "Windows.Foundation.IReference`1<Int64>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="ulong"/> type.</summary>
        public static void RegisterNullableULongMapping() => RegisterCustomAbiTypeMapping(
            typeof(ulong?),
            typeof(ABI.System.Nullable_ulong),
            "Windows.Foundation.IReference`1<UInt64>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="float"/> type.</summary>
        public static void RegisterNullableFloatMapping() => RegisterCustomAbiTypeMapping(
            typeof(float?),
            typeof(ABI.System.Nullable_float),
            "Windows.Foundation.IReference`1<Single>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="double"/> type.</summary>
        public static void RegisterNullableDoubleMapping() => RegisterCustomAbiTypeMapping(
            typeof(double?),
            typeof(ABI.System.Nullable_double),
            "Windows.Foundation.IReference`1<Double>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="char"/> type.</summary>
        public static void RegisterNullableCharMapping() => RegisterCustomAbiTypeMapping(
            typeof(char?),
            typeof(ABI.System.Nullable_char),
            "Windows.Foundation.IReference`1<Char16>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="bool"/> type.</summary>
        public static void RegisterNullableBoolMapping() => RegisterCustomAbiTypeMapping(
            typeof(bool?),
            typeof(ABI.System.Nullable_bool),
            "Windows.Foundation.IReference`1<Boolean>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="Guid"/> type.</summary>
        public static void RegisterNullableGuidMapping() => RegisterCustomAbiTypeMapping(
            typeof(Guid?),
            typeof(ABI.System.Nullable_guid),
            "Windows.Foundation.IReference`1<Guid>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="DateTimeOffset"/> type.</summary>
        public static void RegisterNullableDateTimeOffsetMapping() => RegisterCustomAbiTypeMapping(
            typeof(DateTimeOffset?),
            typeof(ABI.System.Nullable_DateTimeOffset),
            "Windows.Foundation.IReference`1<Windows.Foundation.DateTime>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Nullable{T}"/> of <see cref="TimeSpan"/> type.</summary>
        public static void RegisterNullableTimeSpanMapping() => RegisterCustomAbiTypeMapping(
            typeof(TimeSpan?),
            typeof(ABI.System.Nullable_TimeSpan),
            "Windows.Foundation.IReference`1<TimeSpan>");

        /// <summary>Registers the custom ABI type mapping for the <see cref="DateTimeOffset"/> type.</summary>
        public static void RegisterDateTimeOffsetMapping() => RegisterCustomAbiTypeMapping(
            typeof(DateTimeOffset),
            typeof(ABI.System.DateTimeOffset),
            "Windows.Foundation.DateTime");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Exception"/> type.</summary>
        public static void RegisterExceptionMapping() => RegisterCustomAbiTypeMapping(
            typeof(Exception),
            typeof(ABI.System.Exception),
            "Windows.Foundation.HResult");

        /// <summary>Registers the custom ABI type mapping for the <see cref="TimeSpan"/> type.</summary>
        public static void RegisterTimeSpanMapping() => RegisterCustomAbiTypeMapping(
            typeof(TimeSpan),
            typeof(ABI.System.TimeSpan),
            "Windows.Foundation.TimeSpan");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Uri"/> type.</summary>
        public static void RegisterUriMapping() => RegisterCustomAbiTypeMapping(
            typeof(Uri),
            typeof(ABI.System.Uri),
            "Windows.Foundation.Uri", isRuntimeClass: true);

        /// <summary>Registers the custom ABI type mapping for the <see cref="DataErrorsChangedEventArgs"/> type.</summary>
        public static void RegisterDataErrorsChangedEventArgsMapping() => RegisterCustomAbiTypeMapping(
            typeof(DataErrorsChangedEventArgs),
            typeof(ABI.System.ComponentModel.DataErrorsChangedEventArgs),
            "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", isRuntimeClass: true);

        /// <summary>Registers the custom ABI type mapping for the <see cref="PropertyChangedEventArgs"/> type.</summary>
        public static void RegisterPropertyChangedEventArgsMapping() => RegisterCustomAbiTypeMapping(
            typeof(PropertyChangedEventArgs),
            typeof(ABI.System.ComponentModel.PropertyChangedEventArgs),
            "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", isRuntimeClass: true);

        /// <summary>Registers the custom ABI type mapping for the <see cref="PropertyChangedEventHandler"/> type.</summary>
        public static void RegisterPropertyChangedEventHandlerMapping() => RegisterCustomAbiTypeMapping(
            typeof(PropertyChangedEventHandler),
            typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
            "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler");

        /// <summary>Registers the custom ABI type mapping for the <see cref="INotifyDataErrorInfo"/> type.</summary>
        public static void RegisterINotifyDataErrorInfoMapping() => RegisterCustomAbiTypeMapping(
            typeof(INotifyDataErrorInfo),
            typeof(ABI.System.ComponentModel.INotifyDataErrorInfo),
            "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo");

        /// <summary>Registers the custom ABI type mapping for the <see cref="INotifyPropertyChanged"/> type.</summary>
        public static void RegisterINotifyPropertyChangedMapping() => RegisterCustomAbiTypeMapping(
            typeof(INotifyPropertyChanged),
            typeof(ABI.System.ComponentModel.INotifyPropertyChanged),
            "Microsoft.UI.Xaml.Data.INotifyPropertyChanged");

        /// <summary>Registers the custom ABI type mapping for the <see cref="ICommand"/> type.</summary>
        public static void RegisterICommandMapping() => RegisterCustomAbiTypeMapping(
            typeof(ICommand),
            typeof(ABI.System.Windows.Input.ICommand),
            "Microsoft.UI.Xaml.Interop.ICommand");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IServiceProvider"/> type.</summary>
        public static void RegisterIServiceProviderMapping() => RegisterCustomAbiTypeMapping(
            typeof(IServiceProvider),
            typeof(ABI.System.IServiceProvider),
            "Microsoft.UI.Xaml.IXamlServiceProvider");

        /// <summary>Registers the custom ABI type mapping for the <see cref="EventHandler{TEventArgs}"/> type.</summary>
        public static void RegisterEventHandlerOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(EventHandler<>),
            typeof(ABI.System.EventHandler<>),
            "Windows.Foundation.EventHandler`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="KeyValuePair{TKey, TValue}"/> type.</summary>
        public static void RegisterKeyValuePairOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(KeyValuePair<,>),
            typeof(ABI.System.Collections.Generic.KeyValuePair<,>),
            "Windows.Foundation.Collections.IKeyValuePair`2");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IEnumerable{T}"/> type.</summary>
        public static void RegisterIEnumerableOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IEnumerable<>),
            typeof(ABI.System.Collections.Generic.IEnumerable<>),
            "Windows.Foundation.Collections.IIterable`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IEnumerator{T}"/> type.</summary>
        public static void RegisterIEnumeratorOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IEnumerator<>),
            typeof(ABI.System.Collections.Generic.IEnumerator<>),
            "Windows.Foundation.Collections.IIterator`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IList{T}"/> type.</summary>
        public static void RegisterIListOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IList<>),
            typeof(ABI.System.Collections.Generic.IList<>),
            "Windows.Foundation.Collections.IVector`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IReadOnlyList{T}"/> type.</summary>
        public static void RegisterIReadOnlyListOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IReadOnlyList<>),
            typeof(ABI.System.Collections.Generic.IReadOnlyList<>),
            "Windows.Foundation.Collections.IVectorView`1");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IDictionary{TKey, TValue}"/> type.</summary>
        public static void RegisterIDictionaryOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IDictionary<,>),
            typeof(ABI.System.Collections.Generic.IDictionary<,>),
            "Windows.Foundation.Collections.IMap`2");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.</summary>
        public static void RegisterIReadOnlyDictionaryOpenGenericMapping() => RegisterCustomAbiTypeMapping(
            typeof(IReadOnlyDictionary<,>),
            typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>),
            "Windows.Foundation.Collections.IMapView`2");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IDisposable"/> type.</summary>
        public static void RegisterIDisposableMapping() => RegisterCustomAbiTypeMapping(
            typeof(IDisposable),
            typeof(ABI.System.IDisposable),
            "Windows.Foundation.IClosable");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IEnumerable"/> type.</summary>
        public static void RegisterIEnumerableMapping() => RegisterCustomAbiTypeMapping(
            typeof(IEnumerable),
            typeof(ABI.System.Collections.IEnumerable),
            "Microsoft.UI.Xaml.Interop.IBindableIterable");

        /// <summary>Registers the custom ABI type mapping for the <see cref="IList"/> type.</summary>
        public static void RegisterIListMapping() => RegisterCustomAbiTypeMapping(
            typeof(IList),
            typeof(ABI.System.Collections.IList),
            "Microsoft.UI.Xaml.Interop.IBindableVector");

        /// <summary>Registers the custom ABI type mapping for the <see cref="INotifyCollectionChanged"/> type.</summary>
        public static void RegisterINotifyCollectionChangedMapping() => RegisterCustomAbiTypeMapping(
            typeof(INotifyCollectionChanged),
            typeof(ABI.System.Collections.Specialized.INotifyCollectionChanged),
            "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged");

        /// <summary>Registers the custom ABI type mapping for the <see cref="NotifyCollectionChangedAction"/> type.</summary>
        public static void RegisterNotifyCollectionChangedActionMapping() => RegisterCustomAbiTypeMapping(
            typeof(NotifyCollectionChangedAction),
            typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedAction),
            "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction");

        /// <summary>Registers the custom ABI type mapping for the <see cref="NotifyCollectionChangedEventArgs"/> type.</summary>
        public static void RegisterNotifyCollectionChangedEventArgsMapping() => RegisterCustomAbiTypeMapping(
            typeof(NotifyCollectionChangedEventArgs),
            typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs),
            "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", isRuntimeClass: true);

        /// <summary>Registers the custom ABI type mapping for the <see cref="NotifyCollectionChangedEventHandler"/> type.</summary>
        public static void RegisterNotifyCollectionChangedEventHandlerMapping() => RegisterCustomAbiTypeMapping(
            typeof(NotifyCollectionChangedEventHandler),
            typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
            "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Matrix3x2"/> type.</summary>
        public static void RegisterMatrix3x2Mapping() => RegisterCustomAbiTypeMapping(
            typeof(Matrix3x2),
            typeof(ABI.System.Numerics.Matrix3x2),
            "Windows.Foundation.Numerics.Matrix3x2");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Matrix4x4"/> type.</summary>
        public static void RegisterMatrix4x4Mapping() => RegisterCustomAbiTypeMapping(
            typeof(Matrix4x4),
            typeof(ABI.System.Numerics.Matrix4x4),
            "Windows.Foundation.Numerics.Matrix4x4");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Plane"/> type.</summary>
        public static void RegisterPlaneMapping() => RegisterCustomAbiTypeMapping(
            typeof(Plane),
            typeof(ABI.System.Numerics.Plane),
            "Windows.Foundation.Numerics.Plane");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Quaternion"/> type.</summary>
        public static void RegisterQuaternionMapping() => RegisterCustomAbiTypeMapping(
            typeof(Quaternion),
            typeof(ABI.System.Numerics.Quaternion),
            "Windows.Foundation.Numerics.Quaternion");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Vector2"/> type.</summary>
        public static void RegisterVector2Mapping() => RegisterCustomAbiTypeMapping(
            typeof(Vector2),
            typeof(ABI.System.Numerics.Vector2),
            "Windows.Foundation.Numerics.Vector2");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Vector3"/> type.</summary>
        public static void RegisterVector3Mapping() => RegisterCustomAbiTypeMapping(
            typeof(Vector3),
            typeof(ABI.System.Numerics.Vector3),
            "Windows.Foundation.Numerics.Vector3");

        /// <summary>Registers the custom ABI type mapping for the <see cref="Vector4"/> type.</summary>
        public static void RegisterVector4Mapping() => RegisterCustomAbiTypeMapping(
            typeof(Vector4),
            typeof(ABI.System.Numerics.Vector4),
            "Windows.Foundation.Numerics.Vector4");

        /// <summary>Registers the custom ABI type mapping for the <see cref="EventHandler"/> type.</summary>
        public static void RegisterEventHandlerMapping()
        {
            rwlock.EnterWriteLock();

            try
            {
                RegisterCustomAbiTypeMappingNoLock(typeof(EventHandler), typeof(ABI.System.EventHandler));
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
    }
}