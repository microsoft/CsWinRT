using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace ObjectLifetimeTests
{
    internal static class CollectionExtensions
    {
        internal static void Append(this UIElementCollection collection, UIElement element)
        {
            ((IList<UIElement>)collection).Add(element);
        }

        internal static void Clear(this UIElementCollection collection)
        {
            ((IList<UIElement>)collection).Clear();
        }

        internal static UIElement GetAt(this UIElementCollection collection, int index)
        {
            return ((IList<UIElement>)collection)[index];
        }

        internal static void RemoveAt(this UIElementCollection collection, int index)
        {
            ((IList<UIElement>)collection).RemoveAt(index);
        }

        internal static void Append(this ItemCollection collection, UIElement element)
        {
            ((IList<object>)collection).Add(element);
        }

        internal static void Clear(this ItemCollection collection)
        {
            ((IList<object>)collection).Clear();
        }

        internal static object GetAt(this ItemCollection collection, int index)
        {
            return ((IList<object>)collection)[index];
        }

        internal static void RemoveAt(this ItemCollection collection, int index)
        {
            ((IList<object>)collection).RemoveAt(index);
        }

        internal static ColumnDefinition GetAt(this ColumnDefinitionCollection collection, int index)
        {
            return ((IList<ColumnDefinition>)collection)[index];
        }

        internal static object Lookup(this ResourceDictionary dictionary, object key)
        {
            return ((IDictionary<object, object>)dictionary)[key];
        }

        internal static void Remove(this ResourceDictionary dictionary, object key)
        {
            ((IDictionary<object, object>)dictionary).Remove(key);
        }
    }
}