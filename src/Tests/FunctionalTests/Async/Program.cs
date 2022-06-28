using System;
using System.Threading.Tasks;
using TestComponentCSharp;

var instance = new Class();

instance.IntProperty = 12;
var async_get_int = instance.GetIntAsync();
int async_int = 0;
async_get_int.Completed = (info, status) => async_int = info.GetResults();
async_get_int.GetResults();

if (async_int != 12)
{
    return 101;
}

instance.StringProperty = "foo";
var async_get_string = instance.GetStringAsync();
string async_string = "";
async_get_string.Completed = (info, status) => async_string = info.GetResults();
int async_progress;
async_get_string.Progress = (info, progress) => async_progress = progress;
async_get_string.GetResults();

if (async_string != "foo")
{
    return 101;
}

var task = InvokeAddAsync(instance, 20, 10);
if (task.Wait(25))
{
    return 101;
}

instance.CompleteAsync();
if (!task.Wait(1000))
{
    return 101;
}

if (task.Status != TaskStatus.RanToCompletion || task.Result != 30)
{
    return 101;
}

return 100;

static async Task<int> InvokeAddAsync(Class instance, int lhs, int rhs)
{
    return await instance.AddAsync(lhs, rhs);
}