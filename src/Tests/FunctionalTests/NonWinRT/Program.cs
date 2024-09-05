using System;
using System.Collections.Generic;
using System.Collections.Specialized;

// This is a compile test to validate that
// non WinRT scenarios continue to build without issues.

// Test calling .NET functions which are WinRT custom mappings
// to ensure we can build them without a reference to the Windows SDK projection.
// This is to ensure we do our best to only generate for WinRT scenarios.

char[] charValues = new[] {'a', 'b', 'c'};
_= string.Join(',', charValues);

_ = bool.Parse("true");

string[] strValues = new[] { "a", "b", "c" };
_= string.Concat(strValues);

List<string> strList = new List<string>(){ "a", "b", "c" };
_= string.Concat(strList);

// Test scenarios where we can't tell for sure whether
// it is not a WinRT scenario so we will still generate. But if
// unsafe is disabled, we will not actually generate allowing the
// project to still build.
// This is scoped to debug which is when TEST_UNSAFE_DISABLED is set
// so we can test both scenarios (above with unsafe enabled and
// below with it disabled).
#if TEST_UNSAFE_DISABLED

CustomConcat(strList);
CustomConcat(strValues);

TestComponentCSharp.Class c = new TestComponentCSharp.Class();
c.BindableIterableProperty = strValues;
c.BindableIterableProperty = strList;
c.ObjectProperty = new System.EventHandler<int>((sender, e) => { });

IEnumerable<string> ienumString = new List<string>();
CustomConcat(ienumString);

static void CustomConcat(IEnumerable<string> strList)
{
    _ = string.Concat(strList);
}

partial class CustomEnumerable : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException();
}

partial class CustomNotifyCollectionChanged : INotifyCollectionChanged
{
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public IEnumerable<double> CustomCollection { get; } = new List<double>();

    public static IEnumerable<string> GetStringCollection()
    {
        return new string[] { "a", "b", "c" };
    }

    public Windows.Foundation.IAsyncOperation<Int32> GetIntAsyncOperation()
    {
        var task = System.Threading.Tasks.Task<int>.Run(() =>
        {
            System.Threading.Thread.Sleep(100);
            return 4;
        });
        return task.AsAsyncOperation();
    }

    public Windows.Foundation.IAsyncOperationWithProgress<double, double> GetDoubleAsyncOperation()
    {
        return System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run<double, double>(async (cancellationToken, progress) =>
        {
            await System.Threading.Tasks.Task.Delay(100);
            return 4.0;
        });
    }
}

#endif