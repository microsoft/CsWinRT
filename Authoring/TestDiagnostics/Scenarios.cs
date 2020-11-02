using System;
using Windows.Foundation;

namespace TestDiagnostics
{
    public class Scenarios : IAsyncAction
    {
        public AsyncActionCompletedHandler Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Exception ErrorCode => throw new NotImplementedException();

        public uint Id => throw new NotImplementedException();

        public AsyncStatus Status => throw new NotImplementedException();

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void GetResults()
        {
            throw new NotImplementedException();
        }
    }
}
