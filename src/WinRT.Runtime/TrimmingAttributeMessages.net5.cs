namespace WinRT
{
    /// <summary>
    /// Messages for the trim warnings over methods that are not trim/AOT-safe.
    /// </summary>
    internal static class TrimmingAttributeMessages
    {
        /// <summary>
        /// Message for marshalling or generic code requiring <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute"/>.
        /// </summary>
        public const string MarshallingOrGenericInstantiationsRequiresDynamicCode = "The necessary marshalling code or generic instantiations might not be available.";
    }
}
