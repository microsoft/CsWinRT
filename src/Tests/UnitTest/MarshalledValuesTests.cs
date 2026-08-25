using System;
using TestComponentCSharp;

namespace UnitTest
{
    // 'Windows.Foundation.HResult' (projected as 'System.Exception'), the mapped value types
    // 'Windows.Foundation.DateTime' / 'Windows.Foundation.TimeSpan' (projected as 'System.DateTimeOffset'
    // and 'System.TimeSpan'), and 'Windows.UI.Xaml.Interop.TypeName' (projected as 'System.Type') all
    // have an ABI representation that differs from their projected one, but they don't cross the ABI as
    // an opaque pointer like strings and reference types do. Every parameter position therefore needs
    // its own marshalling step, in both the RCW (managed caller) and the CCW (managed callee) direction.
    // 'IMarshalledValues' is a standalone interface precisely so that CCW stubs are generated for it,
    // which is what exercises the managed side as the callee.
    [TestClass]
    public class MarshalledValuesTests
    {
        private const int ERROR_INVALID_WINDOW_HANDLE = unchecked((int)0x80070578);

        [TestMethod]
        public void ManagedImplementation_HResultPropertySetter_MarshalsValue()
        {
            ManagedMarshalledValues target = new();
            Exception value = new NotImplementedException();

            Exception result = MarshalledValuesTest.CallResultProperty(target, value);

            Assert.AreEqual(value.HResult, target.Result.HResult);
            Assert.AreEqual(value.HResult, result.HResult);
        }

