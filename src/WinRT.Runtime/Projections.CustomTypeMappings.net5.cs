// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;

namespace WinRT
{
    /// <inheritdoc cref="Projections"/>
    partial class Projections
    {
        /// <summary>Registers the custom ABI type mapping for the <see cref="IMap{K, V}"/> type.</summary>
        public static void RegisterIMapOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IMap<,>),
            typeof(ABI.System.Collections.Generic.IDictionary<,>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="IVector{T}"/> type.</summary>
        public static void RegisterIVectorOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IVector<>),
            typeof(ABI.System.Collections.Generic.IList<>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="IMapView{K, V}"/> type.</summary>
        public static void RegisterIMapViewOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IMapView<,>),
            typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="IVectorView{T}"/> type.</summary>
        public static void RegisterIVectorViewOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IVectorView<>),
            typeof(ABI.System.Collections.Generic.IReadOnlyList<>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="IBindableVector"/> type.</summary>
        public static void RegisterIBindableVectorMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IBindableVector),
            typeof(ABI.System.Collections.IList));

        /// <summary>Registers the custom ABI type mapping for the <see cref="ICollection{T}"/> type.</summary>
        public static void RegisterICollectionOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(ICollection<>),
            typeof(ABI.System.Collections.Generic.ICollection<>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="IReadOnlyCollection{T}"/> type.</summary>
        public static void RegisterIReadOnlyCollectionOpenGenericMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(IReadOnlyCollection<>),
            typeof(ABI.System.Collections.Generic.IReadOnlyCollection<>));

        /// <summary>Registers the custom ABI type mapping for the <see cref="ICollection"/> type.</summary>
        public static void RegisterICollectionMapping() => RegisterCustomTypeToHelperTypeMapping(
            typeof(ICollection),
            typeof(ABI.System.Collections.ICollection));
    }
}