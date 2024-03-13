namespace System.Threading.Tasks
{
    internal interface ITaskAwareAsyncInfo
    {
        Task Task { get; }
    }
}
