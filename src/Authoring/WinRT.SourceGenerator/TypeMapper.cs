using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using static Generator.GeneratorHelper;

namespace Generator
{
    internal sealed class TypeMapper
    {
        private readonly Dictionary<string, MappedType> typeMapping;

        // Based on whether System.Type is used in an attribute declaration or elsewhere, we need to choose the correct custom mapping
        // as attributes don't use the TypeName mapping.
        private static (string, string, string, bool, bool) GetSystemTypeCustomMapping(ISymbol containingSymbol)
        {
            bool isDefinedInAttribute =
                containingSymbol != null &&
                    string.CompareOrdinal((containingSymbol as INamedTypeSymbol).BaseType?.ToString(), "System.Attribute") == 0;
            return isDefinedInAttribute ?
                ("System", "Type", "mscorlib", true, false) :
                ("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", false, true);
        }

        public TypeMapper(bool useWindowsUIXamlProjections)
        {
            // This should be in sync with the reverse mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
            if (useWindowsUIXamlProjections)
            {
                typeMapping = new(StringComparer.Ordinal)
                {
                    { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract") },
                    { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) },
                    { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
                    { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
                    { "System.Object", new MappedType("System", "Object", "mscorlib" ) },
                    { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
                    { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Windows.UI.Xaml.Data", "INotifyPropertyChanged", "Windows.UI.Xaml") },
                    { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Windows.UI.Xaml.Data", "PropertyChangedEventArgs", "Windows.UI.Xaml") },
                    { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Windows.UI.Xaml.Data", "PropertyChangedEventHandler", "Windows.UI.Xaml") },
                    { "System.Windows.Input.ICommand", new MappedType("Windows.UI.Xaml.Input", "ICommand", "Windows.UI.Xaml") },
                    { "System.Collections.IEnumerable", new MappedType("Windows.UI.Xaml.Interop", "IBindableIterable", "Windows.UI.Xaml") },
                    { "System.Collections.IList", new MappedType("Windows.UI.Xaml.Interop", "IBindableVector", "Windows.UI.Xaml") },
                    { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Windows.UI.Xaml.Interop", "INotifyCollectionChanged", "Windows.UI.Xaml") },
                    { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Windows.UI.Xaml", true, true) },
                    { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Windows.UI.Xaml") },
                    { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Windows.UI.Xaml") },
                    { "WinRT.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
                    { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
                    { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
                    { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true) },
                };
            }
            else
            {
                typeMapping = new(StringComparer.Ordinal)
                {
                    { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract") },
                    { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) },
                    { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
                    { "System.IServiceProvider", new MappedType("Microsoft.UI.Xaml", "IXamlServiceProvider", "Microsoft.UI") },
                    { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
                    { "System.Object", new MappedType("System", "Object", "mscorlib" ) },
                    { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true, false) },
                    { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
                    { "System.ComponentModel.DataErrorsChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI") },
                    { "System.ComponentModel.INotifyDataErrorInfo", new MappedType("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo", "Microsoft.UI") },
                    { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged", "Microsoft.UI") },
                    { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "Microsoft.UI") },
                    { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI") },
                    { "System.Windows.Input.ICommand", new MappedType("Microsoft.UI.Xaml.Input", "ICommand", "Microsoft.UI") },
                    { "System.Collections.IEnumerable", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableIterable", "Microsoft.UI") },
                    { "System.Collections.IList", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableVector", "Microsoft.UI") },
                    { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged", "Microsoft.UI") },
                    { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Microsoft.UI", true, true) },
                    { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Microsoft.UI") },
                    { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI") },
                    { "WinRT.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
                    { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true) },
                    { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
                    { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
                    { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
                    { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true) },
                };
            }
        }

        public bool HasMappingForType(string typeName) => typeMapping.ContainsKey(typeName);

        public MappedType GetMappedType(string typeName) => typeMapping[typeName];
    }
}
