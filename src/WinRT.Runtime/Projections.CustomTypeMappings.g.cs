// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Threading;
using System.Windows.Input;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;

namespace WinRT
{
    /// <inheritdoc cref="Projections"/>
    partial class Projections
    {
        private static int _EventRegistrationToken;
        private static int _Nullable__;
        private static int _int_;
        private static int _byte_;
        private static int _sbyte_;
        private static int _short_;
        private static int _ushort_;
        private static int _uint_;
        private static int _long_;
        private static int _ulong_;
        private static int _float_;
        private static int _double_;
        private static int _char_;
        private static int _bool_;
        private static int _Guid_;
        private static int _DateTimeOffset_;
        private static int _TimeSpan_;
        private static int _DateTimeOffset;
        private static int _Exception;
        private static int _TimeSpan;
        private static int _Uri;
        private static int _DataErrorsChangedEventArgs;
        private static int _PropertyChangedEventArgs;
        private static int _PropertyChangedEventHandler;
        private static int _INotifyDataErrorInfo;
        private static int _INotifyPropertyChanged;
        private static int _ICommand;
        private static int _IServiceProvider;
        private static int _EventHandler__;
        private static int _KeyValuePair___;
        private static int _IEnumerable__;
        private static int _IEnumerator__;
        private static int _IList__;
        private static int _IReadOnlyList__;
        private static int _IDictionary___;
        private static int _IReadOnlyDictionary___;
        private static int _IDisposable;
        private static int _IEnumerable;
        private static int _IList;
        private static int _INotifyCollectionChanged;
        private static int _NotifyCollectionChangedAction;
        private static int _NotifyCollectionChangedEventArgs;
        private static int _NotifyCollectionChangedEventHandler;
        private static int _Matrix3x2;
        private static int _Matrix4x4;
        private static int _Plane;
        private static int _Quaternion;
        private static int _Vector2;
        private static int _Vector3;
        private static int _Vector4;
        private static int _EventHandler;
        private static int _IMap___;
        private static int _IVector__;
        private static int _IMapView___;
        private static int _IVectorView__;
        private static int _IBindableVector;
        private static int _ICollection__;
        private static int _IReadOnlyCollection__;
        private static int _ICollection;

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.EventRegistrationToken"</c> WinRT type.</summary>
        public static void RegisterEventRegistrationTokenMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _EventRegistrationToken, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(EventRegistrationToken),
                typeof(ABI.WinRT.EventRegistrationToken),
                "Windows.Foundation.EventRegistrationToken",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1"</c> WinRT type.</summary>
        public static void RegisterNullableOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Nullable__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Nullable<>),
                typeof(ABI.System.Nullable<>),
                "Windows.Foundation.IReference`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Int32>"</c> WinRT type.</summary>
        public static void RegisterNullableIntMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _int_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(int?),
                typeof(ABI.System.Nullable_int),
                "Windows.Foundation.IReference`1<Int32>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<UInt8>"</c> WinRT type.</summary>
        public static void RegisterNullableByteMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _byte_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(byte?),
                typeof(ABI.System.Nullable_byte),
                "Windows.Foundation.IReference`1<UInt8>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Int8>"</c> WinRT type.</summary>
        public static void RegisterNullableSByteMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _sbyte_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(sbyte?),
                typeof(ABI.System.Nullable_sbyte),
                "Windows.Foundation.IReference`1<Int8>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Int16>"</c> WinRT type.</summary>
        public static void RegisterNullableShortMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _short_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(short?),
                typeof(ABI.System.Nullable_short),
                "Windows.Foundation.IReference`1<Int16>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<UInt16>"</c> WinRT type.</summary>
        public static void RegisterNullableUShortMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _ushort_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(ushort?),
                typeof(ABI.System.Nullable_ushort),
                "Windows.Foundation.IReference`1<UInt16>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<UInt32>"</c> WinRT type.</summary>
        public static void RegisterNullableUIntMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _uint_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(uint?),
                typeof(ABI.System.Nullable_uint),
                "Windows.Foundation.IReference`1<UInt32>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Int64>"</c> WinRT type.</summary>
        public static void RegisterNullableLongMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _long_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(long?),
                typeof(ABI.System.Nullable_long),
                "Windows.Foundation.IReference`1<Int64>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<UInt64>"</c> WinRT type.</summary>
        public static void RegisterNullableULongMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _ulong_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(ulong?),
                typeof(ABI.System.Nullable_ulong),
                "Windows.Foundation.IReference`1<UInt64>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Single>"</c> WinRT type.</summary>
        public static void RegisterNullableFloatMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _float_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(float?),
                typeof(ABI.System.Nullable_float),
                "Windows.Foundation.IReference`1<Single>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Double>"</c> WinRT type.</summary>
        public static void RegisterNullableDoubleMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _double_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(double?),
                typeof(ABI.System.Nullable_double),
                "Windows.Foundation.IReference`1<Double>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Char16>"</c> WinRT type.</summary>
        public static void RegisterNullableCharMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _char_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(char?),
                typeof(ABI.System.Nullable_char),
                "Windows.Foundation.IReference`1<Char16>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Boolean>"</c> WinRT type.</summary>
        public static void RegisterNullableBoolMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _bool_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(bool?),
                typeof(ABI.System.Nullable_bool),
                "Windows.Foundation.IReference`1<Boolean>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Guid>"</c> WinRT type.</summary>
        public static void RegisterNullableGuidMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Guid_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Guid?),
                typeof(ABI.System.Nullable_guid),
                "Windows.Foundation.IReference`1<Guid>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<Windows.Foundation.DateTime>"</c> WinRT type.</summary>
        public static void RegisterNullableDateTimeOffsetMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _DateTimeOffset_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(DateTimeOffset?),
                typeof(ABI.System.Nullable_DateTimeOffset),
                "Windows.Foundation.IReference`1<Windows.Foundation.DateTime>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IReference`1<TimeSpan>"</c> WinRT type.</summary>
        public static void RegisterNullableTimeSpanMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _TimeSpan_, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(TimeSpan?),
                typeof(ABI.System.Nullable_TimeSpan),
                "Windows.Foundation.IReference`1<TimeSpan>",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.DateTime"</c> WinRT type.</summary>
        public static void RegisterDateTimeOffsetMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _DateTimeOffset, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(DateTimeOffset),
                typeof(ABI.System.DateTimeOffset),
                "Windows.Foundation.DateTime",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.HResult"</c> WinRT type.</summary>
        public static void RegisterExceptionMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Exception, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Exception),
                typeof(ABI.System.Exception),
                "Windows.Foundation.HResult",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.TimeSpan"</c> WinRT type.</summary>
        public static void RegisterTimeSpanMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _TimeSpan, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(TimeSpan),
                typeof(ABI.System.TimeSpan),
                "Windows.Foundation.TimeSpan",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Uri"</c> WinRT type.</summary>
        public static void RegisterUriMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Uri, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Uri),
                typeof(ABI.System.Uri),
                "Windows.Foundation.Uri",
                isRuntimeClass: true);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs"</c> WinRT type.</summary>
        public static void RegisterDataErrorsChangedEventArgsMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _DataErrorsChangedEventArgs, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(DataErrorsChangedEventArgs),
                typeof(ABI.System.ComponentModel.DataErrorsChangedEventArgs),
                "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs",
                isRuntimeClass: true);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Data.PropertyChangedEventArgs"</c> WinRT type.</summary>
        public static void RegisterPropertyChangedEventArgsMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _PropertyChangedEventArgs, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(PropertyChangedEventArgs),
                typeof(ABI.System.ComponentModel.PropertyChangedEventArgs),
                "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",
                isRuntimeClass: true);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Data.PropertyChangedEventHandler"</c> WinRT type.</summary>
        public static void RegisterPropertyChangedEventHandlerMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _PropertyChangedEventHandler, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(PropertyChangedEventHandler),
                typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
                "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Data.INotifyDataErrorInfo"</c> WinRT type.</summary>
        public static void RegisterINotifyDataErrorInfoMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _INotifyDataErrorInfo, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(INotifyDataErrorInfo),
                typeof(ABI.System.ComponentModel.INotifyDataErrorInfo),
                "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Data.INotifyPropertyChanged"</c> WinRT type.</summary>
        public static void RegisterINotifyPropertyChangedMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _INotifyPropertyChanged, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(INotifyPropertyChanged),
                typeof(ABI.System.ComponentModel.INotifyPropertyChanged),
                "Microsoft.UI.Xaml.Data.INotifyPropertyChanged",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.ICommand"</c> WinRT type.</summary>
        public static void RegisterICommandMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _ICommand, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(ICommand),
                typeof(ABI.System.Windows.Input.ICommand),
                "Microsoft.UI.Xaml.Interop.ICommand",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.IXamlServiceProvider"</c> WinRT type.</summary>
        public static void RegisterIServiceProviderMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IServiceProvider, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IServiceProvider),
                typeof(ABI.System.IServiceProvider),
                "Microsoft.UI.Xaml.IXamlServiceProvider",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.EventHandler`1"</c> WinRT type.</summary>
        public static void RegisterEventHandlerOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _EventHandler__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(EventHandler<>),
                typeof(ABI.System.EventHandler<>),
                "Windows.Foundation.EventHandler`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IKeyValuePair`2"</c> WinRT type.</summary>
        public static void RegisterKeyValuePairOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _KeyValuePair___, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(KeyValuePair<,>),
                typeof(ABI.System.Collections.Generic.KeyValuePair<,>),
                "Windows.Foundation.Collections.IKeyValuePair`2",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IIterable`1"</c> WinRT type.</summary>
        public static void RegisterIEnumerableOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IEnumerable__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IEnumerable<>),
                typeof(ABI.System.Collections.Generic.IEnumerable<>),
                "Windows.Foundation.Collections.IIterable`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IIterator`1"</c> WinRT type.</summary>
        public static void RegisterIEnumeratorOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IEnumerator__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IEnumerator<>),
                typeof(ABI.System.Collections.Generic.IEnumerator<>),
                "Windows.Foundation.Collections.IIterator`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IVector`1"</c> WinRT type.</summary>
        public static void RegisterIListOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IList__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IList<>),
                typeof(ABI.System.Collections.Generic.IList<>),
                "Windows.Foundation.Collections.IVector`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IVectorView`1"</c> WinRT type.</summary>
        public static void RegisterIReadOnlyListOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IReadOnlyList__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IReadOnlyList<>),
                typeof(ABI.System.Collections.Generic.IReadOnlyList<>),
                "Windows.Foundation.Collections.IVectorView`1",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IMap`2"</c> WinRT type.</summary>
        public static void RegisterIDictionaryOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IDictionary___, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IDictionary<,>),
                typeof(ABI.System.Collections.Generic.IDictionary<,>),
                "Windows.Foundation.Collections.IMap`2",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Collections.IMapView`2"</c> WinRT type.</summary>
        public static void RegisterIReadOnlyDictionaryOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IReadOnlyDictionary___, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IReadOnlyDictionary<,>),
                typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>),
                "Windows.Foundation.Collections.IMapView`2",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.IClosable"</c> WinRT type.</summary>
        public static void RegisterIDisposableMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IDisposable, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IDisposable),
                typeof(ABI.System.IDisposable),
                "Windows.Foundation.IClosable",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.IBindableIterable"</c> WinRT type.</summary>
        public static void RegisterIEnumerableMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IEnumerable, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IEnumerable),
                typeof(ABI.System.Collections.IEnumerable),
                "Microsoft.UI.Xaml.Interop.IBindableIterable",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.IBindableVector"</c> WinRT type.</summary>
        public static void RegisterIListMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IList, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(IList),
                typeof(ABI.System.Collections.IList),
                "Microsoft.UI.Xaml.Interop.IBindableVector",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.INotifyCollectionChanged"</c> WinRT type.</summary>
        public static void RegisterINotifyCollectionChangedMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _INotifyCollectionChanged, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(INotifyCollectionChanged),
                typeof(ABI.System.Collections.Specialized.INotifyCollectionChanged),
                "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction"</c> WinRT type.</summary>
        public static void RegisterNotifyCollectionChangedActionMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _NotifyCollectionChangedAction, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(NotifyCollectionChangedAction),
                typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedAction),
                "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs"</c> WinRT type.</summary>
        public static void RegisterNotifyCollectionChangedEventArgsMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _NotifyCollectionChangedEventArgs, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(NotifyCollectionChangedEventArgs),
                typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs),
                "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
                isRuntimeClass: true);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler"</c> WinRT type.</summary>
        public static void RegisterNotifyCollectionChangedEventHandlerMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _NotifyCollectionChangedEventHandler, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(NotifyCollectionChangedEventHandler),
                typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
                "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Matrix3x2"</c> WinRT type.</summary>
        public static void RegisterMatrix3x2Mapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Matrix3x2, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Matrix3x2),
                typeof(ABI.System.Numerics.Matrix3x2),
                "Windows.Foundation.Numerics.Matrix3x2",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Matrix4x4"</c> WinRT type.</summary>
        public static void RegisterMatrix4x4Mapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Matrix4x4, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Matrix4x4),
                typeof(ABI.System.Numerics.Matrix4x4),
                "Windows.Foundation.Numerics.Matrix4x4",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Plane"</c> WinRT type.</summary>
        public static void RegisterPlaneMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Plane, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Plane),
                typeof(ABI.System.Numerics.Plane),
                "Windows.Foundation.Numerics.Plane",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Quaternion"</c> WinRT type.</summary>
        public static void RegisterQuaternionMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Quaternion, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Quaternion),
                typeof(ABI.System.Numerics.Quaternion),
                "Windows.Foundation.Numerics.Quaternion",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Vector2"</c> WinRT type.</summary>
        public static void RegisterVector2Mapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Vector2, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Vector2),
                typeof(ABI.System.Numerics.Vector2),
                "Windows.Foundation.Numerics.Vector2",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Vector3"</c> WinRT type.</summary>
        public static void RegisterVector3Mapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Vector3, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Vector3),
                typeof(ABI.System.Numerics.Vector3),
                "Windows.Foundation.Numerics.Vector3",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <c>"Windows.Foundation.Numerics.Vector4"</c> WinRT type.</summary>
        public static void RegisterVector4Mapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _Vector4, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(Vector4),
                typeof(ABI.System.Numerics.Vector4),
                "Windows.Foundation.Numerics.Vector4",
                isRuntimeClass: false);
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="EventHandler"/> type.</summary>
        public static void RegisterEventHandlerMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _EventHandler, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomAbiTypeMapping(
                typeof(EventHandler),
                typeof(ABI.System.EventHandler));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IMap{K, V}"/> type.</summary>
        public static void RegisterIMapOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IMap___, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IMap<,>),
                typeof(ABI.System.Collections.Generic.IDictionary<,>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IVector{T}"/> type.</summary>
        public static void RegisterIVectorOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IVector__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IVector<>),
                typeof(ABI.System.Collections.Generic.IList<>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IMapView{K, V}"/> type.</summary>
        public static void RegisterIMapViewOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IMapView___, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IMapView<,>),
                typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IVectorView{T}"/> type.</summary>
        public static void RegisterIVectorViewOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IVectorView__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IVectorView<>),
                typeof(ABI.System.Collections.Generic.IReadOnlyList<>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IBindableVector"/> type.</summary>
        public static void RegisterIBindableVectorMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IBindableVector, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IBindableVector),
                typeof(ABI.System.Collections.IList));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="ICollection{T}"/> type.</summary>
        public static void RegisterICollectionOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _ICollection__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(ICollection<>),
                typeof(ABI.System.Collections.Generic.ICollection<>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="IReadOnlyCollection{T}"/> type.</summary>
        public static void RegisterIReadOnlyCollectionOpenGenericMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _IReadOnlyCollection__, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(IReadOnlyCollection<>),
                typeof(ABI.System.Collections.Generic.IReadOnlyCollection<>));
        }

        /// <summary>Registers the custom ABI type mapping for the <see cref="ICollection"/> type.</summary>
        public static void RegisterICollectionMapping()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _ICollection, 1, 0) == 1)
            {
                return;
            }

            RegisterCustomTypeToHelperTypeMapping(
                typeof(ICollection),
                typeof(ABI.System.Collections.ICollection));
        }
    }
}

#endif