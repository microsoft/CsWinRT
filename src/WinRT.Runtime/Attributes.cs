// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WinRT
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public        
#endif
    sealed class ProjectedRuntimeClassAttribute : Attribute
    {
        public ProjectedRuntimeClassAttribute(string defaultInterfaceProp)
        {
            DefaultInterfaceProperty = defaultInterfaceProp;
        }

        public ProjectedRuntimeClassAttribute(Type defaultInterface)
        {
            DefaultInterface = defaultInterface;
        }

        public string DefaultInterfaceProperty { get; }
        public Type DefaultInterface { get; }
    }

#if NET
    [Obsolete("This attribute is only used for the .NET Standard 2.0 projections.")]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public        
#endif
    sealed class ObjectReferenceWrapperAttribute : Attribute
    {
        public ObjectReferenceWrapperAttribute(string objectReferenceField)
        {
            ObjectReferenceField = objectReferenceField;
        }

        public string ObjectReferenceField { get; }
    }

    /// <summary>
    /// When applied to a type, designates to WinRT.Runtime that this type represents a type defined in WinRT metadata.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public        
#endif 
    sealed class WindowsRuntimeTypeAttribute : Attribute
    {
        public WindowsRuntimeTypeAttribute(string sourceMetadata = null)
        {
            SourceMetadata = sourceMetadata;
        }

        public WindowsRuntimeTypeAttribute(string sourceMetadata, string guidSignature)
            :this(sourceMetadata)
        {
            GuidSignature = guidSignature;
        }

        public string SourceMetadata { get; }
        public string GuidSignature { get; }
    }

    /// <summary>
    /// When applied to a type, it specifies the provided type, if one is provided, is the ABI helper type for this type.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public
#endif
    sealed class WindowsRuntimeHelperTypeAttribute : Attribute
    {
        // Indicates no associated helper types (i.e. blittable types).
        public WindowsRuntimeHelperTypeAttribute()
        {
        }

        public WindowsRuntimeHelperTypeAttribute(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type helperType)
        {
            HelperType = helperType;
        }

#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public Type HelperType { get; }
    }

#if NET

#if EMBED
    internal
#else
    public
#endif
    interface IWinRTExposedTypeDetails
    {
        ComWrappers.ComInterfaceEntry[] GetExposedInterfaces();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public
#endif
    sealed class WinRTExposedTypeAttribute : Attribute
    {
        public WinRTExposedTypeAttribute()
        {
        }

        public WinRTExposedTypeAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type winrtExposedTypeDetails)
        {
            WinRTExposedTypeDetails = winrtExposedTypeDetails;
        }

        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return WinRTExposedTypeDetails != null ? 
                ((IWinRTExposedTypeDetails)Activator.CreateInstance(WinRTExposedTypeDetails)).GetExposedInterfaces() : 
                Array.Empty<ComWrappers.ComInterfaceEntry>();
        }

#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        internal Type WinRTExposedTypeDetails { get; }
    }

    /// <summary>
    /// An attributes used for generated RCW types, to expose a factory method for them.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
#if EMBED
    internal
#else
    public
#endif
    abstract class WinRTImplementationTypeRcwFactoryAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of a given RCW type, from an input <see cref="IInspectable"/> object.
        /// </summary>
        /// <param name="inspectable">The native <see cref="IInspectable"/> object to use to construct the RCW instance.</param>
        /// <returns>The resulting RCW instance wrapping the same native object as <paramref name="inspectable"/>.</returns>
        public abstract object CreateInstance(IInspectable inspectable);
    }

#endif
}

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
#if EMBED
    internal
#else
    public
#endif
    sealed class ReadOnlyArrayAttribute : Attribute
    {
    }

    [AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
#if EMBED
    internal
#else
    public
#endif
    sealed class WriteOnlyArrayAttribute : Attribute
    {
    }
}
