﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WinRT.SourceGenerator {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class CsWinRTDiagnosticStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CsWinRTDiagnosticStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WinRT.SourceGenerator.CsWinRTDiagnosticStrings", typeof(CsWinRTDiagnosticStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array parameter marked InAttribute or OutAttribute.
        /// </summary>
        internal static string ArrayMarkedInOrOut_Brief {
            get {
                return ResourceManager.GetString("ArrayMarkedInOrOut_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; which is an array, and which has either a System.Runtime.InteropServices.InAttribute or a System.Runtime.InteropServices.OutAttribute..
        /// </summary>
        internal static string ArrayMarkedInOrOut_Text1 {
            get {
                return ResourceManager.GetString("ArrayMarkedInOrOut_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In the Windows Runtime, array parameters must have either ReadOnlyArray or WriteOnlyArray..
        /// </summary>
        internal static string ArrayMarkedInOrOut_Text2 {
            get {
                return ResourceManager.GetString("ArrayMarkedInOrOut_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please remove these attributes or replace them with the appropriate Windows Runtime attribute if necessary..
        /// </summary>
        internal static string ArrayMarkedInOrOut_Text3 {
            get {
                return ResourceManager.GetString("ArrayMarkedInOrOut_Text3", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array parameter marked `out` and ReadOnlyArray.
        /// </summary>
        internal static string ArrayOutputParamMarkedRead_Brief {
            get {
                return ResourceManager.GetString("ArrayOutputParamMarkedRead_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has an output parameter &apos;{1}&apos; which is an array, but which has ReadOnlyArray attribute..
        /// </summary>
        internal static string ArrayOutputParamMarkedRead_Text1 {
            get {
                return ResourceManager.GetString("ArrayOutputParamMarkedRead_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In the Windows Runtime, the contents of output arrays are writable. Please remove the attribute from &apos;{1}&apos;..
        /// </summary>
        internal static string ArrayOutputParamMarkedRead_Text2 {
            get {
                return ResourceManager.GetString("ArrayOutputParamMarkedRead_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array paramter marked both ReadOnlyArray and WriteOnlyArray.
        /// </summary>
        internal static string ArrayParamMarkedBoth_Brief {
            get {
                return ResourceManager.GetString("ArrayParamMarkedBoth_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; which is an array, and which has both ReadOnlyArray and WriteOnlyArray..
        /// </summary>
        internal static string ArrayParamMarkedBoth_Text1 {
            get {
                return ResourceManager.GetString("ArrayParamMarkedBoth_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In the Windows Runtime, the contents array parameters must be either readable or writable, please remove one of the attributes from &apos;{1}&apos;..
        /// </summary>
        internal static string ArrayParamMarkedBoth_Text2 {
            get {
                return ResourceManager.GetString("ArrayParamMarkedBoth_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array parameter not marked ReadOnlyArray or WriteOnlyArray way.
        /// </summary>
        internal static string ArrayParamNotMarked_Brief {
            get {
                return ResourceManager.GetString("ArrayParamNotMarked_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; which is an array..
        /// </summary>
        internal static string ArrayParamNotMarked_Text1 {
            get {
                return ResourceManager.GetString("ArrayParamNotMarked_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In the Windows Runtime, the contents of array parameters must be either readable or writable; please apply either ReadOnlyArray or WriteOnlyArray to &apos;{1}&apos;..
        /// </summary>
        internal static string ArrayParamNotMarked_Text2 {
            get {
                return ResourceManager.GetString("ArrayParamNotMarked_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; has attribute GeneratedBindableCustomProperty but it or a parent type isn&apos;t marked partial.  Type and any parent types should be marked partial to allow source generation for trimming and AOT compatibility..
        /// </summary>
        internal static string BindableCustomPropertyClassNotMarkedPartial_Text {
            get {
                return ResourceManager.GetString("BindableCustomPropertyClassNotMarkedPartial_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class Constructor Rule.
        /// </summary>
        internal static string ClassConstructorRule_Brief {
            get {
                return ResourceManager.GetString("ClassConstructorRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Classes cannot have multiple constructors of the same arity in the Windows Runtime, class {0} has multiple {1}-arity constructors.
        /// </summary>
        internal static string ClassConstructorRule_Text {
            get {
                return ResourceManager.GetString("ClassConstructorRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class not trimming / AOT compatible.
        /// </summary>
        internal static string ClassImplementsOldProjection_Brief {
            get {
                return ResourceManager.GetString("ClassImplementsOldProjection_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; implements WinRT interface(s) {1} generated using an older version of CsWinRT.  Update to a projection generated using CsWinRT 2.1.0 or later for trimming and AOT compatibility..
        /// </summary>
        internal static string ClassImplementsOldProjection_Text {
            get {
                return ResourceManager.GetString("ClassImplementsOldProjection_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class is not marked partial.
        /// </summary>
        internal static string ClassNotMarkedPartial_Brief {
            get {
                return ResourceManager.GetString("ClassNotMarkedPartial_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; implements WinRT interfaces but it or a parent type isn&apos;t marked partial.  Type and any parent types should be marked partial for trimming and AOT compatibility if passed across the WinRT ABI..
        /// </summary>
        internal static string ClassNotMarkedPartial_Text {
            get {
                return ResourceManager.GetString("ClassNotMarkedPartial_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; was generated using an older version of CsWinRT and due to the type being defined in multiple DLLs, CsWinRT can not generate compat code to make it trimming safe.  Update to a projection generated using CsWinRT 2.1.0 or later for trimming and AOT compatibility..
        /// </summary>
        internal static string ClassOldProjectionMultipleInstances_Text {
            get {
                return ResourceManager.GetString("ClassOldProjectionMultipleInstances_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Namespace is disjoint from main (winmd) namespace.
        /// </summary>
        internal static string DisjointNamespaceRule_Brief {
            get {
                return ResourceManager.GetString("DisjointNamespaceRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A public type has a namespace (&apos;{1}&apos;) that shares no common prefix with other namespaces (&apos;{0}&apos;)..
        /// </summary>
        internal static string DisjointNamespaceRule_Text1 {
            get {
                return ResourceManager.GetString("DisjointNamespaceRule_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to All types within a Windows Metadata file must exist in a sub namespace of the namespace that is implied by the file name..
        /// </summary>
        internal static string DisjointNamespaceRule_Text2 {
            get {
                return ResourceManager.GetString("DisjointNamespaceRule_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Project does not enable unsafe blocks.
        /// </summary>
        internal static string EnableUnsafe_Brief {
            get {
                return ResourceManager.GetString("EnableUnsafe_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type &apos;{0}&apos; implements generic WinRT interfaces which requires generated code using unsafe for trimming and AOT compatibility if passed across the WinRT ABI. Project needs to be updated with &apos;&lt;AllowUnsafeBlocks&gt;true&lt;/AllowUnsafeBlocks&gt;&apos;..
        /// </summary>
        internal static string EnableUnsafe_Text {
            get {
                return ResourceManager.GetString("EnableUnsafe_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class (or interface) is generic.
        /// </summary>
        internal static string GenericTypeRule_Brief {
            get {
                return ResourceManager.GetString("GenericTypeRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type {0} is generic, Windows Runtime types cannot be generic.
        /// </summary>
        internal static string GenericTypeRule_Text {
            get {
                return ResourceManager.GetString("GenericTypeRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array signature found with jagged array, which is not a valid WinRT type.
        /// </summary>
        internal static string JaggedArrayRule_Brief {
            get {
                return ResourceManager.GetString("JaggedArrayRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method {0} has a nested array of type {1} in its signature; arrays in Windows Runtime method signature cannot be nested.
        /// </summary>
        internal static string JaggedArrayRule_Text {
            get {
                return ResourceManager.GetString("JaggedArrayRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Array signature found with multi-dimensional array, which is not a valid Windows Runtime type.
        /// </summary>
        internal static string MultiDimensionalArrayRule_Brief {
            get {
                return ResourceManager.GetString("MultiDimensionalArrayRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has a multi-dimensional array of type &apos;{1}&apos; in its signature; arrays in Windows Runtime method signatures must be one dimensional.
        /// </summary>
        internal static string MultiDimensionalArrayRule_Text {
            get {
                return ResourceManager.GetString("MultiDimensionalArrayRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only one overload should be designated default.
        /// </summary>
        internal static string MultipleDefaultOverloadAttribute_Brief {
            get {
                return ResourceManager.GetString("MultipleDefaultOverloadAttribute_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In class {2}: Multiple {0}-parameter overloads of &apos;{1}&apos; are decorated with Windows.Foundation.Metadata.DefaultOverloadAttribute..
        /// </summary>
        internal static string MultipleDefaultOverloadAttribute_Text1 {
            get {
                return ResourceManager.GetString("MultipleDefaultOverloadAttribute_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The attribute may only be applied to one overload of the method..
        /// </summary>
        internal static string MultipleDefaultOverloadAttribute_Text2 {
            get {
                return ResourceManager.GetString("MultipleDefaultOverloadAttribute_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Namespace names cannot differ only by case.
        /// </summary>
        internal static string NamespacesDifferByCase_Brief {
            get {
                return ResourceManager.GetString("NamespacesDifferByCase_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple namespaces found with the name &apos;{0}&apos;; namespace names cannot differ only by case in the Windows Runtime.
        /// </summary>
        internal static string NamespacesDifferByCase_Text {
            get {
                return ResourceManager.GetString("NamespacesDifferByCase_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple overloads seen without DefaultOverload attribute.
        /// </summary>
        internal static string NeedDefaultOverloadAttribute_Brief {
            get {
                return ResourceManager.GetString("NeedDefaultOverloadAttribute_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In class {2}: The {0}-parameter overloads of {1} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.
        /// </summary>
        internal static string NeedDefaultOverloadAttribute_Text {
            get {
                return ResourceManager.GetString("NeedDefaultOverloadAttribute_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Non-array parameter marked with ReadOnlyArray or WriteOnlyArray.
        /// </summary>
        internal static string NonArrayMarked_Brief {
            get {
                return ResourceManager.GetString("NonArrayMarked_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; which is not an array, and which has either a ReadOnlyArray attribute or a WriteOnlyArray attribute..
        /// </summary>
        internal static string NonArrayMarked_Text1 {
            get {
                return ResourceManager.GetString("NonArrayMarked_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Runtime does not support marking non-array parameters with ReadOnlyArray or WriteOnlyArray..
        /// </summary>
        internal static string NonArrayMarked_Text2 {
            get {
                return ResourceManager.GetString("NonArrayMarked_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter (not array type) marked InAttribute or OutAttribute.
        /// </summary>
        internal static string NonArrayMarkedInOrOut_Brief {
            get {
                return ResourceManager.GetString("NonArrayMarkedInOrOut_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; with a System.Runtime.InteropServices.InAttribute or System.Runtime.InteropServices.OutAttribute.Windows Runtime does not support marking parameters with System.Runtime.InteropServices.InAttribute or System.Runtime.InteropServices.OutAttribute..
        /// </summary>
        internal static string NonArrayMarkedInOrOut_Text1 {
            get {
                return ResourceManager.GetString("NonArrayMarkedInOrOut_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please consider removing System.Runtime.InteropServices.InAttribute and replace System.Runtime.InteropServices.OutAttribute with &apos;out&apos; modifier instead..
        /// </summary>
        internal static string NonArrayMarkedInOrOut_Text2 {
            get {
                return ResourceManager.GetString("NonArrayMarkedInOrOut_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Interface Inherited.
        /// </summary>
        internal static string NonWinRTInterface_Brief {
            get {
                return ResourceManager.GetString("NonWinRTInterface_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Runtime component type {0} cannot implement interface {1}, as the interface is not a valid Windows Runtime interface.
        /// </summary>
        internal static string NonWinRTInterface_Text {
            get {
                return ResourceManager.GetString("NonWinRTInterface_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No public types defined.
        /// </summary>
        internal static string NoPublicTypesRule_Brief {
            get {
                return ResourceManager.GetString("NoPublicTypesRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Runtime components must have at least one public type.
        /// </summary>
        internal static string NoPublicTypesRule_Text {
            get {
                return ResourceManager.GetString("NoPublicTypesRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operator overload exposed.
        /// </summary>
        internal static string OperatorOverloadedRule_Brief {
            get {
                return ResourceManager.GetString("OperatorOverloadedRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is an operator overload, managed types cannot expose operator overloads in the Windows Runtime.
        /// </summary>
        internal static string OperatorOverloadedRule_Text {
            get {
                return ResourceManager.GetString("OperatorOverloadedRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter Named Value Rule.
        /// </summary>
        internal static string ParameterNamedValueRule_Brief {
            get {
                return ResourceManager.GetString("ParameterNamedValueRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The parameter name {1} in method {0} is the same as the return value parameter name used in the generated C#/WinRT interop; use a different parameter name.
        /// </summary>
        internal static string ParameterNamedValueRule_Text {
            get {
                return ResourceManager.GetString("ParameterNamedValueRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property must have public getter.
        /// </summary>
        internal static string PrivateGetterRule_Brief {
            get {
                return ResourceManager.GetString("PrivateGetterRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property &apos;{0}&apos; does not have a public getter method. Windows Runtime does not support setter-only properties..
        /// </summary>
        internal static string PrivateGetterRule_Text {
            get {
                return ResourceManager.GetString("PrivateGetterRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter passed by reference.
        /// </summary>
        internal static string RefParameterFound_Brief {
            get {
                return ResourceManager.GetString("RefParameterFound_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; has parameter &apos;{1}&apos; marked `ref`; reference parameters are not allowed in Windows Runtime.
        /// </summary>
        internal static string RefParameterFound_Text {
            get {
                return ResourceManager.GetString("RefParameterFound_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source generator failed.
        /// </summary>
        internal static string SourceGeneratorFailed_Brief {
            get {
                return ResourceManager.GetString("SourceGeneratorFailed_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CsWinRT component authoring source generator &apos;WinRT.SourceGenerator&apos; failed to generate WinMD and projection because of &apos;{0}&apos;.
        /// </summary>
        internal static string SourceGeneratorFailed_Text {
            get {
                return ResourceManager.GetString("SourceGeneratorFailed_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Const field in struct.
        /// </summary>
        internal static string StructHasConstFieldRule_Brief {
            get {
                return ResourceManager.GetString("StructHasConstFieldRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Structure {0} has const field - constants can only appear on Windows Runtime enumerations..
        /// </summary>
        internal static string StructHasConstFieldRule_Text {
            get {
                return ResourceManager.GetString("StructHasConstFieldRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid field in struct.
        /// </summary>
        internal static string StructHasInvalidFieldRule_Brief {
            get {
                return ResourceManager.GetString("StructHasInvalidFieldRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Structure {0} has field of type {1}; {1} is not a valid Windows Runtime field type..
        /// </summary>
        internal static string StructHasInvalidFieldRule_Text1 {
            get {
                return ResourceManager.GetString("StructHasInvalidFieldRule_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Each field in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure..
        /// </summary>
        internal static string StructHasInvalidFieldRule_Text2 {
            get {
                return ResourceManager.GetString("StructHasInvalidFieldRule_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Private field in struct.
        /// </summary>
        internal static string StructHasPrivateFieldRule_Brief {
            get {
                return ResourceManager.GetString("StructHasPrivateFieldRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Structure {0} has non-public field. All fields must be public for Windows Runtime structures..
        /// </summary>
        internal static string StructHasPrivateFieldRule_Text {
            get {
                return ResourceManager.GetString("StructHasPrivateFieldRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Empty struct rule.
        /// </summary>
        internal static string StructWithNoFieldsRule_Brief {
            get {
                return ResourceManager.GetString("StructWithNoFieldsRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Structure {0} contains no public fields. Windows Runtime structures must contain at least one public field..
        /// </summary>
        internal static string StructWithNoFieldsRule_Text {
            get {
                return ResourceManager.GetString("StructWithNoFieldsRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class incorrectly implements an interface.
        /// </summary>
        internal static string UnimplementedInterface_Brief {
            get {
                return ResourceManager.GetString("UnimplementedInterface_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class &apos;{0}&apos; does not correctly implement interface &apos;{1}&apos; because member &apos;{2}&apos; is missing or non-public.
        /// </summary>
        internal static string UnimplementedInterface_Text {
            get {
                return ResourceManager.GetString("UnimplementedInterface_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Class is unsealed.
        /// </summary>
        internal static string UnsealedClassRule_Brief {
            get {
                return ResourceManager.GetString("UnsealedClassRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exporting unsealed types is not supported in CsWinRT, please mark type {0} as sealed.
        /// </summary>
        internal static string UnsealedClassRule_Text {
            get {
                return ResourceManager.GetString("UnsealedClassRule_Text", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exposing unsupported type.
        /// </summary>
        internal static string UnsupportedTypeRule_Brief {
            get {
                return ResourceManager.GetString("UnsupportedTypeRule_Brief", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The member &apos;{0}&apos; has the type &apos;{1}&apos; in its signature..
        /// </summary>
        internal static string UnsupportedTypeRule_Text1 {
            get {
                return ResourceManager.GetString("UnsupportedTypeRule_Text1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The type &apos;{1}&apos; is not a valid Windows Runtime type..
        /// </summary>
        internal static string UnsupportedTypeRule_Text2 {
            get {
                return ResourceManager.GetString("UnsupportedTypeRule_Text2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Yet, the type (or its generic parameters) implement interfaces that are valid Windows Runtime types..
        /// </summary>
        internal static string UnsupportedTypeRule_Text3 {
            get {
                return ResourceManager.GetString("UnsupportedTypeRule_Text3", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Consider changing the type &apos;{1} in the member signature to one of the following types from System.Collections.Generic: {2}..
        /// </summary>
        internal static string UnsupportedTypeRule_Text4 {
            get {
                return ResourceManager.GetString("UnsupportedTypeRule_Text4", resourceCulture);
            }
        }
    }
}
