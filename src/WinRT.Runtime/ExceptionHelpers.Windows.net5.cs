// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Windows.UI.Xaml
{
    namespace Automation
    {
#if EMBED
        internal
#else
        public
#endif
        class ElementNotAvailableException : Exception
        {
            public ElementNotAvailableException()
                : base("The element is not available.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }
        }

#if EMBED
        internal
#else
        public
#endif
        class ElementNotEnabledException : Exception
        {
            public ElementNotEnabledException()
                : base("The element is not enabled.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }
        }
    }

    namespace Markup
    {

#if EMBED
        internal
#else
        public
#endif
        class XamlParseException : Exception
        {
            public XamlParseException()
                : base("XAML parsing failed.")
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }
        }
    }

#if EMBED
    internal
#else
    public
#endif
    class LayoutCycleException : Exception
    {
        public LayoutCycleException()
            : base("A cycle occurred while laying out the GUI.")
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message)
            : base(message)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }
    }
}
