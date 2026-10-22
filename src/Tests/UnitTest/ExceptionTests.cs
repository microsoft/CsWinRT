using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Input;
using WinRT;
using Xunit;

namespace UnitTest
{
    public class ExceptionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public unsafe void TestAbiExceptionPreservesErrorPropagation(bool overwriteErrorInfo)
        {
            var original = new ArgumentOutOfRangeException("value", "ABI debugging test");
            var command = new ThrowingCommand(original, overwriteErrorInfo);
            using var marshaler = MarshalInterface<ICommand>.CreateMarshaler(command);
            IntPtr thisPtr = marshaler.ThisPtr;
            IntPtr executePtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(thisPtr), 9 * IntPtr.Size);
            var execute = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)executePtr;

            int hr = execute(thisPtr, IntPtr.Zero);

            Assert.True(command.FinallyRan);
            Assert.Equal(unchecked((int)0x8000000B), hr);
            Assert.Same(original, ExceptionHelpers.GetExceptionForHR(hr));
            Assert.Contains(nameof(ThrowingCommand.Execute), original.StackTrace);
            GC.KeepAlive(command);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public unsafe void TestAbiEventExceptionSetsErrorInfo(bool remove)
        {
            var original = new ArgumentOutOfRangeException("handler", "Event accessor error");
            var command = new ThrowingEventCommand(original, throwOnAdd: !remove);
            using var marshaler = MarshalInterface<ICommand>.CreateMarshaler(command);
            EventHandler handler = (sender, args) => { };
            using var handlerMarshaler = MarshalDelegate.CreateMarshaler(handler, GuidGenerator.GetIID(typeof(EventHandler)));
            IntPtr thisPtr = marshaler.ThisPtr;
            IntPtr vtable = Marshal.ReadIntPtr(thisPtr);
            var add = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int>)Marshal.ReadIntPtr(vtable, 6 * IntPtr.Size);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            EventRegistrationToken token;
            int hr = add(thisPtr, handlerMarshaler.ThisPtr, &token);
            if (remove)
            {
                Assert.Equal(0, hr);
                var removeHandler = (delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int>)Marshal.ReadIntPtr(vtable, 7 * IntPtr.Size);
                hr = removeHandler(thisPtr, token);
            }

            Assert.Equal(unchecked((int)0x8000000B), hr);
            Assert.Same(original, ExceptionHelpers.GetExceptionForHR(hr));
            Assert.Contains(remove ? "remove_CanExecuteChanged" : "add_CanExecuteChanged", original.StackTrace);

            var canExecute = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, byte*, int>)Marshal.ReadIntPtr(vtable, 8 * IntPtr.Size);
            byte result;
            int canExecuteHr = canExecute(thisPtr, IntPtr.Zero, &result);
            Assert.Equal(0, canExecuteHr);
            Assert.Equal((byte)1, result);
            GC.KeepAlive(command);
        }

        private sealed class ThrowingEventCommand : ICommand
        {
            private readonly Exception exception;
            private readonly bool throwOnAdd;

            public ThrowingEventCommand(Exception exception, bool throwOnAdd)
            {
                this.exception = exception;
                this.throwOnAdd = throwOnAdd;
            }

            public event EventHandler CanExecuteChanged
            {
                add
                {
                    if (throwOnAdd)
                    {
                        throw exception;
                    }
                }
                remove => throw exception;
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) { }
        }

        private sealed class ThrowingCommand : ICommand
        {
            private readonly Exception exception;
            private readonly bool overwriteErrorInfo;

            public ThrowingCommand(Exception exception, bool overwriteErrorInfo)
            {
                this.exception = exception;
                this.overwriteErrorInfo = overwriteErrorInfo;
            }

            public bool FinallyRan { get; private set; }

            public event EventHandler CanExecuteChanged { add { } remove { } }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                try
                {
                    throw exception;
                }
                finally
                {
                    FinallyRan = true;
                    if (overwriteErrorInfo)
                    {
                        ExceptionHelpers.SetErrorInfo(new InvalidOperationException("Error info from finally"));
                    }
                }
            }
        }

        [Fact]
        public void TestGetExceptionForHR_WithValidHResult_ReturnsSystemFormattedException()
        {
            const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010E);

            Exception exception = ExceptionHelpers.GetExceptionForHR(RPC_E_WRONG_THREAD);
            Assert.NotNull(exception);
            Assert.False(string.IsNullOrWhiteSpace(exception.Message));
            if (CultureInfo.CurrentUICulture.Name == "en-US")
            {
                Assert.Equal("The application called an interface that was marshalled for a different thread. (0x8001010E)", exception.Message);
            }
        }
    }
}
