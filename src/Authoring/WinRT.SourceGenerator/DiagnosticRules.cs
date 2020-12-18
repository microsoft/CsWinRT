using Microsoft.CodeAnalysis;

namespace WinRT.SourceGenerator
{
    public class DiagnosticRules
    {
        /// <summary>Helper function that does most of the boilerplate information needed for Diagnostics</summary> 
        /// <param name="id">string, a short identifier for the diagnostic </param>
        /// <param name="title">string, a few words generally describing the diagnostic</param>
        /// <param name="messageFormat">string, describes the diagnostic -- formatted with {0}, ... -- 
        /// such that data can be passed in for the code the diagnostic is reported for</param>
        private static DiagnosticDescriptor MakeRule(string id, string title, string messageFormat)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: "Usage",
                /* Warnings dont fail command line build; winmd generation is prevented regardless of severity.
                * Make this error when making final touches on this deliverable. */
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://docs.microsoft.com/en-us/previous-versions/hh977010(v=vs.110)");
        }

        public static DiagnosticDescriptor GenericTypeRule = MakeRule(
            "WME",
            "Class (or interface) is generic",
            "Type {0} is generic. Windows Runtime types cannot be generic.");


        public static DiagnosticDescriptor UnsealedClassRule = MakeRule(
            "WME",
            "Class is unsealed",
            "Exporting unsealed types is not supported. Please mark type {0} as sealed.");

        /* 
        Method '{0}' has a parameter of type '{1}' in its signature. 
        Although this generic type is not a valid Windows Runtime type, the type or its generic parameters implement interfaces that are valid Windows Runtime types. 
        Consider changing the type '{1}' in the method signature to one of the following types instead: 
        'System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyList<T>'.	
        */

        public static DiagnosticDescriptor UnsupportedTypeRule = MakeRule(
            "WME",
            "Exposing unsupported type",
            "The member '{0}' has the type '{1}' in its signature. The type '{1}' is not a valid Windows Runtime type."  
            + "Yet, the type (or its generic parameters) implement interfaces that are valid Windows Runtime types." 
            + "Consider changing the type '{1} in the member signature to one of the following types from System.Collections.Generic:\n{2}");
        
        public static DiagnosticDescriptor StructWithNoFieldsRule = MakeRule(
            "WME1060",
            "Empty struct rule",
            "Structure {0} contains no public fields. Windows Runtime structures must contain at least one public field.");
 
        public static DiagnosticDescriptor NonWinRTInterface = MakeRule(
            "WME1084",
            "Invalid Interface Inherited",
            "Runtime component class {0} cannot implement interface {1}, as the interface is not a valid Windows Runtime interface");

        public static DiagnosticDescriptor ClassConstructorRule = MakeRule(
            "WME1099",
            "Class Constructor Rule",
            "Runtime component class {0} cannot have multiple constructors of the same arity {1}");

        public static DiagnosticDescriptor ParameterNamedValueRule = MakeRule(
            "WME1092",
            "Parameter Named Value Rule",
            "The method {0} has a parameter named {1} which is the same as the default return value name. "
            + "Consider using another name for the parameter or use the System.Runtime.InteropServices.WindowsRuntime.ReturnValueNameAttribute "
            + "to explicitly specify the name of the return value.");

        public static DiagnosticDescriptor StructHasPrivateFieldRule = MakeRule(
            "WME1060(b)",
            "Private field in struct",
            "Structure {0} has non-public field. All fields must be public for Windows Runtime structures.");

        public static DiagnosticDescriptor StructHasConstFieldRule = MakeRule(
            "WME1060(b)",
            "Const field in struct",
            "Structure {0} has const field. Constants can only appear on Windows Runtime enumerations.");

        public static DiagnosticDescriptor StructHasInvalidFieldRule = MakeRule(
            "WME1060",
            "Invalid field in struct",
            "Structure {0} has field '{1}' of type {2}; {2} is not a valid Windows Runtime field type. Each field "
            + "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure.");

        public static DiagnosticDescriptor StructHasInvalidFieldRule2 = MakeRule(
            "WME1060",
            "Invalid field in struct",
            "Structure {0} has a field of type {1}; {1} is not a valid Windows Runtime field type. Each field "
            + "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure.");

        public static DiagnosticDescriptor OperatorOverloadedRule = MakeRule(
            "WME1087",
            "Operator overload exposed",
            "{0} is an operator overload. Managed types cannot expose operator overloads in the Windows Runtime");