        // Same as above, but with an unrelated 'IRestrictedErrorInfo' left on the current thread, exactly
        // like any previously failed native call would leave behind (eg. the COM interop tests calling
        // 'GetForWindow' with an invalid window handle). That state is ambient and sticky, so marshalling a
        // 'Windows.Foundation.HResult' must never pick it up: the exception the CCW setter creates from the
        // incoming 'HRESULT' would otherwise carry the unrelated error object, and the CCW getter would then
        // marshal that exception back out as the unrelated error code instead of the original one.
        [TestMethod]
        public void ManagedImplementation_HResultPropertySetter_MarshalsValueWithUnrelatedThreadErrorInfo()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, "Unrelated originated error"));

                ManagedMarshalledValues target = new();
                Exception value = new NotImplementedException();

                Exception result = MarshalledValuesTest.CallResultProperty(target, value);

                Assert.AreEqual(value.HResult, target.Result.HResult);
                Assert.AreEqual(value.HResult, result.HResult);
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void ManagedImplementation_HResultPropertySetter_MarshalsNull()
        {
            ManagedMarshalledValues target = new() { Result = new NotImplementedException() };

            Exception result = MarshalledValuesTest.CallResultProperty(target, null);

            Assert.IsNull(target.Result);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ManagedImplementation_HResultMethodParameter_MarshalsValue()
        {
            Exception previous = new NotImplementedException();
            Exception value = new NotSupportedException();
            ManagedMarshalledValues target = new() { Result = previous };

            Exception result = MarshalledValuesTest.CallSwapResult(target, value);

            Assert.AreEqual(value.HResult, target.Result.HResult);
            Assert.AreEqual(previous.HResult, result.HResult);
        }

        [TestMethod]
        public void ManagedImplementation_HResultOutParameter_MarshalsValue()
        {
            Exception previous = new NotImplementedException();
            Exception value = new NotSupportedException();
            ManagedMarshalledValues target = new() { Result = previous };

            Exception result = MarshalledValuesTest.CallExchangeResult(target, value);

            Assert.AreEqual(value.HResult, target.Result.HResult);
            Assert.AreEqual(previous.HResult, result.HResult);
        }

        [TestMethod]
        public void ManagedImplementation_DateTimeOutParameter_MarshalsValue()
        {
            DateTimeOffset previous = new(1993, 3, 22, 8, 30, 0, TimeSpan.Zero);
            DateTimeOffset value = DateTimeOffset.Now;
            ManagedMarshalledValues target = new() { DateTime = previous };

            DateTimeOffset result = MarshalledValuesTest.CallExchangeDateTime(target, value);

            Assert.AreEqual(value, target.DateTime);
            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void ManagedImplementation_TimeSpanOutParameter_MarshalsValue()
        {
            TimeSpan previous = TimeSpan.FromSeconds(42);
            TimeSpan value = TimeSpan.FromMinutes(7);
            ManagedMarshalledValues target = new() { TimeSpan = previous };

            TimeSpan result = MarshalledValuesTest.CallExchangeTimeSpan(target, value);

            Assert.AreEqual(value, target.TimeSpan);
            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void ManagedImplementation_TypeNameOutParameter_MarshalsValue()
        {
            Type previous = typeof(int);
            Type value = typeof(MarshalledValuesTests);
            ManagedMarshalledValues target = new() { TypeName = previous };

            Type result = MarshalledValuesTest.CallExchangeTypeName(target, value);

            Assert.AreEqual(value, target.TypeName);
            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void ManagedImplementation_MappedValueTypeInParameters_MarshalValues()
        {
            DateTimeOffset value = new(1993, 3, 22, 8, 30, 0, TimeSpan.Zero);
            TimeSpan offset = TimeSpan.FromHours(3);

            DateTimeOffset result = MarshalledValuesTest.CallOffsetDateTime(new ManagedMarshalledValues(), value, offset);

            Assert.AreEqual(value + offset, result);
        }

        [TestMethod]
        public void ManagedDelegate_HResultParameter_MarshalsValue()
        {
            Exception observed = null;
            Exception value = new NotImplementedException();

            Exception result = MarshalledValuesTest.InvokeHandleResult(
                argument =>
                {
                    observed = argument;

                    return new NotSupportedException();
                },
                value);

            Assert.AreEqual(value.HResult, observed.HResult);
            Assert.AreEqual(new NotSupportedException().HResult, result.HResult);
        }

        [TestMethod]
        public void NativeImplementation_HResultProperty_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            Exception value = new NotImplementedException();

            target.Result = value;

            Assert.AreEqual(value.HResult, target.Result.HResult);
        }

        [TestMethod]
        public void NativeImplementation_HResultMethodParameter_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            Exception previous = new NotImplementedException();
            Exception value = new NotSupportedException();

            target.Result = previous;

            Exception result = target.SwapResult(value);

            Assert.AreEqual(previous.HResult, result.HResult);
            Assert.AreEqual(value.HResult, target.Result.HResult);
        }

        [TestMethod]
        public void NativeImplementation_HResultOutParameter_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            Exception previous = new NotImplementedException();
            Exception value = new NotSupportedException();

            target.Result = previous;

            target.ExchangeResult(value, out Exception result);

            Assert.AreEqual(previous.HResult, result.HResult);
            Assert.AreEqual(value.HResult, target.Result.HResult);
        }

        [TestMethod]
        public void NativeImplementation_DateTimeOutParameter_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            DateTimeOffset previous = new(1993, 3, 22, 8, 30, 0, TimeSpan.Zero);
            DateTimeOffset value = DateTimeOffset.Now;

            target.ExchangeDateTime(previous, out _);
            target.ExchangeDateTime(value, out DateTimeOffset result);

            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void NativeImplementation_TimeSpanOutParameter_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            TimeSpan previous = TimeSpan.FromSeconds(42);
            TimeSpan value = TimeSpan.FromMinutes(7);

            target.ExchangeTimeSpan(previous, out _);
            target.ExchangeTimeSpan(value, out TimeSpan result);

            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void NativeImplementation_TypeNameOutParameter_MarshalsValue()
        {
            MarshalledValuesTest target = new();
            Type previous = typeof(int);
            Type value = typeof(MarshalledValuesTests);

            target.ExchangeTypeName(previous, out _);
            target.ExchangeTypeName(value, out Type result);

            Assert.AreEqual(previous, result);
        }

        [TestMethod]
        public void NativeImplementation_MappedValueTypeInParameters_MarshalValues()
        {
            MarshalledValuesTest target = new();
            DateTimeOffset value = new(1993, 3, 22, 8, 30, 0, TimeSpan.Zero);
            TimeSpan offset = TimeSpan.FromHours(3);

            DateTimeOffset result = target.OffsetDateTime(value, offset);

            Assert.AreEqual(value + offset, result);
        }

        // Managed implementation of the interface, so that the native side of the tests above calls
        // back into managed code through the generated CCW stubs.
        private sealed class ManagedMarshalledValues : IMarshalledValues
        {
            public Exception Result { get; set; }

            public DateTimeOffset DateTime { get; set; }

            public TimeSpan TimeSpan { get; set; }

            public Type TypeName { get; set; }

            public Exception SwapResult(Exception value)
            {
                Exception previous = Result;

                Result = value;

                return previous;
            }

            public void ExchangeResult(Exception value, out Exception previous)
            {
                previous = Result;
                Result = value;
            }

            public void ExchangeDateTime(DateTimeOffset value, out DateTimeOffset previous)
            {
                previous = DateTime;
                DateTime = value;
            }

            public void ExchangeTimeSpan(TimeSpan value, out TimeSpan previous)
            {
                previous = TimeSpan;
                TimeSpan = value;
            }

            public void ExchangeTypeName(Type value, out Type previous)
            {
                previous = TypeName;
                TypeName = value;
            }

            public DateTimeOffset OffsetDateTime(in DateTimeOffset value, in TimeSpan offset)
            {
                return value + offset;
            }
        }
    }
}
