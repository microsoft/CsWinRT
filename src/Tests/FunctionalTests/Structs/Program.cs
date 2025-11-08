using System.Runtime.InteropServices;
using TestComponentCSharp;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete
// TODO: This shouldn't be needed if transitive references are detected correctly.
[assembly: WindowsRuntime.WindowsRuntimeReferenceAssembly]

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("Test")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
#pragma warning restore CSWINRT3001 // Type or member is obsolete

var instance = new Class();
var blittable = instance.GetBlittableStructVector();

int sum = 0;
int expected_sum = 0;
for (int i = 0; i < blittable.Count; i++)
{
    sum += blittable[i].blittable.i32;
    expected_sum += i;
}

var nonblittable = instance.GetNonBlittableStructVector();
for (int i = 0; i < nonblittable.Count; i++)
{
    sum += nonblittable[i].blittable.i32;
    expected_sum += i;
}

return sum == expected_sum ? 100 : 101;

