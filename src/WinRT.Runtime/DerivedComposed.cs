using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace WinRT
{
    /// <summary>
    /// Marker type for a composable object to identify when it is being constructed
    /// as a base class of another composable object.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DerivedComposed
    {
        public static readonly DerivedComposed Instance = new DerivedComposed();

        private DerivedComposed() { }
    }
}
