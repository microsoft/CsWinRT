namespace WinRT
{
    /// <summary>
    /// Messages for attributes used on CsWinRT APIs.
    /// </summary>
    internal static class AttributeMessages
    {
        /// <summary>
        /// Message for a generic deprecated message that's annotated with <see cref="System.ObsoleteAttribute"/>.
        /// </summary>
        public const string GenericDeprecatedMessage = "This method is deprecated and will be removed in a future release.";

        /// <summary>
        /// Message for a generic annotation for a method using <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute"/>.
        /// </summary>
        public const string GenericRequiresUnreferencedCodeMessage = "This method is not trim-safe, and is only supported for use when not using trimming (or AOT).";

        /// <summary>
        /// Message for marshalling or generic code requiring <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute"/>.
        /// </summary>
        public const string MarshallingOrGenericInstantiationsRequiresDynamicCode = "The necessary marshalling code or generic instantiations might not be available.";

        /// <summary>
        /// Message for APIs not supported when dynamic code is not available (ie. in AOT environments).
        /// </summary>
        public const string NotSupportedIfDynamicCodeIsNotAvailable = "The annotated API is not supported when dynamic code is not available (ie. in AOT environments).";

        /// <summary>
        /// Message for suppressing trim warnings for <see cref="System.Type.MakeGenericType"/> calls with ABI types as type arguments for <see langword="unmanaged"/> constrained type parameters.
        /// </summary>
        public const string AbiTypesNeverHaveConstructors = "All ABI types never have a constructor that would need to be accessed via reflection.";
    }
}