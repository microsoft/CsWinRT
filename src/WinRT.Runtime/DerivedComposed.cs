using System.ComponentModel;

namespace WinRT
{
    /// <summary>
    /// Marker type for a composable object to identify when it is being constructed
    /// as a base class of another composable object.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif 
    class DerivedComposed
    {
        public static readonly DerivedComposed Instance = new DerivedComposed();

        private DerivedComposed() { }
    }
}
