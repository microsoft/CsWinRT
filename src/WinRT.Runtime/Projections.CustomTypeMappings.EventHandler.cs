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

        /// <summary>
        /// ABI interfaces for <see cref="EventHandler"/>, conditionally set from <see cref="RegisterEventHandlerMapping"/>
        /// to avoid rooting <see cref="ABI.System.EventHandler"/> when the type mapping for this type isn't needed.
        /// </summary>
        private static ComInterfaceEntry[]? _AbiEventHandlerExposedInterfaces;

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
    }
}

#endif