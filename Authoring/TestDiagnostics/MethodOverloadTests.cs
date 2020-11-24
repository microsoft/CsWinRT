using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    // valid method overload tests
    public sealed class TwoOverloads_DiffParamCount_Valid
    {

        // pretty sure diagnostic will get thrown for this right now BUT it shouldn't 

        // the fix could be to append the method name (as a string) with its arity (as a string) and add that as the key to the map
        // so the map isnt exactly the method name, but methodnameN where N is the arity of the methodname at that time
        // think this fix would mean no logic has to change on the "have we seen this methodname before and did it have the attribute"
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n, int m) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_OneInList_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_OneIrrelevatAttribute_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_TwoLists_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class ThreeOverloads_OneAttribute_Valid
    {

        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        public bool OverloadExample(bool b) { return b; }
    }

    public sealed class ThreeOverloads_OneAttribute_2_Valid
    {
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }

    public sealed class TwoOverloads_OneAttribute_3_Valid
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    // invalid method overload tests 
    public sealed class TwoOverloads_NoAttribute_Invalid
    {
        // hmm 
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_OneInList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_NoAttribute_OneIrrevAttr_Invalid
    {
        // hmm
        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_BothInList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_TwoLists_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
    
    public sealed class TwoOverloads_TwoAttribute_OneInSeparateList_OneNot_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_BothInSeparateList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_Invalid
    {

        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class ThreeOverloads_TwoAttributes_Invalid
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }
}
