// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

#pragma warning disable CA2255

#if !NET
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif

namespace WinRT
{
    /// <summary>
    /// Contains a module initializer for generated CsWinRT projections.
    /// </summary>
    internal static class ProjectionInitializer
    {
        /// <summary>
        /// The module initializer registering the current assembly via <see cref="ComWrappersSupport.RegisterProjectionAssembly"/>.
        /// </summary>
        [ModuleInitializer]
        public static void InitalizeProjection()
        {
            // ComWrappersSupport.RegisterProjectionAssembly(typeof(ProjectionInitializer).Assembly);
        }
    }
}
