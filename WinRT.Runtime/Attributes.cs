using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ProjectedRuntimeClassAttribute : Attribute
    {
        public ProjectedRuntimeClassAttribute(string defaultInterfaceField)
        {
            DefaultInterfaceField = defaultInterfaceField;
        }

        public string DefaultInterfaceField { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ObjectReferenceWrapperAttribute : Attribute
    {
        public ObjectReferenceWrapperAttribute(string objectReferenceField)
        {
            ObjectReferenceField = objectReferenceField;
        }

        public string ObjectReferenceField { get; }
    }
}
