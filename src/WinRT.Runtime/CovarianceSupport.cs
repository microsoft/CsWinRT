using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT
{
    internal class CovarianceSupport
    {
        /* Hold on to the type we might have covariants for */
        public Type baseType { get; set; }

        /* And those covariant types */
        public HashSet<Type> covariantTypes { get; set; }

        /*  Implementation holds the type we want to return to support covariance
         *  E.g. if we want to use IReadOnlyList<Foo> as IEnumerable<IFoo> then we need an object that supports both
         *  and we get this object by double casting the IWinRTObject IDIC is trying to cast                          */
        public object? Implementation { get; set; }
       
        /* Memoize adding variants */
        private HashSet<Type> covariantCache { get; set; }

        /* Constructor */
        public CovarianceSupport(Type t) { baseType = t; covariantTypes = new(); covariantCache = new(); Implementation = null; }

        private void AddVariant(Type type)
        {
            /* Only generic types support covariance -- might help to consider that we really 
             * want to know about covariance of the type arguments of a generic type */
            if (type.IsGenericType)
            { 
                /* If we've already seen this type's covariants, just return the values we computed then */
                if (covariantCache.Contains(type)) { return; }

                /* Otherwise compute all the covariant types for this type and add them to our set */
                Projections.TryGetCompatibleWindowsRuntimeTypesForVariantType(type, out var variantTypes);
                if (variantTypes != null) { covariantTypes.UnionWith(variantTypes); }

                /* Make a note that we have processed this type already, so we can use that result if we see this type again */
                covariantCache.Add(type);
            } 
        }
#if NET5_0
        public object FindImplementation(IWinRTObject _this, Type covariant)
        { 
            if (covariantTypes.Contains(covariant))
            {
                RuntimeTypeHandle implementationType = _this.GetInterfaceImplementation(baseType.TypeHandle);
                // Type implementationType2 = implementationType.GetType();
                var objRef = _this.GetObjectReferenceForType(implementationType);
                
                /* 
                 * We either implement something like the following (if we can) or we take the logic we have in IEnumerable.net5.cs 
                 *   and add it to the other covariant-compatible projections
                
                switch (covariant.Name)
                {
                    case typeof(global::System.Collections.Generic.IEnumerable<T>):
                        Implementation = (global::System.Collections.Generic.IEnumerable<T>)objRef;
                        break;
                    case typeof(global::System.Collections.Generic.IReadOnlyList<T>):
                        Implementation = (global::System.Collections.Generic.IReadOnlyList<T>)objRef;
                        break;
                    case typeof(global::System.Collections.Generic.IEnumerator<T>):
                        Implementation = (global::System.Collections.Generic.IEnumerator<T>)objRef;
                        break;
                    case typeof(global::System.Collections.Generic.IReadOnlyCollection<T>):
                        Implementation = (global::System.Collections.Generic.IReadOnlyCollection<T>)objRef;
                        break;
                }
                implementation = (global::System.Collections.Generic.IEnumerable<T>)objRef;
                
                */ 
            }
            return null;
        }
#endif
        public void FindAllVariants()
        { 
            AddVariant(baseType); 
            foreach (var inheritedType in baseType.GetInterfaces())
            {
                AddVariant(inheritedType); 
            }
        }

        public void FindAllVariants(Type type)
        { 
            AddVariant(type); 
            foreach (var inheritedType in type.GetInterfaces())
            {
                AddVariant(inheritedType); 
            }
        }
    }
}
