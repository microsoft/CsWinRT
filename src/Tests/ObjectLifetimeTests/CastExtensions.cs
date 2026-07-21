namespace WinRT
{
    // Shim for the WinUI markup compiler, which emits global::WinRT.CastExtensions.As<T>(target) in the
    // generated connect code; that type is gone in CsWinRT 3.0, and the target is already the right RCW type.
    internal static class CastExtensions
    {
        public static T As<T>(object target) => (T)target;
    }
}
