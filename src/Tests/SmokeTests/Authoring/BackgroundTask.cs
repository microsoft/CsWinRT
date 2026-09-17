using Windows.ApplicationModel.Background;

namespace Authoring;

public sealed class BackgroundTask : IBackgroundTask
{
    public BackgroundTask()
    {
    }

    public BackgroundTask(IBackgroundTaskInstance taskInstance)
    {
    }

    public void Run(IBackgroundTaskInstance taskInstance)
    {
    }
}
