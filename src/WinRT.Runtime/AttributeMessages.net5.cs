﻿namespace WinRT
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
        /// Message for marshalling or generic code requiring <see cref="System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute"/>.
        /// </summary>
        public const string MarshallingOrGenericInstantiationsRequiresDynamicCode = "The necessary marshalling code or generic instantiations might not be available.";
    }
}