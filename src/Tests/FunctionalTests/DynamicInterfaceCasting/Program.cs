using TestComponent;
using WinRT;
using WinRT.Interop;

var instance = new Class();
var agileObject = (IAgileObject)(IWinRTObject)instance;
return agileObject != null ? 100 : 101;