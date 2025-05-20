using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace ComServerHelpers.Internal.Windows;

/// <summary>
/// Retrieves the activation factory from a DLL that contains activatable Windows Runtime classes.
/// </summary>
/// <param name="activatableClassId">The class identifier that is associated with an activatable runtime class.</param>
/// <param name="factory">A pointer to the activation factory that corresponds with the class specified by <paramref name="activatableClassId"/>.</param>
/// <returns>
/// <para>This entry point can return one of these values.</para>
/// <list type="table">
///     <listheader>
///         <description>Return code</description>
///         <description>Description</description>
///     </listheader>
///     <item>
///         <description><c>S_OK</c></description>
///         <description>The activation factory was found successfully.</description>
///     </item>
///     <item>
///         <description><c>E_NOINTERFACE</c></description>
///         <description>The activation factory corresponding to the class specified by <paramref name="activatableClassId"/> was not found in the DLL.</description>
///     </item>
///     <item>
///         <description><c>E_INVALIDARG</c></description>
///         <description><paramref name="activatableClassId"/> or <paramref name="factory"/> is <see langword="null"/>.</description>
///     </item>
///     <item>
///         <description><c>E_OUTOFMEMORY</c></description>
///         <description>The activation factory store for the class specified by <paramref name="activatableClassId"/> could not be populated.</description>
///     </item>
///     <item>
///         <description><c>REGDB_E_READREGDB</c></description>
///         <description>An error occurred while reading the registration database.</description>
///     </item>
///     <item>
///         <description><c>REGDB_E_CLASSNOTREG</c></description>
///         <description>The class specified by <paramref name="activatableClassId"/> is not supported.</description>
///     </item>
/// </list>
/// </returns>
/// <seealso href="https://learn.microsoft.com/en-us/previous-versions/br205771(v=vs.85)">DllGetActivationFactory entry point</seealso>
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal unsafe delegate HRESULT DllGetActivationFactory(HSTRING activatableClassId, IActivationFactory** factory);