        public static DiagnosticDescriptor MethodOverload_MultipleDefaultAttribute = MakeRule(
            "WME1059",
            "Only one overload should be designated default", 
            "In class {2}: Multiple {0}-parameter overloads of '{1}' are decorated with Windows.Foundation.Metadata.DefaultOverloadAttribute. The attribute may only be applied to one overload of the method.");

        public static DiagnosticDescriptor MethodOverload_NeedDefaultAttribute = MakeRule(
            "WME1085",
            "Multiple overloads seen, one needs a default", // todo better msg
            "In class {2}: The {0}-parameter overloads of {1} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        public static DiagnosticDescriptor ArraySignature_JaggedArrayRule = MakeRule(
            "WME1036",
            "Array signature found with jagged array, which is not a valid WinRT type",
            "Method {0} has a nested array of type {1} in its signature. Arrays in Windows Runtime method signature cannot be nested.");

        public static DiagnosticDescriptor ArraySignature_MultiDimensionalArrayRule = MakeRule(
            "WME1035",
            "Array signature found with multi-dimensional array, which is not a valid WinRT type",
            "Method '{0}' has a multi-dimensional array of type '{1}' in its signature. Arrays in Windows Runtime method signatures must be one dimensional.");

        public static DiagnosticDescriptor ArraySignature_SystemArrayRule = MakeRule(
            "WME1034",
            "Array signature found with System.Array instance, which is not a valid WinRT type",
            "In type {0}: the method {1} has signature that contains a System.Array instance; SystemArray is not a valid Windows Runtime type. Try using a different type like IList");

        public static DiagnosticDescriptor RefParameterFound = MakeRule(
           "WME",
           "Parameter passed by reference",
           "Method '{0}' has parameter '{1}' marked `ref`. Reference parameters are not allowed in Windows Runtime.");

        public static DiagnosticDescriptor ArrayMarkedInOrOut = MakeRule(
            "WME1103",
            "Array parameter marked InAttribute or OutAttribute",
            "Method '{0}' has parameter '{1}' which is an array, and which has either a "
            + "System.Runtime.InteropServices.InAttribute or a System.Runtime.InteropServices.OutAttribute. "
            + "In the Windows Runtime, array parameters must have either ReadOnlyArray or WriteOnlyArray. "
            + "Please remove these attributes or replace them with the appropriate Windows "
            + "Runtime attribute if necessary.");

        public static DiagnosticDescriptor NonArrayMarkedInOrOut = MakeRule(
            "WME1105",
            "Parameter (not array type) marked InAttribute or OutAttribute",
            "Method '{0}' has parameter '{1}' with a System.Runtime.InteropServices.InAttribute "
            + "or System.Runtime.InteropServices.OutAttribute.Windows Runtime does not support "
            + "marking parameters with System.Runtime.InteropServices.InAttribute or "
            + "System.Runtime.InteropServices.OutAttribute. Please consider removing "
            + "System.Runtime.InteropServices.InAttribute and replace "
            + "System.Runtime.InteropServices.OutAttribute with 'out' modifier instead.");

        public static DiagnosticDescriptor ArrayParamMarkedBoth = MakeRule(
            "WME1101",
            "Array paramter marked both ReadOnlyArray and WriteOnlyArray",
            "Method '{0}' has parameter '{1}' which is an array, and which has both ReadOnlyArray and WriteOnlyArray. "
            + "In the Windows Runtime, the contents array parameters must be either readable "
            + "or writable.Please remove one of the attributes from '{1}'.");

        public static DiagnosticDescriptor ArrayOutputParamMarkedRead = MakeRule(
            "WME1102",
            "Array parameter marked `out` and ReadOnlyArray",
            "Method '{0}' has an output parameter '{1}' which is an array, but which has ReadOnlyArray attribute. In the Windows Runtime, "
            + "the contents of output arrays are writable.Please remove the attribute from '{1}'.");

        public static DiagnosticDescriptor ArrayParamNotMarked = MakeRule(
            "WME1106",
            "Array parameter not marked ReadOnlyArray or WriteOnlyArray way",
            "Method '{0}' has parameter '{1}' which is an array. In the Windows Runtime, the "
            + "contents of array parameters must be either readable or writable.Please apply either ReadOnlyArray or WriteOnlyArray to '{1}'.");

        public static DiagnosticDescriptor NonArrayMarked = MakeRule(
            "WME1104",
            "Non-array parameter marked with ReadOnlyArray or WriteOnlyArray",
            "Method '{0}' has parameter '{1}' which is not an array, and which has either a "
            + "ReadOnlyArray attribute or a WriteOnlyArray attribute . Windows Runtime does "
            + "not support marking non-array parameters with ReadOnlyArray or WriteOnlyArray.");
    }
}
