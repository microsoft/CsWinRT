// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading;
using System.Windows.Input;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;
using static System.Runtime.InteropServices.ComWrappers;

#nullable enable

namespace WinRT
{
    /// <inheritdoc cref="Projections"/>
    partial class Projections
    {
        private static int _EventHandler;
        private static int _NotifyCollectionChangedEventHandler;
        private static int _PropertyChangedEventHandler;

        /// <summary>
        /// ABI interfaces for <see cref="EventHandler"/>, conditionally set from <see cref="RegisterEventHandlerMapping"/>
        /// to avoid rooting <see cref="ABI.System.EventHandler"/> when the type mapping for this type isn't needed.
        /// </summary>
        private static ComInterfaceEntry[]? _AbiEventHandlerExposedInterfaces;

        /// <summary>
        /// ABI interfaces for <see cref="NotifyCollectionChangedEventHandler"/>, conditionally set from <see cref="RegisterNotifyCollectionChangedEventHandlerMapping"/>
        /// to avoid rooting <see cref="ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler"/> when the type mapping for this type isn't needed.
        /// </summary>
        private static ComInterfaceEntry[]? _AbiNotifyCollectionChangedEventHandlerExposedInterfaces;

        /// <summary>
        /// ABI interfaces for <see cref="PropertyChangedEventHandler"/>, conditionally set from <see cref="RegisterPropertyChangedEventHandlerMapping"/>
        /// to avoid rooting <see cref="ABI.System.ComponentModel.PropertyChangedEventHandler"/> when the type mapping for this type isn't needed.
        /// </summary>
        private static ComInterfaceEntry[]? _AbiPropertyChangedEventHandlerExposedInterfaces;

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

            _AbiEventHandlerExposedInterfaces = ABI.System.EventHandler.GetExposedInterfaces();
        }

        /// <summary>
        /// Gets the ABI interfaces for <see cref="EventHandler"/>.
        /// </summary>
        /// <returns>The ABI interfaces for <see cref="EventHandler"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown if the type mapping is disabled.</exception>
        internal static ComInterfaceEntry[] GetAbiEventHandlerExposedInterfaces()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return ABI.System.EventHandler.GetExposedInterfaces();
            }

            ComInterfaceEntry[]? interfaces = _AbiEventHandlerExposedInterfaces;

            if (interfaces is null)
            {
                throw new NotSupportedException(
                    "Support for type mapping for the 'EventHandler' type is currently disabled. " +
                    "To enable it, either make sure to not set the 'CsWinRTEnableCustomTypeMappings' property to 'false', " +
                    "or manually enable support for this specific type by calling 'Projections.RegisterEventHandlerMapping()'.");
            }

            return interfaces;
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

            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                RegisterCustomAbiTypeMapping(
                    typeof(NotifyCollectionChangedEventHandler),
                    typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
                    "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler",
                    isRuntimeClass: false);
            }
            else
            {
                RegisterCustomAbiTypeMapping(
                    typeof(NotifyCollectionChangedEventHandler),
                    typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
                    "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler",
                    isRuntimeClass: false);
            }

            _AbiNotifyCollectionChangedEventHandlerExposedInterfaces = ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.GetExposedInterfaces();
        }

        /// <summary>
        /// Gets the ABI interfaces for <see cref="NotifyCollectionChangedEventHandler"/>.
        /// </summary>
        /// <returns>The ABI interfaces for <see cref="NotifyCollectionChangedEventHandler"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown if the type mapping is disabled.</exception>
        internal static ComInterfaceEntry[] GetAbiNotifyCollectionChangedEventHandlerExposedInterfaces()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.GetExposedInterfaces();
            }

            ComInterfaceEntry[]? interfaces = _AbiNotifyCollectionChangedEventHandlerExposedInterfaces;

            if (interfaces is null)
            {
                throw new NotSupportedException(
                    "Support for type mapping for the 'NotifyCollectionChangedEventHandler' type is currently disabled. " +
                    "To enable it, either make sure to not set the 'CsWinRTEnableCustomTypeMappings' property to 'false', " +
                    "or manually enable support for this specific type by calling 'Projections.RegisterNotifyCollectionChangedEventHandlerMapping()'.");
            }

            return interfaces;
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

            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                RegisterCustomAbiTypeMapping(
                    typeof(PropertyChangedEventHandler),
                    typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
                    "Windows.UI.Xaml.Data.PropertyChangedEventHandler",
                    isRuntimeClass: false);
            }
            else
            {
                RegisterCustomAbiTypeMapping(
                    typeof(PropertyChangedEventHandler),
                    typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
                    "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler",
                    isRuntimeClass: false);
            }

            _AbiPropertyChangedEventHandlerExposedInterfaces = ABI.System.ComponentModel.PropertyChangedEventHandler.GetExposedInterfaces();
        }

        /// <summary>
        /// Gets the ABI interfaces for <see cref="PropertyChangedEventHandler"/>.
        /// </summary>
        /// <returns>The ABI interfaces for <see cref="PropertyChangedEventHandler"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown if the type mapping is disabled.</exception>
        internal static ComInterfaceEntry[] GetAbiPropertyChangedEventHandlerExposedInterfaces()
        {
            if (FeatureSwitches.EnableDefaultCustomTypeMappings)
            {
                return ABI.System.ComponentModel.PropertyChangedEventHandler.GetExposedInterfaces();
            }

            ComInterfaceEntry[]? interfaces = _AbiPropertyChangedEventHandlerExposedInterfaces;

            if (interfaces is null)
            {
                throw new NotSupportedException(
                    "Support for type mapping for the 'PropertyChangedEventHandler' type is currently disabled. " +
                    "To enable it, either make sure to not set the 'CsWinRTEnableCustomTypeMappings' property to 'false', " +
                    "or manually enable support for this specific type by calling 'Projections.RegisterPropertyChangedEventHandlerMapping()'.");
            }

            return interfaces;
        }
    }
}

#endif