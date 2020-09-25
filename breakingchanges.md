# Breaking Changes with C#/WinRT

-  Casting between interfaces that are not related in metadata: instead of a dynamic regular cast, you now need to use the `.As<T>()` extension method.

-  If you want to hold a weak reference to a WinRT object, you need to use `WinRT.WeakReference<T>` instead of `System.WeakReference` or `System.WeakReference<T>`.

-  Calling any of the [RuntimeReflectionExtension](https://docs.microsoft.com/dotnet/api/system.reflection.runtimereflectionextensions?view=net-5.0) methods is likely to cause issues. The runtime type of a projected class now carries around some state information and helper methods, and should not be dynamically reflected on.

- Some WinRT types may have differences in default values when projected. This may raise issues if relying on default values.