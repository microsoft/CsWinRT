using Microsoft.CodeAnalysis;

namespace WinRT.SourceGenerator
{
    public class WinRTRules
    {
        /// <summary>Helper function that does most of the boilerplate information needed for Diagnostics</summary> 
        /// <param name="id">string, a short identifier for the diagnostic </param>
        /// <param name="title">string, a few words generally describing the diagnostic</param>
        /// <param name="messageFormat">string, describes the diagnostic -- formatted with {0}, ... -- 
        /// such that data can be passed in for the code the diagnostic is reported for</param>
        private static DiagnosticDescriptor MakeRule(string id, string title, string messageFormat, bool isError = true, bool isWarning = false)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: "Usage",
                defaultSeverity: isError ? DiagnosticSeverity.Error : isWarning? DiagnosticSeverity.Warning : DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                helpLinkUri: "https://github.com/microsoft/CsWinRT/tree/master/src/Authoring/WinRT.SourceGenerator/AnalyzerReleases.Unshipped.md");
        }

        public static DiagnosticDescriptor PrivateGetterRule = MakeRule(
            "CsWinRT1000",
            CsWinRTDiagnosticStrings.PrivateGetterRule_Brief,
            CsWinRTDiagnosticStrings.PrivateGetterRule_Text);

        public static DiagnosticDescriptor DisjointNamespaceRule = MakeRule(
            "CsWinRT1001",
            CsWinRTDiagnosticStrings.DisjointNamespaceRule_Brief,
            CsWinRTDiagnosticStrings.DisjointNamespaceRule_Text1 + " " + CsWinRTDiagnosticStrings.DisjointNamespaceRule_Text2);

        public static DiagnosticDescriptor NamespacesDifferByCase = MakeRule(
            "CsWinRT1002",
            CsWinRTDiagnosticStrings.NamespacesDifferByCase_Brief,
            CsWinRTDiagnosticStrings.NamespacesDifferByCase_Text);

        public static DiagnosticDescriptor NoPublicTypesRule = MakeRule(
            "CsWinRT1003",
            CsWinRTDiagnosticStrings.NoPublicTypesRule_Brief,
            CsWinRTDiagnosticStrings.NoPublicTypesRule_Text);

        public static DiagnosticDescriptor GenericTypeRule = MakeRule(
            "CsWinRT1004",
            CsWinRTDiagnosticStrings.GenericTypeRule_Brief,
            CsWinRTDiagnosticStrings.GenericTypeRule_Text);

        public static DiagnosticDescriptor UnsealedClassRule = MakeRule(
            "CsWinRT1005",
            CsWinRTDiagnosticStrings.UnsealedClassRule_Brief,
            CsWinRTDiagnosticStrings.UnsealedClassRule_Text);

        public static DiagnosticDescriptor UnsupportedTypeRule = MakeRule(
            "CsWinRT1006",
            CsWinRTDiagnosticStrings.UnsupportedTypeRule_Brief,
            CsWinRTDiagnosticStrings.UnsupportedTypeRule_Text1
            + " " 
            + CsWinRTDiagnosticStrings.UnsupportedTypeRule_Text2
            + " "
            + CsWinRTDiagnosticStrings.UnsupportedTypeRule_Text3
            + " "
            + CsWinRTDiagnosticStrings.UnsupportedTypeRule_Text4);

        public static DiagnosticDescriptor StructWithNoFieldsRule = MakeRule(
            "CsWinRT1007",
            CsWinRTDiagnosticStrings.StructWithNoFieldsRule_Brief,
            CsWinRTDiagnosticStrings.StructWithNoFieldsRule_Text);

        public static DiagnosticDescriptor NonWinRTInterface = MakeRule(
            "CsWinRT1008",
            CsWinRTDiagnosticStrings.NonWinRTInterface_Brief,
            CsWinRTDiagnosticStrings.NonWinRTInterface_Text);

        public static DiagnosticDescriptor ClassConstructorRule = MakeRule(
            "CsWinRT1009",
            CsWinRTDiagnosticStrings.ClassConstructorRule_Brief,
            CsWinRTDiagnosticStrings.ClassConstructorRule_Text);

        public static DiagnosticDescriptor ParameterNamedValueRule = MakeRule(
            "CsWinRT1010",
            CsWinRTDiagnosticStrings.ParameterNamedValueRule_Brief,
            CsWinRTDiagnosticStrings.ParameterNamedValueRule_Text);

        public static DiagnosticDescriptor StructHasPrivateFieldRule = MakeRule(
            "CsWinRT1011",
            CsWinRTDiagnosticStrings.StructHasPrivateFieldRule_Brief,
            CsWinRTDiagnosticStrings.StructHasPrivateFieldRule_Text);

        public static DiagnosticDescriptor StructHasConstFieldRule = MakeRule(
            "CsWinRT1012",
            CsWinRTDiagnosticStrings.StructHasConstFieldRule_Brief,
            CsWinRTDiagnosticStrings.StructHasConstFieldRule_Text);

        public static DiagnosticDescriptor StructHasInvalidFieldRule = MakeRule(
            "CsWinRT1013",
            CsWinRTDiagnosticStrings.StructHasInvalidFieldRule_Brief,
            CsWinRTDiagnosticStrings.StructHasInvalidFieldRule_Text1 
            + " " 
            + CsWinRTDiagnosticStrings.StructHasInvalidFieldRule_Text2);

        public static DiagnosticDescriptor OperatorOverloadedRule = MakeRule(
            "CsWinRT1014",
            CsWinRTDiagnosticStrings.OperatorOverloadedRule_Brief,
            CsWinRTDiagnosticStrings.OperatorOverloadedRule_Text);

        public static DiagnosticDescriptor MultipleDefaultOverloadAttribute = MakeRule(
            "CsWinRT1015",
            CsWinRTDiagnosticStrings.MultipleDefaultOverloadAttribute_Brief,
            CsWinRTDiagnosticStrings.MultipleDefaultOverloadAttribute_Text1
            + " " 
            + CsWinRTDiagnosticStrings.MultipleDefaultOverloadAttribute_Text2);

        public static DiagnosticDescriptor NeedDefaultOverloadAttribute = MakeRule(
            "CsWinRT1016",
            CsWinRTDiagnosticStrings.NeedDefaultOverloadAttribute_Brief,
            CsWinRTDiagnosticStrings.NeedDefaultOverloadAttribute_Text);

        public static DiagnosticDescriptor JaggedArrayRule = MakeRule(
            "CsWinRT1017",
            CsWinRTDiagnosticStrings.JaggedArrayRule_Brief,
            CsWinRTDiagnosticStrings.JaggedArrayRule_Text);

        public static DiagnosticDescriptor MultiDimensionalArrayRule = MakeRule(
            "CsWinRT1018",
            CsWinRTDiagnosticStrings.MultiDimensionalArrayRule_Brief,
            CsWinRTDiagnosticStrings.MultiDimensionalArrayRule_Text);

        public static DiagnosticDescriptor RefParameterFound = MakeRule(
           "CsWinRT1020",
           CsWinRTDiagnosticStrings.RefParameterFound_Brief,
           CsWinRTDiagnosticStrings.RefParameterFound_Text);

        public static DiagnosticDescriptor ArrayMarkedInOrOut = MakeRule(
            "CsWinRT1021",
            CsWinRTDiagnosticStrings.ArrayMarkedInOrOut_Brief,
            CsWinRTDiagnosticStrings.ArrayMarkedInOrOut_Text1
            + " "
            + CsWinRTDiagnosticStrings.ArrayMarkedInOrOut_Text2
            + " "
            + CsWinRTDiagnosticStrings.ArrayMarkedInOrOut_Text3);

        public static DiagnosticDescriptor NonArrayMarkedInOrOut = MakeRule(
            "CsWinRT1022",
            CsWinRTDiagnosticStrings.NonArrayMarkedInOrOut_Brief,
            CsWinRTDiagnosticStrings.NonArrayMarkedInOrOut_Text1
            + " "
            + CsWinRTDiagnosticStrings.NonArrayMarkedInOrOut_Text2);

        public static DiagnosticDescriptor ArrayParamMarkedBoth = MakeRule(
            "CsWinRT1023",
            CsWinRTDiagnosticStrings.ArrayParamMarkedBoth_Brief,
            CsWinRTDiagnosticStrings.ArrayParamMarkedBoth_Text1 
            + " " 
            + CsWinRTDiagnosticStrings.ArrayParamMarkedBoth_Text2);

        public static DiagnosticDescriptor ArrayOutputParamMarkedRead = MakeRule(
            "CsWinRT1024",
            CsWinRTDiagnosticStrings.ArrayOutputParamMarkedRead_Brief,
            CsWinRTDiagnosticStrings.ArrayOutputParamMarkedRead_Text1
            + " " 
            + CsWinRTDiagnosticStrings.ArrayOutputParamMarkedRead_Text2);

        public static DiagnosticDescriptor ArrayParamNotMarked = MakeRule(
            "CsWinRT1025",
            CsWinRTDiagnosticStrings.ArrayParamNotMarked_Brief,
            CsWinRTDiagnosticStrings.ArrayParamNotMarked_Text1
            + " "
            + CsWinRTDiagnosticStrings.ArrayParamNotMarked_Text2);

        public static DiagnosticDescriptor NonArrayMarked = MakeRule(
            "CsWinRT1026",
            CsWinRTDiagnosticStrings.NonArrayMarked_Brief,
            CsWinRTDiagnosticStrings.NonArrayMarked_Text1 
            + " " 
            + CsWinRTDiagnosticStrings.NonArrayMarked_Text2);

        public static DiagnosticDescriptor UnimplementedInterface = MakeRule(
            "CsWinRT1027",
            CsWinRTDiagnosticStrings.UnimplementedInterface_Brief,
            CsWinRTDiagnosticStrings.UnimplementedInterface_Text);

        public static DiagnosticDescriptor ClassNotAotCompatibleWarning = MakeRule(
            "CsWinRT1028",
            CsWinRTDiagnosticStrings.ClassNotMarkedPartial_Brief,
            CsWinRTDiagnosticStrings.ClassNotMarkedPartial_Text,
            false,
            true);

        public static DiagnosticDescriptor ClassNotAotCompatibleInfo = MakeRule(
            "CsWinRT1028",
            CsWinRTDiagnosticStrings.ClassNotMarkedPartial_Brief,
            CsWinRTDiagnosticStrings.ClassNotMarkedPartial_Text,
            false);

        public static DiagnosticDescriptor ClassNotAotCompatibleOldProjectionWarning = MakeRule(
            "CsWinRT1029",
            CsWinRTDiagnosticStrings.ClassImplementsOldProjection_Brief,
            CsWinRTDiagnosticStrings.ClassImplementsOldProjection_Text,
            false,
            true);

        public static DiagnosticDescriptor ClassNotAotCompatibleOldProjectionInfo = MakeRule(
            "CsWinRT1029",
            CsWinRTDiagnosticStrings.ClassImplementsOldProjection_Brief,
            CsWinRTDiagnosticStrings.ClassImplementsOldProjection_Text,
            false);
    }
} 
