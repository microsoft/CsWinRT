// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

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

        public string SourceMetadata { get; }
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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type helperType)
        {
            HelperType = helperType;
        }

#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public Type HelperType { get; }
    }
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
