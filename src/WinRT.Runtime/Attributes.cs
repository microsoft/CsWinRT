// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

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
}
