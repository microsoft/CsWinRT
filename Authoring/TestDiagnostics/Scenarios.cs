using ABI.System.Numerics;
using ABI.Windows.ApplicationModel.Contacts;
using System;
using System.Runtime.ExceptionServices;
using Windows.Foundation;
using Windows.Foundation.Numerics;
using Windows.Security.Cryptography.Core;
using Windows.Web.Syndication;

namespace TestDiagnostics
{
 
    // ----------------------------------------------------------------------------    

    public sealed class ClassThatOverloadsOperator
    {
        public static ClassThatOverloadsOperator operator +(ClassThatOverloadsOperator thing)
        {
            return thing;
        }
    }

    
    public sealed class ParameterNamedDunderRetVal
    {
        public int Identity(int __retval)
        {
            return __retval;
        }
    }

    public sealed class SameArityConstructors
    {
        private int num;
        private string word;

        public SameArityConstructors(int i)
        {
            num = i;
            word = "dog";
        }
       
        public SameArityConstructors(string s)
        {
            num = 38;
            word = s;
        } 

        // Can test that multiple constructors are allowed (has been verified already, too)
        // as long as they have different arities by making one take none or 2 arguments 
    }

}
